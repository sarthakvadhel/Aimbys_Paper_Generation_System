using Aimbys.Application.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.ViewComponents;

/// <summary>
/// Renders the bell badge showing the current user's unread notification
/// count. Invoked from the topbar partial in every role layout.
/// </summary>
public class NotificationsViewComponent : ViewComponent
{
    private readonly INotificationService _notifications;
    private readonly UserManager<IdentityUser> _userManager;

    public NotificationsViewComponent(
        INotificationService notifications,
        UserManager<IdentityUser> userManager)
    {
        _notifications = notifications;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal?.Identity?.IsAuthenticated != true)
        {
            return View("Default", 0);
        }

        var userId = _userManager.GetUserId(UserClaimsPrincipal);
        if (string.IsNullOrEmpty(userId))
        {
            return View("Default", 0);
        }

        var count = await _notifications.GetUnreadCountAsync(userId);
        return View("Default", count);
    }
}
