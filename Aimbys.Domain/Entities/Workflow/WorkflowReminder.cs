using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Workflow;

/// <summary>
/// Audit row for each reminder/escalation message dispatched by the
/// escalation job. Append-only; one row per (deadline, recipient,
/// channel) attempt.
/// </summary>
public class WorkflowReminder
{
    public long Id { get; set; }

    /// <summary>FK to <see cref="WorkflowDeadline.Id"/>.</summary>
    public Guid DeadlineId { get; set; }

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>FK to <c>AspNetUsers.Id</c>; the recipient.</summary>
    public string RecipientUserId { get; set; } = string.Empty;

    public WorkflowReminderChannel Channel { get; set; } = WorkflowReminderChannel.InApp;

    /// <summary>True for the final escalation message; false for an early-warning reminder.</summary>
    public bool IsEscalation { get; set; }
}
