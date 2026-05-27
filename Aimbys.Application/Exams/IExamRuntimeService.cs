namespace Aimbys.Application.Exams;

public interface IExamRuntimeService
{
    Task<ExamAttemptResult> StartAttemptAsync(Guid examId, Guid studentProfileId, CancellationToken ct = default);
    Task<SaveAnswerResult> SaveAnswerAsync(Guid attemptId, Guid questionId, string? answerJson, string studentUserId, CancellationToken ct = default);
    Task<bool> FlagQuestionAsync(Guid attemptId, Guid questionId, bool flagged, string studentUserId, CancellationToken ct = default);
    Task<SubmitResult> SubmitAsync(Guid attemptId, string studentUserId, CancellationToken ct = default);
}
