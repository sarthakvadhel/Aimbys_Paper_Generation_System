namespace Aimbys.Domain.Entities.Papers;

public class PublishedSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaperVersionId { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
