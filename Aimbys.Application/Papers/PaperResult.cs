namespace Aimbys.Application.Papers;

public sealed record PaperResult(
    bool Success,
    string? Error = null,
    Guid? PaperId = null,
    Guid? VersionId = null);
