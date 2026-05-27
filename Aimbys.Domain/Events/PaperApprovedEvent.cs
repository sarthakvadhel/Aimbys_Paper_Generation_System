namespace Aimbys.Domain.Events;

public sealed record PaperApprovedEvent : DomainEventBase
{
    public Guid PaperId { get; init; }
    public string PaperTitle { get; init; } = string.Empty;
    public string ApprovedByUserId { get; init; } = string.Empty;
}
