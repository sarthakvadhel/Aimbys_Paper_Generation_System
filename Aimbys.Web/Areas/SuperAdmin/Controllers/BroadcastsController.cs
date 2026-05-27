using Aimbys.Application.Broadcasts;
using Aimbys.Infrastructure.Identity;
using Aimbys.Web.Areas.SuperAdmin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Areas.SuperAdmin.Controllers;

/// <summary>
/// SuperAdmin broadcast governance (Chunk 35). Lists existing
/// broadcasts, accepts new ones, and supports cancelling an active
/// broadcast. Body HTML is sanitised inside <see cref="IBroadcastService"/>
/// so the layout banner can safely <c>@@Html.Raw</c> the value.
/// </summary>
[Area("SuperAdmin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class BroadcastsController : Controller
{
    private readonly IBroadcastService _broadcasts;

    public BroadcastsController(IBroadcastService broadcasts)
    {
        _broadcasts = broadcasts;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct = default)
    {
        var rows = await _broadcasts.ListAsync(ct);
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new BroadcastCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BroadcastCreateViewModel model, CancellationToken ct = default)
    {
        if (model.EndsAtUtc <= model.StartsAtUtc)
        {
            ModelState.AddModelError(nameof(model.EndsAtUtc), "EndsAtUtc must be after StartsAtUtc.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _broadcasts.CreateAsync(
            new BroadcastCreateRequest(
                Subject: model.Subject,
                BodyHtml: model.BodyHtml,
                AudienceFilterJson: model.AudienceFilterJson,
                StartsAtUtc: model.StartsAtUtc,
                EndsAtUtc: model.EndsAtUtc),
            User,
            ct);

        TempData["StatusMessage"] = "Broadcast scheduled.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct = default)
    {
        var ok = await _broadcasts.CancelAsync(id, User, ct);
        TempData["StatusMessage"] = ok
            ? "Broadcast cancelled."
            : "Broadcast not found.";
        return RedirectToAction(nameof(Index));
    }
}
