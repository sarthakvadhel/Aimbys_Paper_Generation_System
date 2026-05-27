namespace Aimbys.Application.Blueprints;

public sealed record BlueprintResult(
    bool Success,
    string? Error = null,
    Guid? BlueprintId = null,
    Guid? VersionId = null);
