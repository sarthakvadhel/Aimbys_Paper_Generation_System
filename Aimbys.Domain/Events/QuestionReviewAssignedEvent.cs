namespace Aimbys.Domain.Events;

public sealed record QuestionReviewAssignedEvent : DomainEventBase
{
    public Guid QuestionId { get; init; }
    public string ReviewerUserId { get; init; } = string.Empty;
    public Guid SubjectId { get; init; }
}
