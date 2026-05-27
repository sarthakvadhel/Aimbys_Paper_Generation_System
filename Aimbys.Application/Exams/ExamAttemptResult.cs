namespace Aimbys.Application.Exams;

public sealed record ExamAttemptResult(bool Success, string? Error = null, Guid? AttemptId = null);
