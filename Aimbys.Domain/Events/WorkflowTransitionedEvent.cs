namespace Aimbys.Domain.Events;

/// <summary>
/// Raised by the <c>WorkflowEngine</c> after every successful state
/// transition. Notification projections subscribe to this generic event
/// and inspect <see cref="DefinitionKey"/> + <see cref="ToState"/> to
/// decide whether and how to project an inbox alert.
/// </summary>
public sealed record WorkflowTransitionedEvent : DomainEventBase
{
    /// <summary>FK to <c>WorkflowInstance.Id</c>.</summary>
    public Guid InstanceId { get; init; }

    /// <summary>Stable workflow key, e.g. <c>"PaperApproval"</c>.</summary>
    public string DefinitionKey { get; init; } = string.Empty;

    /// <summary>Discriminator for the business entity, e.g. <c>"Paper"</c>.</summary>
    public string SubjectType { get; init; } = string.Empty;

    /// <summary>Primary key of the business entity.</summary>
    public Guid SubjectId { get; init; }

    /// <summary>State the instance left.</summary>
    public string FromState { get; init; } = string.Empty;

    /// <summary>State the instance entered.</summary>
    public string ToState { get; init; } = string.Empty;

    /// <summary>Identity user id of the actor who performed the transition.</summary>
    public string ActorUserId { get; init; } = string.Empty;

    /// <summary>Optional comment captured at transition time.</summary>
    public string? Comment { get; init; }
}
