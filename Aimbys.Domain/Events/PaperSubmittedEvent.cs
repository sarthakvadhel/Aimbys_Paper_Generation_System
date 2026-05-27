namespace Aimbys.Domain.Events;

public sealed record PaperSubmittedEvent : DomainEventBase
{
    public Guid PaperId { get; init; }
    public string PaperTitle { get; init; } = string.Empty;
    public string SubmittedByUserId { get; init; } = string.Empty;
}
