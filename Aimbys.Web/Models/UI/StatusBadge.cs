namespace Aimbys.Web.Models.UI;

/// <summary>
/// Server-side renderer for the same coloured status-pill markup as
/// <c>Views/Shared/_StatusBadge.cshtml</c>. Useful when the badge is
/// embedded inside a <see cref="DataTableCell"/> whose
/// <c>IsHtml = true</c> path expects a finished HTML string &mdash;
/// calling <c>Html.Partial(...)</c> from a Razor code block trips
/// the MVC1000 analyzer (deadlock risk).
///
/// <para>
/// Both surfaces (this class and the partial) keep the same colour
/// mapping; the partial owns the canonical lookup and this class
/// mirrors it. Update both together if a new status arrives.
/// </para>
/// </summary>
public static class StatusBadge
{
    /// <summary>
    /// Returns the badge as an HTML string. The status is HTML-encoded
    /// so a hostile caller-supplied string can't inject markup; only
    /// the wrapping <c>&lt;span&gt;</c> + class names are emitted as
    /// raw HTML.
    /// </summary>
    public static string Render(string? status)
    {
        var key = (status ?? string.Empty).Trim().ToLowerInvariant();
        var classes = key switch
        {
            "approved" or "operational" or "success" or "active"      => "text-bg-success",
            "pending" or "warning"                                    => "text-bg-warning",
            "degraded" or "draft" or "archived"                       => "text-bg-secondary",
            "critical" or "failed" or "error" or "rejected"           => "text-bg-danger",
            "info" or "submitted"                                     => "text-bg-info",
            _                                                          => "text-bg-light"
        };

        var safeStatus = string.IsNullOrEmpty(status) ? "Unknown" : status;
        var capitalised = char.ToUpper(safeStatus[0]) + safeStatus[1..];
        var encoded = System.Net.WebUtility.HtmlEncode(capitalised);

        return $"<span class=\"badge rounded-pill text-uppercase {classes}\" "
             + $"style=\"font-size: 0.7rem; letter-spacing: 0.04em;\">{encoded}</span>";
    }
}
