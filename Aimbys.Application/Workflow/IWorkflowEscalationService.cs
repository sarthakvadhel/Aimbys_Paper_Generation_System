namespace Aimbys.Application.Workflow;

/// <summary>
/// Background-job surface that scans <c>WorkflowDeadline</c> rows for
/// reminders and escalations. Invoked by <c>ISchedulingService</c> on a
/// cadence (target: hourly).
///
/// The split between
/// <see cref="CheckDeadlinesAsync"/> (the orchestrator) and the
/// per-deadline methods (<see cref="SendReminderAsync"/>,
/// <see cref="EscalateAsync"/>) lets test code drive a single deadline
/// deterministically without the orchestrator's "find candidates" query.
/// </summary>
public interface IWorkflowEscalationService
{
    /// <summary>
    /// Sweeps active deadlines: marks overdue rows, sends reminders for
    /// rows past <c>ReminderAtPercent</c> of their SLA window, and
    /// escalates rows whose deadline has passed.
    /// Returns a summary count <c>(remindersSent, escalationsSent)</c>.
    /// </summary>
    Task<(int RemindersSent, int EscalationsSent)> CheckDeadlinesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Sends a reminder for the given deadline (idempotent).</summary>
    Task SendReminderAsync(Guid deadlineId, CancellationToken cancellationToken = default);

    /// <summary>Sends an escalation for the given deadline (idempotent).</summary>
    Task EscalateAsync(Guid deadlineId, CancellationToken cancellationToken = default);
}
