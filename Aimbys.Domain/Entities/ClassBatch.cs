namespace Aimbys.Domain.Entities;

/// <summary>
/// A cohort of students (e.g. <c>Class XII-A</c>) for a given
/// <see cref="AcademicYear"/>. Examinations are scheduled at the
/// class-batch level; results are aggregated at the same boundary.
/// </summary>
public class ClassBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InstituteId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid? DepartmentId { get; set; }

    /// <summary>Display name, e.g. <c>Class XII-A</c>. Required, max 100.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Free-form grade level token (e.g. <c>"10"</c>, <c>"12"</c>,
    /// <c>"BSc-Year-2"</c>). Kept as a string because Indian educational
    /// systems use heterogeneous grade vocabularies.
    /// </summary>
    public string? GradeLevel { get; set; }

    /// <summary>Optional stream the batch belongs to (Chunk 18).</summary>
    public Guid? StreamId { get; set; }

    /// <summary>Optional class teacher assigned to this batch.</summary>
    public Guid? ClassTeacherProfileId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Institute? Institute { get; set; }
    public AcademicYear? AcademicYear { get; set; }
    public Department? Department { get; set; }
    public TeacherProfile? ClassTeacher { get; set; }
    public ICollection<StudentProfile> Students { get; set; } = new List<StudentProfile>();
}
