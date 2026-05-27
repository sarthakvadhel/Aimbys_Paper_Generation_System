namespace Aimbys.Domain.Entities.Moderation;

public class ModerationSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ModerationId { get; set; }
    public string EvaluatorScoresJson { get; set; } = "{}";
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
}
