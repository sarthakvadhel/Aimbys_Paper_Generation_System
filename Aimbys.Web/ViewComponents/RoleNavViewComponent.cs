using Aimbys.Web.Navigation;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.ViewComponents;

/// <summary>
/// Renders the section-grouped sidebar nav for the supplied area, with
/// the link matching <c>RouteData.Values["controller"]</c> +
/// <c>RouteData.Values["action"]</c> highlighted as active.
///
/// <para>
/// The catalogue lives in <see cref="RoleNavCatalog"/> as static data;
/// this view component is the seam between the route values exposed by
/// MVC and that data so views never need to import the catalogue
/// directly.
/// </para>
/// </summary>
public class RoleNavViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string area, string variant = "sidebar")
    {
        var sections = RoleNavCatalog.ForArea(area);

        var currentArea = (RouteData?.Values["area"] as string) ?? area;
        var currentController = RouteData?.Values["controller"] as string ?? "";
        var currentAction = RouteData?.Values["action"] as string ?? "";

        var model = new RoleNavViewModel(
            Area: area,
            AccentHex: RoleNavCatalog.GetAccentHex(area),
            Sections: sections,
            CurrentArea: currentArea,
            CurrentController: currentController,
            CurrentAction: currentAction);

        // Two view variants share the same model: a vertical sidebar
        // for lg+ screens and a horizontal bottom bar for mobile.
        return variant switch
        {
            "bottom" => View("Bottom", model),
            _        => View("Default", model)
        };
    }
}

/// <summary>
/// View model rendered by <see cref="RoleNavViewComponent"/>. Surfaces
/// the active-link comparator inputs so the view can highlight without
/// re-reading <c>RouteData</c>.
/// </summary>
public sealed record RoleNavViewModel(
    string Area,
    string AccentHex,
    IReadOnlyList<RoleNavSection> Sections,
    string CurrentArea,
    string CurrentController,
    string CurrentAction)
{
    public bool IsActive(RoleNavLink link) =>
        string.Equals(link.Area,       CurrentArea,       StringComparison.OrdinalIgnoreCase)
     && string.Equals(link.Controller, CurrentController, StringComparison.OrdinalIgnoreCase)
     && string.Equals(link.Action,     CurrentAction,     StringComparison.OrdinalIgnoreCase);
}
