using Aimbys.Application.Authorization;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Middleware;

/// <summary>
/// Pipeline gate for the institute / teacher / student request
/// surfaces. Refuses requests when the owning institute's
/// <see cref="InstituteSubscriptionStatus"/> blocks access:
///
/// <list type="bullet">
///   <item><see cref="InstituteSubscriptionStatus.Suspended"/> &mdash; explicit pause; no access.</item>
///   <item><see cref="InstituteSubscriptionStatus.Expired"/> &mdash; subscription elapsed past grace; no access.</item>
///   <item><see cref="InstituteSubscriptionStatus.GracePeriod"/> with
///         <c>SubscriptionExpiresAtUtc</c> &lt; <c>UtcNow</c> &mdash;
///         grace-period window has elapsed; no access.</item>
/// </list>
///
/// <para>
/// SuperAdmin bypasses entirely so platform support can still
/// inspect a frozen tenant. Anonymous users are passed through; the
/// downstream <c>[Authorize]</c> redirects them to the login page,
/// which is the more useful UX than a generic suspension page.
/// </para>
/// </summary>
public sealed class SubscriptionEnforcementMiddleware
{
    /// <summary>
    /// Path the middleware redirects blocked users to. The matching
    /// action lives on <c>HomeController</c> and the view at
    /// <c>Views/Home/SubscriptionSuspended.cshtml</c>.
    /// </summary>
    public const string SuspendedPath = "/Home/SubscriptionSuspended";

    private static readonly string[] GuardedPrefixes =
    {
        "/Institute",
        "/Teacher",
        "/Student"
    };

    private readonly RequestDelegate _next;

    public SubscriptionEnforcementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IInstituteScope instituteScope,
        AppDbContext db)
    {
        // 1) Path filter. Inexpensive prefix scan keeps the middleware
        //    cheap on the hot path (Home, Files, Notifications, etc.).
        if (!IsGuardedPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            // Anonymous: let [Authorize] handle the redirect to login.
            await _next(context);
            return;
        }

        // SuperAdmin always bypasses; platform support must be able to
        // reach a frozen tenant.
        if (user.IsInRole(Roles.SuperAdmin))
        {
            await _next(context);
            return;
        }

        // The middleware must not break paths users land on after a
        // suspension redirect. SuspendedPath itself is not under a
        // guarded prefix so this is mostly defensive.
        if (context.Request.Path.StartsWithSegments(SuspendedPath, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var instituteId = await instituteScope.GetCurrentInstituteIdAsync(user, context.RequestAborted);
        if (instituteId is null)
        {
            // No institute resolves &mdash; can't enforce a tenant
            // policy, fall through. Tenancy enforcement happens in
            // the controllers/IInstituteScope itself.
            await _next(context);
            return;
        }

        // Cheap lookup: read just the two columns we need.
        // IgnoreQueryFilters() is intentional &mdash; a soft-deleted
        // institute is itself a "no access" condition we'd want to
        // catch here if it ever happened.
        var snapshot = await db.Institutes
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => i.Id == instituteId.Value)
            .Select(i => new
            {
                i.SubscriptionStatus,
                i.SubscriptionExpiresAtUtc,
                i.IsDeleted
            })
            .FirstOrDefaultAsync(context.RequestAborted);

        if (snapshot is null || snapshot.IsDeleted || IsBlocked(snapshot.SubscriptionStatus, snapshot.SubscriptionExpiresAtUtc))
        {
            // Avoid an infinite redirect: if the user is already on the
            // suspension path (defensive; SuspendedPath isn't under the
            // guarded prefixes anyway), just render in-place.
            context.Response.Redirect(SuspendedPath);
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Path-prefix gate. Case-insensitive so route casing differences
    /// between conventional controllers and area routes don't slip
    /// past.
    /// </summary>
    private static bool IsGuardedPath(PathString path)
    {
        foreach (var prefix in GuardedPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// True when the given subscription state should block access.
    /// <see cref="InstituteSubscriptionStatus.GracePeriod"/> blocks
    /// only when the explicit expiry timestamp has elapsed; without
    /// an expiry timestamp it remains permissive (typical "Trial"-
    /// style configuration).
    /// </summary>
    private static bool IsBlocked(InstituteSubscriptionStatus status, DateTime? expiresAtUtc)
    {
        return status switch
        {
            InstituteSubscriptionStatus.Suspended => true,
            InstituteSubscriptionStatus.Expired   => true,
            InstituteSubscriptionStatus.GracePeriod when expiresAtUtc is { } e && e < DateTime.UtcNow => true,
            _ => false
        };
    }
}

/// <summary>
/// Extension method that fluent-registers
/// <see cref="SubscriptionEnforcementMiddleware"/> in the request
/// pipeline. Keeps <c>Program.cs</c> readable.
/// </summary>
public static class SubscriptionEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UseSubscriptionEnforcement(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SubscriptionEnforcementMiddleware>();
    }
}
