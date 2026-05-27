namespace Aimbys.Domain.Events;

public sealed record EvaluationSubmittedEvent : DomainEventBase
{
    public Guid EvaluationId { get; init; }
    public Guid AttemptAnswerId { get; init; }
    public string EvaluatorUserId { get; init; } = string.Empty;
}
