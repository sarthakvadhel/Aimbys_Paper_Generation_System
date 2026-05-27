using Aimbys.Application.Authorization;
using Aimbys.Application.Storage;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Permissions;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Aimbys.Web.Controllers;

/// <summary>
/// Token-based authorised file downloads. The only sanctioned way to read
/// any file persisted by <see cref="IFileStorageService"/>.
///
/// <para>
/// The controller is deliberately thin: it resolves the token, runs the
/// three required checks (tenancy / permission / uploader-self), writes an
/// <c>AuditLog</c> row, and streams the file. No business logic.
/// </para>
/// </summary>
[Authorize]
[Route("files")]
public class FilesController : Controller
{
    private readonly IFileStorageService _storage;
    private readonly IPermissionGuard _permissionGuard;
    private readonly IInstituteScope _instituteScope;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AppDbContext _db;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IFileStorageService storage,
        IPermissionGuard permissionGuard,
        IInstituteScope instituteScope,
        UserManager<IdentityUser> userManager,
        AppDbContext db,
        ILogger<FilesController> logger)
    {
        _storage = storage;
        _permissionGuard = permissionGuard;
        _instituteScope = instituteScope;
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    [HttpGet("{token:guid}")]
    public async Task<IActionResult> Download(Guid token, CancellationToken cancellationToken)
    {
        var asset = await _storage.GetAsync(token, cancellationToken);
        if (asset is null)
        {
            return NotFound();
        }

        var currentUserId = _userManager.GetUserId(User);
        var isUploader = !string.IsNullOrEmpty(currentUserId)
            && string.Equals(currentUserId, asset.UploadedByUserId, StringComparison.Ordinal);
        var isSuperAdmin = User.IsInRole(Roles.SuperAdmin);

        // 1) Tenancy. Uploaders and SuperAdmin bypass; everyone else must
        //    be in the same institute as the asset (404, not 403, to avoid
        //    leaking the existence of cross-tenant files).
        if (!isUploader && !isSuperAdmin && asset.InstituteId.HasValue)
        {
            var scope = await _instituteScope.GetCurrentInstituteIdAsync(User, cancellationToken);
            if (scope != asset.InstituteId)
            {
                return NotFound();
            }
        }

        // 2) Permission. The uploader can always read their own file. For
        //    everyone else, the file area's policy decides whether a
        //    permission key is required.
        if (!isUploader)
        {
            var requiredPermission = FileAreaPolicies.GetReadPermissionFor(asset.Area);
            if (requiredPermission is not null)
            {
                var allowed = await _permissionGuard.HasAsync(User, requiredPermission, cancellationToken);
                if (!allowed)
                {
                    return Forbid();
                }
            }
        }

        // 3) Open + audit + stream. A failed Open after this point means
        //    the metadata exists but the bytes are gone — surface as 404
        //    rather than 500 so retries are well-defined.
        var open = await _storage.OpenReadAsync(token, cancellationToken);
        if (open is null)
        {
            return NotFound();
        }

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "File.Read",
            EntityType = nameof(FileAsset),
            EntityId = asset.Id.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "File served. Token={Token} ActorUserId={ActorUserId} SizeBytes={SizeBytes}",
            token, currentUserId, asset.SizeBytes);

        return File(
            open.Content,
            asset.ContentType,
            fileDownloadName: asset.OriginalFileName);
    }
}
