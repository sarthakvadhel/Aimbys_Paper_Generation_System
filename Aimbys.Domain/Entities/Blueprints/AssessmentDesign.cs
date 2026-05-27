using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Blueprints;

public class AssessmentDesign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstituteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid SubjectId { get; set; }
    public AssessmentType AssessmentType { get; set; }
    public string? Description { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
