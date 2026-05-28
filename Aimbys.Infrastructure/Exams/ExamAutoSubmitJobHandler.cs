using Aimbys.Application.Exams;
using Aimbys.Application.Scheduling;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Exams;

/// <summary>
/// Recurring job that finds in-progress exam attempts past their
/// deadline and force-submits them through
/// <see cref="IExamRuntimeService.AutoSubmitAsync"/> &mdash; not by
/// touching the entity directly. That delegation guarantees the
/// auto-submitted attempts are auto-evaluated, persisted in the
/// <c>FinalPublishedScores</c> table, and (for MCQ-only papers)
/// auto-published with student notifications + leaderboard updates,
/// exactly the same way as a manually-submitted attempt.
/// </summary>
public sealed class ExamAutoSubmitJobHandler : IScheduledJobHandler
{
    /// <summary>Stable key matched against <c>ScheduledJob.JobKey</c>.</summary>
    public const string Key = "exam.autosubmit";

    /// <summary>Default cron: every 5 minutes.</summary>
    public const string DefaultCron = "*/5 * * * *";

    public string JobKey => Key;

    private readonly AppDbContext _db;
    private readonly IExamRuntimeService _runtime;
    private readonly ILogger<ExamAutoSubmitJobHandler> _logger;

    public ExamAutoSubmitJobHandler(
        AppDbContext db,
        IExamRuntimeService runtime,
        ILogger<ExamAutoSubmitJobHandler> logger)
    {
        _db = db;
        _runtime = runtime;
        _logger = logger;
    }

    public async Task ExecuteAsync(string? payload, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Pull just the ids + deadline-relevant columns; the runtime
        // service re-reads each attempt with the includes it needs.
        var candidates = await _db.ExamAttempts
            .AsNoTracking()
            .Where(a => a.Status == AttemptStatus.InProgress
                     && a.StartedAtUtc != null)
            .Select(a => new
            {
                a.Id,
                a.StartedAtUtc,
                ExamDuration = a.Exam!.DurationMinutes
            })
            .ToListAsync(cancellationToken);

        var autoSubmittedCount = 0;
        var failureCount = 0;

        foreach (var c in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!c.StartedAtUtc.HasValue) continue;
            var deadline = c.StartedAtUtc.Value.AddMinutes(c.ExamDuration);
            if (now <= deadline) continue;

            // Per-attempt try/catch so one bad attempt doesn't kill the
            // whole job tick.
            try
            {
                var result = await _runtime.AutoSubmitAsync(c.Id, cancellationToken);
                if (result.Success)
                {
                    autoSubmittedCount++;
                }
                else
                {
                    failureCount++;
                    _logger.LogWarning(
                        "Auto-submit failed for attempt {AttemptId}: {Error}",
                        c.Id, result.Error);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failureCount++;
                _logger.LogError(ex,
                    "Auto-submit threw for attempt {AttemptId}; continuing.", c.Id);
            }
        }

        if (autoSubmittedCount > 0 || failureCount > 0)
        {
            _logger.LogInformation(
                "ExamAutoSubmit: submitted={Submitted} failed={Failed} considered={Considered}",
                autoSubmittedCount, failureCount, candidates.Count);
        }
    }
}
