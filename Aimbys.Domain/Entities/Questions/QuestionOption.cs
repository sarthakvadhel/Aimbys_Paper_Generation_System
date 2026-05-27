namespace Aimbys.Domain.Entities.Questions;

public class QuestionOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VersionId { get; set; }
    public string Label { get; set; } = string.Empty; // A, B, C, D
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }

    public QuestionVersion? Version { get; set; }
}
