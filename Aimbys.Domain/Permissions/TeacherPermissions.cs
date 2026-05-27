using Aimbys.Domain.Entities;

namespace Aimbys.Domain.Permissions;

/// <summary>
/// Canonical operational-capability keys assignable to a
/// <see cref="TeacherProfile"/>. Use these constants everywhere instead of
/// magic strings so a typo is a compile error.
///
/// In the PARAKH model, <c>Evaluator</c>, <c>Moderator</c>, <c>Reviewer</c>,
/// <c>Proctor</c> are <em>not</em> Identity roles. They are dynamic
/// permissions assigned by an Institute Admin (Chunk 17) and checked by
/// <c>IPermissionGuard</c> / <c>[RequiresPermission(...)]</c>.
/// </summary>
public static class TeacherPermissions
{
    public const string CanCreateQuestions        = nameof(CanCreateQuestions);
    public const string CanGeneratePaper          = nameof(CanGeneratePaper);
    public const string CanManageBlueprints       = nameof(CanManageBlueprints);
    public const string CanEvaluate               = nameof(CanEvaluate);
    public const string CanModerate               = nameof(CanModerate);
    public const string CanPublishResults         = nameof(CanPublishResults);
    public const string CanScheduleExam           = nameof(CanScheduleExam);
    public const string CanReviewCodingQuestions  = nameof(CanReviewCodingQuestions);
    public const string CanManageQuestionBank     = nameof(CanManageQuestionBank);
    public const string CanAssignEvaluators       = nameof(CanAssignEvaluators);
    public const string CanManageAnalytics        = nameof(CanManageAnalytics);
    public const string CanApproveQuestions       = nameof(CanApproveQuestions);
    public const string CanProctor                = nameof(CanProctor);

    /// <summary>The full set of permission keys, in stable display order.</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        CanCreateQuestions,
        CanGeneratePaper,
        CanManageBlueprints,
        CanEvaluate,
        CanModerate,
        CanPublishResults,
        CanScheduleExam,
        CanReviewCodingQuestions,
        CanManageQuestionBank,
        CanAssignEvaluators,
        CanManageAnalytics,
        CanApproveQuestions,
        CanProctor
    };

    private static readonly IReadOnlyDictionary<string, Func<TeacherProfile, bool>> Accessors =
        new Dictionary<string, Func<TeacherProfile, bool>>(StringComparer.Ordinal)
        {
            [CanCreateQuestions]       = p => p.CanCreateQuestions,
            [CanGeneratePaper]         = p => p.CanGeneratePaper,
            [CanManageBlueprints]      = p => p.CanManageBlueprints,
            [CanEvaluate]              = p => p.CanEvaluate,
            [CanModerate]              = p => p.CanModerate,
            [CanPublishResults]        = p => p.CanPublishResults,
            [CanScheduleExam]          = p => p.CanScheduleExam,
            [CanReviewCodingQuestions] = p => p.CanReviewCodingQuestions,
            [CanManageQuestionBank]    = p => p.CanManageQuestionBank,
            [CanAssignEvaluators]      = p => p.CanAssignEvaluators,
            [CanManageAnalytics]       = p => p.CanManageAnalytics,
            [CanApproveQuestions]      = p => p.CanApproveQuestions,
            [CanProctor]               = p => p.CanProctor
        };

    /// <summary>True if the given key matches a permission this build knows about.</summary>
    public static bool IsKnown(string permissionKey) =>
        permissionKey is not null && Accessors.ContainsKey(permissionKey);

    /// <summary>
    /// Reads the named permission flag from a <see cref="TeacherProfile"/>.
    /// Returns false for unknown keys so a stale call site never accidentally
    /// grants access.
    /// </summary>
    public static bool Has(TeacherProfile profile, string permissionKey)
    {
        if (profile is null) return false;
        return Accessors.TryGetValue(permissionKey, out var read) && read(profile);
    }
}
