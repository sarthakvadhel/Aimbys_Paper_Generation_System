namespace Aimbys.Application.Questions;

public sealed record QuestionEditResult(bool Success, string? Error, bool NewVersionCreated, Guid? VersionId);
