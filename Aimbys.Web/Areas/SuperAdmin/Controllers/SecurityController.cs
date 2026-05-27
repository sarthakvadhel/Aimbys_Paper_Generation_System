using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Aimbys.Web.Areas.SuperAdmin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class SecurityController : Controller
{
    private static readonly string[] SecurityActionKeywords =
    {
        "Security",
        "Suspicious",
        "Violation",
        "Login",
        "AccessDenied",
        "Lockout",
        "GoLive"
    };

    private readonly AppDbContext _db;

    public SecurityController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var sinceUtc = DateTime.UtcNow.AddHours(-24);

        var securityQuery = _db.AuditLogs
            .AsNoTracking()
            .Where(a =>
                a.OccurredAtUtc >= sinceUtc &&
                (a.Severity != AuditSeverity.Information ||
                 SecurityActionKeywords.Any(k => EF.Functions.Like(a.Action, $"%{k}%"))));

        var recent = await securityQuery
            .OrderByDescending(a => a.OccurredAtUtc)
            .Take(50)
            .Select(a => new SecurityEventRowViewModel
            {
                OccurredAtUtc = a.OccurredAtUtc,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                ActorUserId = a.ActorUserId,
                IpAddress = a.IpAddress,
                Severity = a.Severity.ToString()
            })
            .ToListAsync(ct);

        var model = new SecurityMonitorViewModel
        {
            EventsLast24Hours = await securityQuery.CountAsync(ct),
            HighSeverityEventsLast24Hours = await securityQuery.CountAsync(a => a.Severity == AuditSeverity.Warning || a.Severity == AuditSeverity.Error, ct),
            DistinctActorsLast24Hours = await securityQuery
                .Where(a => a.ActorUserId != null)
                .Select(a => a.ActorUserId)
                .Distinct()
                .CountAsync(ct),
            RecentEvents = recent
        };

        return View(model);
    }
}
