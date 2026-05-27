namespace Aimbys.Application.Blueprints;

public sealed record BlueprintValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);
