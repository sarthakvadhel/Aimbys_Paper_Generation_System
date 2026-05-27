namespace Aimbys.Domain.Entities.Questions;

public class QuestionUsageAnalytics
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public Guid AcademicYearId { get; set; }
    public int PapersUsedIn { get; set; }
    public int AttemptsCount { get; set; }
    public double MeanTimeSeconds { get; set; }
    public double PValue { get; set; } // proportion answering correctly (0.0 - 1.0)
    public double DiscriminationIndex { get; set; } // point-biserial correlation
    public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
}
