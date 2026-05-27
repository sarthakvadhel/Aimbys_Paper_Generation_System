using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;

namespace Aimbys.Application.Notifications;

/// <summary>
/// Read/write surface for notifications. Controllers and ViewComponents
/// consume this to show the bell badge, list notifications, and mark them
/// read. Write path is used by the dispatcher to persist projected rows.
/// </summary>
public interface INotificationService
{
    /// <summary>Returns the count of unread notifications for the given user.</summary>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of notifications for the given user, optionally
    /// filtered by severity.
    /// </summary>
    Task<IReadOnlyList<Notification>> GetPageAsync(
        string userId,
        NotificationSeverity? severityFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a single notification as read.</summary>
    Task MarkReadAsync(Guid notificationId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Marks all unread notifications as read for the given user.</summary>
    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Persists one or more notifications (called by the dispatcher).</summary>
    Task CreateBatchAsync(IReadOnlyList<Notification> notifications, CancellationToken cancellationToken = default);
}
