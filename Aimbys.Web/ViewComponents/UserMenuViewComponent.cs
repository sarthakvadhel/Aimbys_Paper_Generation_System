using Aimbys.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.ViewComponents;

/// <summary>
/// Renders the user-card chunk that lives at the bottom of the role
/// sidebar: avatar initials, display name (email), role badge, and the
/// sign-out form. Centralising it as a view component keeps the layout
/// partial declarative.
/// </summary>
public class UserMenuViewComponent : ViewComponent
{
    private readonly UserManager<IdentityUser> _userManager;

    public UserMenuViewComponent(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync(string area)
    {
        var principal = UserClaimsPrincipal;
        var displayName = principal?.Identity?.Name ?? string.Empty;
        var initials = ComputeInitials(displayName);

        // Resolve the canonical role label from the area key to keep
        // the badge consistent with the sidebar header.
        var roleBadge = ResolveRoleBadge(area);

        // The view component is invoked from layout pages, so a
        // database round-trip per request is fine here — sign-in is
        // already cookie-cached. We keep the call defensive: an
        // unauthenticated invocation just renders the empty state.
        var hasUser = false;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(principal);
            hasUser = user is not null;
        }

        return View("Default", new UserMenuViewModel(
            DisplayName: displayName,
            Initials: initials,
            RoleBadge: roleBadge,
            IsAuthenticated: hasUser));
    }

    private static string ComputeInitials(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName)) return "U";

        // Prefer the part before '@' for emails so 'jane.doe@example.com'
        // renders 'JD' rather than 'JE'.
        var local = displayName.Split('@', 2)[0];
        var parts = local.Split(new[] { '.', '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return char.ToUpperInvariant(local[0]).ToString();

        var first = char.ToUpperInvariant(parts[0][0]);
        if (parts.Length == 1) return first.ToString();

        var second = char.ToUpperInvariant(parts[^1][0]);
        return $"{first}{second}";
    }

    private static string ResolveRoleBadge(string area) => area switch
    {
        "SuperAdmin" => Roles.SuperAdmin,
        "Institute"  => Roles.InstituteAdmin,
        "Teacher"    => Roles.Teacher,
        "Student"    => Roles.Student,
        _            => string.Empty
    };
}

public sealed record UserMenuViewModel(
    string DisplayName,
    string Initials,
    string RoleBadge,
    bool IsAuthenticated);
