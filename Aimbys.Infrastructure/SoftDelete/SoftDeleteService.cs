using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.SoftDelete;
using Aimbys.Domain.SoftDelete;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.SoftDelete;

/// <summary>
/// Default <see cref="ISoftDeleteService"/>. Wraps the soft-delete /
/// restore / purge operations behind a single transactional surface so
/// every "delete" in the platform writes a matching audit row and
/// passes through the same governance check.
///
/// <para>
/// Purge (hard-delete) is gated to the <c>SuperAdmin</c> role &mdash;
/// the implementation verifies the role itself rather than trusting
/// the caller, so a misconfigured controller can't accidentally
/// expose a purge endpoint to a less-privileged user.
/// </para>
/// </summary>
public sealed class SoftDeleteService : ISoftDeleteService
{
    private readonly AppDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly UserManager<IdentityUser> _userManager;

    public SoftDeleteService(
        AppDbContext db,
        IAuditWriter audit,
        UserManager<IdentityUser> userManager)
    {
        _db = db;
        _audit = audit;
        _userManager = userManager;
    }

    public async Task DeleteAsync<T>(T entity, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
        where T : class, ISoftDelete
    {
        if (entity.IsDeleted)
        {
            return; // idempotent
        }

        var actorUserId = _userManager.GetUserId(actor);
        var now = DateTime.UtcNow;

        entity.IsDeleted = true;
        entity.DeletedAtUtc = now;
        entity.DeletedByUserId = actorUserId;

        _db.Update(entity);

        await _audit.WriteAsync(
            "Entity.SoftDeleted",
            entityType: typeof(T).Name,
            entityId: ResolveEntityId(entity),
            actorUserId: actorUserId,
            detailsJson: JsonSerializer.Serialize(new { deletedAtUtc = now }),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync<T>(T entity, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
        where T : class, IRestoreable
    {
        if (!entity.IsDeleted)
        {
            throw new InvalidOperationException(
                $"Entity of type '{typeof(T).Name}' is not currently soft-deleted; nothing to restore.");
        }

        var actorUserId = _userManager.GetUserId(actor);
        var now = DateTime.UtcNow;

        entity.IsDeleted = false;
        entity.DeletedAtUtc = null;
        entity.DeletedByUserId = null;
        entity.RestoredAtUtc = now;

        _db.Update(entity);

        await _audit.WriteAsync(
            "Entity.Restored",
            entityType: typeof(T).Name,
            entityId: ResolveEntityId(entity),
            actorUserId: actorUserId,
            detailsJson: JsonSerializer.Serialize(new { restoredAtUtc = now }),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task PurgeAsync<T>(T entity, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
        where T : class, ISoftDelete
    {
        // Hard-delete is reserved for SuperAdmin governance; verifying
        // the role here means a misrouted controller can't widen the
        // contract by accident.
        if (!actor.IsInRole(Roles.SuperAdmin))
        {
            throw new UnauthorizedAccessException(
                "Purge (hard-delete) is restricted to SuperAdmin.");
        }

        var actorUserId = _userManager.GetUserId(actor);
        var entityId = ResolveEntityId(entity);

        _db.Remove(entity);

        await _audit.WriteAsync(
            "Entity.Purged",
            entityType: typeof(T).Name,
            entityId: entityId,
            actorUserId: actorUserId,
            detailsJson: JsonSerializer.Serialize(new { purgedAtUtc = DateTime.UtcNow }),
            severity: Aimbys.Domain.Enums.AuditSeverity.Warning,
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves the primary-key value of <paramref name="entity"/> as a
    /// string for the audit row. Falls back to a placeholder rather
    /// than throwing &mdash; an audit row with a missing id is still
    /// more useful than a swallowed delete.
    /// </summary>
    private string ResolveEntityId(object entity)
    {
        var entry = _db.Entry(entity);
        var pk = entry.Metadata.FindPrimaryKey();
        if (pk is null)
        {
            return "(unknown)";
        }

        var values = pk.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "")
            .ToArray();

        return values.Length == 1 ? values[0] : string.Join(":", values);
    }
}
