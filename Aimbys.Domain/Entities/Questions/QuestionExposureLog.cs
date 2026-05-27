namespace Aimbys.Domain.Entities.Questions;

public class QuestionExposureLog
{
    public long Id { get; set; }
    public Guid QuestionId { get; set; }
    public Guid PaperId { get; set; }
    public Guid InstituteId { get; set; }
    public DateTime ExposedAtUtc { get; set; } = DateTime.UtcNow;
}
