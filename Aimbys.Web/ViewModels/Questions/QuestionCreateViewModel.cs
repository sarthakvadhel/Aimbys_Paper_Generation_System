using Aimbys.Domain.Enums;

namespace Aimbys.Web.ViewModels.Questions;

public class QuestionCreateViewModel
{
    public QuestionType Type { get; set; }
    public Guid SubjectId { get; set; }
    public Guid? ChapterId { get; set; }
    public string BodyHtml { get; set; } = string.Empty;
    public DifficultyLevel Difficulty { get; set; }
    public BloomLevel BloomLevel { get; set; }
    public decimal Marks { get; set; }
    public int? EstimatedTimeMinutes { get; set; }
    public string? InstructionsHtml { get; set; }

    // Options for MCQ/MultiSelect/TrueFalse
    public List<OptionViewModel> Options { get; set; } = new();

    // Rubric criteria for Descriptive/FileUpload
    public List<RubricViewModel> RubricCriteria { get; set; } = new();

    // Test cases for Coding
    public List<TestCaseViewModel> TestCases { get; set; } = new();
}

public class OptionViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
}

public class RubricViewModel
{
    public string Criterion { get; set; } = string.Empty;
    public decimal MaxPoints { get; set; }
    public int SortOrder { get; set; }
}

public class TestCaseViewModel
{
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public int TimeoutMs { get; set; } = 5000;
    public int MemoryLimitMb { get; set; } = 256;
    public int SortOrder { get; set; }
}
