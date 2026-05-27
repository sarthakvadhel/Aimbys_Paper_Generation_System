namespace Aimbys.Application.Scheduling;

/// <summary>
/// Handler for one specific job kind. The scheduling host resolves
/// implementations from DI and dispatches by
/// <see cref="JobKey"/>.
///
/// <para>
/// Implementations should be idempotent &mdash; a recurring sweep that
/// races with a one-shot reminder must not double-act when both fire
/// against the same row. Use the row's "already-handled" stamps
/// (e.g. <c>WorkflowDeadline.ReminderSentAtUtc</c>) as the gate.
/// </para>
/// </summary>
public interface IScheduledJobHandler
{
    /// <summary>
    /// Stable key matching <c>ScheduledJob.JobKey</c>. Lower-case
    /// dot-separated by convention (e.g. <c>"retention.enforce"</c>).
    /// </summary>
    string JobKey { get; }

    /// <summary>
    /// Executes the job. Returning normally marks the run successful;
    /// throwing causes the dispatcher to record the error and
    /// re-schedule subject to <c>MaxRetries</c>.
    /// </summary>
    /// <param name="payload">
    /// Opaque JSON payload from <c>ScheduledJob.Payload</c>; null for
    /// jobs that take no input.
    /// </param>
    Task ExecuteAsync(string? payload, CancellationToken cancellationToken);
}
