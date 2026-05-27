using Aimbys.Domain.Enums;

namespace Aimbys.Web.ViewModels.Questions;

public class QuestionIndexViewModel
{
    public IReadOnlyList<QuestionRowViewModel> Questions { get; set; } = Array.Empty<QuestionRowViewModel>();
}

public class QuestionRowViewModel
{
    public Guid Id { get; set; }
    public QuestionType Type { get; set; }
    public QuestionStatus Status { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public DifficultyLevel Difficulty { get; set; }
    public decimal Marks { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string BodyPreview { get; set; } = string.Empty;
}
