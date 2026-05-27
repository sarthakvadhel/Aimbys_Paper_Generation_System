using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications.Projections;

public class ResultPublishedProjection : INotificationProjection<ResultPublishedEvent>
{
    public Task<IReadOnlyList<Notification>> ProjectAsync(ResultPublishedEvent e, CancellationToken ct = default)
    {
        var list = new List<Notification>
        {
            new()
            {
                InstituteId = e.InstituteId,
                RecipientUserId = e.PublishedByUserId,
                Title = $"Results published: {e.ExamTitle}",
                Body = $"{e.StudentCount} student result(s) are now visible.",
                Severity = NotificationSeverity.Success,
                RouteUrl = $"/Institute/Results"
            }
        };
        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }
}
