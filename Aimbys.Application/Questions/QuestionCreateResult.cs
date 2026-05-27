namespace Aimbys.Application.Questions;

public sealed record QuestionCreateResult(bool Success, string? Error, Guid? QuestionId, Guid? VersionId);
