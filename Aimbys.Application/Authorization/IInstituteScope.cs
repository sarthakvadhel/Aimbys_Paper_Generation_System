using System.Security.Claims;

namespace Aimbys.Application.Authorization;

/// <summary>
/// Resolves the tenancy boundary for the current user. Used by every
/// controller (and service) that filters by institute. SuperAdmin returns
/// <c>null</c> &mdash; "no tenancy filter, see across institutes".
/// </summary>
public interface IInstituteScope
{
    /// <summary>
    /// Returns the InstituteId the user belongs to, or <c>null</c> if the
    /// user is a Super Admin (cross-tenant) or has no institute profile.
    /// Anonymous users return <c>null</c>; authorisation must be checked
    /// separately.
    /// </summary>
    Task<Guid?> GetCurrentInstituteIdAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
