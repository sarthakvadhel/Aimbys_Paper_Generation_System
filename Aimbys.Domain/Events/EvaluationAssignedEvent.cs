namespace Aimbys.Domain.Events;

public sealed record EvaluationAssignedEvent : DomainEventBase
{
    public Guid EvaluationId { get; init; }
    public string AssignedToUserId { get; init; } = string.Empty;
    public string ExamTitle { get; init; } = string.Empty;
    public string StudentName { get; init; } = string.Empty;
}
