namespace Aimbys.Domain.Enums;

/// <summary>
/// Lifecycle for <see cref="Aimbys.Domain.Entities.TeacherProfile"/> and
/// <see cref="Aimbys.Domain.Entities.StudentProfile"/>. Distinct from the
/// owning identity user's lockout state &mdash; an institute admin can
/// deactivate a teacher profile without touching their login credentials.
/// </summary>
public enum ProfileStatus
{
    Active = 0,
    Inactive = 1,
    Suspended = 2,
    /// <summary>Reserved for student profiles after the academic year ends.</summary>
    Alumni = 3
}
