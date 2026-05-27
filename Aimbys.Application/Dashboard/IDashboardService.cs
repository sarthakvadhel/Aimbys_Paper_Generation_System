namespace Aimbys.Application.Dashboard;

/// <summary>
/// Aggregation surface for the four role landing dashboards
/// (SuperAdmin / Institute / Teacher / Student). Each method returns
/// an immutable snapshot the controller renders directly &mdash; the
/// controller does no business logic itself.
///
/// <para>
/// Tenancy is the caller's responsibility: <see cref="GetSuperAdminSnapshotAsync"/>
/// is platform-wide, the institute / teacher / student methods are
/// scoped to a resolved id obtained from <c>IInstituteScope</c> /
/// <c>UserManager</c>. All implementations must call
/// <c>AsNoTracking()</c> on every query &mdash; dashboards are read-only.
/// </para>
///
/// <para>
/// The chart-feed endpoints on each <c>HomeController</c> reuse the
/// same snapshot's <c>Series</c> records; that keeps the KPI card and
/// the chart strictly in sync.
/// </para>
/// </summary>
public interface IDashboardService
{
    /// <summary>Platform-wide aggregates for the SuperAdmin dashboard.</summary>
    Task<SuperAdminDashboardSnapshot> GetSuperAdminSnapshotAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tenant-scoped aggregates for the Institute Admin dashboard. Pass
    /// the institute id resolved from <c>IInstituteScope</c>.
    /// </summary>
    Task<InstituteDashboardSnapshot> GetInstituteSnapshotAsync(
        Guid instituteId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Teacher-scoped aggregates &mdash; papers authored, pending
    /// evaluations assigned, class averages for exams the teacher
    /// authored. Pass the Identity user id of the signed-in teacher.
    /// </summary>
    Task<TeacherDashboardSnapshot> GetTeacherSnapshotAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Student-scoped aggregates &mdash; recent published results,
    /// running averages, the next scheduled exam for the student's
    /// class batch. Pass the Identity user id of the signed-in student.
    /// </summary>
    Task<StudentDashboardSnapshot> GetStudentSnapshotAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
