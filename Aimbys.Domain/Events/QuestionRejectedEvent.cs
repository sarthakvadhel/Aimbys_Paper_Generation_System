namespace Aimbys.Domain.Events;

public sealed record QuestionRejectedEvent : DomainEventBase
{
    public Guid QuestionId { get; init; }
    public string AuthorUserId { get; init; } = string.Empty;
    public string ReviewerUserId { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
}
