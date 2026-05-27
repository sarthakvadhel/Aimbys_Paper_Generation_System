using Aimbys.Application.Notifications;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Notifications;

/// <summary>
/// Default <see cref="INotificationService"/> backed by the
/// <c>Notifications</c> table in <see cref="AppDbContext"/>.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db) => _db = db;

    public Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default)
    {
        return _db.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .CountAsync(ct);
    }

    public async Task<IReadOnlyList<Notification>> GetPageAsync(
        string userId,
        NotificationSeverity? severityFilter,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == userId);

        if (severityFilter.HasValue)
        {
            query = query.Where(n => n.Severity == severityFilter.Value);
        }

        return await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task MarkReadAsync(Guid notificationId, string userId, CancellationToken ct = default)
    {
        var n = await _db.Notifications
            .Where(x => x.Id == notificationId && x.RecipientUserId == userId && !x.IsRead)
            .FirstOrDefaultAsync(ct);

        if (n is null) return;

        n.IsRead = true;
        n.ReadAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _db.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAtUtc, now), ct);
    }

    public async Task CreateBatchAsync(IReadOnlyList<Notification> notifications, CancellationToken ct = default)
    {
        if (notifications.Count == 0) return;
        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync(ct);
    }
}
