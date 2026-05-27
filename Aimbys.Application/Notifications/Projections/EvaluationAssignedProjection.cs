using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications.Projections;

public class EvaluationAssignedProjection : INotificationProjection<EvaluationAssignedEvent>
{
    public Task<IReadOnlyList<Notification>> ProjectAsync(EvaluationAssignedEvent e, CancellationToken ct = default)
    {
        var list = new List<Notification>
        {
            new()
            {
                InstituteId = e.InstituteId,
                RecipientUserId = e.AssignedToUserId,
                Title = $"Evaluation assigned: {e.StudentName} — {e.ExamTitle}",
                Body = "A new submission has been assigned to you for evaluation.",
                Severity = NotificationSeverity.Information,
                RouteUrl = $"/Teacher/Evaluation/{e.EvaluationId}"
            }
        };
        return Task.FromResult<IReadOnlyList<Notification>>(list);
    }
}
