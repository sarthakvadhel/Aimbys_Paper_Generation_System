using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Authorization;
using Aimbys.Application.Moderation;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Entities.Moderation;
using Aimbys.Domain.Entities.Results;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Aimbys.Infrastructure.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Moderation;

/// <summary>
/// Moderation desk workflow: enqueue evaluations, approve/require-changes/
/// override, write FinalPublishedScore, and trigger result publication when
/// all moderations for an attempt are complete.
/// </summary>
public class ModerationService : IModerationService
{
    private readonly AppDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly DomainEventCollector _events;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IInstituteScope _instituteScope;

    public ModerationService(
        AppDbContext db,
        IAuditWriter audit,
        DomainEventCollector events,
        UserManager<IdentityUser> userManager,
        IInstituteScope instituteScope)
    {
        _db = db;
        _audit = audit;
        _events = events;
        _userManager = userManager;
        _instituteScope = instituteScope;
    }

    public async Task<ModerationResult> EnqueueForModerationAsync(
        Guid evaluationId, CancellationToken ct = default)
    {
        var moderator = await _db.TeacherProfiles
            .Where(t => t.CanModerate && t.Status == ProfileStatus.Active)
            .OrderBy(t => _db.Set<Domain.Entities.Moderation.Moderation>()
                .Where(m => m.ModeratorTeacherProfileId == t.Id)
                .Max(m => (DateTime?)m.AssignedAtUtc) ?? DateTime.MinValue)
            .FirstOrDefaultAsync(ct);

        if (moderator is null)
            return ModerationResult.Fail("No available moderator found.");

        // Capture evaluator scores into snapshot JSON.
        var evaluatedScore = await _db.EvaluatedScores
            .FirstOrDefaultAsync(es => es.EvaluationId == evaluationId, ct);

        var snapshotJson = evaluatedScore != null
            ? JsonSerializer.Serialize(new
            {
                evaluatedScore.TotalPointsAwarded,
                evaluatedScore.MaxPointsPossible,
                evaluatedScore.Feedback
            })
            : "{}";

        var moderation = new Domain.Entities.Moderation.Moderation
        {
            EvaluationId = evaluationId,
            ModeratorTeacherProfileId = moderator.Id,
            Verdict = ModerationVerdict.Pending,
            AssignedAtUtc = DateTime.UtcNow
        };
        _db.Set<Domain.Entities.Moderation.Moderation>().Add(moderation);

        _db.Set<ModerationSnapshot>().Add(new ModerationSnapshot
        {
            ModerationId = moderation.Id,
            EvaluatorScoresJson = snapshotJson,
            CapturedAtUtc = DateTime.UtcNow
        });

        _events.Enqueue(new ModerationAssignedEvent
        {
            ModerationId = moderation.Id,
            EvaluationId = evaluationId,
            ModeratorUserId = moderator.UserId
        });

        await _audit.WriteAsync(
            "Moderation.Assigned",
            "Moderation",
            moderation.Id.ToString(),
            null,
            JsonSerializer.Serialize(new { evaluationId, moderatorProfileId = moderator.Id }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);
        return ModerationResult.Ok();
    }

    public async Task<ModerationResult> ApproveAsync(
        Guid moderationId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var moderation = await _db.Set<Domain.Entities.Moderation.Moderation>()
            .FirstOrDefaultAsync(m => m.Id == moderationId, ct);

        if (moderation is null)
            return ModerationResult.Fail("Moderation not found.");

        var actorUserId = _userManager.GetUserId(actor);

        // Pull the evaluated score to pass through as the final score.
        var evaluatedScore = await _db.EvaluatedScores
            .FirstOrDefaultAsync(es => es.EvaluationId == moderation.EvaluationId, ct);

        moderation.Verdict = ModerationVerdict.Approved;
        moderation.CompletedAtUtc = DateTime.UtcNow;

        var moderatedScore = new ModeratedScore
        {
            ModerationId = moderationId,
            EvaluationId = moderation.EvaluationId,
            TotalPointsAwarded = evaluatedScore?.TotalPointsAwarded ?? 0m,
            MaxPointsPossible = evaluatedScore?.MaxPointsPossible ?? 0m,
            ModeratedByUserId = actorUserId ?? string.Empty,
            ModeratedAtUtc = DateTime.UtcNow,
            IsOverride = false
        };
        _db.Set<ModeratedScore>().Add(moderatedScore);

        // Write FinalPublishedScore for the answer.
        var evaluation = await _db.Evaluations
            .FirstOrDefaultAsync(e => e.Id == moderation.EvaluationId, ct);

        if (evaluation != null)
        {
            _db.FinalPublishedScores.Add(new FinalPublishedScore
            {
                ExamAttemptAnswerId = evaluation.AttemptAnswerId,
                PointsAwarded = moderatedScore.TotalPointsAwarded,
                MaxPoints = moderatedScore.MaxPointsPossible,
                Source = ScoreSource.Moderated,
                ComputedAtUtc = DateTime.UtcNow
            });
        }

        _events.Enqueue(new ModerationApprovedEvent
        {
            ModerationId = moderationId,
            EvaluationId = moderation.EvaluationId
        });

        await _audit.WriteAsync(
            "Moderation.Approved",
            "Moderation",
            moderationId.ToString(),
            actorUserId,
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        // Check if all moderations for this attempt are now complete and
        // update the Result state accordingly.
        if (evaluation != null)
            await TryAdvanceResultStateAsync(evaluation.AttemptAnswerId, ct);

        return ModerationResult.Ok();
    }

    public async Task<ModerationResult> RequireChangesAsync(
        Guid moderationId, ClaimsPrincipal actor, string comment, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(comment))
            return ModerationResult.Fail("Comment is required when requesting changes.");

        var moderation = await _db.Set<Domain.Entities.Moderation.Moderation>()
            .FirstOrDefaultAsync(m => m.Id == moderationId, ct);

        if (moderation is null)
            return ModerationResult.Fail("Moderation not found.");

        var actorUserId = _userManager.GetUserId(actor);
        moderation.Verdict = ModerationVerdict.RequiresChanges;
        moderation.Comment = comment;

        // Return the evaluation to the evaluator.
        var evaluation = await _db.Evaluations
            .FirstOrDefaultAsync(e => e.Id == moderation.EvaluationId, ct);
        if (evaluation != null)
            evaluation.Status = EvaluationStatus.Returned;

        _events.Enqueue(new ModerationReturnedEvent
        {
            ModerationId = moderationId,
            ReturnedToUserId = evaluation?.EvaluatorTeacherProfileId.ToString() ?? string.Empty,
            Comment = comment
        });

        await _audit.WriteAsync(
            "Moderation.RequiresChanges",
            "Moderation",
            moderationId.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { comment }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);
        return ModerationResult.Ok();
    }

    public async Task<ModerationResult> OverrideAsync(
        Guid moderationId, ClaimsPrincipal actor,
        decimal newScore, decimal maxScore, string reason,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ModerationResult.Fail("Override reason is required.");

        var moderation = await _db.Set<Domain.Entities.Moderation.Moderation>()
            .FirstOrDefaultAsync(m => m.Id == moderationId, ct);

        if (moderation is null)
            return ModerationResult.Fail("Moderation not found.");

        var actorUserId = _userManager.GetUserId(actor);
        moderation.Verdict = ModerationVerdict.Overridden;
        moderation.OverrideReason = reason;
        moderation.CompletedAtUtc = DateTime.UtcNow;

        var moderatedScore = new ModeratedScore
        {
            ModerationId = moderationId,
            EvaluationId = moderation.EvaluationId,
            TotalPointsAwarded = newScore,
            MaxPointsPossible = maxScore,
            ModeratedByUserId = actorUserId ?? string.Empty,
            ModeratedAtUtc = DateTime.UtcNow,
            IsOverride = true,
            OverrideReason = reason
        };
        _db.Set<ModeratedScore>().Add(moderatedScore);

        // Write FinalPublishedScore with the overridden value.
        var evaluation = await _db.Evaluations
            .FirstOrDefaultAsync(e => e.Id == moderation.EvaluationId, ct);

        if (evaluation != null)
        {
            _db.FinalPublishedScores.Add(new FinalPublishedScore
            {
                ExamAttemptAnswerId = evaluation.AttemptAnswerId,
                PointsAwarded = newScore,
                MaxPoints = maxScore,
                Source = ScoreSource.Overridden,
                ComputedAtUtc = DateTime.UtcNow
            });
        }

        await _audit.WriteAsync(
            "Moderation.Override",
            "Moderation",
            moderationId.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { newScore, maxScore, reason }),
            AuditSeverity.Warning,
            ct);

        _events.Enqueue(new ModerationOverriddenEvent
        {
            ModerationId = moderationId,
            EvaluationId = moderation.EvaluationId,
            OverrideReason = reason
        });

        await _db.SaveChangesAsync(ct);

        if (evaluation != null)
            await TryAdvanceResultStateAsync(evaluation.AttemptAnswerId, ct);

        return ModerationResult.Ok();
    }

    public async Task<ModerationContext?> GetContextAsync(
        Guid moderationId, CancellationToken ct = default)
    {
        var moderation = await _db.Set<Domain.Entities.Moderation.Moderation>()
            .FirstOrDefaultAsync(m => m.Id == moderationId, ct);

        if (moderation is null) return null;

        var snapshot = await _db.Set<ModerationSnapshot>()
            .FirstOrDefaultAsync(s => s.ModerationId == moderationId, ct);

        var evaluatedScore = await _db.EvaluatedScores
            .FirstOrDefaultAsync(es => es.EvaluationId == moderation.EvaluationId, ct);

        var evaluation = await _db.Evaluations
            .FirstOrDefaultAsync(e => e.Id == moderation.EvaluationId, ct);

        string? answerJson = null;
        if (evaluation != null)
        {
            var answer = await _db.ExamAttemptAnswers
                .FirstOrDefaultAsync(a => a.Id == evaluation.AttemptAnswerId, ct);
            answerJson = answer?.AnswerJson;
        }

        return new ModerationContext(
            ModerationId: moderation.Id,
            EvaluationId: moderation.EvaluationId,
            EvaluatorScoresJson: snapshot?.EvaluatorScoresJson,
            AnswerJson: answerJson,
            Verdict: moderation.Verdict,
            EvaluatedTotal: evaluatedScore?.TotalPointsAwarded,
            MaxPossible: evaluatedScore?.MaxPointsPossible);
    }

    // ----- helpers ----------------------------------------------------------

    /// <summary>
    /// After a moderation completes, checks whether all manual answers for
    /// the attempt now have a FinalPublishedScore. If so, advances the
    /// Result to Approved state so it can be published.
    /// </summary>
    private async Task TryAdvanceResultStateAsync(Guid answerId, CancellationToken ct)
    {
        var attemptId = await _db.ExamAttemptAnswers
            .Where(a => a.Id == answerId)
            .Select(a => a.AttemptId)
            .FirstOrDefaultAsync(ct);

        if (attemptId == Guid.Empty) return;

        var allAnswerIds = await _db.ExamAttemptAnswers
            .Where(a => a.AttemptId == attemptId)
            .Select(a => a.Id)
            .ToListAsync(ct);

        // Check every answer has a FinalPublishedScore.
        var scoredCount = await _db.FinalPublishedScores
            .Where(fps => allAnswerIds.Contains(fps.ExamAttemptAnswerId))
            .Select(fps => fps.ExamAttemptAnswerId)
            .Distinct()
            .CountAsync(ct);

        if (scoredCount < allAnswerIds.Count) return;

        // All answers scored — advance Result to Approved.
        var result = await _db.Results
            .FirstOrDefaultAsync(r => r.ExamAttemptId == attemptId, ct);

        if (result == null || result.IsPublished) return;

        result.State = ResultState.Approved;
        await _db.SaveChangesAsync(ct);
    }
}
