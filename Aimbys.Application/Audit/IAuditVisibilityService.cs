using System.Security.Claims;
using Aimbys.Domain.Entities;

namespace Aimbys.Application.Audit;

/// <summary>
/// Single sanctioned route for filtering audit-log rows before they
/// land in any read surface (admin viewer, exports, compliance
/// reports). Applies three layers in order:
///
/// <list type="number">
///   <item><b>Visibility</b> &mdash; drops rows whose action matches
///         a <c>AuditVisibilityRule</c> the actor doesn't satisfy.</item>
///   <item><b>Compliance gate</b> &mdash; rules with
///         <c>RequiresComplianceMode</c> = <c>true</c> are dropped
///         unless the caller passes <c>complianceMode = true</c>
///         (typically asserted after a 2FA step).</item>
///   <item><b>Field masking</b> &mdash; replaces sensitive properties
///         inside <c>DetailsJson</c> with <c>"***"</c> before the
///         row is returned.</item>
/// </list>
///
/// <para>
/// SuperAdmin bypasses all three layers. The viewer must still
/// supply a <see cref="ClaimsPrincipal"/> so the implementation can
/// audit who saw what.
/// </para>
/// </summary>
public interface IAuditVisibilityService
{
    /// <summary>
    /// Filters <paramref name="rows"/> for the given actor. Pure with
    /// respect to its inputs &mdash; never mutates them; returns a
    /// new result with cloned rows when masking is required.
    /// </summary>
    Task<AuditVisibilityResult> FilterAsync(
        IReadOnlyList<AuditLog> rows,
        ClaimsPrincipal actor,
        bool complianceMode = false,
        CancellationToken cancellationToken = default);
}
