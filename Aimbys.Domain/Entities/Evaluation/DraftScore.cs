namespace Aimbys.Domain.Entities.Evaluation;

public class DraftScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EvaluationId { get; set; }
    public int CriterionIndex { get; set; }
    public decimal PointsAwarded { get; set; }
    public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;
}
