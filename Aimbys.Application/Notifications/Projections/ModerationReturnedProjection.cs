using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications.Projections;

public class ModerationReturnedProjection : INotificationProjection<ModerationReturnedEvent>
{
    public Task<IReadOnlyList<Notification>> ProjectAsync(ModerationReturnedEvent e, CancellationToken ct = default)
    {
        var list = new List<Notification>
        {
            new()
            {
                InstituteId = e.InstituteId,
                RecipientUserId = e.ReturnedToUserId,
                Title = "Evaluation returned for revision",
                Body = string.IsNullOrWhiteSpace(e.Comment)
                    ? "A moderation review has returned your evaluation for revision."
                    : $"Returned: {e.Comment}",
                Severity = NotificationSeverity.Warning,
                RouteUrl = $"/Teacher/Evaluation/{e.ModerationId}"
            }
        };
        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }
}
