using Aimbys.Application.Subscriptions;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Aimbys.Web.Areas.SuperAdmin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.SuperAdmin.Controllers;

/// <summary>
/// SuperAdmin controller for managing institute subscriptions and license tiers.
/// Provides listing, editing tier/expiry, and suspend/activate operations.
/// </summary>
[Area("SuperAdmin")]
[Authorize(Roles = Roles.SuperAdmin)]
public class SubscriptionsController : Controller
{
    private const int PageSize = 20;

    private readonly AppDbContext _db;
    private readonly ISubscriptionManagementService _subscriptions;

    public SubscriptionsController(AppDbContext db, ISubscriptionManagementService subscriptions)
    {
        _db = db;
        _subscriptions = subscriptions;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var query = _db.Institutes
            .IgnoreQueryFilters()
            .Where(i => !i.IsDeleted);

        var totalCount = await query.CountAsync();
        var totalPages = PageSize <= 0 ? 1 : (int)Math.Ceiling(totalCount / (double)PageSize);
        page = Math.Clamp(page, 1, Math.Max(1, totalPages));

        var rows = await query
            .OrderBy(i => i.Name)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(i => new SubscriptionRow
            {
                InstituteId = i.Id,
                Name = i.Name,
                Code = i.Code,
                Tier = i.LicenseTier,
                Status = i.SubscriptionStatus,
                ExpiresAtUtc = i.SubscriptionExpiresAtUtc
            })
            .ToListAsync();

        var model = new SubscriptionListViewModel
        {
            Rows = rows,
            Page = page,
            TotalCount = totalCount,
            PageSize = PageSize
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var institute = await _db.Institutes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (institute is null)
            return NotFound();

        var model = new SubscriptionEditViewModel
        {
            InstituteId = institute.Id,
            InstituteName = institute.Name,
            Tier = institute.LicenseTier,
            Status = institute.SubscriptionStatus,
            ExpiresAtUtc = institute.SubscriptionExpiresAtUtc
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, SubscriptionEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var institute = await _db.Institutes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (institute is null)
            return NotFound();

        // Apply tier change if different
        if (model.Tier != institute.LicenseTier)
        {
            await _subscriptions.ChangeTierAsync(id, model.Tier, User);
        }

        // Apply expiry change if different
        if (model.ExpiresAtUtc.HasValue && model.ExpiresAtUtc != institute.SubscriptionExpiresAtUtc)
        {
            await _subscriptions.ExtendAsync(id, model.ExpiresAtUtc.Value, User);
        }

        TempData["Success"] = "Subscription updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(Guid id, string? reason)
    {
        await _subscriptions.SuspendSubscriptionAsync(id, User, reason);
        TempData["Success"] = "Subscription suspended.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id)
    {
        await _subscriptions.ActivateSubscriptionAsync(id, User);
        TempData["Success"] = "Subscription activated.";
        return RedirectToAction(nameof(Index));
    }
}
