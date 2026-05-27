using Aimbys.Application.Notifications;
using Aimbys.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Controllers;

/// <summary>
/// Activity feed / notifications page. Paginated, filterable by severity,
/// mark-as-read. Accessible from every role via <c>/Notifications</c>.
/// </summary>
[Authorize]
public class NotificationsController : Controller
{
    private const int PageSize = 20;

    private readonly INotificationService _notifications;
    private readonly UserManager<IdentityUser> _userManager;

    public NotificationsController(
        INotificationService notifications,
        UserManager<IdentityUser> userManager)
    {
        _notifications = notifications;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        NotificationSeverity? severity = null,
        int page = 1,
        CancellationToken ct = default)
    {
        var userId = _userManager.GetUserId(User)!;
        var items = await _notifications.GetPageAsync(userId, severity, page, PageSize, ct);
        var unread = await _notifications.GetUnreadCountAsync(userId, ct);

        ViewData["UnreadCount"] = unread;
        ViewData["CurrentPage"] = page;
        ViewData["SeverityFilter"] = severity;

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct = default)
    {
        var userId = _userManager.GetUserId(User)!;
        await _notifications.MarkReadAsync(id, userId, ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
    {
        var userId = _userManager.GetUserId(User)!;
        await _notifications.MarkAllReadAsync(userId, ct);
        return RedirectToAction(nameof(Index));
    }
}
