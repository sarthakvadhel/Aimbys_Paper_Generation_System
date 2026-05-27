namespace Aimbys.Domain.Events;

/// <summary>
/// Marker interface for domain events. Every concrete event implements this
/// so the dispatcher can collect them generically from entity state.
/// Events are dispatched AFTER <c>SaveChangesAsync</c> commits (via the
/// <c>DomainEventInterceptor</c>) so downstream projections never see
/// uncommitted data.
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC instant the event was raised.</summary>
    DateTime OccurredAtUtc { get; }

    /// <summary>Optional institute scope for routing notifications.</summary>
    Guid? InstituteId { get; }
}
