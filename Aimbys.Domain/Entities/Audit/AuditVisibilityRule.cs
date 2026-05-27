namespace Aimbys.Domain.Entities.Audit;

/// <summary>
/// Per-action visibility specification. Drives
/// <c>IAuditVisibilityService</c>: when a non-platform user opens the
/// audit viewer, rows whose <c>Action</c> matches a rule are shown
/// only when the actor passes that rule's role / permission gate. Rows
/// with no matching rule fall through to the default (visible to all
/// authenticated viewers in scope).
///
/// <para>
/// The arrays of role / permission keys are stored as JSON strings so
/// adding a role doesn't require a schema migration. Empty arrays
/// mean "no constraint on this dimension".
/// </para>
///
/// <para>
/// <see cref="MaskFieldsJson"/> lists property names inside
/// <c>AuditLog.DetailsJson</c> that should be redacted before the row
/// is returned to a non-elevated viewer; the field list lives at the
/// rule level so multiple actions can share the same redaction policy
/// without duplication.
/// </para>
/// </summary>
public class AuditVisibilityRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Action pattern. Supports trailing-<c>*</c> wildcards
    /// (e.g. <c>"Workflow.*"</c>, <c>"Configuration.Set"</c>); a row
    /// matches the most-specific rule first, then falls back to a
    /// wildcard.
    /// </summary>
    public string ActionPattern { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of Identity role names that may see rows matching
    /// the pattern. Empty array means "any authenticated viewer".
    /// </summary>
    public string VisibleToRolesJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of permission keys
    /// (<see cref="Aimbys.Domain.Permissions.TeacherPermissions"/>)
    /// that grant visibility. Empty array means "no permission
    /// constraint".
    /// </summary>
    public string VisibleToPermissionsJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of property names inside
    /// <c>AuditLog.DetailsJson</c> to redact for non-elevated viewers.
    /// </summary>
    public string MaskFieldsJson { get; set; } = "[]";

    /// <summary>
    /// True for high-sensitivity actions that should be hidden unless
    /// the request supplies a "compliance mode" flag, asserted by the
    /// audit-viewer controller after the actor passes a second-factor
    /// check.
    /// </summary>
    public bool RequiresComplianceMode { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
