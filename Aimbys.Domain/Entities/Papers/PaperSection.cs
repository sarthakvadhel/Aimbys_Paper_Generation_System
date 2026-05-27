namespace Aimbys.Domain.Entities.Papers;

public class PaperSection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Marks { get; set; }
    public int SortOrder { get; set; }

    public PaperVersion? Version { get; set; }
}
