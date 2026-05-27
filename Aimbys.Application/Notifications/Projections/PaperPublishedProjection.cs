using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications.Projections;

/// <summary>
/// Notifies the paper author when their approved paper is published
/// for exam scheduling. The notification deep-links to the read-only
/// preview surface.
/// </summary>
public class PaperPublishedProjection : INotificationProjection<PaperPublishedEvent>
{
    public Task<IReadOnlyList<Notification>> ProjectAsync(PaperPublishedEvent e, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(e.AuthorUserId))
        {
            return Task.FromResult<IReadOnlyList<Notification>>(Array.Empty<Notification>());
        }

        var list = new List<Notification>
        {
            new()
            {
                InstituteId = e.InstituteId,
                RecipientUserId = e.AuthorUserId,
                Title = $"Paper \"{e.PaperTitle}\" published",
                Body = "The paper is now available for exam scheduling.",
                Severity = NotificationSeverity.Success,
                RouteUrl = $"/Teacher/Papers/Preview/{e.PaperId}"
            }
        };
        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }
}
