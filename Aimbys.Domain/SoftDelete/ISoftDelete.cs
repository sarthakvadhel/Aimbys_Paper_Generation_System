namespace Aimbys.Domain.SoftDelete;

/// <summary>
/// Marker contract for entities whose deletion is performed by setting a
/// flag rather than removing the row. Every implementer is automatically
/// fitted with a global EF Core query filter
/// (<c>e =&gt; !e.IsDeleted</c>) at model-build time, so default
/// reads return only live rows. Code that genuinely needs to see
/// soft-deleted rows must call <c>.IgnoreQueryFilters()</c> on the
/// query &mdash; it's a one-way escape hatch reserved for super-admin
/// governance and the retention/restore workflows.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// True once the entity is soft-deleted. Mutated only by
    /// <c>ISoftDeleteService</c>.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>UTC instant the soft-delete occurred (null while live).</summary>
    DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Identity user id of the actor that performed the soft-delete.
    /// Null while live; null also for system-driven deletes (e.g. the
    /// retention job, which writes an audit row instead).
    /// </summary>
    string? DeletedByUserId { get; set; }
}
