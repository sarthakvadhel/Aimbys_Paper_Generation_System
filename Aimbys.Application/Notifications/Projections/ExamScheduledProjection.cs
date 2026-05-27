using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications.Projections;

public class ExamScheduledProjection : INotificationProjection<ExamScheduledEvent>
{
    public Task<IReadOnlyList<Notification>> ProjectAsync(ExamScheduledEvent e, CancellationToken ct = default)
    {
        // Placeholder: in future chunks, query eligible students in the
        // exam's target batch and emit one notification per student.
        var list = new List<Notification>
        {
            new()
            {
                InstituteId = e.InstituteId,
                RecipientUserId = e.ScheduledByUserId,
                Title = $"Exam \"{e.ExamTitle}\" scheduled",
                Body = $"Scheduled for {e.ScheduledAtUtc:yyyy-MM-dd HH:mm} UTC.",
                Severity = NotificationSeverity.Success,
                RouteUrl = $"/Institute/Calendar"
            }
        };
        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }
}
