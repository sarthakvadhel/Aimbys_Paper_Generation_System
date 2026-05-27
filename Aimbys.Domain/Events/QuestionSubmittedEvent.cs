namespace Aimbys.Domain.Events;

public sealed record QuestionSubmittedEvent : DomainEventBase
{
    public Guid QuestionId { get; init; }
    public Guid SubjectId { get; init; }
    public string AuthorUserId { get; init; } = string.Empty;
}
