using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Authorization;
using Aimbys.Application.Questions;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Entities.Questions;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Questions;

/// <summary>
/// Implements question authoring with versioning.
/// </summary>
public class QuestionAuthoringService : IQuestionAuthoringService
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _instituteScope;
    private readonly IAuditWriter _audit;
    private readonly DomainEventCollector _events;
    private readonly UserManager<IdentityUser> _userManager;

    public QuestionAuthoringService(
        AppDbContext db,
        IInstituteScope instituteScope,
        IAuditWriter audit,
        DomainEventCollector events,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _instituteScope = instituteScope;
        _audit = audit;
        _events = events;
        _userManager = userManager;
    }

    public async Task<QuestionCreateResult> CreateAsync(QuestionCreateRequest request, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var userId = _userManager.GetUserId(actor);
        if (string.IsNullOrEmpty(userId))
            return new QuestionCreateResult(false, "User not authenticated.", null, null);

        var instituteId = await _instituteScope.GetCurrentInstituteIdAsync(actor, ct);
        if (instituteId is null)
            return new QuestionCreateResult(false, "No institute context.", null, null);

        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacherProfile is null)
            return new QuestionCreateResult(false, "Teacher profile not found.", null, null);

        var sanitizedBody = HtmlSanitizer.Sanitize(request.BodyHtml);
        var sanitizedInstructions = HtmlSanitizer.Sanitize(request.InstructionsHtml);

        var question = new Question
        {
            InstituteId = instituteId.Value,
            SubjectId = request.SubjectId,
            ChapterId = request.ChapterId,
            AuthorTeacherProfileId = teacherProfile.Id,
            Status = QuestionStatus.Draft,
            Type = request.Type
        };

        var version = new QuestionVersion
        {
            QuestionId = question.Id,
            VersionNumber = 1,
            BodyHtml = sanitizedBody,
            Difficulty = request.Difficulty,
            BloomLevel = request.BloomLevel,
            Marks = request.Marks,
            EstimatedTimeMinutes = request.EstimatedTimeMinutes,
            InstructionsHtml = sanitizedInstructions,
            IsCurrentVersion = true,
            AuthorUserId = userId
        };

        // Options
        foreach (var opt in request.Options)
        {
            version.Options.Add(new QuestionOption
            {
                VersionId = version.Id,
                Label = opt.Label,
                Text = opt.Text,
                IsCorrect = opt.IsCorrect,
                SortOrder = opt.SortOrder
            });
        }

        // Rubric criteria
        foreach (var rc in request.RubricCriteria)
        {
            version.RubricCriteria.Add(new QuestionRubricCriterion
            {
                VersionId = version.Id,
                Criterion = rc.Criterion,
                MaxPoints = rc.MaxPoints,
                SortOrder = rc.SortOrder
            });
        }

        // Test cases
        foreach (var tc in request.TestCases)
        {
            version.TestCases.Add(new QuestionTestCase
            {
                VersionId = version.Id,
                Input = tc.Input,
                ExpectedOutput = tc.ExpectedOutput,
                IsHidden = tc.IsHidden,
                TimeoutMs = tc.TimeoutMs,
                MemoryLimitMb = tc.MemoryLimitMb,
                SortOrder = tc.SortOrder
            });
        }

        question.CurrentVersionId = version.Id;
        question.Versions.Add(version);

        _db.Set<Question>().Add(question);

        // Audit
        await _audit.WriteAsync(
            "Question.Created",
            "Question",
            question.Id.ToString(),
            userId,
            JsonSerializer.Serialize(new { question.Type, request.SubjectId }),
            cancellationToken: ct);

        // Domain event
        _events.Enqueue(new QuestionCreatedEvent
        {
            InstituteId = instituteId.Value,
            QuestionId = question.Id,
            SubjectId = request.SubjectId,
            AuthorUserId = userId
        });

        await _db.SaveChangesAsync(ct);

        return new QuestionCreateResult(true, null, question.Id, version.Id);
    }

    public async Task<QuestionEditResult> EditAsync(Guid questionId, QuestionEditRequest request, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var userId = _userManager.GetUserId(actor);
        if (string.IsNullOrEmpty(userId))
            return new QuestionEditResult(false, "User not authenticated.", false, null);

        var question = await _db.Set<Question>()
            .Include(q => q.Versions)
                .ThenInclude(v => v.Options)
            .Include(q => q.Versions)
                .ThenInclude(v => v.RubricCriteria)
            .Include(q => q.Versions)
                .ThenInclude(v => v.TestCases)
            .FirstOrDefaultAsync(q => q.Id == questionId, ct);

        if (question is null)
            return new QuestionEditResult(false, "Question not found.", false, null);

        var sanitizedBody = HtmlSanitizer.Sanitize(request.BodyHtml);
        var sanitizedInstructions = HtmlSanitizer.Sanitize(request.InstructionsHtml);

        // If Approved or Retired, create a NEW version
        if (question.Status == QuestionStatus.Approved || question.Status == QuestionStatus.Retired)
        {
            var currentVersion = question.Versions.FirstOrDefault(v => v.IsCurrentVersion);
            if (currentVersion is not null)
                currentVersion.IsCurrentVersion = false;

            var maxVersion = question.Versions.Max(v => v.VersionNumber);
            var newVersion = new QuestionVersion
            {
                QuestionId = question.Id,
                VersionNumber = maxVersion + 1,
                BodyHtml = sanitizedBody,
                Difficulty = request.Difficulty,
                BloomLevel = request.BloomLevel,
                Marks = request.Marks,
                EstimatedTimeMinutes = request.EstimatedTimeMinutes,
                InstructionsHtml = sanitizedInstructions,
                IsCurrentVersion = true,
                AuthorUserId = userId
            };

            ApplyChildRecords(newVersion, request);
            question.Versions.Add(newVersion);
            question.CurrentVersionId = newVersion.Id;
            question.UpdatedAtUtc = DateTime.UtcNow;

            await _audit.WriteAsync(
                "Question.NewVersion",
                "Question",
                question.Id.ToString(),
                userId,
                JsonSerializer.Serialize(new { VersionNumber = newVersion.VersionNumber }),
                cancellationToken: ct);

            await _db.SaveChangesAsync(ct);
            return new QuestionEditResult(true, null, true, newVersion.Id);
        }
        else
        {
            // Draft / Submitted / UnderReview / Rejected — update existing draft version inline
            var currentVersion = question.Versions.FirstOrDefault(v => v.IsCurrentVersion);
            if (currentVersion is null)
                return new QuestionEditResult(false, "No current version found.", false, null);

            currentVersion.BodyHtml = sanitizedBody;
            currentVersion.Difficulty = request.Difficulty;
            currentVersion.BloomLevel = request.BloomLevel;
            currentVersion.Marks = request.Marks;
            currentVersion.EstimatedTimeMinutes = request.EstimatedTimeMinutes;
            currentVersion.InstructionsHtml = sanitizedInstructions;
            currentVersion.AuthorUserId = userId;

            // Replace child records
            _db.Set<QuestionOption>().RemoveRange(currentVersion.Options);
            _db.Set<QuestionRubricCriterion>().RemoveRange(currentVersion.RubricCriteria);
            _db.Set<QuestionTestCase>().RemoveRange(currentVersion.TestCases);

            currentVersion.Options.Clear();
            currentVersion.RubricCriteria.Clear();
            currentVersion.TestCases.Clear();

            ApplyChildRecords(currentVersion, request);
            question.UpdatedAtUtc = DateTime.UtcNow;

            await _audit.WriteAsync(
                "Question.Edited",
                "Question",
                question.Id.ToString(),
                userId,
                JsonSerializer.Serialize(new { VersionId = currentVersion.Id }),
                cancellationToken: ct);

            await _db.SaveChangesAsync(ct);
            return new QuestionEditResult(true, null, false, currentVersion.Id);
        }
    }

    public async Task<IReadOnlyList<QuestionVersionSummary>> GetRevisionHistoryAsync(Guid questionId, CancellationToken ct = default)
    {
        var versions = await _db.Set<QuestionVersion>()
            .Where(v => v.QuestionId == questionId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new QuestionVersionSummary(
                v.Id,
                v.VersionNumber,
                v.CreatedAtUtc,
                v.AuthorUserId,
                v.IsCurrentVersion))
            .ToListAsync(ct);

        return versions;
    }

    private static void ApplyChildRecords(QuestionVersion version, QuestionEditRequest request)
    {
        foreach (var opt in request.Options)
        {
            version.Options.Add(new QuestionOption
            {
                VersionId = version.Id,
                Label = opt.Label,
                Text = opt.Text,
                IsCorrect = opt.IsCorrect,
                SortOrder = opt.SortOrder
            });
        }

        foreach (var rc in request.RubricCriteria)
        {
            version.RubricCriteria.Add(new QuestionRubricCriterion
            {
                VersionId = version.Id,
                Criterion = rc.Criterion,
                MaxPoints = rc.MaxPoints,
                SortOrder = rc.SortOrder
            });
        }

        foreach (var tc in request.TestCases)
        {
            version.TestCases.Add(new QuestionTestCase
            {
                VersionId = version.Id,
                Input = tc.Input,
                ExpectedOutput = tc.ExpectedOutput,
                IsHidden = tc.IsHidden,
                TimeoutMs = tc.TimeoutMs,
                MemoryLimitMb = tc.MemoryLimitMb,
                SortOrder = tc.SortOrder
            });
        }
    }
}
