using System.Security.Claims;
using Aimbys.Infrastructure.Identity;

namespace Aimbys.Web.Identity;

/// <summary>
/// Single sanctioned route for "where does this user land after sign-in?".
/// Used by both <c>HomeController.Index</c> (to bounce already-authenticated
/// visitors off the landing page) and <c>AccountController.Login</c> (after
/// a successful credential check).
///
/// <para>
/// Mapping:
/// </para>
/// <list type="bullet">
///   <item><see cref="Aimbys.Infrastructure.Identity.Roles.SuperAdmin"/> &rArr; <c>/SuperAdmin</c></item>
///   <item><see cref="Aimbys.Infrastructure.Identity.Roles.InstituteAdmin"/> &rArr; <c>/Institute</c></item>
///   <item><see cref="Aimbys.Infrastructure.Identity.Roles.Teacher"/> &rArr; <c>/Teacher</c></item>
///   <item><see cref="Aimbys.Infrastructure.Identity.Roles.Student"/> &rArr; <c>/Student</c></item>
///   <item>any other (or no) role &rArr; <c>/</c> with a "no access" flash</item>
/// </list>
///
/// <para>
/// SuperAdmin wins when a user is in multiple roles (a deliberate
/// elevation-rights default; platform support staff land on the
/// SuperAdmin console even if they also hold a teacher seat at one
/// institute).
/// </para>
/// </summary>
public static class RoleHomeRedirector
{
    /// <summary>Path returned when the user has no recognised role.</summary>
    public const string FallbackHome = "/";

    /// <summary>
    /// Resolves the home path for <paramref name="user"/>. Returns
    /// <see cref="FallbackHome"/> when the user is unauthenticated or
    /// in none of the four canonical roles.
    /// </summary>
    public static string GetHomePath(ClaimsPrincipal user)
    {
        if (user?.Identity?.IsAuthenticated != true) return FallbackHome;

        if (user.IsInRole(Roles.SuperAdmin))     return "/SuperAdmin";
        if (user.IsInRole(Roles.InstituteAdmin)) return "/Institute";
        if (user.IsInRole(Roles.Teacher))        return "/Teacher";
        if (user.IsInRole(Roles.Student))        return "/Student";

        return FallbackHome;
    }

    /// <summary>
    /// True when <paramref name="user"/> is in any of the four canonical
    /// roles. Used to distinguish "authenticated but no role" (which the
    /// landing page treats as a "no access" state) from "fully provisioned".
    /// </summary>
    public static bool HasCanonicalRole(ClaimsPrincipal user) =>
        user.IsInRole(Roles.SuperAdmin)
        || user.IsInRole(Roles.InstituteAdmin)
        || user.IsInRole(Roles.Teacher)
        || user.IsInRole(Roles.Student);
}
