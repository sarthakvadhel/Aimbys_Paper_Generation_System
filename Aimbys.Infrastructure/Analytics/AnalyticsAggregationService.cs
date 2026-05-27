using System.Text.Json;
using Aimbys.Application.Analytics;
using Aimbys.Domain.Entities.Analytics;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Analytics;

/// <summary>
/// Computes leaderboard rankings and analytics snapshots from published
/// Result rows. Called after auto-evaluation completes or after moderation
/// approves the final score.
/// </summary>
public sealed class AnalyticsAggregationService : IAnalyticsAggregationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AnalyticsAggregationService> _logger;

    public AnalyticsAggregationService(AppDbContext db, ILogger<AnalyticsAggregationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task AggregateInstituteMetricsAsync(Guid instituteId, CancellationToken ct = default)
    {
        var examIds = await _db.Exams
            .Where(e => e.InstituteId == instituteId)
            .Select(e => e.Id)
            .ToListAsync(ct);

        var attemptIds = await _db.ExamAttempts
            .Where(a => examIds.Contains(a.ExamId))
            .Select(a => a.Id)
            .ToListAsync(ct);

        var publishedResults = await _db.Results
            .Where(r => attemptIds.Contains(r.ExamAttemptId) && r.IsPublished)
            .ToListAsync(ct);

        var metrics = new
        {
            TotalExams = examIds.Count,
            TotalAttempts = attemptIds.Count,
            PublishedResults = publishedResults.Count,
            AverageScore = publishedResults.Count > 0
                ? publishedResults.Average(r => r.TotalScore)
                : 0m,
            AveragePercentage = publishedResults.Count > 0
                ? publishedResults.Average(r => r.Percentage)
                : 0.0
        };

        _db.AnalyticsSnapshots.Add(new AnalyticsSnapshot
        {
            Scope = AnalyticsScope.Institute,
            ScopeId = instituteId,
            MetricKey = "institute.metrics",
            MetricValueJson = JsonSerializer.Serialize(metrics),
            CapturedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Institute metrics aggregated for {InstituteId}: {Exams} exams, {Results} published results.",
            instituteId, examIds.Count, publishedResults.Count);
    }

    public async Task AggregateStudentPerformanceAsync(Guid instituteId, CancellationToken ct = default)
    {
        var studentProfiles = await _db.StudentProfiles
            .Where(sp => sp.InstituteId == instituteId)
            .Select(sp => new { sp.Id, sp.ClassBatchId })
            .ToListAsync(ct);

        foreach (var student in studentProfiles)
        {
            var attempts = await _db.ExamAttempts
                .Where(a => a.StudentProfileId == student.Id)
                .Select(a => a.Id)
                .ToListAsync(ct);

            if (attempts.Count == 0) continue;

            var results = await _db.Results
                .Where(r => attempts.Contains(r.ExamAttemptId) && r.IsPublished)
                .ToListAsync(ct);

            if (results.Count == 0) continue;

            var performance = new
            {
                TotalAttempts = attempts.Count,
                PublishedResults = results.Count,
                AverageScore = results.Average(r => r.TotalScore),
                AveragePercentage = results.Average(r => r.Percentage),
                BestPercentage = results.Max(r => r.Percentage),
                WorstPercentage = results.Min(r => r.Percentage)
            };

            _db.AnalyticsSnapshots.Add(new AnalyticsSnapshot
            {
                Scope = AnalyticsScope.Student,
                ScopeId = student.Id,
                MetricKey = "student.performance",
                MetricValueJson = JsonSerializer.Serialize(performance),
                CapturedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Student performance aggregated for institute {InstituteId}: {Count} students processed.",
            instituteId, studentProfiles.Count);
    }

    public async Task AggregateEvaluatorEfficiencyAsync(Guid instituteId, CancellationToken ct = default)
    {
        var evaluators = await _db.TeacherProfiles
            .Where(t => t.InstituteId == instituteId && t.CanEvaluate)
            .Select(t => new { t.Id, t.UserId })
            .ToListAsync(ct);

        foreach (var evaluator in evaluators)
        {
            var evaluations = await _db.Evaluations
                .Where(e => e.EvaluatorTeacherProfileId == evaluator.Id)
                .ToListAsync(ct);

            if (evaluations.Count == 0) continue;

            var completed = evaluations.Where(e => e.Status == EvaluationStatus.Submitted).ToList();
            var avgTurnaround = completed
                .Where(e => e.CompletedAtUtc.HasValue)
                .Select(e => (e.CompletedAtUtc!.Value - e.AssignedAtUtc).TotalHours)
                .DefaultIfEmpty(0)
                .Average();

            var efficiency = new
            {
                TotalAssigned = evaluations.Count,
                Completed = completed.Count,
                Pending = evaluations.Count(e => e.Status == EvaluationStatus.Pending),
                InProgress = evaluations.Count(e => e.Status == EvaluationStatus.InProgress),
                AverageTurnaroundHours = Math.Round(avgTurnaround, 2)
            };

            _db.AnalyticsSnapshots.Add(new AnalyticsSnapshot
            {
                Scope = AnalyticsScope.Institute,
                ScopeId = evaluator.Id,
                MetricKey = "evaluator.efficiency",
                MetricValueJson = JsonSerializer.Serialize(efficiency),
                CapturedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Evaluator efficiency aggregated for institute {InstituteId}: {Count} evaluators processed.",
            instituteId, evaluators.Count);
    }

    public async Task RecomputeLeaderboardAsync(Guid examId, CancellationToken ct = default)
    {
        var exam = await _db.Exams
            .FirstOrDefaultAsync(e => e.Id == examId, ct);

        if (exam == null) return;

        var attemptIds = await _db.ExamAttempts
            .Where(a => a.ExamId == examId)
            .Select(a => new { a.Id, a.StudentProfileId })
            .ToListAsync(ct);

        if (attemptIds.Count == 0) return;

        var attemptIdList = attemptIds.Select(a => a.Id).ToList();
        var results = await _db.Results
            .Where(r => attemptIdList.Contains(r.ExamAttemptId) && r.IsPublished)
            .OrderByDescending(r => r.TotalScore)
            .ToListAsync(ct);

        if (results.Count == 0) return;

        // Remove stale leaderboard entries for this exam.
        var stale = await _db.Set<CachedLeaderboardEntry>()
            .Where(e => e.ExamId == examId)
            .ToListAsync(ct);
        _db.Set<CachedLeaderboardEntry>().RemoveRange(stale);

        var attemptToStudent = attemptIds.ToDictionary(a => a.Id, a => a.StudentProfileId);
        int total = results.Count;

        for (int i = 0; i < total; i++)
        {
            var result = results[i];
            if (!attemptToStudent.TryGetValue(result.ExamAttemptId, out var studentProfileId))
                continue;

            var percentile = total > 1
                ? Math.Round((double)(total - i - 1) / (total - 1) * 100, 2)
                : 100.0;

            _db.Set<CachedLeaderboardEntry>().Add(new CachedLeaderboardEntry
            {
                ExamId = examId,
                ClassBatchId = exam.ClassBatchId,
                StudentProfileId = studentProfileId,
                Rank = i + 1,
                Percentile = percentile,
                TotalScore = result.TotalScore,
                ComputedAtUtc = DateTime.UtcNow
            });
        }

        _db.AnalyticsSnapshots.Add(new AnalyticsSnapshot
        {
            Scope = AnalyticsScope.Exam,
            ScopeId = examId,
            MetricKey = "leaderboard.recomputed",
            MetricValueJson = JsonSerializer.Serialize(new { ExamId = examId, Entries = total }),
            CapturedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Leaderboard recomputed for exam {ExamId}: {Count} entries.", examId, total);
    }
}
