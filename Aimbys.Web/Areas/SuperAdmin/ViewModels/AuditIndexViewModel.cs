using System.ComponentModel.DataAnnotations;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;

namespace Aimbys.Web.Areas.SuperAdmin.ViewModels;

/// <summary>
/// View model for the SuperAdmin audit-log viewer. Carries the
/// already-filtered rows produced by <c>IAuditVisibilityService</c>
/// plus enough context to re-render the filter form and pagination
/// controls without re-executing the underlying query.
/// </summary>
public sealed class AuditIndexViewModel
{
    public IReadOnlyList<AuditLog> Rows { get; set; } = Array.Empty<AuditLog>();

    /// <summary>Rows excluded from the current page by visibility rules.</summary>
    public int HiddenCount { get; set; }

    /// <summary>Rows on the current page where one or more fields were redacted.</summary>
    public int MaskedCount { get; set; }

    public AuditSeverity? Severity { get; set; }

    public string? EntityType { get; set; }

    public string? Search { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public int TotalCount { get; set; }

    public List<string> EntityTypes { get; set; } = new();

    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// Form input for creating a new broadcast. <c>BodyHtml</c> is sanitized
/// server-side at create time; the textarea ships raw HTML for V1.
/// </summary>
public sealed class BroadcastCreateViewModel
{
    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string BodyHtml { get; set; } = string.Empty;

    public string AudienceFilterJson { get; set; } = "{\"roles\":[\"all\"]}";

    public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime EndsAtUtc { get; set; } = DateTime.UtcNow.AddDays(7);
}
