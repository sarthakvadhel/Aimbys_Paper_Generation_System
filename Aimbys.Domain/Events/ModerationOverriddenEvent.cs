namespace Aimbys.Domain.Events;

public sealed record ModerationOverriddenEvent : DomainEventBase
{
    public Guid ModerationId { get; init; }
    public Guid EvaluationId { get; init; }
    public string OverrideReason { get; init; } = string.Empty;
}
