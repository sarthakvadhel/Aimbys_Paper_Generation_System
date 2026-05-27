namespace Aimbys.Domain.Entities.Workflow;

/// <summary>
/// Append-only history row written for every successful state change.
/// Never updated, never deleted &mdash; the table is the audit trail for
/// every lifecycle transition in the platform.
/// </summary>
public class WorkflowTransition
{
    public long Id { get; set; }

    /// <summary>FK to <see cref="WorkflowInstance.Id"/>.</summary>
    public Guid InstanceId { get; set; }

    /// <summary>State the instance transitioned out of.</summary>
    public string FromState { get; set; } = string.Empty;

    /// <summary>State the instance transitioned into.</summary>
    public string ToState { get; set; } = string.Empty;

    /// <summary>FK to <c>AspNetUsers.Id</c>; the actor who performed the transition.</summary>
    public string ActorUserId { get; set; } = string.Empty;

    /// <summary>Optional free-form comment captured at transition time.</summary>
    public string? Comment { get; set; }

    public DateTime TransitionedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public WorkflowInstance? Instance { get; set; }
}
