namespace Aimbys.Domain.Events;

public sealed record ModerationApprovedEvent : DomainEventBase
{
    public Guid ModerationId { get; init; }
    public Guid EvaluationId { get; init; }
}
