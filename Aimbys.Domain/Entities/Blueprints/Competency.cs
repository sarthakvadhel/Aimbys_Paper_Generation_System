using Aimbys.Domain.SoftDelete;

namespace Aimbys.Domain.Entities.Blueprints;

public class Competency : ISoftDelete
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstituteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public Guid? ParentCompetencyId { get; set; }
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Competency? Parent { get; set; }
    public ICollection<Competency> Children { get; set; } = new List<Competency>();
}
