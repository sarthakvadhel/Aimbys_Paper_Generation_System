using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Blueprints;

public class Blueprint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstituteId { get; set; }
    public Guid? AssessmentDesignId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid? CurrentVersionId { get; set; }
    public BlueprintStatus Status { get; set; } = BlueprintStatus.Draft;
    public Guid CreatedByTeacherProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<BlueprintVersion> Versions { get; set; } = new List<BlueprintVersion>();
}
