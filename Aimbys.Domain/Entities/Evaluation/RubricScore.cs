namespace Aimbys.Domain.Entities.Evaluation;

public class RubricScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EvaluationId { get; set; }
    public int CriterionIndex { get; set; }
    public decimal PointsAwarded { get; set; }
    public decimal MaxPoints { get; set; }
}
