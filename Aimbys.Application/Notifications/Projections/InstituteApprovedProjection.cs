using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications.Projections;

public class InstituteApprovedProjection : INotificationProjection<InstituteApprovedEvent>
{
    public Task<IReadOnlyList<Notification>> ProjectAsync(InstituteApprovedEvent e, CancellationToken ct = default)
    {
        var list = new List<Notification>
        {
            new()
            {
                InstituteId = e.InstituteId,
                RecipientUserId = e.InstituteAdminUserId,
                Title = $"Institute \"{e.InstituteName}\" approved",
                Body = "Your institute has been approved. You can now configure departments, subjects, and users.",
                Severity = NotificationSeverity.Success,
                RouteUrl = "/Institute"
            }
        };
        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }
}
