using System.Text;
using Aimbys.Application.Audit;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Aimbys.Web.Areas.SuperAdmin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.SuperAdmin.Controllers;

/// <summary>
/// SuperAdmin audit-log viewer (Chunk 35). Surfaces a paginated,
/// filterable table of <c>AuditLog</c> rows after running them
/// through <see cref="IAuditVisibilityService"/> (SuperAdmin
/// bypasses every visibility / masking layer, but the service is
/// still the single sanctioned read path so future masking rules
/// or compliance gates apply automatically).
/// </summary>
[Area("SuperAdmin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class AuditController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAuditVisibilityService _visibility;

    public AuditController(AppDbContext db, IAuditVisibilityService visibility)
    {
        _db = db;
        _visibility = visibility;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        AuditSeverity? severity,
        string? entityType,
        string? search,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 200);

        var query = _db.AuditLogs.AsNoTracking();
        if (severity.HasValue)
        {
            query = query.Where(a => a.Severity == severity.Value);
        }
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                a.Action.Contains(search)
                || a.EntityId.Contains(search)
                || (a.DetailsJson != null && a.DetailsJson.Contains(search)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var filtered = await _visibility.FilterAsync(rows, User, complianceMode: false, ct);

        var distinctEntityTypes = await _db.AuditLogs.AsNoTracking()
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);

        return View(new AuditIndexViewModel
        {
            Rows = filtered.VisibleRows,
            HiddenCount = filtered.HiddenCount,
            MaskedCount = filtered.MaskedCount,
            Severity = severity,
            EntityType = entityType,
            Search = search,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            EntityTypes = distinctEntityTypes
        });
    }

    [HttpGet]
    [EnableRateLimiting("export")]
    public async Task<IActionResult> Export(
        AuditSeverity? severity,
        string? entityType,
        string? search,
        CancellationToken ct = default)
    {
        var query = _db.AuditLogs.AsNoTracking();
        if (severity.HasValue)
        {
            query = query.Where(a => a.Severity == severity.Value);
        }
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(a => a.EntityType == entityType);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                a.Action.Contains(search)
                || a.EntityId.Contains(search));
        }

        // Bound the export at 10k rows so a runaway filter can't OOM
        // the host; the UI hint mentions this cap.
        var rows = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(10_000)
            .ToListAsync(ct);

        var filtered = await _visibility.FilterAsync(rows, User, complianceMode: false, ct);

        var sb = new StringBuilder();
        sb.AppendLine("OccurredAtUtc,Severity,Action,EntityType,EntityId,ActorUserId,IpAddress,Details");
        foreach (var r in filtered.VisibleRows)
        {
            sb.Append(r.OccurredAtUtc.ToString("o")).Append(',');
            sb.Append(r.Severity).Append(',');
            sb.Append(CsvEscape(r.Action)).Append(',');
            sb.Append(CsvEscape(r.EntityType)).Append(',');
            sb.Append(CsvEscape(r.EntityId)).Append(',');
            sb.Append(CsvEscape(r.ActorUserId ?? string.Empty)).Append(',');
            sb.Append(CsvEscape(r.IpAddress ?? string.Empty)).Append(',');
            sb.AppendLine(CsvEscape(r.DetailsJson ?? string.Empty));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    /// <summary>
    /// RFC 4180 CSV escaping: quote any value containing a comma,
    /// quote, or newline; double up internal quotes.
    /// </summary>
    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
