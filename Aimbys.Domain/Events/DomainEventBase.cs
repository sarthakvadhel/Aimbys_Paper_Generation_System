namespace Aimbys.Domain.Events;

/// <summary>
/// Convenience base record for domain events. Concrete events inherit this
/// and add their payload fields.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public Guid? InstituteId { get; init; }
}
