using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications.Projections;

/// <summary>
/// Notifies the paper author when an Institute Admin returns their
/// paper with a reviewer comment. The author needs to know what to
/// fix and can edit + resubmit from <c>Teacher/Papers/Edit/{id}</c>.
/// </summary>
public class PaperReturnedProjection : INotificationProjection<PaperReturnedEvent>
{
    public Task<IReadOnlyList<Notification>> ProjectAsync(PaperReturnedEvent e, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(e.AuthorUserId))
        {
            return Task.FromResult<IReadOnlyList<Notification>>(Array.Empty<Notification>());
        }

        var body = string.IsNullOrWhiteSpace(e.Comment)
            ? "The paper was returned. Please review the workflow history and resubmit when ready."
            : $"Reviewer comment: {e.Comment}";

        var list = new List<Notification>
        {
            new()
            {
                InstituteId = e.InstituteId,
                RecipientUserId = e.AuthorUserId,
                Title = $"Paper \"{e.PaperTitle}\" returned for revision",
                Body = body,
                Severity = NotificationSeverity.Warning,
                RouteUrl = $"/Teacher/Papers/Edit/{e.PaperId}"
            }
        };
        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }
}
