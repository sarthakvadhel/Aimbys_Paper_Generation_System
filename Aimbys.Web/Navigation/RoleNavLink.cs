namespace Aimbys.Web.Navigation;

/// <summary>
/// One navigation entry inside a <see cref="RoleNavSection"/>. The
/// (<see cref="Area"/>, <see cref="Controller"/>, <see cref="Action"/>)
/// triple is what the view component compares against
/// <c>RouteData.Values</c> to compute the active highlight; it's also
/// what the link points at.
///
/// <para>
/// <see cref="IsImplemented"/> distinguishes "this surface ships in a
/// later chunk; render the row but disable the link" from "fully
/// wired-up". Disabled rows still render so reviewers can see the
/// roadmap at a glance, but tab-focus and pointer hits both no-op.
/// </para>
/// </summary>
/// <param name="Label">Visible label, copied verbatim from the React shell.</param>
/// <param name="IconKey">
/// Lookup key into <c>Aimbys.Web.Navigation.RoleNavIcons</c> so the partial
/// renders an inline SVG without depending on a webfont.
/// </param>
public sealed record RoleNavLink(
    string Label,
    string Action,
    string Controller,
    string Area,
    string IconKey,
    bool IsImplemented = false);
