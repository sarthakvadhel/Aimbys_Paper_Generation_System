using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Results;

public class ResultArchive
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExamId { get; set; }
    public ArchiveType ArchiveType { get; set; }
    public Guid FileAssetId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
