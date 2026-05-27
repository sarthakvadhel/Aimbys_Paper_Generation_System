namespace Aimbys.Domain.Entities.Workflow;

/// <summary>
/// Per-instance SLA tracker. One row per (instance, state) combination
/// where the active state has a configured escalation rule. The
/// escalation job (<c>WorkflowEscalationService</c>) scans this table
/// hourly looking for due reminders and overdue escalations.
/// </summary>
public class WorkflowDeadline
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK to <see cref="WorkflowInstance.Id"/>.</summary>
    public Guid InstanceId { get; set; }

    /// <summary>State the deadline tracks.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Tenancy boundary mirrored from the instance.</summary>
    public Guid? InstituteId { get; set; }

    /// <summary>UTC instant the SLA window expires.</summary>
    public DateTime DueAtUtc { get; set; }

    /// <summary>UTC instant the reminder was sent (if any).</summary>
    public DateTime? ReminderSentAtUtc { get; set; }

    /// <summary>UTC instant the escalation was fired (if any).</summary>
    public DateTime? EscalatedAtUtc { get; set; }

    /// <summary>True once <c>UtcNow > DueAtUtc</c>; updated by the escalation job.</summary>
    public bool IsOverdue { get; set; }

    /// <summary>
    /// True once the underlying instance has transitioned out of the
    /// tracked state. Resolved deadlines are skipped by the job but kept
    /// for audit.
    /// </summary>
    public bool IsResolved { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
