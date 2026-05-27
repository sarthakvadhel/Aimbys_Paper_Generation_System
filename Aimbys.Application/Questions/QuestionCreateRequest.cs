using Aimbys.Domain.Enums;

namespace Aimbys.Application.Questions;

public sealed record QuestionCreateRequest
{
    public QuestionType Type { get; init; }
    public Guid SubjectId { get; init; }
    public Guid? ChapterId { get; init; }
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

public sealed record OptionInput(string Label, string Text, bool IsCorrect, int SortOrder);
public sealed record RubricInput(string Criterion, decimal MaxPoints, int SortOrder);
public sealed record TestCaseInput(string Input, string ExpectedOutput, bool IsHidden, int TimeoutMs, int MemoryLimitMb, int SortOrder);
