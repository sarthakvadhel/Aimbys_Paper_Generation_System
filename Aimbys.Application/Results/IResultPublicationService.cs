using System.Security.Claims;

namespace Aimbys.Application.Results;

/// <summary>
/// Result publication surface. Two entry points:
///
/// <list type="bullet">
///   <item><see cref="PublishAttemptAsync"/> &mdash; per-attempt publish,
///         used by the exam runtime auto-only path so MCQ-only papers
///         publish individual results immediately as students submit.</item>
///   <item><see cref="PublishAsync"/> &mdash; batch publish for an entire
///         exam, used by Institute Admins after manual evaluation +
///         moderation are complete. Triggers the archive snapshot.</item>
/// </list>
/// </summary>
public interface IResultPublicationService
{
    /// <summary>
    /// Returns whether every submitted attempt for the exam has its
    /// manual evaluations resolved (all in Submitted/Approved/Returned
    /// state &mdash; nothing still Pending or InProgress).
    /// </summary>
    Task<(bool CanPublish, string? BlockingReason)> CanPublishAsync(
        Guid examId,
        CancellationToken ct = default);

    /// <summary>
    /// Publishes a single attempt's result. Used by the auto-only path
    /// when an MCQ-only exam is submitted; the actor is typically
    /// <c>"system"</c>. Recomputes ranks + leaderboard for the parent
    /// exam and emits a per-attempt
    /// <see cref="Aimbys.Domain.Events.ResultPublishedEvent"/> with the
    /// student as the only recipient.
    /// </summary>
    Task<ResultPublishResult> PublishAttemptAsync(
        Guid attemptId,
        string actorUserId,
        CancellationToken ct = default);

    /// <summary>
    /// Publishes <em>every</em> ready attempt of an exam in one batch.
    /// On success: recomputes ranks + leaderboard, generates the JSON
    /// archive snapshot, and emits a single batched
    /// <see cref="Aimbys.Domain.Events.ResultPublishedEvent"/> with
    /// every recipient student id populated.
    /// </summary>
    Task<ResultPublishResult> PublishAsync(
        Guid examId,
        ClaimsPrincipal actor,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a student's published-result view for an attempt, or
    /// <c>null</c> if the result is not yet published or the student
    /// did not own the attempt.
    /// </summary>
    Task<StudentResultView?> GetStudentResultAsync(
        Guid attemptId,
        string studentUserId,
        CancellationToken ct = default);
}
