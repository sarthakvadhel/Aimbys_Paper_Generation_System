namespace Aimbys.Domain.Entities.Blueprints;

public class BlueprintCohort
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VersionId { get; set; }
    public Guid? StreamId { get; set; }
    public Guid? MajorId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? AcademicYearId { get; set; }
    public Guid? ClassBatchId { get; set; }

    // Navigation
    public BlueprintVersion? Version { get; set; }
}
