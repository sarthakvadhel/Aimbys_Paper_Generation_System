namespace Aimbys.Domain.Events;

public sealed record ModerationReturnedEvent : DomainEventBase
{
    public Guid ModerationId { get; init; }
    public string ReturnedToUserId { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
}
