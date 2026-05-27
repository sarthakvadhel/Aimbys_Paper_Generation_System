using Aimbys.Domain.Events;

namespace Aimbys.Infrastructure.Notifications;

/// <summary>
/// Scoped collector. Services call <see cref="Enqueue"/> during a
/// unit-of-work; the <see cref="DomainEventInterceptor"/> drains it after
/// <c>SaveChangesAsync</c> commits. Thread-safe within a single request
/// scope (no concurrent writes expected, but guarded anyway).
/// </summary>
public class DomainEventCollector
{
    private readonly List<IDomainEvent> _events = new();
    private readonly object _lock = new();

    /// <summary>Queue an event for post-commit dispatch.</summary>
    public void Enqueue(IDomainEvent domainEvent)
    {
        lock (_lock) { _events.Add(domainEvent); }
    }

    /// <summary>Drain all queued events and reset the collector.</summary>
    public IReadOnlyList<IDomainEvent> DrainAll()
    {
        lock (_lock)
        {
            if (_events.Count == 0) return Array.Empty<IDomainEvent>();
            var copy = _events.ToList();
            _events.Clear();
            return copy;
        }
    }
}
