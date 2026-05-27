namespace Aimbys.Domain.Entities;

/// <summary>
/// An organisational unit inside an <see cref="Institute"/> (e.g. Mathematics,
/// Physics, Computer Science, Administration). Subjects and teachers hang off
/// departments; students do not (students belong to a <see cref="ClassBatch"/>).
/// </summary>
public class Department
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InstituteId { get; set; }

    /// <summary>Department display name. Required, max 200.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short department code (e.g. <c>MATH</c>). Unique within the institute.</summary>
    public string? Code { get; set; }

    /// <summary>Optional pointer at the head-of-department teacher profile.</summary>
    public Guid? HeadTeacherProfileId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Institute? Institute { get; set; }
    public TeacherProfile? HeadTeacher { get; set; }
    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<ClassBatch> ClassBatches { get; set; } = new List<ClassBatch>();
    public ICollection<TeacherProfile> Teachers { get; set; } = new List<TeacherProfile>();
}
