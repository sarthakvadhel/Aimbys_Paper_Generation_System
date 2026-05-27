using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications.Projections;

public class UserSuspendedProjection : INotificationProjection<UserSuspendedEvent>
{
    public Task<IReadOnlyList<Notification>> ProjectAsync(UserSuspendedEvent e, CancellationToken ct = default)
    {
        var list = new List<Notification>
        {
            new()
            {
                InstituteId = e.InstituteId,
                RecipientUserId = e.SuspendedUserId,
                Title = "Your account has been suspended",
                Body = string.IsNullOrWhiteSpace(e.Reason)
                    ? "Contact your institute administrator for details."
                    : $"Reason: {e.Reason}",
                Severity = NotificationSeverity.Error
            }
        };
        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }
}
