using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Analytics;
using Aimbys.Application.Audit;
using Aimbys.Application.Results;
using Aimbys.Domain.Entities.Results;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Analytics;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Results;

/// <summary>
/// Publishes exam results. For auto-only exams this is called immediately
/// after submission. For mixed/manual exams it is called after all
/// moderations for an attempt are approved.
/// </summary>
public class ResultPublicationService : IResultPublicationService
{
    private readonly AppDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly DomainEventCollector _events;
    private readonly IAnalyticsAggregationService _analytics;

    public ResultPublicationService(
        AppDbContext db,
        IAuditWriter audit,
        DomainEventCollector events,
        IAnalyticsAggregationService analytics)
    {
        _db = db;
        _audit = audit;
        _events = events;
        _analytics = analytics;
    }

    public async Task<(bool CanPublish, string? BlockingReason)> CanPublishAsync(
        Guid examId, CancellationToken ct = default)
    {
        // Check that every submitted attempt has all its manual evaluations
        // moderated and approved (no pending/in-progress evaluations remain).
        var attemptIds = await _db.ExamAttempts
            .Where(a => a.ExamId == examId && a.Status != AttemptStatus.InProgress)
            .Select(a => a.Id)
            .ToListAsync(ct);

        if (attemptIds.Count == 0)
            return (false, "No submitted attempts found for this exam.");

        var pendingEvaluations = await _db.Evaluations
            .Where(e => attemptIds.Contains(
                _db.ExamAttemptAnswers
                    .Where(a => a.Id == e.AttemptAnswerId)
                    .Select(a => a.AttemptId)
                    .FirstOrDefault())
                && (e.Status == EvaluationStatus.Pending || e.Status == EvaluationStatus.InProgress))
            .AnyAsync(ct);

        if (pendingEvaluations)
            return (false, "Some evaluations are still pending or in progress.");

        return (true, null);
    }

    public async Task<ResultPublishResult> PublishAsync(
        Guid examId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var actorUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(actorUserId))
            return new ResultPublishResult(false, "Actor identity not resolved.", 0);

        var attempts = await _db.ExamAttempts
            .Where(a => a.ExamId == examId
                        && a.Status != AttemptStatus.InProgress)
            .ToListAsync(ct);

        if (attempts.Count == 0)
            return new ResultPublishResult(false, "No submitted attempts found.", 0);

        var now = DateTime.UtcNow;
        int published = 0;

        foreach (var attempt in attempts)
        {
            var result = await _db.Results
                .FirstOrDefaultAsync(r => r.ExamAttemptId == attempt.Id, ct);

            if (result == null) continue;
            if (result.IsPublished) continue;

            // Compute final total from FinalPublishedScores for this attempt.
            var answerIds = await _db.ExamAttemptAnswers
                .Where(a => a.AttemptId == attempt.Id)
                .Select(a => a.Id)
                .ToListAsync(ct);

            var scores = await _db.FinalPublishedScores
                .Where(fps => answerIds.Contains(fps.ExamAttemptAnswerId))
                .ToListAsync(ct);

            var totalScore = scores.Sum(s => s.PointsAwarded);
            var maxScore = scores.Sum(s => s.MaxPoints);

            result.TotalScore = totalScore;
            result.MaxScore = maxScore;
            result.Percentage = maxScore > 0 ? (double)(totalScore / maxScore * 100) : 0;
            result.Grade = ComputeGrade(result.Percentage);
            result.State = ResultState.Published;
            result.IsPublished = true;
            result.PublishedAtUtc = now;
            result.PublishedByUserId = actorUserId;

            attempt.Status = AttemptStatus.Published;
            published++;
        }

        if (published == 0)
            return new ResultPublishResult(false, "No unpublished results to publish.", 0);

        await _db.SaveChangesAsync(ct);

        // Recompute ranks and leaderboard after publishing.
        await RecomputeRanksAsync(examId, ct);
        await _analytics.RecomputeLeaderboardAsync(examId, ct);

        await _audit.WriteAsync(
            "Result.Published",
            "Exam",
            examId.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new { ExamId = examId, Count = published }),
            cancellationToken: ct);

        _events.Enqueue(new ResultPublishedEvent
        {
            ExamId = examId,
            PublishedByUserId = actorUserId,
            StudentCount = published
        });

        return new ResultPublishResult(true, null, published);
    }

    public async Task<StudentResultView?> GetStudentResultAsync(
        Guid attemptId, string studentUserId, CancellationToken ct = default)
    {
        var result = await _db.Results
            .FirstOrDefaultAsync(r => r.ExamAttemptId == attemptId && r.IsPublished, ct);

        if (result is null) return null;

        var answerIds = await _db.ExamAttemptAnswers
            .Where(a => a.AttemptId == attemptId)
            .Select(a => a.Id)
            .ToListAsync(ct);

        var scores = await _db.FinalPublishedScores
            .Where(fps => answerIds.Contains(fps.ExamAttemptAnswerId))
            .ToListAsync(ct);

        var answerItems = scores.Select(s => new AnswerScoreItem(
            s.ExamAttemptAnswerId,
            s.PointsAwarded,
            s.MaxPoints,
            s.Source.ToString())).ToList();

        return new StudentResultView(
            result.ExamAttemptId,
            result.TotalScore,
            result.MaxScore,
            result.Percentage,
            result.Grade,
            result.RankInBatch,
            answerItems);
    }

    // ----- helpers ----------------------------------------------------------

    /// <summary>
    /// Assigns rank and percentile to all published results for an exam,
    /// ordered by TotalScore descending.
    /// </summary>
    private async Task RecomputeRanksAsync(Guid examId, CancellationToken ct)
    {
        var attemptIds = await _db.ExamAttempts
            .Where(a => a.ExamId == examId)
            .Select(a => a.Id)
            .ToListAsync(ct);

        var results = await _db.Results
            .Where(r => attemptIds.Contains(r.ExamAttemptId) && r.IsPublished)
            .OrderByDescending(r => r.TotalScore)
            .ToListAsync(ct);

        int total = results.Count;
        for (int i = 0; i < total; i++)
        {
            results[i].RankInBatch = i + 1;
            results[i].Percentile = total > 1
                ? Math.Round((double)(total - i - 1) / (total - 1) * 100, 2)
                : 100.0;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static string ComputeGrade(double pct) => pct switch
    {
        >= 90 => "A+",
        >= 80 => "A",
        >= 70 => "B+",
        >= 60 => "B",
        >= 50 => "C",
        >= 40 => "D",
        _ => "F"
    };
}
