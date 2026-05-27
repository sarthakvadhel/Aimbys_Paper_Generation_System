namespace Aimbys.Domain.Events;

public sealed record QuestionCreatedEvent : DomainEventBase
{
    public Guid QuestionId { get; init; }
    public Guid SubjectId { get; init; }
    public string AuthorUserId { get; init; } = string.Empty;
}
