using Aimbys.Domain.Enums;

namespace Aimbys.Application.Audit;

/// <summary>
/// Single sanctioned surface for writing <c>AuditLog</c> rows. Workflow
/// transitions, security events, and any other state-changing action
/// must funnel through this interface so the call site remains
/// declarative and the persistence/transactional concerns are owned by
/// the implementation.
///
/// Implementations are expected to:
///
/// <list type="bullet">
///   <item>Persist via the same <c>DbContext</c> the caller is using
///         (so the audit row commits with the business change).</item>
///   <item>Record IP / actor metadata when an
///         <c>HttpContext</c>-backed request is in scope.</item>
/// </list>
///
/// Existing controllers that today call
/// <c>_db.AuditLogs.Add(new AuditLog{...})</c> are kept untouched for
/// now; future chunks should migrate them onto this interface.
/// </summary>
public interface IAuditWriter
{
    /// <summary>
    /// Writes an audit row. Does NOT call <c>SaveChanges</c> &mdash; the
    /// caller's unit-of-work is responsible for committing.
    /// </summary>
    /// <param name="action">Action verb, e.g. <c>"Workflow.Transitioned"</c>.</param>
    /// <param name="entityType">Domain entity type, e.g. <c>"Paper"</c>.</param>
    /// <param name="entityId">Stringified primary key of the entity.</param>
    /// <param name="actorUserId">Identity user id, or <c>null</c> for system actions.</param>
    /// <param name="detailsJson">Optional JSON payload describing the change.</param>
    /// <param name="severity">Severity tier; defaults to Information.</param>
    Task WriteAsync(
        string action,
        string entityType,
        string entityId,
        string? actorUserId,
        string? detailsJson = null,
        AuditSeverity severity = AuditSeverity.Information,
        CancellationToken cancellationToken = default);
}
