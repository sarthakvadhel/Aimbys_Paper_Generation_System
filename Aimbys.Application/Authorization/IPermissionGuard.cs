using System.Security.Claims;

namespace Aimbys.Application.Authorization;

/// <summary>
/// Authorisation surface for the 13 teacher-permission flags.
///
/// This is the <em>only</em> sanctioned way controllers (and services) check
/// operational capabilities &mdash; no controller should read
/// <c>TeacherProfile</c> flags directly, and no controller should call
/// <c>User.IsInRole("Evaluator")</c> (those are not Identity roles in the
/// PARAKH model).
///
/// Implementations live in the Infrastructure layer; the
/// <c>[RequiresPermission(...)]</c> action filter consumes this interface.
/// </summary>
public interface IPermissionGuard
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="user"/> currently holds the
    /// named permission. The contract:
    ///
    /// <list type="bullet">
    ///   <item>Anonymous user &rArr; always <c>false</c>.</item>
    ///   <item>Unknown <paramref name="permissionKey"/> &rArr; always
    ///         <c>false</c> (fail-closed).</item>
    ///   <item><c>SuperAdmin</c> &rArr; <c>true</c> (system-level).</item>
    ///   <item><c>InstituteAdmin</c> &rArr; <c>true</c> (tenant-level; the
    ///         13 flags describe teacher capabilities, and an institute
    ///         admin owns those rights for their tenant).</item>
    ///   <item><c>Teacher</c> &rArr; the matching column on the active
    ///         <c>TeacherProfile</c>.</item>
    ///   <item><c>Student</c> &rArr; <c>false</c>.</item>
    /// </list>
    /// </summary>
    Task<bool> HasAsync(ClaimsPrincipal user, string permissionKey, CancellationToken cancellationToken = default);
}
