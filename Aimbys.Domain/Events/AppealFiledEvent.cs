namespace Aimbys.Domain.Events;

public sealed record AppealFiledEvent : DomainEventBase
{
    public Guid AppealId { get; init; }
    public Guid ExamAttemptAnswerId { get; init; }
    public Guid StudentProfileId { get; init; }
}
