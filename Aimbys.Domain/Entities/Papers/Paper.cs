using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Papers;

public class Paper
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstituteId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid AuthorTeacherProfileId { get; set; }
    public Guid? CurrentVersionId { get; set; }
    public PaperStatus Status { get; set; } = PaperStatus.Draft;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<PaperVersion> Versions { get; set; } = new List<PaperVersion>();
}
