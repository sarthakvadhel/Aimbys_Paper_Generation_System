namespace Aimbys.Domain.Entities.Analytics;

public class CachedLeaderboardEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExamId { get; set; }
    public Guid ClassBatchId { get; set; }
    public Guid StudentProfileId { get; set; }
    public int Rank { get; set; }
    public double Percentile { get; set; }
    public decimal TotalScore { get; set; }
    public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
}
