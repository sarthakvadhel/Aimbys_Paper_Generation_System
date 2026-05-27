namespace Aimbys.Application.Results;

public record AppealResult(bool Success, string? Error, Guid? AppealId);
