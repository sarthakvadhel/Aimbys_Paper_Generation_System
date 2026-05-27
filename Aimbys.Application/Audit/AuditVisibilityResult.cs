using Aimbys.Domain.Entities;

namespace Aimbys.Application.Audit;

/// <summary>
/// Result of <c>IAuditVisibilityService.FilterAsync</c>. Carries the
/// rows the actor is allowed to see, with sensitive fields already
/// masked, plus a count of rows that were dropped by visibility
/// rules. Surfacing the dropped count lets the audit viewer render
/// "37 entries hidden by visibility policy" without re-running the
/// query at higher privilege.
/// </summary>
/// <param name="VisibleRows">
/// Rows the actor may see. Each row is a copy of the original
/// <see cref="AuditLog"/> with masked fields replaced by
/// <c>"***"</c> in the cloned <c>DetailsJson</c>.
/// </param>
/// <param name="HiddenCount">Number of rows excluded by visibility rules.</param>
/// <param name="MaskedCount">Number of rows where one or more fields were redacted.</param>
public sealed record AuditVisibilityResult(
    IReadOnlyList<AuditLog> VisibleRows,
    int HiddenCount,
    int MaskedCount);
