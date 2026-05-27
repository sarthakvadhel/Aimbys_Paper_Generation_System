namespace Aimbys.Domain.Entities;

/// <summary>
/// A scheduling window (e.g. <c>"2025-2026"</c>) inside an
/// <see cref="Institute"/>. <see cref="ClassBatch"/>es are scoped to one
/// academic year so the same "Class 10-A" label can be reused year over year.
/// </summary>
public class AcademicYear
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InstituteId { get; set; }

    /// <summary>Display name, e.g. <c>2025-2026</c>. Unique within an institute.</summary>
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// True for at most one academic year per institute at a time; helpful for
    /// dashboards that "default to the current year". Enforced softly &mdash; the
    /// service layer keeps the invariant.
    /// </summary>
    public bool IsCurrent { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Institute? Institute { get; set; }
    public ICollection<ClassBatch> ClassBatches { get; set; } = new List<ClassBatch>();
}
