namespace Aimbys.Domain.Entities.Questions;

public class QuestionRubricCriterion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VersionId { get; set; }
    public string Criterion { get; set; } = string.Empty;
    public decimal MaxPoints { get; set; }
    public int SortOrder { get; set; }

    public QuestionVersion? Version { get; set; }
}
