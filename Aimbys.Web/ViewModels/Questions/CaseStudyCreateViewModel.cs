using Aimbys.Domain.Enums;

namespace Aimbys.Web.ViewModels.Questions;

public class CaseStudyCreateViewModel
{
    public Guid SubjectId { get; set; }
    public Guid? ChapterId { get; set; }
    public string CaseStudyContextHtml { get; set; } = string.Empty;
    public List<CaseStudySubQuestionViewModel> SubQuestions { get; set; } = new();
}

public class CaseStudySubQuestionViewModel
{
    public QuestionType Type { get; set; }
    public string BodyHtml { get; set; } = string.Empty;
    public DifficultyLevel Difficulty { get; set; }
    public BloomLevel BloomLevel { get; set; }
    public decimal Marks { get; set; }
    public int? EstimatedTimeMinutes { get; set; }
    public string? InstructionsHtml { get; set; }
    public List<OptionViewModel> Options { get; set; } = new();
    public List<RubricViewModel> RubricCriteria { get; set; } = new();
}
