namespace Aimbys.Domain.Events;

public sealed record ExamScheduledEvent : DomainEventBase
{
    public Guid ExamId { get; init; }
    public Guid PaperVersionId { get; init; }
    public Guid ClassBatchId { get; init; }
    public string ExamTitle { get; init; } = string.Empty;
    public DateTime ScheduledAtUtc { get; init; }
    public string ScheduledByUserId { get; init; } = string.Empty;
}
