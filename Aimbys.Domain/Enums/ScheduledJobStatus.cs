namespace Aimbys.Domain.Enums;

/// <summary>
/// Lifecycle of a <see cref="Aimbys.Domain.Entities.Scheduling.ScheduledJob"/>
/// row as it moves through the
/// <c>SchedulingHostedService</c> dispatcher.
///
/// <list type="bullet">
///   <item><see cref="Pending"/> &mdash; queued; will be picked up at or after <c>NextRunAtUtc</c>.</item>
///   <item><see cref="Running"/> &mdash; the dispatcher has claimed the job and is executing it.</item>
///   <item><see cref="Succeeded"/> &mdash; one-shot job that completed cleanly. Recurring jobs flip back to Pending after success.</item>
///   <item><see cref="Failed"/> &mdash; final failure (out of retries).</item>
///   <item><see cref="Cancelled"/> &mdash; explicitly cancelled via <c>ISchedulingService.CancelAsync</c>.</item>
/// </list>
/// </summary>
public enum ScheduledJobStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4
}
