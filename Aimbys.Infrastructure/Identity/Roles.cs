namespace Aimbys.Infrastructure.Identity;

/// <summary>
/// The four canonical Identity roles in the PARAKH model. Use these
/// constants everywhere instead of magic strings so a typo is a compile
/// error.
///
/// <para>
/// Operational capabilities like <em>Evaluator</em>, <em>Moderator</em>,
/// <em>Reviewer</em>, <em>Proctor</em> are <strong>not</strong> Identity
/// roles. They are dynamic per-teacher permission flags, declared in
/// <c>Aimbys.Domain.Permissions.TeacherPermissions</c> and checked through
/// <c>IPermissionGuard</c> / <c>[RequiresPermission(...)]</c>.
/// </para>
/// </summary>
public static class Roles
{
    public const string SuperAdmin     = nameof(SuperAdmin);
    public const string InstituteAdmin = nameof(InstituteAdmin);
    public const string Teacher        = nameof(Teacher);
    public const string Student        = nameof(Student);

    /// <summary>The full set of roles seeded at application startup.</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        SuperAdmin,
        InstituteAdmin,
        Teacher,
        Student
    };
}
