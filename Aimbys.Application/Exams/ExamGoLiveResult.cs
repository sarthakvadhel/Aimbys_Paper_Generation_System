namespace Aimbys.Application.Exams;

public sealed record ExamGoLiveResult(bool Success, string? Error = null, Guid? ExamId = null, string? Title = null);
