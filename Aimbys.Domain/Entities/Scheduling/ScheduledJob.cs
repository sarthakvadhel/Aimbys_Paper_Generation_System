using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Scheduling;

/// <summary>
/// One unit of deferred work tracked by
/// <c>SchedulingHostedService</c>. The combination
/// (<see cref="JobKey"/>, <see cref="CronExpression"/>) acts as the
/// natural key &mdash; "schedule recurring" upserts on this pair so
/// repeated registrations don't multiply rows.
///
/// <para>
/// One-shot jobs carry <see cref="CronExpression"/> = <c>null</c> and
/// are removed (or flipped to <see cref="ScheduledJobStatus.Succeeded"/>)
/// after a successful run. Recurring jobs reset
/// <see cref="NextRunAtUtc"/> from the cron expression and flip back to
/// <see cref="ScheduledJobStatus.Pending"/>.
/// </para>
/// </summary>
public class ScheduledJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Stable, human-readable key (e.g. <c>"workflow.escalation"</c>,
    /// <c>"retention.enforce"</c>). Must match an
    /// <c>IScheduledJobHandler.JobKey</c> registered in DI; jobs
    /// without a handler are logged and skipped at dispatch time.
    /// </summary>
    public string JobKey { get; set; } = string.Empty;

    /// <summary>
    /// Standard 5-field cron expression
    /// (<c>minute hour day-of-month month day-of-week</c>) for
    /// recurring jobs; null for one-shot jobs.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// UTC instant the job is due. The dispatcher polls jobs where
    /// <see cref="Status"/> = <c>Pending</c> and
    /// <see cref="NextRunAtUtc"/> &lt;= <c>UtcNow</c>.
    /// </summary>
    public DateTime NextRunAtUtc { get; set; }

    /// <summary>UTC instant the most recent run finished (success or failure).</summary>
    public DateTime? LastRunAtUtc { get; set; }

    /// <summary>
    /// Opaque payload passed to the handler. By convention a JSON object
    /// describing the work; handlers parse it themselves so the
    /// scheduling subsystem stays type-agnostic.
    /// </summary>
    public string? Payload { get; set; }

    public ScheduledJobStatus Status { get; set; } = ScheduledJobStatus.Pending;

    /// <summary>Number of consecutive failed attempts on this job.</summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Maximum allowed consecutive failures before the dispatcher gives
    /// up and parks the job in <see cref="ScheduledJobStatus.Failed"/>.
    /// Defaults to 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Last error message recorded. Surfaced via the (future) admin
    /// dashboard; kept short so the row stays compact.
    /// </summary>
    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
