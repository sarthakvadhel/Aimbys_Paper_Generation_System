using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Middleware;

/// <summary>
/// Pipeline gate that intercepts authenticated <c>InstituteAdmin</c> users
/// whose <c>UserPasswordPolicy.MustChangePassword</c> flag is <c>true</c>
/// and redirects them to <c>/Account/SetFirstPassword</c>.
///
/// This ensures an Institute Admin who has just been created by the Super
/// Admin cannot access any protected surface until they have replaced
/// their auto-generated 8-digit initial password with a personal one.
///
/// Exclusions (allowed through without check):
/// <list type="bullet">
///   <item><c>/Account/*</c> — login, logout, set-password itself.</item>
///   <item><c>/files/*</c> — static asset serving.</item>
///   <item>Any unauthenticated request (let <c>[Authorize]</c> handle it).</item>
///   <item>Roles other than <c>InstituteAdmin</c>.</item>
/// </list>
/// </summary>
public sealed class ForcePasswordResetMiddleware
{
    public const string SetFirstPasswordPath = "/Account/SetFirstPassword";

    private static readonly string[] AllowedPrefixes =
    {
        "/Account",
        "/files",
        "/health"
    };

    private readonly RequestDelegate _next;

    public ForcePasswordResetMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        // 1. Let unauthenticated requests pass; [Authorize] handles them.
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // 2. Only apply to InstituteAdmins.
        if (!context.User.IsInRole(Roles.InstituteAdmin))
        {
            await _next(context);
            return;
        }

        // 3. Allow the Account area and asset paths to avoid redirect loops.
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsAllowedPath(path))
        {
            await _next(context);
            return;
        }

        // 4. Check the flag. Use AsNoTracking — read-only hot path.
        var userId = context.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            var policy = await db.UserPasswordPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (policy?.MustChangePassword == true)
            {
                context.Response.Redirect(SetFirstPasswordPath);
                return;
            }
        }

        await _next(context);
    }

    private static bool IsAllowedPath(string path)
    {
        foreach (var prefix in AllowedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
