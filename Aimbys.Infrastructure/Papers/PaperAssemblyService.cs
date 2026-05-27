using System.Security.Claims;
using Aimbys.Application.Audit;
using Aimbys.Application.Papers;
using Aimbys.Application.Workflow;
using Aimbys.Domain.Entities.Papers;
using Aimbys.Domain.Entities.Workflow;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Domain.Workflow;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Papers;

/// <summary>
/// Implements the full paper-assembly + approval lifecycle. State
/// transitions go through <see cref="IWorkflowService"/>; this class
/// only sets <c>Paper.Status</c> after the engine has accepted the
/// transition. Magic strings are forbidden &mdash; use
/// <see cref="PaperWorkflow"/> constants for everything the engine
/// receives.
/// </summary>
public class PaperAssemblyService : IPaperAssemblyService
{
    private readonly AppDbContext _db;
    private readonly IPaperValidationService _validation;
    private readonly IWorkflowService _workflow;
    private readonly DomainEventCollector _events;
    private readonly IAuditWriter _audit;
    private readonly ILogger<PaperAssemblyService> _logger;

    public PaperAssemblyService(
        AppDbContext db,
        IPaperValidationService validation,
        IWorkflowService workflow,
        DomainEventCollector events,
        IAuditWriter audit,
        ILogger<PaperAssemblyService> logger)
    {
        _db = db;
        _validation = validation;
        _workflow = workflow;
        _events = events;
        _audit = audit;
        _logger = logger;
    }

    // =========================================================================
    // Authoring
    // =========================================================================

    public async Task<PaperResult> CreateDraftAsync(PaperCreateRequest request, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var userId = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new PaperResult(false, "User not authenticated.");

        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacherProfile is null)
            return new PaperResult(false, "Teacher profile not found.");

        var paper = new Paper
        {
            InstituteId = teacherProfile.InstituteId,
            SubjectId = request.SubjectId,
            AuthorTeacherProfileId = teacherProfile.Id,
            Status = PaperStatus.Draft
        };

        var version = new PaperVersion
        {
            PaperId = paper.Id,
            VersionNumber = 1,
            Title = request.Title,
            TotalMarks = request.TotalMarks,
            DurationMinutes = request.DurationMinutes,
            AuthorUserId = userId,
            IsLocked = false
        };

        paper.CurrentVersionId = version.Id;
        paper.Versions.Add(version);

        _db.Papers.Add(paper);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Paper.Created", "Paper", paper.Id.ToString(), userId, cancellationToken: ct);

        return new PaperResult(true, PaperId: paper.Id, VersionId: version.Id);
    }

    public async Task<PaperResult> SaveDraftAsync(Guid paperId, PaperSaveRequest request, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var userId = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new PaperResult(false, "User not authenticated.");

        var paper = await _db.Papers
            .Include(p => p.Versions)
                .ThenInclude(v => v.Sections)
            .Include(p => p.Versions)
                .ThenInclude(v => v.Questions)
            .FirstOrDefaultAsync(p => p.Id == paperId, ct);

        if (paper is null)
            return new PaperResult(false, "Paper not found.");

        // Ownership check
        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacherProfile is null || paper.AuthorTeacherProfileId != teacherProfile.Id)
            return new PaperResult(false, "Access denied.");

        if (paper.Status != PaperStatus.Draft && paper.Status != PaperStatus.Returned)
            return new PaperResult(false, "Paper can only be edited in Draft or Returned status.");

        var currentVersion = paper.Versions.FirstOrDefault(v => v.Id == paper.CurrentVersionId);
        if (currentVersion is null)
            return new PaperResult(false, "Current version not found.");

        if (currentVersion.IsLocked)
            return new PaperResult(false, "Current version is locked and cannot be edited.");

        var validationResult = _validation.Validate(request, currentVersion.TotalMarks);
        if (!validationResult.IsValid)
            return new PaperResult(false, string.Join("; ", validationResult.Errors));

        _db.PaperSections.RemoveRange(currentVersion.Sections);
        _db.PaperQuestions.RemoveRange(currentVersion.Questions);

        var newSections = new List<PaperSection>();
        foreach (var sInput in request.Sections)
        {
            var section = new PaperSection
            {
                VersionId = currentVersion.Id,
                Name = sInput.Name,
                Marks = sInput.Marks,
                SortOrder = sInput.SortOrder
            };
            newSections.Add(section);
            _db.PaperSections.Add(section);
        }

        foreach (var qInput in request.Questions)
        {
            var section = newSections.ElementAtOrDefault(qInput.SectionIndex);
            if (section is null) continue;

            var question = new PaperQuestion
            {
                VersionId = currentVersion.Id,
                SectionId = section.Id,
                QuestionId = qInput.QuestionId,
                QuestionVersionId = qInput.QuestionVersionId,
                SortOrder = qInput.SortOrder,
                MarksOverride = qInput.MarksOverride
            };
            _db.PaperQuestions.Add(question);
        }

        paper.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Paper.DraftSaved", "Paper", paper.Id.ToString(), userId, cancellationToken: ct);

        return new PaperResult(true, PaperId: paper.Id, VersionId: currentVersion.Id);
    }

    public async Task<PaperResult> GenerateFromBlueprintAsync(Guid paperId, Guid blueprintVersionId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var userId = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new PaperResult(false, "User not authenticated.");

        var paper = await _db.Papers
            .Include(p => p.Versions)
                .ThenInclude(v => v.Sections)
            .FirstOrDefaultAsync(p => p.Id == paperId, ct);

        if (paper is null)
            return new PaperResult(false, "Paper not found.");

        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacherProfile is null || paper.AuthorTeacherProfileId != teacherProfile.Id)
            return new PaperResult(false, "Access denied.");

        var currentVersion = paper.Versions.FirstOrDefault(v => v.Id == paper.CurrentVersionId);
        if (currentVersion is null)
            return new PaperResult(false, "Current version not found.");

        if (currentVersion.IsLocked)
            return new PaperResult(false, "Current version is locked.");

        currentVersion.BlueprintVersionId = blueprintVersionId;
        _db.PaperSections.RemoveRange(currentVersion.Sections);

        var placeholderSections = new[]
        {
            new PaperSection { VersionId = currentVersion.Id, Name = "Section A – Objective", Marks = 20, SortOrder = 1 },
            new PaperSection { VersionId = currentVersion.Id, Name = "Section B – Short Answer", Marks = 30, SortOrder = 2 },
            new PaperSection { VersionId = currentVersion.Id, Name = "Section C – Long Answer", Marks = 50, SortOrder = 3 }
        };

        foreach (var section in placeholderSections)
            _db.PaperSections.Add(section);

        paper.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Auto-selection requires question bank data from later chunks. Blueprint {BlueprintVersionId} linked to Paper {PaperId}.",
            blueprintVersionId, paperId);

        await _audit.WriteAsync("Paper.GeneratedFromBlueprint", "Paper", paper.Id.ToString(), userId, cancellationToken: ct);

        return new PaperResult(true, PaperId: paper.Id, VersionId: currentVersion.Id);
    }

    // =========================================================================
    // Workflow transitions
    // =========================================================================

    public async Task<PaperResult> SubmitForApprovalAsync(Guid paperId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var userId = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new PaperResult(false, "User not authenticated.");

        var paper = await _db.Papers
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == paperId, ct);

        if (paper is null)
            return new PaperResult(false, "Paper not found.");

        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacherProfile is null || paper.AuthorTeacherProfileId != teacherProfile.Id)
            return new PaperResult(false, "Access denied.");

        if (paper.Status != PaperStatus.Draft && paper.Status != PaperStatus.Returned)
            return new PaperResult(false, "Paper can only be submitted from Draft or Returned status.");

        var currentVersion = paper.Versions.FirstOrDefault(v => v.Id == paper.CurrentVersionId);
        if (currentVersion is null)
            return new PaperResult(false, "Current version not found.");

        // Lock the version so no further author edits land between submit
        // and review.
        currentVersion.IsLocked = true;

        // Find an existing open instance (resubmits from Returned reuse it)
        // or start a new one for first-time submissions.
        var existingInstance = await _db.WorkflowInstances
            .FirstOrDefaultAsync(wi =>
                wi.SubjectType == PaperWorkflow.SubjectType
                && wi.SubjectId == paper.Id
                && !wi.IsCompleted, ct);

        Guid instanceId;
        if (existingInstance is null)
        {
            var startResult = await _workflow.StartAsync(
                PaperWorkflow.DefinitionKey,
                PaperWorkflow.SubjectType,
                paper.Id,
                userId,
                paper.InstituteId,
                ct);

            if (!startResult.IsSuccess || startResult.InstanceId is null)
                return new PaperResult(false, startResult.ErrorMessage ?? "Failed to start approval workflow.");

            instanceId = startResult.InstanceId.Value;
        }
        else
        {
            instanceId = existingInstance.Id;
        }

        var transitionResult = await _workflow.TransitionAsync(
            instanceId,
            PaperWorkflow.States.SubmittedForApproval,
            actor,
            cancellationToken: ct);

        if (!transitionResult.IsSuccess)
            return new PaperResult(false, transitionResult.ErrorMessage ?? "Failed to transition workflow.");

        paper.Status = PaperStatus.SubmittedForApproval;
        paper.UpdatedAtUtc = DateTime.UtcNow;

        _events.Enqueue(new PaperSubmittedEvent
        {
            PaperId = paper.Id,
            PaperTitle = currentVersion.Title,
            SubmittedByUserId = userId,
            InstituteId = paper.InstituteId
        });

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Paper.SubmittedForApproval", "Paper", paper.Id.ToString(), userId, cancellationToken: ct);

        return new PaperResult(true, PaperId: paper.Id, VersionId: currentVersion.Id);
    }

    public async Task<PaperResult> ApproveAsync(Guid paperId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var userId = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new PaperResult(false, "User not authenticated.");

        var paper = await _db.Papers
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == paperId, ct);

        if (paper is null)
            return new PaperResult(false, "Paper not found.");

        if (paper.Status != PaperStatus.SubmittedForApproval)
            return new PaperResult(false, $"Paper cannot be approved from {paper.Status} status.");

        var currentVersion = paper.Versions.FirstOrDefault(v => v.Id == paper.CurrentVersionId);
        if (currentVersion is null)
            return new PaperResult(false, "Current version not found.");

        var instance = await GetActiveInstanceAsync(paper.Id, ct);
        if (instance is null)
            return new PaperResult(false, "No active workflow instance for this paper.");

        var transitionResult = await _workflow.TransitionAsync(
            instance.Id,
            PaperWorkflow.States.Approved,
            actor,
            cancellationToken: ct);

        if (!transitionResult.IsSuccess)
            return new PaperResult(false, transitionResult.ErrorMessage ?? "Failed to transition workflow.");

        paper.Status = PaperStatus.Approved;
        paper.UpdatedAtUtc = DateTime.UtcNow;

        var authorUserId = await ResolveAuthorUserIdAsync(paper.AuthorTeacherProfileId, ct);

        _events.Enqueue(new PaperApprovedEvent
        {
            PaperId = paper.Id,
            PaperTitle = currentVersion.Title,
            ApprovedByUserId = userId,
            AuthorUserId = authorUserId,
            InstituteId = paper.InstituteId
        });

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Paper.Approved", "Paper", paper.Id.ToString(), userId, cancellationToken: ct);

        return new PaperResult(true, PaperId: paper.Id, VersionId: currentVersion.Id);
    }

    public async Task<PaperResult> ReturnAsync(Guid paperId, ClaimsPrincipal actor, string comment, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return new PaperResult(false, "A reviewer comment is required when returning a paper.");

        var userId = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new PaperResult(false, "User not authenticated.");

        var paper = await _db.Papers
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == paperId, ct);

        if (paper is null)
            return new PaperResult(false, "Paper not found.");

        if (paper.Status != PaperStatus.SubmittedForApproval)
            return new PaperResult(false, $"Paper cannot be returned from {paper.Status} status.");

        var currentVersion = paper.Versions.FirstOrDefault(v => v.Id == paper.CurrentVersionId);
        if (currentVersion is null)
            return new PaperResult(false, "Current version not found.");

        var instance = await GetActiveInstanceAsync(paper.Id, ct);
        if (instance is null)
            return new PaperResult(false, "No active workflow instance for this paper.");

        var transitionResult = await _workflow.TransitionAsync(
            instance.Id,
            PaperWorkflow.States.Returned,
            actor,
            comment,
            ct);

        if (!transitionResult.IsSuccess)
            return new PaperResult(false, transitionResult.ErrorMessage ?? "Failed to transition workflow.");

        paper.Status = PaperStatus.Returned;
        paper.UpdatedAtUtc = DateTime.UtcNow;

        // Unlock so the author can edit + resubmit.
        currentVersion.IsLocked = false;

        var authorUserId = await ResolveAuthorUserIdAsync(paper.AuthorTeacherProfileId, ct);

        _events.Enqueue(new PaperReturnedEvent
        {
            PaperId = paper.Id,
            PaperTitle = currentVersion.Title,
            AuthorUserId = authorUserId,
            ReturnedByUserId = userId,
            Comment = comment.Trim(),
            InstituteId = paper.InstituteId
        });

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Paper.Returned", "Paper", paper.Id.ToString(), userId, cancellationToken: ct);

        return new PaperResult(true, PaperId: paper.Id, VersionId: currentVersion.Id);
    }

    public async Task<PaperResult> PublishAsync(Guid paperId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var userId = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new PaperResult(false, "User not authenticated.");

        var paper = await _db.Papers
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == paperId, ct);

        if (paper is null)
            return new PaperResult(false, "Paper not found.");

        if (paper.Status != PaperStatus.Approved)
            return new PaperResult(false, $"Only approved papers can be published; this paper is {paper.Status}.");

        var currentVersion = paper.Versions.FirstOrDefault(v => v.Id == paper.CurrentVersionId);
        if (currentVersion is null)
            return new PaperResult(false, "Current version not found.");

        var instance = await GetActiveInstanceAsync(paper.Id, ct);
        if (instance is null)
            return new PaperResult(false, "No active workflow instance for this paper.");

        var transitionResult = await _workflow.TransitionAsync(
            instance.Id,
            PaperWorkflow.States.Published,
            actor,
            cancellationToken: ct);

        if (!transitionResult.IsSuccess)
            return new PaperResult(false, transitionResult.ErrorMessage ?? "Failed to transition workflow.");

        paper.Status = PaperStatus.Published;
        paper.UpdatedAtUtc = DateTime.UtcNow;

        var authorUserId = await ResolveAuthorUserIdAsync(paper.AuthorTeacherProfileId, ct);

        _events.Enqueue(new PaperPublishedEvent
        {
            PaperId = paper.Id,
            PaperTitle = currentVersion.Title,
            PublishedByUserId = userId,
            AuthorUserId = authorUserId,
            InstituteId = paper.InstituteId
        });

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Paper.Published", "Paper", paper.Id.ToString(), userId, cancellationToken: ct);

        return new PaperResult(true, PaperId: paper.Id, VersionId: currentVersion.Id);
    }

    public async Task<PaperResult> ArchiveAsync(Guid paperId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var userId = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new PaperResult(false, "User not authenticated.");

        var paper = await _db.Papers
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == paperId, ct);

        if (paper is null)
            return new PaperResult(false, "Paper not found.");

        if (paper.Status != PaperStatus.Published)
            return new PaperResult(false, $"Only published papers can be archived; this paper is {paper.Status}.");

        var currentVersion = paper.Versions.FirstOrDefault(v => v.Id == paper.CurrentVersionId);
        if (currentVersion is null)
            return new PaperResult(false, "Current version not found.");

        var instance = await GetActiveInstanceAsync(paper.Id, ct);
        if (instance is null)
            return new PaperResult(false, "No active workflow instance for this paper.");

        var transitionResult = await _workflow.TransitionAsync(
            instance.Id,
            PaperWorkflow.States.Archived,
            actor,
            cancellationToken: ct);

        if (!transitionResult.IsSuccess)
            return new PaperResult(false, transitionResult.ErrorMessage ?? "Failed to transition workflow.");

        paper.Status = PaperStatus.Archived;
        paper.UpdatedAtUtc = DateTime.UtcNow;

        var authorUserId = await ResolveAuthorUserIdAsync(paper.AuthorTeacherProfileId, ct);

        _events.Enqueue(new PaperArchivedEvent
        {
            PaperId = paper.Id,
            PaperTitle = currentVersion.Title,
            ArchivedByUserId = userId,
            AuthorUserId = authorUserId,
            InstituteId = paper.InstituteId
        });

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Paper.Archived", "Paper", paper.Id.ToString(), userId, cancellationToken: ct);

        return new PaperResult(true, PaperId: paper.Id, VersionId: currentVersion.Id);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Find the open <see cref="WorkflowInstance"/> for a paper. The
    /// engine guarantees there is at most one open instance per
    /// (subjectType, subjectId).
    /// </summary>
    private Task<WorkflowInstance?> GetActiveInstanceAsync(Guid paperId, CancellationToken ct) =>
        _db.WorkflowInstances.FirstOrDefaultAsync(wi =>
            wi.SubjectType == PaperWorkflow.SubjectType
            && wi.SubjectId == paperId
            && !wi.IsCompleted, ct);

    /// <summary>
    /// Resolve the Identity user id of the paper's author. Used to
    /// route notifications to the correct recipient. Returns an empty
    /// string if the teacher profile has no associated user (which
    /// projections handle gracefully by emitting zero notifications).
    /// </summary>
    private async Task<string> ResolveAuthorUserIdAsync(Guid teacherProfileId, CancellationToken ct)
    {
        var userId = await _db.TeacherProfiles
            .AsNoTracking()
            .Where(t => t.Id == teacherProfileId)
            .Select(t => t.UserId)
            .FirstOrDefaultAsync(ct);

        return userId ?? string.Empty;
    }
}
