using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Permissions;

/// <summary>
/// Maps a <see cref="FileArea"/> to the permission key (from
/// <see cref="TeacherPermissions"/>) that an authenticated user must hold to
/// download files in that area, in addition to passing the tenancy check.
///
/// <para>
/// The uploader can always read their own file regardless of this map &mdash;
/// the controller short-circuits before calling <see cref="GetReadPermissionFor"/>.
/// SuperAdmin / InstituteAdmin always pass via <c>IPermissionGuard</c>.
/// </para>
///
/// <para>
/// Returning <c>null</c> means "any authenticated user inside the tenancy
/// may read"; finer-grained ownership checks (e.g. a student reading their
/// own certificate) live in the consuming chunk's controller.
/// </para>
/// </summary>
public static class FileAreaPolicies
{
    /// <summary>
    /// Permission key required to read files in the given area, or
    /// <c>null</c> if any authenticated tenant member may read.
    /// </summary>
    public static string? GetReadPermissionFor(FileArea area) => area switch
    {
        FileArea.Questions    => TeacherPermissions.CanManageQuestionBank,
        FileArea.Papers       => TeacherPermissions.CanGeneratePaper,
        FileArea.Answers      => TeacherPermissions.CanEvaluate,
        FileArea.Coding       => TeacherPermissions.CanReviewCodingQuestions,
        FileArea.Reports      => TeacherPermissions.CanManageAnalytics,
        FileArea.Exams        => TeacherPermissions.CanScheduleExam,
        FileArea.Certificates => null, // any tenant member; ownership checked by caller
        FileArea.Temp         => null, // uploader-only; controller enforces
        _ => null
    };
}
