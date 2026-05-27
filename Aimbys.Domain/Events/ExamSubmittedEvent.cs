namespace Aimbys.Domain.Events;

public sealed record ExamSubmittedEvent : DomainEventBase
{
    public Guid AttemptId { get; init; }
    public Guid ExamId { get; init; }
    public Guid StudentProfileId { get; init; }
    public bool HasManualQuestions { get; init; }
    public decimal TotalAutoScore { get; init; }
}
