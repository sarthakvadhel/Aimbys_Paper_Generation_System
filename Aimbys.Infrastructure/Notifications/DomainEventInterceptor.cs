using Aimbys.Application.Notifications;
using Aimbys.Domain.Events;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Aimbys.Infrastructure.Notifications;

/// <summary>
/// EF Core <see cref="ISaveChangesInterceptor"/> that collects domain events
/// queued on a thread-local list and dispatches them AFTER the commit so
/// projections never see uncommitted state.
///
/// Services enqueue events via <see cref="DomainEventCollector.Enqueue"/>;
/// the interceptor drains the collector after <c>SavedChangesAsync</c>.
/// </summary>
public class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly DomainEventCollector _collector;

    public DomainEventInterceptor(
        IDomainEventDispatcher dispatcher,
        DomainEventCollector collector)
    {
        _dispatcher = dispatcher;
        _collector = collector;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var events = _collector.DrainAll();
        if (events.Count > 0)
        {
            await _dispatcher.DispatchAsync(events, cancellationToken);
        }

        return result;
    }
}
