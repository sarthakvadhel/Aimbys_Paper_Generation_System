namespace Aimbys.Domain.Entities.Papers;

public class PaperQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VersionId { get; set; }
    public Guid SectionId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid QuestionVersionId { get; set; }
    public int SortOrder { get; set; }
    public decimal? MarksOverride { get; set; }

    public PaperVersion? Version { get; set; }
    public PaperSection? Section { get; set; }
}
