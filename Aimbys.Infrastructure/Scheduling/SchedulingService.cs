using Aimbys.Application.Scheduling;
using Aimbys.Domain.Entities.Scheduling;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Scheduling;

/// <summary>
/// Default <see cref="ISchedulingService"/>. Persists each schedule
/// request as a <see cref="ScheduledJob"/> row; the hosted dispatcher
/// (<see cref="SchedulingHostedService"/>) picks them up on its
/// 60-second tick.
///
/// <para>
/// Recurring jobs are upserted by <c>JobKey</c> so re-registering at
/// startup is idempotent. Cancellation transitions every still-pending
/// row to <see cref="ScheduledJobStatus.Cancelled"/> rather than
/// deleting them &mdash; the audit trail of "we tried to schedule X
/// then cancelled it" is more useful than a vanished row.
/// </para>
/// </summary>
public sealed class SchedulingService : ISchedulingService
{
    private readonly AppDbContext _db;

    public SchedulingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> ScheduleOnceAsync(
        string jobKey,
        DateTime executeAtUtc,
        string? payload = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobKey))
            throw new ArgumentException("jobKey is required.", nameof(jobKey));

        var job = new ScheduledJob
        {
            JobKey = jobKey,
            CronExpression = null,
            NextRunAtUtc = DateTime.SpecifyKind(executeAtUtc, DateTimeKind.Utc),
            Payload = payload,
            Status = ScheduledJobStatus.Pending
        };

        _db.Set<ScheduledJob>().Add(job);
        await _db.SaveChangesAsync(cancellationToken);
        return job.Id;
    }

    public async Task<Guid> ScheduleRecurringAsync(
        string jobKey,
        string cronExpression,
        string? payload = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobKey))
            throw new ArgumentException("jobKey is required.", nameof(jobKey));

        // Validate at registration time so a malformed expression
        // doesn't lurk until the first dispatch attempt.
        var cron = CronExpression.Parse(cronExpression);
        var nextRun = cron.GetNextOccurrence(DateTime.UtcNow);

        var existing = await _db.Set<ScheduledJob>()
            .FirstOrDefaultAsync(
                j => j.JobKey == jobKey
                     && j.CronExpression != null
                     && j.Status != ScheduledJobStatus.Cancelled,
                cancellationToken);

        if (existing is not null)
        {
            existing.CronExpression = cronExpression;
            existing.Payload = payload;
            existing.NextRunAtUtc = nextRun;
            existing.Status = ScheduledJobStatus.Pending;
            existing.RetryCount = 0;
            existing.LastError = null;
            await _db.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var job = new ScheduledJob
        {
            JobKey = jobKey,
            CronExpression = cronExpression,
            NextRunAtUtc = nextRun,
            Payload = payload,
            Status = ScheduledJobStatus.Pending
        };

        _db.Set<ScheduledJob>().Add(job);
        await _db.SaveChangesAsync(cancellationToken);
        return job.Id;
    }

    public async Task<int> CancelAsync(string jobKey, CancellationToken cancellationToken = default)
    {
        var pending = await _db.Set<ScheduledJob>()
            .Where(j => j.JobKey == jobKey
                        && (j.Status == ScheduledJobStatus.Pending
                            || j.Status == ScheduledJobStatus.Running))
            .ToListAsync(cancellationToken);

        foreach (var job in pending)
        {
            job.Status = ScheduledJobStatus.Cancelled;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return pending.Count;
    }

    public async Task<IReadOnlyList<ScheduledJob>> GetPendingAsync(
        string jobKey,
        CancellationToken cancellationToken = default)
    {
        return await _db.Set<ScheduledJob>()
            .AsNoTracking()
            .Where(j => j.JobKey == jobKey
                        && (j.Status == ScheduledJobStatus.Pending
                            || j.Status == ScheduledJobStatus.Running))
            .OrderBy(j => j.NextRunAtUtc)
            .ToListAsync(cancellationToken);
    }
}
