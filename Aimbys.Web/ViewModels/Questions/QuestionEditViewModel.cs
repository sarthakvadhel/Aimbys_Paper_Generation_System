using Aimbys.Domain.Enums;

namespace Aimbys.Web.ViewModels.Questions;

public class QuestionEditViewModel
{
    public Guid QuestionId { get; set; }
    public QuestionType Type { get; set; }
    public QuestionStatus Status { get; set; }
    public string BodyHtml { get; set; } = string.Empty;
    public DifficultyLevel Difficulty { get; set; }
    public BloomLevel BloomLevel { get; set; }
    public decimal Marks { get; set; }
    public int? EstimatedTimeMinutes { get; set; }
    public string? InstructionsHtml { get; set; }
    public List<OptionViewModel> Options { get; set; } = new();
    public List<RubricViewModel> RubricCriteria { get; set; } = new();
    public List<TestCaseViewModel> TestCases { get; set; } = new();
}
