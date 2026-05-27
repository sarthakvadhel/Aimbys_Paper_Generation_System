using Aimbys.Application.Audit;
using Aimbys.Application.Evaluation;
using Aimbys.Application.Moderation;
using Aimbys.Domain.Entities.Evaluation;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Evaluation;

/// <summary>
/// Core evaluation service: draft-save, submit (→ moderation), and scoring context retrieval.
/// </summary>
public class EvaluationService : IEvaluationService
{
    private readonly AppDbContext _db;
    private readonly DomainEventCollector _events;
    private readonly IAuditWriter _audit;
    private readonly IModerationService _moderation;

    public EvaluationService(
        AppDbContext db,
        DomainEventCollector events,
        IAuditWriter audit,
        IModerationService moderation)
    {
        _db = db;
        _events = events;
        _audit = audit;
        _moderation = moderation;
    }

    public async Task<bool> SaveDraftScoreAsync(
        Guid evaluationId, int criterionIndex, decimal points,
        string actorUserId, CancellationToken ct = default)
    {
        var evaluation = await _db.Evaluations.FindAsync(new object[] { evaluationId }, ct);
        if (evaluation is null) return false;

        var existing = await _db.DraftScores
            .FirstOrDefaultAsync(d => d.EvaluationId == evaluationId && d.CriterionIndex == criterionIndex, ct);

        if (existing is not null)
        {
            existing.PointsAwarded = points;
            existing.SavedAtUtc = DateTime.UtcNow;
        }
        else
        {
            _db.DraftScores.Add(new DraftScore
            {
                EvaluationId = evaluationId,
                CriterionIndex = criterionIndex,
                PointsAwarded = points,
            });
        }

        if (evaluation.Status == EvaluationStatus.Pending)
            evaluation.Status = EvaluationStatus.InProgress;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SaveFeedbackDraftAsync(
        Guid evaluationId, string feedback,
        string actorUserId, CancellationToken ct = default)
    {
        var evaluation = await _db.Evaluations.FindAsync(new object[] { evaluationId }, ct);
        if (evaluation is null) return false;

        evaluation.Feedback = feedback;

        if (evaluation.Status == EvaluationStatus.Pending)
            evaluation.Status = EvaluationStatus.InProgress;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<EvaluationSubmitResult> SubmitAsync(
        Guid evaluationId, string actorUserId, CancellationToken ct = default)
    {
        var evaluation = await _db.Evaluations.FindAsync(new object[] { evaluationId }, ct);
        if (evaluation is null)
            return new EvaluationSubmitResult(false, "Evaluation not found.");

        var draftScores = await _db.DraftScores
            .Where(d => d.EvaluationId == evaluationId)
            .ToListAsync(ct);

        if (draftScores.Count == 0)
            return new EvaluationSubmitResult(false, "No scores have been entered.");

        var totalAwarded = draftScores.Sum(d => d.PointsAwarded);

        // Compute max from rubric criteria on the question version.
        var answer = await _db.ExamAttemptAnswers
            .FirstOrDefaultAsync(a => a.Id == evaluation.AttemptAnswerId, ct);

        decimal maxPossible = 0m;
        if (answer != null)
        {
            maxPossible = await _db.QuestionVersions
                .Where(v => v.Id == answer.QuestionVersionId)
                .SelectMany(v => v.RubricCriteria)
                .SumAsync(r => r.MaxPoints, ct);

            // Fall back to version marks if no rubric criteria defined.
            if (maxPossible == 0m)
            {
                maxPossible = await _db.QuestionVersions
                    .Where(v => v.Id == answer.QuestionVersionId)
                    .Select(v => v.Marks)
                    .FirstOrDefaultAsync(ct);
            }
        }

        var evaluatedScore = new EvaluatedScore
        {
            EvaluationId = evaluationId,
            TotalPointsAwarded = totalAwarded,
            MaxPointsPossible = maxPossible,
            Feedback = evaluation.Feedback,
            EvaluatedByUserId = actorUserId,
            EvaluatedAtUtc = DateTime.UtcNow,
        };
        _db.EvaluatedScores.Add(evaluatedScore);

        evaluation.Status = EvaluationStatus.Submitted;
        evaluation.CompletedAtUtc = DateTime.UtcNow;

        _events.Enqueue(new EvaluationSubmittedEvent
        {
            EvaluationId = evaluationId,
            AttemptAnswerId = evaluation.AttemptAnswerId,
            EvaluatorUserId = actorUserId,
        });

        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync(
            action: "Evaluation.Submitted",
            entityType: "Evaluation",
            entityId: evaluationId.ToString(),
            actorUserId: actorUserId,
            detailsJson: $"{{\"totalAwarded\":{totalAwarded},\"maxPossible\":{maxPossible}}}",
            cancellationToken: ct);

        // Route to moderation queue.
        await _moderation.EnqueueForModerationAsync(evaluationId, ct);

        return new EvaluationSubmitResult(true);
    }

    public async Task<EvaluationContext?> GetScoringContextAsync(
        Guid evaluationId, CancellationToken ct = default)
    {
        var evaluation = await _db.Evaluations.FindAsync(new object[] { evaluationId }, ct);
        if (evaluation is null) return null;

        var draftScores = await _db.DraftScores
            .Where(d => d.EvaluationId == evaluationId)
            .Select(d => new DraftScoreItem(d.CriterionIndex, d.PointsAwarded))
            .ToListAsync(ct);

        // Load the student's answer and rubric criteria.
        var answer = await _db.ExamAttemptAnswers
            .FirstOrDefaultAsync(a => a.Id == evaluation.AttemptAnswerId, ct);

        string? criteriaJson = null;
        if (answer != null)
        {
            var criteria = await _db.QuestionVersions
                .Where(v => v.Id == answer.QuestionVersionId)
                .SelectMany(v => v.RubricCriteria)
                .Select(r => new { r.SortOrder, r.Criterion, r.MaxPoints })
                .ToListAsync(ct);

            if (criteria.Count > 0)
                criteriaJson = System.Text.Json.JsonSerializer.Serialize(criteria);
        }

        return new EvaluationContext(
            EvaluationId: evaluationId,
            AnswerJson: answer?.AnswerJson,
            CriteriaJson: criteriaJson,
            DraftScores: draftScores,
            Feedback: evaluation.Feedback);
    }
}
