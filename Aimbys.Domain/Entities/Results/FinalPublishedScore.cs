using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Results;

public class FinalPublishedScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExamAttemptAnswerId { get; set; }
    public decimal PointsAwarded { get; set; }
    public decimal MaxPoints { get; set; }
    public ScoreSource Source { get; set; }
    public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
}
