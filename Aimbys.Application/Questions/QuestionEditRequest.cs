using Aimbys.Domain.Enums;

namespace Aimbys.Application.Questions;

public sealed record QuestionEditRequest
{
    public string BodyHtml { get; init; } = string.Empty;
    public DifficultyLevel Difficulty { get; init; }
    public BloomLevel BloomLevel { get; init; }
    public decimal Marks { get; init; }
    public int? EstimatedTimeMinutes { get; init; }
    public string? InstructionsHtml { get; init; }
    public IReadOnlyList<OptionInput> Options { get; init; } = Array.Empty<OptionInput>();
    public IReadOnlyList<RubricInput> RubricCriteria { get; init; } = Array.Empty<RubricInput>();
    public IReadOnlyList<TestCaseInput> TestCases { get; init; } = Array.Empty<TestCaseInput>();
}
