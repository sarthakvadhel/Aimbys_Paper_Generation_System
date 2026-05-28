namespace Aimbys.Application.Exams;

/// <summary>
/// Runtime lifecycle of an exam attempt. The four mutating methods that
/// take a <c>studentUserId</c> enforce ownership at the service boundary
/// &mdash; the controller cannot bypass it. <see cref="AutoSubmitAsync"/>
/// is the system-triggered counterpart used by the auto-submit
/// background job and the suspicion auto-submit path.
/// </summary>
public interface IExamRuntimeService
{
    Task<ExamAttemptResult> StartAttemptAsync(Guid examId, Guid studentProfileId, CancellationToken ct = default);

    /// <summary>Saves an answer. Rejects with <c>"timer_expired"</c> if past the deadline; <c>"forbidden"</c> if the attempt is not owned by <paramref name="studentUserId"/>.</summary>
    Task<SaveAnswerResult> SaveAnswerAsync(Guid attemptId, Guid questionId, string? answerJson, string studentUserId, CancellationToken ct = default);

    /// <summary>Toggles the flagged state for an answer. Returns <c>false</c> when the attempt is not owned by <paramref name="studentUserId"/>.</summary>
    Task<bool> FlagQuestionAsync(Guid attemptId, Guid questionId, bool flagged, string studentUserId, CancellationToken ct = default);

    /// <summary>HTTP-path submit. Owned by <paramref name="studentUserId"/>; <c>AutoSubmitted</c> stays <c>false</c>.</summary>
    Task<SubmitResult> SubmitAsync(Guid attemptId, string studentUserId, CancellationToken ct = default);

    /// <summary>
    /// System-path submit for the auto-submit background job and the
    /// suspicion-driven force-submit path. Identical scoring + publish
    /// behaviour to <see cref="SubmitAsync"/>, but does not require a
    /// student user id and stamps <c>ExamAttempt.AutoSubmitted = true</c>.
    /// </summary>
    Task<SubmitResult> AutoSubmitAsync(Guid attemptId, CancellationToken ct = default);
}
