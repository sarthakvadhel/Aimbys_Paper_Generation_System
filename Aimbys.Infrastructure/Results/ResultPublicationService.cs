using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Analytics;
using Aimbys.Application.Audit;
using Aimbys.Application.Results;
using Aimbys.Domain.Entities.Exams;
using Aimbys.Domain.Entities.Results;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Results;

/// <summary>
/// Reference implementation of <see cref="IResultPublicationService"/>.
/// Both publish entry points share a private <c>MarkAttemptPublished</c>
/// helper so the data mutations stay identical regardless of who triggers
/// the publish (system / institute admin).
/// </summary>
public class ResultPublicationService : IResultPublicationService
{
    /// <summary>Sentinel actor id used by background / runtime auto-publish.</summary>
    public const string SystemActorId = "system";

    private readonly AppDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly DomainEventCollector _events;
    private readonly IAnalyticsAggregationService _analytics;
    private readonly IResultArchiveService _archive;
    private readonly ILogger<ResultPublicationService> _logger;

    public ResultPublicationService(
        AppDbContext db,
        IAuditWriter audit,
        DomainEventCollector events,
        IAnalyticsAggregationService analytics,
        IResultArchiveService archive,
        ILogger<ResultPublicationService> logger)
    {
        _db = db;
        _audit = audit;
        _events = events;
        _analytics = analytics;
        _archive = archive;
        _logger = logger;
    }

    // =========================================================================
    // CanPublish
    // =========================================================================

    public async Task<(bool CanPublish, string? BlockingReason)> CanPublishAsync(
        Guid examId, CancellationToken ct = default)
    {
        var attemptIds = await _db.ExamAttempts
            .AsNoTracking()
            .Where(a => a.ExamId == examId && a.Status != AttemptStatus.InProgress)
            .Select(a => a.Id)
            .ToListAsync(ct);

        if (attemptIds.Count == 0)
            return (false, "No submitted attempts found for this exam.");

        // Check for any evaluation that is still in flight on these attempts.
        // We pre-resolve answer ids so the EF query stays a clean IN-list.
        var attemptAnswerIds = await _db.ExamAttemptAnswers
            .AsNoTracking()
            .Where(a => attemptIds.Contains(a.AttemptId))
            .Select(a => a.Id)
            .ToListAsync(ct);

        var pendingEvaluations = await _db.Evaluations
            .AsNoTracking()
            .AnyAsync(e => attemptAnswerIds.Contains(e.AttemptAnswerId)
                       && (e.Status == EvaluationStatus.Pending
                        || e.Status == EvaluationStatus.InProgress), ct);

        if (pendingEvaluations)
            return (false, "Some evaluations are still pending or in progress.");

        return (true, null);
    }

    // =========================================================================
    // Per-attempt publish (auto-only path)
    // =========================================================================

    public async Task<ResultPublishResult> PublishAttemptAsync(
        Guid attemptId, string actorUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(actorUserId))
            return new ResultPublishResult(false, "Actor identity not resolved.", 0);

        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

        if (attempt is null || attempt.Exam is null)
            return new ResultPublishResult(false, "Attempt not found.", 0);

        var result = await _db.Results
            .FirstOrDefaultAsync(r => r.ExamAttemptId == attempt.Id, ct);

        if (result is null)
            return new ResultPublishResult(false, "Result row not found for attempt.", 0);

        if (result.IsPublished)
            return new ResultPublishResult(true, null, 0);

        await MarkAttemptPublishedAsync(attempt, result, actorUserId, ct);
        await _db.SaveChangesAsync(ct);

        // Per-attempt recompute keeps ranks honest as more students publish.
        await RecomputeRanksAsync(attempt.ExamId, ct);
        await _analytics.RecomputeLeaderboardAsync(attempt.ExamId, ct);

        await _audit.WriteAsync(
            "Result.Published",
            "ExamAttempt",
            attempt.Id.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new
            {
                attemptId = attempt.Id,
                examId = attempt.ExamId,
                examTitle = attempt.Exam.Title
            }),
            cancellationToken: ct);

        _events.Enqueue(new ResultPublishedEvent
        {
            ExamId = attempt.ExamId,
            ExamTitle = attempt.Exam.Title,
            PublishedByUserId = actorUserId,
            StudentCount = 1,
            InstituteId = attempt.Exam.InstituteId,
            RecipientUserIds = string.IsNullOrEmpty(attempt.StudentUserId)
                ? Array.Empty<string>()
                : new[] { attempt.StudentUserId },
            AttemptId = attempt.Id
        });

        return new ResultPublishResult(true, null, 1);
    }

    // =========================================================================
    // Batch publish (institute admin path)
    // =========================================================================

    public async Task<ResultPublishResult> PublishAsync(
        Guid examId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var actorUserId = actor.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(actorUserId))
            return new ResultPublishResult(false, "Actor identity not resolved.", 0);

        var (canPublish, blockingReason) = await CanPublishAsync(examId, ct);
        if (!canPublish)
            return new ResultPublishResult(false, blockingReason, 0);

        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId, ct);
        if (exam is null)
            return new ResultPublishResult(false, "Exam not found.", 0);

        var attempts = await _db.ExamAttempts
            .Where(a => a.ExamId == examId && a.Status != AttemptStatus.InProgress)
            .ToListAsync(ct);

        if (attempts.Count == 0)
            return new ResultPublishResult(false, "No submitted attempts found.", 0);

        var attemptIds = attempts.Select(a => a.Id).ToList();
        var resultsByAttempt = await _db.Results
            .Where(r => attemptIds.Contains(r.ExamAttemptId))
            .ToDictionaryAsync(r => r.ExamAttemptId, ct);

        var recipients = new List<string>();
        var publishedCount = 0;

        foreach (var attempt in attempts)
        {
            if (!resultsByAttempt.TryGetValue(attempt.Id, out var result))
                continue;
            if (result.IsPublished)
                continue;

            await MarkAttemptPublishedAsync(attempt, result, actorUserId, ct);
            publishedCount++;

            if (!string.IsNullOrEmpty(attempt.StudentUserId))
                recipients.Add(attempt.StudentUserId);
        }

        if (publishedCount == 0)
            return new ResultPublishResult(false, "No unpublished results to publish.", 0);

        await _db.SaveChangesAsync(ct);

        await RecomputeRanksAsync(examId, ct);
        await _analytics.RecomputeLeaderboardAsync(examId, ct);

        // Snapshot last so the archive captures the final ranks/percentiles.
        var archiveResult = await _archive.CreateSnapshotAsync(examId, actorUserId, ct);
        if (!archiveResult.Success)
        {
            _logger.LogWarning(
                "Result publish succeeded but archive snapshot failed for Exam {ExamId}: {Error}",
                examId, archiveResult.Error);
        }

        await _audit.WriteAsync(
            "Result.Published",
            "Exam",
            examId.ToString(),
            actorUserId,
            JsonSerializer.Serialize(new
            {
                examId,
                examTitle = exam.Title,
                count = publishedCount,
                archiveId = archiveResult.ArchiveId
            }),
            cancellationToken: ct);

        _events.Enqueue(new ResultPublishedEvent
        {
            ExamId = examId,
            ExamTitle = exam.Title,
            PublishedByUserId = actorUserId,
            StudentCount = publishedCount,
            InstituteId = exam.InstituteId,
            RecipientUserIds = recipients
        });

        return new ResultPublishResult(true, null, publishedCount);
    }

    // =========================================================================
    // Student read
    // =========================================================================

    public async Task<StudentResultView?> GetStudentResultAsync(
        Guid attemptId, string studentUserId, CancellationToken ct = default)
    {
        // Tenancy: a student can only read their own attempt's published result.
        var attempt = await _db.ExamAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentUserId == studentUserId, ct);

        if (attempt is null) return null;

        var result = await _db.Results
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ExamAttemptId == attemptId && r.IsPublished, ct);

        if (result is null) return null;

        var answerIds = await _db.ExamAttemptAnswers
            .AsNoTracking()
            .Where(a => a.AttemptId == attemptId)
            .Select(a => a.Id)
            .ToListAsync(ct);

        var scores = await _db.FinalPublishedScores
            .AsNoTracking()
            .Where(fps => answerIds.Contains(fps.ExamAttemptAnswerId))
            .ToListAsync(ct);

        var answerItems = scores
            .Select(s => new AnswerScoreItem(
                s.ExamAttemptAnswerId,
                s.PointsAwarded,
                s.MaxPoints,
                s.Source.ToString()))
            .ToList();

        return new StudentResultView(
            result.ExamAttemptId,
            result.TotalScore,
            result.MaxScore,
            result.Percentage,
            result.Grade,
            result.RankInBatch,
            answerItems);
    }

    // =========================================================================
    // Shared mutation
    // =========================================================================

    /// <summary>
    /// Pure data mutation. Sets totals/percentages/grade/published flags on
    /// the attempt + result. Does NOT save, audit, fire events, or
    /// recompute ranks &mdash; the caller composes those.
    /// </summary>
    private async Task MarkAttemptPublishedAsync(
        ExamAttempt attempt, Result result, string actorUserId, CancellationToken ct)
    {
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
        result.PublishedAtUtc = DateTime.UtcNow;
        result.PublishedByUserId = actorUserId;

        attempt.Status = AttemptStatus.Published;
    }

    // =========================================================================
    // Helpers
    // =========================================================================

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

        var total = results.Count;
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
