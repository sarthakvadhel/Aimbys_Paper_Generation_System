namespace Aimbys.Domain.Events;

public sealed record UserInvitedEvent : DomainEventBase
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public new Guid? InstituteId { get; init; }
}
