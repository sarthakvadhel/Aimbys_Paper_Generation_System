using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications.Projections;

public class PaperSubmittedProjection : INotificationProjection<PaperSubmittedEvent>
{
    public Task<IReadOnlyList<Notification>> ProjectAsync(PaperSubmittedEvent e, CancellationToken ct = default)
    {
        // In future chunks, this will query for institute admins with
        // paper-approval permission. For now, emit a placeholder addressed
        // to the submitter confirming receipt.
        var list = new List<Notification>
        {
            new()
            {
                InstituteId = e.InstituteId,
                RecipientUserId = e.SubmittedByUserId,
                Title = $"Paper \"{e.PaperTitle}\" submitted for approval",
                Body = "Your paper has been submitted and is pending review by the institute admin.",
                Severity = NotificationSeverity.Information,
                RouteUrl = $"/Teacher/Papers/Details/{e.PaperId}"
            }
        };
        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }
}
