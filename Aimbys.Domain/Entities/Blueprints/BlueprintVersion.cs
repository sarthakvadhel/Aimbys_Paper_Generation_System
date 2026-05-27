namespace Aimbys.Domain.Entities.Blueprints;

public class BlueprintVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BlueprintId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public int TotalMarks { get; set; }
    public int DurationMinutes { get; set; }
    public string? SectionsJson { get; set; }
    public string? ConstraintsJson { get; set; }
    public string? CohortJson { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Blueprint? Blueprint { get; set; }
    public ICollection<BlueprintSection> Sections { get; set; } = new List<BlueprintSection>();
    public ICollection<BlueprintConstraint> Constraints { get; set; } = new List<BlueprintConstraint>();
    public ICollection<BlueprintCohort> Cohorts { get; set; } = new List<BlueprintCohort>();
}
