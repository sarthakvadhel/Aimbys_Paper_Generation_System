namespace Aimbys.Domain.Events;

public sealed record PaperReturnedEvent : DomainEventBase
{
    public Guid PaperId { get; init; }
    public string AuthorUserId { get; init; } = string.Empty;
    public string? Comment { get; init; }
}
