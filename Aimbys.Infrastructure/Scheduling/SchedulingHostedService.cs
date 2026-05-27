using Aimbys.Application.Scheduling;
using Aimbys.Domain.Entities.Scheduling;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Scheduling;

/// <summary>
/// Background dispatcher for <see cref="ScheduledJob"/> rows. Uses a
/// <see cref="PeriodicTimer"/> at a 60-second cadence; on each tick:
///
/// <list type="number">
///   <item>Loads up to N due jobs (status = Pending, NextRunAtUtc &lt;= now).</item>
///   <item>For each: marks Running, resolves an
///         <see cref="IScheduledJobHandler"/> by JobKey, executes it.</item>
///   <item>On success: one-shots flip to Succeeded; recurring jobs
///         compute the next occurrence and flip back to Pending.</item>
///   <item>On failure: increments RetryCount; if &lt; MaxRetries, the
///         job is re-scheduled with exponential backoff
///         (5 min * 2^retryCount); otherwise parked in Failed.</item>
/// </list>
///
/// <para>
/// Every iteration runs in its own DI scope so the
/// <see cref="AppDbContext"/> lifetime tracks the single batch.
/// </para>
/// </summary>
public sealed class SchedulingHostedService : BackgroundService
{
    /// <summary>Polling cadence; tuned for 1-minute precision.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Upper bound on jobs claimed in a single tick. Prevents one
    /// pathologically-slow handler from starving the rest of the queue.
    /// </summary>
    public const int MaxBatchSize = 50;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SchedulingHostedService> _logger;

    public SchedulingHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SchedulingHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "SchedulingHostedService starting; polling every {Seconds}s.",
            PollInterval.TotalSeconds);

        using var timer = new PeriodicTimer(PollInterval);

        // Run one immediate sweep so retention/escalation jobs can fire
        // promptly after a restart, then settle into the periodic cadence.
        try
        {
            await TickAsync(stoppingToken);
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initial scheduling sweep failed; will retry on next tick.");
        }

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await TickAsync(stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    // Don't let a tick failure tear down the whole host.
                    _logger.LogError(ex, "Scheduling sweep failed; will retry on next tick.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path.
        }

        _logger.LogInformation("SchedulingHostedService stopping.");
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var handlers = scope.ServiceProvider
            .GetServices<IScheduledJobHandler>()
            .ToDictionary(h => h.JobKey, StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var due = await db.Set<ScheduledJob>()
            .Where(j => j.Status == ScheduledJobStatus.Pending && j.NextRunAtUtc <= now)
            .OrderBy(j => j.NextRunAtUtc)
            .Take(MaxBatchSize)
            .ToListAsync(cancellationToken);

        if (due.Count == 0) return;

        foreach (var job in due)
        {
            await ExecuteJobAsync(db, handlers, job, cancellationToken);
        }
    }

    private async Task ExecuteJobAsync(
        AppDbContext db,
        Dictionary<string, IScheduledJobHandler> handlers,
        ScheduledJob job,
        CancellationToken cancellationToken)
    {
        if (!handlers.TryGetValue(job.JobKey, out var handler))
        {
            _logger.LogWarning(
                "No IScheduledJobHandler registered for jobKey '{JobKey}'; skipping job {JobId}.",
                job.JobKey, job.Id);

            // Park the row in Failed so it doesn't loop forever; ops can
            // re-enable by re-registering the handler and resetting status.
            job.Status = ScheduledJobStatus.Failed;
            job.LastError = $"No handler registered for '{job.JobKey}'.";
            job.LastRunAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        // Mark Running so a parallel host instance can't claim the same row.
        job.Status = ScheduledJobStatus.Running;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await handler.ExecuteAsync(job.Payload, cancellationToken);

            job.LastRunAtUtc = DateTime.UtcNow;
            job.LastError = null;
            job.RetryCount = 0;

            if (string.IsNullOrEmpty(job.CronExpression))
            {
                // One-shot: terminal success.
                job.Status = ScheduledJobStatus.Succeeded;
            }
            else
            {
                // Recurring: schedule the next occurrence.
                var cron = CronExpression.Parse(job.CronExpression);
                job.NextRunAtUtc = cron.GetNextOccurrence(DateTime.UtcNow);
                job.Status = ScheduledJobStatus.Pending;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Scheduled job {JobKey} ({JobId}) failed (attempt {Attempt}/{MaxAttempts}).",
                job.JobKey, job.Id, job.RetryCount + 1, job.MaxRetries);

            job.LastRunAtUtc = DateTime.UtcNow;
            job.LastError = Truncate(ex.Message, 500);
            job.RetryCount++;

            if (job.RetryCount >= job.MaxRetries)
            {
                job.Status = ScheduledJobStatus.Failed;
            }
            else
            {
                // Exponential backoff: 5 * 2^n minutes.
                var backoffMinutes = 5.0 * Math.Pow(2, job.RetryCount);
                job.NextRunAtUtc = DateTime.UtcNow.AddMinutes(backoffMinutes);
                job.Status = ScheduledJobStatus.Pending;
            }
        }
        finally
        {
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx,
                    "Failed to persist post-run state for job {JobKey} ({JobId}).",
                    job.JobKey, job.Id);
            }
        }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max];
}
