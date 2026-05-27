namespace Aimbys.Application.Questions;

public sealed record QuestionVersionSummary(
    Guid VersionId,
    int VersionNumber,
    DateTime CreatedAtUtc,
    string AuthorUserId,
    bool IsCurrentVersion);
