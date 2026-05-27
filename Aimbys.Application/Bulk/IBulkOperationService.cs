using System.Security.Claims;

namespace Aimbys.Application.Bulk;

/// <summary>
/// Single sanctioned route for batch operations. Every method:
///
/// <list type="bullet">
///   <item>Enforces the actor is in role <c>InstituteAdmin</c> or
///         <c>SuperAdmin</c>; lower roles get
///         <see cref="UnauthorizedAccessException"/>.</item>
///   <item>Returns a <see cref="BulkOperationResult"/> rather than
///         throwing for per-row validation failures &mdash; partial
///         success is the norm, not the exception.</item>
///   <item>Persists in batches of 100 so a 50,000-row CSV doesn't
///         build a 50,000-element change-tracker.</item>
/// </list>
///
/// V1 ships full implementations of <see cref="ImportStudentsAsync"/>,
/// <see cref="ActivateDeactivateBulkAsync"/> and
/// <see cref="NotifyBulkAsync"/>; the remaining methods are surface
/// contracts whose persistence ships when their underlying
/// aggregates land.
/// </summary>
public interface IBulkOperationService
{
    /// <summary>
    /// Imports students from a UTF-8 CSV stream. Required header:
    /// <c>Email,DisplayName,AdmissionNumber,RollNumber,ClassBatchName</c>.
    /// AdmissionNumber and RollNumber are optional. The created
    /// IdentityUser is added to the <c>Student</c> role with a random
    /// password &mdash; institutes follow up with a password-reset
    /// invite (delivered via the notification pipeline).
    /// </summary>
    Task<BulkOperationResult> ImportStudentsAsync(
        Guid instituteId,
        System.IO.Stream csvStream,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk teacher assignments. <em>Stub</em> until the assignment
    /// entity lands; today returns
    /// <see cref="BulkOperationResult.Empty"/> after recording an
    /// audit row so the UI can wire to the contract.
    /// </summary>
    Task<BulkOperationResult> AssignTeachersAsync(
        Guid instituteId,
        IReadOnlyList<BulkTeacherAssignment> assignments,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default);

    /// <summary>Bulk exam scheduling. <em>Stub</em> until the Exam aggregate lands.</summary>
    Task<BulkOperationResult> ScheduleExamsBulkAsync(
        Guid instituteId,
        IReadOnlyList<BulkExamSchedule> schedules,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default);

    /// <summary>Bulk result publication. <em>Stub</em> until results land.</summary>
    Task<BulkOperationResult> PublishResultsBulkAsync(
        IReadOnlyList<Guid> examIds,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles <c>Status</c> on the given teacher / student profiles.
    /// <paramref name="active"/> = <c>true</c> sets <c>Active</c>;
    /// <c>false</c> sets <c>Inactive</c>. Per-row errors are reported
    /// for ids that aren't found.
    /// </summary>
    Task<BulkOperationResult> ActivateDeactivateBulkAsync(
        IReadOnlyList<Guid> profileIds,
        bool active,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default);

    /// <summary>Bulk paper assignment. <em>Stub</em> until the Paper aggregate lands.</summary>
    Task<BulkOperationResult> AssignPapersBulkAsync(
        IReadOnlyList<BulkPaperAssignment> assignments,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to each <paramref name="recipientUserIds"/>
    /// using the supplied template. The template is rendered as the
    /// notification body verbatim &mdash; templated substitutions land
    /// when the messaging chunk extends notifications.
    /// </summary>
    Task<BulkOperationResult> NotifyBulkAsync(
        IReadOnlyList<string> recipientUserIds,
        string title,
        string template,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default);
}
