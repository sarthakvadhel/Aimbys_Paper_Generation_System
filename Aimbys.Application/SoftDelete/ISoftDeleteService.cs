using Aimbys.Domain.SoftDelete;
using System.Security.Claims;

namespace Aimbys.Application.SoftDelete;

/// <summary>
/// Single sanctioned route for soft-delete, restore and hard-delete
/// (purge). Every controller that "deletes" an entity must call into
/// this surface so audit rows, governance checks, and the legal-hold
/// enforcement happen in one place.
///
/// <list type="bullet">
///   <item><see cref="DeleteAsync"/> &mdash; flips
///         <see cref="ISoftDelete.IsDeleted"/> to <c>true</c> and writes
///         the deletion stamps. Idempotent.</item>
///   <item><see cref="RestoreAsync"/> &mdash; clears the deletion
///         stamps. Only works for <see cref="IRestoreable"/> entities.</item>
///   <item><see cref="PurgeAsync"/> &mdash; permanent hard-delete.
///         Restricted to SuperAdmin; the implementation must verify the
///         actor's role itself rather than relying on the controller.</item>
/// </list>
///
/// All three methods commit the unit-of-work (call <c>SaveChangesAsync</c>)
/// internally so a "delete" is observable in a single round-trip.
/// </summary>
public interface ISoftDeleteService
{
    /// <summary>
    /// Soft-deletes <paramref name="entity"/>. No-op if the entity is
    /// already soft-deleted. Always writes an
    /// <c>"Entity.SoftDeleted"</c> audit row.
    /// </summary>
    Task DeleteAsync<T>(T entity, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
        where T : class, ISoftDelete;

    /// <summary>
    /// Restores a soft-deleted <see cref="IRestoreable"/> entity.
    /// Throws <see cref="InvalidOperationException"/> if the entity is
    /// not currently soft-deleted; that's an integrity violation worth
    /// surfacing.
    /// </summary>
    Task RestoreAsync<T>(T entity, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
        where T : class, IRestoreable;

    /// <summary>
    /// Permanently removes <paramref name="entity"/> from the database.
    /// Refuses unless <paramref name="actor"/> is in role
    /// <c>SuperAdmin</c>.
    /// </summary>
    Task PurgeAsync<T>(T entity, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
        where T : class, ISoftDelete;
}
