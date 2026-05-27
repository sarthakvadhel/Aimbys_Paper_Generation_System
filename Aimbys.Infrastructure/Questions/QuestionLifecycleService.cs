using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Authorization;
using Aimbys.Application.Questions;
using Aimbys.Application.Workflow;
using Aimbys.Domain.Entities.Questions;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Questions;

/// <summary>
/// Orchestrates the question lifecycle: submit, assign reviewer, approve,
/// reject, retire. Each action validates preconditions, transitions the
/// workflow, writes audit, and enqueues domain events.
/// </summary>
public sealed class QuestionLifecycleService : IQuestionLifecycleService
{
    private readonly AppDbContext _db;
    private readonly IWorkflowService _workflow;
    private readonly IAuditWriter _audit;
    private readonly DomainEventCollector _events;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IInstituteScope _instituteScope;
    private readonly ILogger<QuestionLifecycleService> _logger;

    public QuestionLifecycleService(
        AppDbContext db,
        IWorkflowService workflow,
        IAuditWriter audit,
        DomainEventCollector events,
        UserManager<IdentityUser> userManager,
        IInstituteScope instituteScope,
        ILogger<QuestionLifecycleService> logger)
    {
        _db = db;
        _workflow = workflow;
        _audit = audit;
        _events = events;
        _userManager = userManager;
        _instituteScope = instituteScope;
        _logger = logger;
    }

    public async Task<QuestionLifecycleResult> SubmitForReviewAsync(
        Guid questionId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var actorUserId = _userManager.GetUserId(actor) ?? string.Empty;

        var question = await _db.Set<Question>()
            .Include(q => q.Versions)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null)
            return new QuestionLifecycleResult(false, "Question not found.");

        if (question.AuthorUserId != actorUserId)
            return new QuestionLifecycleResult(false, "Only the author can submit a question for review.");

        if (question.Status != QuestionStatus.Draft)
            return new QuestionLifecycleResult(false, "Only draft questions can be submitted.");

        // Validate latest version has required fields.
        var latestVersion = question.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (latestVersion is null || string.IsNullOrWhiteSpace(latestVersion.BodyHtml) || latestVersion.Marks <= 0)
            return new QuestionLifecycleResult(false, "Question must have a body and marks > 0 before submission.");

        var instituteId = await _instituteScope.GetCurrentInstituteIdAsync(actor, ct);

        // Start workflow.
        var startResult = await _workflow.StartAsync(
            "QuestionApproval", "Question", questionId, actorUserId, instituteId, ct);

        if (!startResult.IsSuccess)
            return new QuestionLifecycleResult(false, startResult.ErrorMessage);

        // Transition from Draft -> Submitted.
        var transitionResult = await _workflow.TransitionAsync(
            startResult.InstanceId!.Value, "Submitted", actor, null, ct);

        if (!transitionResult.IsSuccess)
            return new QuestionLifecycleResult(false, transitionResult.ErrorMessage);

        question.Status = QuestionStatus.Submitted;
        question.UpdatedAtUtc = DateTime.UtcNow;

        // Create QuestionApproval row.
        _db.Set<QuestionApproval>().Add(new QuestionApproval
        {
            QuestionId = questionId,
            WorkflowInstanceId = startResult.InstanceId!.Value
        });

        await _audit.WriteAsync(
            "Question.Submitted", "Question", questionId.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { questionId, subjectId = question.SubjectId }),
            cancellationToken: ct);

        _events.Enqueue(new QuestionSubmittedEvent
        {
            QuestionId = questionId,
            SubjectId = question.SubjectId,
            AuthorUserId = actorUserId,
            InstituteId = instituteId
        });

        await _db.SaveChangesAsync(ct);
        return new QuestionLifecycleResult(true);
    }

    public async Task<QuestionLifecycleResult> AssignReviewerAsync(
        Guid questionId, Guid reviewerProfileId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var actorUserId = _userManager.GetUserId(actor) ?? string.Empty;

        var question = await _db.Set<Question>()
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null)
            return new QuestionLifecycleResult(false, "Question not found.");

        if (question.Status != QuestionStatus.Submitted)
            return new QuestionLifecycleResult(false, "Question must be in Submitted status to assign a reviewer.");

        var reviewer = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.Id == reviewerProfileId && t.CanApproveQuestions, ct);

        if (reviewer is null)
            return new QuestionLifecycleResult(false, "Reviewer not found or does not have approval permission.");

        // Find the active workflow instance.
        var approval = await _db.Set<QuestionApproval>()
            .FirstOrDefaultAsync(a => a.QuestionId == questionId && a.ApprovedAtUtc == null, ct);

        if (approval is null)
            return new QuestionLifecycleResult(false, "No active approval record found.");

        // Transition to UnderReview.
        var transitionResult = await _workflow.TransitionAsync(
            approval.WorkflowInstanceId, "UnderReview", actor, null, ct);

        if (!transitionResult.IsSuccess)
            return new QuestionLifecycleResult(false, transitionResult.ErrorMessage);

        // Get latest version for the review record.
        var latestVersion = await _db.Set<QuestionVersion>()
            .Where(v => v.QuestionId == questionId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        _db.Set<QuestionReview>().Add(new QuestionReview
        {
            QuestionId = questionId,
            QuestionVersionId = latestVersion?.Id ?? Guid.Empty,
            ReviewerTeacherProfileId = reviewerProfileId,
            AssignedAtUtc = DateTime.UtcNow,
            Verdict = ReviewVerdict.Pending
        });

        question.Status = QuestionStatus.UnderReview;
        question.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            "Question.ReviewerAssigned", "Question", questionId.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { questionId, reviewerProfileId }),
            cancellationToken: ct);

        _events.Enqueue(new QuestionReviewAssignedEvent
        {
            QuestionId = questionId,
            ReviewerUserId = reviewer.UserId,
            SubjectId = question.SubjectId,
            InstituteId = question.InstituteId
        });

        await _db.SaveChangesAsync(ct);
        return new QuestionLifecycleResult(true);
    }

    public async Task<QuestionLifecycleResult> ApproveAsync(
        Guid questionId, ClaimsPrincipal actor, string? comment = null, CancellationToken ct = default)
    {
        var actorUserId = _userManager.GetUserId(actor) ?? string.Empty;

        var question = await _db.Set<Question>()
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null)
            return new QuestionLifecycleResult(false, "Question not found.");

        if (question.Status != QuestionStatus.UnderReview)
            return new QuestionLifecycleResult(false, "Question must be under review to approve.");

        // Find the pending review for this question assigned to the current actor.
        var review = await _db.Set<QuestionReview>()
            .FirstOrDefaultAsync(r => r.QuestionId == questionId && r.Verdict == ReviewVerdict.Pending, ct);

        if (review is null)
            return new QuestionLifecycleResult(false, "No pending review found for this question.");

        var approval = await _db.Set<QuestionApproval>()
            .FirstOrDefaultAsync(a => a.QuestionId == questionId && a.ApprovedAtUtc == null, ct);

        if (approval is null)
            return new QuestionLifecycleResult(false, "No active approval record found.");

        // Transition to Approved.
        var transitionResult = await _workflow.TransitionAsync(
            approval.WorkflowInstanceId, "Approved", actor, comment, ct);

        if (!transitionResult.IsSuccess)
            return new QuestionLifecycleResult(false, transitionResult.ErrorMessage);

        review.Verdict = ReviewVerdict.Approved;
        review.CompletedAtUtc = DateTime.UtcNow;
        review.Comment = comment;

        question.Status = QuestionStatus.Approved;
        question.UpdatedAtUtc = DateTime.UtcNow;

        approval.ApprovedByUserId = actorUserId;
        approval.ApprovedAtUtc = DateTime.UtcNow;

        // Resolve reviewer user id for the event.
        var reviewerProfile = await _db.TeacherProfiles
            .Where(t => t.Id == review.ReviewerTeacherProfileId)
            .Select(t => t.UserId)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        await _audit.WriteAsync(
            "Question.Approved", "Question", questionId.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { questionId, comment }),
            cancellationToken: ct);

        _events.Enqueue(new QuestionApprovedEvent
        {
            QuestionId = questionId,
            AuthorUserId = question.AuthorUserId,
            ReviewerUserId = reviewerProfile,
            InstituteId = question.InstituteId
        });

        await _db.SaveChangesAsync(ct);
        return new QuestionLifecycleResult(true);
    }

    public async Task<QuestionLifecycleResult> RejectAsync(
        Guid questionId, ClaimsPrincipal actor, string comment, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return new QuestionLifecycleResult(false, "A comment is required when rejecting a question.");

        var actorUserId = _userManager.GetUserId(actor) ?? string.Empty;

        var question = await _db.Set<Question>()
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null)
            return new QuestionLifecycleResult(false, "Question not found.");

        if (question.Status != QuestionStatus.UnderReview)
            return new QuestionLifecycleResult(false, "Question must be under review to reject.");

        var review = await _db.Set<QuestionReview>()
            .FirstOrDefaultAsync(r => r.QuestionId == questionId && r.Verdict == ReviewVerdict.Pending, ct);

        if (review is null)
            return new QuestionLifecycleResult(false, "No pending review found for this question.");

        var approval = await _db.Set<QuestionApproval>()
            .FirstOrDefaultAsync(a => a.QuestionId == questionId && a.ApprovedAtUtc == null, ct);

        if (approval is null)
            return new QuestionLifecycleResult(false, "No active approval record found.");

        // Transition to Rejected.
        var transitionResult = await _workflow.TransitionAsync(
            approval.WorkflowInstanceId, "Rejected", actor, comment, ct);

        if (!transitionResult.IsSuccess)
            return new QuestionLifecycleResult(false, transitionResult.ErrorMessage);

        review.Verdict = ReviewVerdict.Rejected;
        review.CompletedAtUtc = DateTime.UtcNow;
        review.Comment = comment;

        question.Status = QuestionStatus.Rejected;
        question.UpdatedAtUtc = DateTime.UtcNow;

        approval.RejectionComment = comment;

        var reviewerProfile = await _db.TeacherProfiles
            .Where(t => t.Id == review.ReviewerTeacherProfileId)
            .Select(t => t.UserId)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        await _audit.WriteAsync(
            "Question.Rejected", "Question", questionId.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { questionId, comment }),
            cancellationToken: ct);

        _events.Enqueue(new QuestionRejectedEvent
        {
            QuestionId = questionId,
            AuthorUserId = question.AuthorUserId,
            ReviewerUserId = reviewerProfile,
            Comment = comment,
            InstituteId = question.InstituteId
        });

        await _db.SaveChangesAsync(ct);
        return new QuestionLifecycleResult(true);
    }

    public async Task<QuestionLifecycleResult> RetireAsync(
        Guid questionId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var actorUserId = _userManager.GetUserId(actor) ?? string.Empty;

        var question = await _db.Set<Question>()
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null)
            return new QuestionLifecycleResult(false, "Question not found.");

        if (question.Status != QuestionStatus.Approved)
            return new QuestionLifecycleResult(false, "Only approved questions can be retired.");

        // Find the workflow instance via the approval record.
        var approval = await _db.Set<QuestionApproval>()
            .FirstOrDefaultAsync(a => a.QuestionId == questionId, ct);

        if (approval is null)
            return new QuestionLifecycleResult(false, "No approval record found.");

        var transitionResult = await _workflow.TransitionAsync(
            approval.WorkflowInstanceId, "Retired", actor, null, ct);

        if (!transitionResult.IsSuccess)
            return new QuestionLifecycleResult(false, transitionResult.ErrorMessage);

        question.Status = QuestionStatus.Retired;
        question.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            "Question.Retired", "Question", questionId.ToString(),
            actorUserId, cancellationToken: ct);

        await _db.SaveChangesAsync(ct);
        return new QuestionLifecycleResult(true);
    }

    public async Task<QuestionLifecycleResult> AutoAssignReviewerAsync(
        Guid questionId, CancellationToken ct = default)
    {
        var question = await _db.Set<Question>()
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null)
            return new QuestionLifecycleResult(false, "Question not found.");

        // Find the least-loaded reviewer in the same institute with CanApproveQuestions
        // who is not the author.
        var leastLoaded = await _db.TeacherProfiles
            .Where(t => t.InstituteId == question.InstituteId
                        && t.CanApproveQuestions
                        && t.UserId != question.AuthorUserId
                        && t.Status == ProfileStatus.Active)
            .OrderBy(t => _db.Set<QuestionReview>()
                .Count(r => r.ReviewerTeacherProfileId == t.Id && r.Verdict == ReviewVerdict.Pending))
            .Select(t => t.Id)
            .FirstOrDefaultAsync(ct);

        if (leastLoaded == Guid.Empty)
            return new QuestionLifecycleResult(false, "No eligible reviewer found.");

        // Create a system ClaimsPrincipal for the assignment.
        var systemPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "system"),
            new Claim(ClaimTypes.Role, Roles.SuperAdmin)
        }, "System"));

        return await AssignReviewerAsync(questionId, leastLoaded, systemPrincipal, ct);
    }
}
