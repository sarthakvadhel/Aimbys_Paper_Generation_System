namespace Aimbys.Domain.Events;

public sealed record ResultPublishedEvent : DomainEventBase
{
    public Guid ExamId { get; init; }
    public string ExamTitle { get; init; } = string.Empty;
    public string PublishedByUserId { get; init; } = string.Empty;
    public int StudentCount { get; init; }
}
