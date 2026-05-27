using Aimbys.Application.Scheduling;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Exams;

/// <summary>
/// Recurring job handler that finds in-progress exam attempts past
/// their deadline and force-submits them (AutoSubmitted = true).
/// Runs every 5 minutes by default.
/// </summary>
public sealed class ExamAutoSubmitJobHandler : IScheduledJobHandler
{
    /// <summary>Stable key matched against <c>ScheduledJob.JobKey</c>.</summary>
    public const string Key = "exam.autosubmit";

    /// <summary>Default cron: every 5 minutes.</summary>
    public const string DefaultCron = "*/5 * * * *";

    public string JobKey => Key;

    private readonly AppDbContext _db;
    private readonly ILogger<ExamAutoSubmitJobHandler> _logger;

    public ExamAutoSubmitJobHandler(AppDbContext db, ILogger<ExamAutoSubmitJobHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(string? payload, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Find all in-progress attempts where the exam duration has expired
        var overdueAttempts = await _db.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.Answers)
            .Where(a => a.Status == AttemptStatus.InProgress
                     && a.StartedAtUtc != null
                     && a.Exam != null)
            .ToListAsync(cancellationToken);

        var autoSubmitCount = 0;

        foreach (var attempt in overdueAttempts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (attempt.Exam == null || !attempt.StartedAtUtc.HasValue)
                continue;

            var deadline = attempt.StartedAtUtc.Value.AddMinutes(attempt.Exam.DurationMinutes);
            if (now <= deadline)
                continue;

            attempt.Status = AttemptStatus.Submitted;
            attempt.SubmittedAtUtc = now;
            attempt.AutoSubmitted = true;

            // Simple auto-score sum
            decimal totalAutoScore = 0;
            foreach (var ans in attempt.Answers)
            {
                totalAutoScore += ans.AutoMarksAwarded ?? 0;
            }
            attempt.TotalAutoScore = totalAutoScore;

            autoSubmitCount++;
        }

        if (autoSubmitCount > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Auto-submitted {Count} overdue exam attempts.", autoSubmitCount);
        }
    }
}
