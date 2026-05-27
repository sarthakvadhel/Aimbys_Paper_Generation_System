using System.Security.Claims;
using Aimbys.Application.Authorization;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Permissions;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Authorization;

/// <summary>
/// Default <see cref="IPermissionGuard"/> implementation: maps the user's
/// Identity role + (for teachers) their <c>TeacherProfile</c> flags to a
/// boolean answer. Fails closed for unknown permission keys, anonymous users,
/// inactive teacher profiles, and missing profile rows.
/// </summary>
public class PermissionGuard : IPermissionGuard
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public PermissionGuard(AppDbContext db, UserManager<IdentityUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<bool> HasAsync(
        ClaimsPrincipal user,
        string permissionKey,
        CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (!TeacherPermissions.IsKnown(permissionKey))
        {
            // Fail closed so a typo in [RequiresPermission("...")] never
            // accidentally grants access.
            return false;
        }

        // Admin tiers always pass; the 13 flags are below them in the model.
        if (user.IsInRole(Roles.SuperAdmin) || user.IsInRole(Roles.InstituteAdmin))
        {
            return true;
        }

        // Students never hold operational teacher permissions.
        if (!user.IsInRole(Roles.Teacher))
        {
            return false;
        }

        var userId = _userManager.GetUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        var profile = await _db.TeacherProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null || profile.Status != ProfileStatus.Active)
        {
            return false;
        }

        return TeacherPermissions.Has(profile, permissionKey);
    }
}
