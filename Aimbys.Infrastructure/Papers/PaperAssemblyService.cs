using System.Security.Claims;
using Aimbys.Application.Audit;
using Aimbys.Application.Papers;
using Aimbys.Application.Workflow;
using Aimbys.Domain.Entities.Papers;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Papers;

/// <summary>
/// Implements paper assembly workflows: draft creation, saving,
/// blueprint-based generation, submission, approval, and return.
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

        // Validate
        var validationResult = _validation.Validate(request, currentVersion.TotalMarks);
        if (!validationResult.IsValid)
            return new PaperResult(false, string.Join("; ", validationResult.Errors));

        // Clear existing sections and questions on the current version
        _db.PaperSections.RemoveRange(currentVersion.Sections);
        _db.PaperQuestions.RemoveRange(currentVersion.Questions);

        // Recreate from request
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

        // V1 stub: create placeholder sections from blueprint concept.
        // Full question-selection logic requires the question bank data from Chunks 20-21.
        currentVersion.BlueprintVersionId = blueprintVersionId;

        // Clear existing sections
        _db.PaperSections.RemoveRange(currentVersion.Sections);

        // Create placeholder sections
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
            "Auto-selection requires question bank data from Chunks 20-21. Blueprint {BlueprintVersionId} linked to Paper {PaperId}.",
            blueprintVersionId, paperId);

        await _audit.WriteAsync("Paper.GeneratedFromBlueprint", "Paper", paper.Id.ToString(), userId, cancellationToken: ct);

        return new PaperResult(true, PaperId: paper.Id, VersionId: currentVersion.Id);
    }

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

        // Lock the version
        currentVersion.IsLocked = true;

        // Start the PaperApproval workflow
        var wfResult = await _workflow.StartAsync(
            "PaperApproval",
            "Paper",
            paper.Id,
            userId,
            paper.InstituteId,
            ct);

        if (!wfResult.IsSuccess)
            return new PaperResult(false, wfResult.ErrorMessage ?? "Failed to start approval workflow.");

        // Transition to SubmittedForApproval
        var transitionResult = await _workflow.TransitionAsync(
            wfResult.InstanceId!.Value,
            "SubmittedForApproval",
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
            return new PaperResult(false, "Paper is not in SubmittedForApproval status.");

        var currentVersion = paper.Versions.FirstOrDefault(v => v.Id == paper.CurrentVersionId);
        if (currentVersion is null)
            return new PaperResult(false, "Current version not found.");

        // Get workflow instance
        var currentState = await _workflow.GetCurrentStateAsync("Paper", paper.Id, ct);
        if (currentState is null)
            return new PaperResult(false, "No active workflow found.");

        // Find the workflow instance for the transition
        var wfInstance = await _db.WorkflowInstances
            .FirstOrDefaultAsync(wi => wi.SubjectType == "Paper" && wi.SubjectId == paper.Id && !wi.IsCompleted, ct);

        if (wfInstance is null)
            return new PaperResult(false, "No active workflow instance.");

        var transitionResult = await _workflow.TransitionAsync(
            wfInstance.Id,
            "Approved",
            actor,
            cancellationToken: ct);

        if (!transitionResult.IsSuccess)
            return new PaperResult(false, transitionResult.ErrorMessage ?? "Failed to transition workflow.");

        paper.Status = PaperStatus.Approved;
        paper.UpdatedAtUtc = DateTime.UtcNow;

        _events.Enqueue(new PaperApprovedEvent
        {
            PaperId = paper.Id,
            PaperTitle = currentVersion.Title,
            ApprovedByUserId = userId,
            InstituteId = paper.InstituteId
        });

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Paper.Approved", "Paper", paper.Id.ToString(), userId, cancellationToken: ct);

        return new PaperResult(true, PaperId: paper.Id, VersionId: currentVersion.Id);
    }

    public async Task<PaperResult> ReturnAsync(Guid paperId, ClaimsPrincipal actor, string comment, CancellationToken ct = default)
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
            return new PaperResult(false, "Paper is not in SubmittedForApproval status.");

        var currentVersion = paper.Versions.FirstOrDefault(v => v.Id == paper.CurrentVersionId);
        if (currentVersion is null)
            return new PaperResult(false, "Current version not found.");

        // Get the workflow instance
        var wfInstance = await _db.WorkflowInstances
            .FirstOrDefaultAsync(wi => wi.SubjectType == "Paper" && wi.SubjectId == paper.Id && !wi.IsCompleted, ct);

        if (wfInstance is null)
            return new PaperResult(false, "No active workflow instance.");

        var transitionResult = await _workflow.TransitionAsync(
            wfInstance.Id,
            "Returned",
            actor,
            comment,
            ct);

        if (!transitionResult.IsSuccess)
            return new PaperResult(false, transitionResult.ErrorMessage ?? "Failed to transition workflow.");

        paper.Status = PaperStatus.Returned;
        paper.UpdatedAtUtc = DateTime.UtcNow;

        // Unlock the version so the author can edit again
        currentVersion.IsLocked = false;

        // Find the author's user id
        var authorProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.Id == paper.AuthorTeacherProfileId, ct);

        _events.Enqueue(new PaperReturnedEvent
        {
            PaperId = paper.Id,
            AuthorUserId = authorProfile?.UserId ?? string.Empty,
            Comment = comment,
            InstituteId = paper.InstituteId
        });

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("Paper.Returned", "Paper", paper.Id.ToString(), userId, cancellationToken: ct);

        return new PaperResult(true, PaperId: paper.Id, VersionId: currentVersion.Id);
    }
}
