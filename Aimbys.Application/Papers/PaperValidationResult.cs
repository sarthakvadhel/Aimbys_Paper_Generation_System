namespace Aimbys.Application.Papers;

public sealed record PaperValidationResult(bool IsValid, IReadOnlyList<string> Errors);
