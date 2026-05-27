namespace Aimbys.Application.Exams;

public sealed record ExamScheduleResult(bool Success, string? Error = null, Guid? ExamId = null);
