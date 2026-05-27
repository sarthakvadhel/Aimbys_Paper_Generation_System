namespace Aimbys.Web.Navigation;

/// <summary>
/// One titled group of links inside the role sidebar. The React shells
/// group by (<c>Main</c> | <c>Overview</c> | <c>Authoring</c> | …);
/// titles are copied verbatim so a future merge with the React tree is
/// mechanical.
/// </summary>
public sealed record RoleNavSection(
    string Title,
    IReadOnlyList<RoleNavLink> Links);
