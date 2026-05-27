namespace Aimbys.Domain.Events;

public sealed record UserSuspendedEvent : DomainEventBase
{
    public string SuspendedUserId { get; init; } = string.Empty;
    public string SuspendedByUserId { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
