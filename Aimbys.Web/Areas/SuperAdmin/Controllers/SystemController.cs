using System.Diagnostics;
using Aimbys.Application.SystemHealth;
using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Aimbys.Web.Areas.SuperAdmin.Controllers;

/// <summary>
/// SuperAdmin "System Health" surface (Chunk 35). Reads the in-memory
/// <see cref="IRequestMetricsCollector"/> snapshot, lists the registered
/// hosted services, and reports basic process metrics. <see cref="Metrics"/>
/// returns Chart.js-shaped JSON so the view's canvas can fetch and render
/// the per-minute series without a server round-trip on every refresh.
/// </summary>
[Area("SuperAdmin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class SystemController : Controller
{
    private readonly IRequestMetricsCollector _metrics;
    private readonly IEnumerable<IHostedService> _hostedServices;

    public SystemController(
        IRequestMetricsCollector metrics,
        IEnumerable<IHostedService> hostedServices)
    {
        _metrics = metrics;
        _hostedServices = hostedServices;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var snap = _metrics.GetSnapshot();

        ViewBag.HostedServices = _hostedServices
            .Select(s => s.GetType().Name)
            .OrderBy(n => n)
            .ToList();
        ViewBag.TotalRequests = snap.TotalRequests;
        ViewBag.ErrorRequests = snap.ErrorRequests;
        ViewBag.AvgResponseMs = Math.Round(snap.AverageResponseTimeMs, 1);
        ViewBag.ProcessMemoryMb = GC.GetTotalMemory(false) / 1024 / 1024;
        ViewBag.UptimeMinutes = Math.Round(
            (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMinutes,
            1);

        return View();
    }

    [HttpGet]
    public IActionResult Metrics()
    {
        var snap = _metrics.GetSnapshot();
        var labels = snap.Buckets.Select(b => b.BucketUtc.ToString("HH:mm")).ToList();
        return Json(new
        {
            labels,
            datasets = new object[]
            {
                new { label = "Requests/min", data = snap.Buckets.Select(b => b.RequestCount).ToList() },
                new { label = "Errors/min",   data = snap.Buckets.Select(b => b.ErrorCount).ToList() },
                new { label = "Avg ms",       data = snap.Buckets.Select(b => Math.Round(b.AverageResponseTimeMs, 1)).ToList() }
            }
        });
    }
}
