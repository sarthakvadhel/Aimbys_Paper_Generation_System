namespace Aimbys.Domain.Entities.Evaluation;

public class ScoringScheme
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaperVersionId { get; set; }
    public Guid QuestionId { get; set; }
    public string CriteriaJson { get; set; } = "[]"; // [{criterion, maxPoints, index}]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
