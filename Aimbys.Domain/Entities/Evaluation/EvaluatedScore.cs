namespace Aimbys.Domain.Entities.Evaluation;

public class EvaluatedScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EvaluationId { get; set; }
    public decimal TotalPointsAwarded { get; set; }
    public decimal MaxPointsPossible { get; set; }
    public string? Feedback { get; set; }
    public string EvaluatedByUserId { get; set; } = string.Empty;
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
}
