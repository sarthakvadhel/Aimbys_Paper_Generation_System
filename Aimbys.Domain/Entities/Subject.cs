namespace Aimbys.Domain.Entities;

/// <summary>
/// A subject taught at an <see cref="Institute"/> (e.g. Mathematics, Physics,
/// Computer Science). Question banks, blueprints, papers and exams are
/// authored against a subject.
/// </summary>
public class Subject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid InstituteId { get; set; }

    /// <summary>Optional department the subject belongs to.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Display name. Required, max 200.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short code (e.g. <c>MATH-101</c>). Unique within the institute.</summary>
    public string? Code { get; set; }

    /// <summary>Optional long-form description, max 2000.</summary>
    public string? Description { get; set; }

    /// <summary>Optional stream the subject belongs to (Chunk 18).</summary>
    public Guid? StreamId { get; set; }

    /// <summary>Optional major the subject belongs to (Chunk 18).</summary>
    public Guid? MajorId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Institute? Institute { get; set; }
    public Department? Department { get; set; }
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}
