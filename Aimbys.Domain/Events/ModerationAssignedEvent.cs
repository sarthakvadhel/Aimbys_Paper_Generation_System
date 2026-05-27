namespace Aimbys.Domain.Events;

public sealed record ModerationAssignedEvent : DomainEventBase
{
    public Guid ModerationId { get; init; }
    public Guid EvaluationId { get; init; }
    public string ModeratorUserId { get; init; } = string.Empty;
}
