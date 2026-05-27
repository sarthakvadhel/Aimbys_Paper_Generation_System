using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications.Projections;

public class PaperApprovedProjection : INotificationProjection<PaperApprovedEvent>
{
    public Task<IReadOnlyList<Notification>> ProjectAsync(PaperApprovedEvent e, CancellationToken ct = default)
    {
        var list = new List<Notification>
        {
            new()
            {
                InstituteId = e.InstituteId,
                RecipientUserId = e.ApprovedByUserId, // placeholder; future: notify paper owner
                Title = $"Paper \"{e.PaperTitle}\" approved",
                Body = "The paper has been approved and is ready for exam scheduling.",
                Severity = NotificationSeverity.Success,
                RouteUrl = $"/Institute/Papers/Details/{e.PaperId}"
            }
        };
        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }
}
