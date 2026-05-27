namespace Aimbys.Domain.SoftDelete;

/// <summary>
/// Marker contract for soft-deletable entities whose deletion is
/// reversible. Adds a <see cref="RestoredAtUtc"/> stamp so the audit
/// trail can distinguish "never deleted" from "deleted then restored".
///
/// Not every <see cref="ISoftDelete"/> entity is restoreable &mdash;
/// some lifecycles (e.g. evaluation submissions) treat soft-delete as
/// terminal even though the row is preserved for compliance.
/// </summary>
public interface IRestoreable : ISoftDelete
{
    /// <summary>UTC instant of the most recent restore. Null on first delete.</summary>
    DateTime? RestoredAtUtc { get; set; }
}
