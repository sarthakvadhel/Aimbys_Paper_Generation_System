using Aimbys.Domain.Entities.Scheduling;

namespace Aimbys.Application.Scheduling;

/// <summary>
/// Single sanctioned route for queueing deferred work. The
/// implementation persists each request as a
/// <see cref="ScheduledJob"/> row and a hosted background service
/// dispatches due rows on a 60-second cadence.
///
/// <para>
/// The interface is deliberately Hangfire-shaped (<c>Once</c> /
/// <c>Recurring</c> / <c>Cancel</c>) so a future migration to Hangfire
/// or another server-backed scheduler is mechanical.
/// </para>
/// </summary>
public interface ISchedulingService
{
    /// <summary>
    /// Queues a one-shot job to run at or after
    /// <paramref name="executeAtUtc"/>. Returns the persisted row id.
    /// </summary>
    Task<Guid> ScheduleOnceAsync(
        string jobKey,
        DateTime executeAtUtc,
        string? payload = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts a recurring job by <paramref name="jobKey"/>. Subsequent
    /// calls with the same key replace the cron expression / payload
    /// rather than creating a duplicate row.
    /// </summary>
    Task<Guid> ScheduleRecurringAsync(
        string jobKey,
        string cronExpression,
        string? payload = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels every <see cref="ScheduledJobStatus.Pending"/> job with
    /// the given key. Returns the count of rows transitioned to
    /// <see cref="ScheduledJobStatus.Cancelled"/>.
    /// </summary>
    Task<int> CancelAsync(string jobKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns pending (or running) jobs matching
    /// <paramref name="jobKey"/>. Used by ops dashboards and tests.
    /// </summary>
    Task<IReadOnlyList<ScheduledJob>> GetPendingAsync(
        string jobKey,
        CancellationToken cancellationToken = default);
}
