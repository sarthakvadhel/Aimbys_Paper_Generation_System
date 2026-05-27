using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications.Projections;

/// <summary>
/// Notifies the paper author when their published paper is archived.
/// Existing attempts and results are preserved; the paper just
/// becomes unavailable for new exam schedules.
/// </summary>
public class PaperArchivedProjection : INotificationProjection<PaperArchivedEvent>
{
    public Task<IReadOnlyList<Notification>> ProjectAsync(PaperArchivedEvent e, CancellationToken ct = default)
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
                Title = $"Paper \"{e.PaperTitle}\" archived",
                Body = "The paper has been archived and is no longer available for new exam schedules. Past attempts and results remain accessible.",
                Severity = NotificationSeverity.Information,
                RouteUrl = $"/Teacher/Papers/Preview/{e.PaperId}"
            }
        };
        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }
}
