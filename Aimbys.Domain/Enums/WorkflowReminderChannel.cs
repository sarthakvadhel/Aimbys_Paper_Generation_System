namespace Aimbys.Domain.Enums;

/// <summary>
/// Delivery channel a <c>WorkflowReminder</c> was sent through. The
/// escalation job records the channel used so audit logs can confirm
/// reminders reached the recipient.
/// </summary>
public enum WorkflowReminderChannel
{
    /// <summary>
    /// Persisted as an in-app notification (the default channel; backed by
    /// <see cref="Aimbys.Domain.Entities.Notification"/> rows).
    /// </summary>
    InApp = 0,

    /// <summary>Email channel (delivered via the registered notification channel).</summary>
    Email = 1,

    /// <summary>Mobile-push channel (reserved for a future channel implementation).</summary>
    Push = 2
}
