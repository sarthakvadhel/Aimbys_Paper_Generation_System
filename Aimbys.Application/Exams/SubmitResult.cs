namespace Aimbys.Application.Exams;

public sealed record SubmitResult(bool Success, string? Error = null, decimal? TotalAutoScore = null);
