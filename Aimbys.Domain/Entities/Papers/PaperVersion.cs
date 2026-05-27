namespace Aimbys.Domain.Entities.Papers;

public class PaperVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaperId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string Title { get; set; } = string.Empty;
    public int TotalMarks { get; set; }
    public int DurationMinutes { get; set; }
    public Guid? BlueprintVersionId { get; set; }
    public bool IsLocked { get; set; }
    public string AuthorUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Paper? Paper { get; set; }
    public ICollection<PaperSection> Sections { get; set; } = new List<PaperSection>();
    public ICollection<PaperQuestion> Questions { get; set; } = new List<PaperQuestion>();
}
