using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;

namespace Aimbys.Application.Notifications.Projections;

/// <summary>
/// Fans out one notification per student whose result was published in
/// this batch. The recipient list is populated by
/// <c>IResultPublicationService</c>; the publishing admin does <em>not</em>
/// receive a notification (they performed the action and don't need one).
/// </summary>
public class ResultPublishedProjection : INotificationProjection<ResultPublishedEvent>
{
    public Task<IReadOnlyList<Notification>> ProjectAsync(ResultPublishedEvent e, CancellationToken ct = default)
    {
        if (e.RecipientUserIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<Notification>>(Array.Empty<Notification>());
        }

        // For a single-recipient publish (auto-only path) we deep-link
        // straight to that attempt's details; for batch publishes the
        // student lands on their results index and picks the row.
        var routeTemplate = e.RecipientUserIds.Count == 1 && e.AttemptId is { } attemptId
            ? $"/Student/Results/Details/{attemptId}"
            : "/Student/Results";

        var title = string.IsNullOrEmpty(e.ExamTitle)
            ? "Your exam result is now available"
            : $"Result published: {e.ExamTitle}";

        var body = string.IsNullOrEmpty(e.ExamTitle)
            ? "Your exam result has been published. Tap to view your score breakdown."
            : $"Your result for \"{e.ExamTitle}\" is now visible. Tap to view your score breakdown.";

        var notifications = e.RecipientUserIds
            .Where(uid => !string.IsNullOrEmpty(uid))
            .Select(uid => new Notification
            {
                InstituteId = e.InstituteId,
                RecipientUserId = uid,
                Title = title,
                Body = body,
                Severity = NotificationSeverity.Success,
                RouteUrl = routeTemplate
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<Notification>>(notifications);
    }
}
