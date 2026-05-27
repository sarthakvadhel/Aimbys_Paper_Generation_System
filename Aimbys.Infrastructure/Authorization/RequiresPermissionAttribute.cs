using Aimbys.Application.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Aimbys.Infrastructure.Authorization;

/// <summary>
/// Action / controller filter that gates a request behind a single permission
/// key resolved by <see cref="IPermissionGuard"/>. Apply at the controller
/// level for blanket protection or at the action level for per-action gates.
/// Multiple instances are AND-ed (every key must pass).
///
/// <para>
/// Anonymous users receive a <c>401 Challenge</c> (which the cookie scheme
/// turns into a redirect to <c>/Account/Login</c>); authenticated users
/// without the permission receive a <c>403 Forbid</c> (which the cookie
/// scheme turns into a redirect to <c>/Account/AccessDenied</c>).
/// </para>
///
/// This is the <em>only</em> sanctioned way to check teacher permissions in
/// MVC pipelines. Controllers must not read <c>TeacherProfile</c> flags
/// directly.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class RequiresPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string PermissionKey { get; }

    public RequiresPermissionAttribute(string permissionKey)
    {
        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            throw new ArgumentException("Permission key is required.", nameof(permissionKey));
        }
        PermissionKey = permissionKey;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var guard = context.HttpContext.RequestServices.GetRequiredService<IPermissionGuard>();

        var allowed = await guard.HasAsync(
            user,
            PermissionKey,
            context.HttpContext.RequestAborted);

        if (!allowed)
        {
            context.Result = new ForbidResult();
        }
    }
}
