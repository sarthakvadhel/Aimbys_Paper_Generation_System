namespace Aimbys.Domain.Events;

public sealed record InstituteSuspendedEvent : DomainEventBase
{
    public string InstituteName { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public string ActorUserId { get; init; } = string.Empty;
}
