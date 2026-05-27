using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Questions;

public class QuestionDifficultyAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public DifficultyLevel AuthoredDifficulty { get; set; }
    public DifficultyLevel ComputedDifficulty { get; set; }
    public DriftDirection DriftDirection { get; set; } = DriftDirection.None;
    public double ConfidencePercent { get; set; }
    public int SampleSize { get; set; }
    public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
}
