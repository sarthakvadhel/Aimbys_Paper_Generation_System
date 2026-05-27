namespace Aimbys.Domain.Events;

public sealed record InstituteApprovedEvent : DomainEventBase
{
    public string InstituteName { get; init; } = string.Empty;
    public string ApprovedByUserId { get; init; } = string.Empty;
    public string InstituteAdminUserId { get; init; } = string.Empty;
}
