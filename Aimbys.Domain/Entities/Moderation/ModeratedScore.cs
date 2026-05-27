namespace Aimbys.Domain.Entities.Moderation;

public class ModeratedScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ModerationId { get; set; }
    public Guid EvaluationId { get; set; }
    public decimal TotalPointsAwarded { get; set; }
    public decimal MaxPointsPossible { get; set; }
    public string ModeratedByUserId { get; set; } = string.Empty;
    public DateTime ModeratedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsOverride { get; set; }
    public string? OverrideReason { get; set; }
}
