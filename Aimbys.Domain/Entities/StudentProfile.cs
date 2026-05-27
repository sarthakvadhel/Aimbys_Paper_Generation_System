using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities;

/// <summary>
/// Profile row for a student inside an <see cref="Institute"/>. Mirrors the
/// shape of <see cref="TeacherProfile"/>: login credentials live on
/// <c>AspNetUsers</c> (<see cref="UserId"/>) and this row carries the
/// academic metadata.
/// </summary>
public class StudentProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK to <c>AspNetUsers.Id</c> (the Identity user). Unique.</summary>
    public string UserId { get; set; } = string.Empty;

    public Guid InstituteId { get; set; }
    public Guid ClassBatchId { get; set; }

    /// <summary>Cached display name, max 200.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Admission number, unique within the institute.</summary>
    public string? AdmissionNumber { get; set; }

    /// <summary>Roll number scoped to the class batch (display only).</summary>
    public string? RollNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public Guid? PreferredLanguageId { get; set; }

    public ProfileStatus Status { get; set; } = ProfileStatus.Active;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Institute? Institute { get; set; }
    public ClassBatch? ClassBatch { get; set; }
}
