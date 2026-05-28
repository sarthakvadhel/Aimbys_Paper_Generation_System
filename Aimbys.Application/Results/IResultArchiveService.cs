namespace Aimbys.Application.Results;

/// <summary>
/// Generates compliance-safe immutable snapshots of an exam's published
/// results. Called by <see cref="IResultPublicationService"/> after a
/// successful batch publish; the snapshot lands in
/// <c>FileArea.Reports</c> (one row per exam) and the audit trail is
/// linked through the <c>ResultArchive</c> table.
///
/// <para>
/// The snapshot is a pure JSON document &mdash; small, diff-friendly, and
/// rehydrate-able into a re-run of the same exam without depending on
/// live tables. Future PDF / DOCX exports plug in alongside it as
/// additional <c>ResultArchive</c> rows.
/// </para>
/// </summary>
public interface IResultArchiveService
{
    /// <summary>
    /// Build and persist a JSON snapshot for the given exam. Idempotent:
    /// re-archiving an exam appends a new <c>ResultArchive</c> row and
    /// keeps the previous snapshots intact (audit-safe history).
    /// </summary>
    Task<ResultArchiveResult> CreateSnapshotAsync(
        Guid examId,
        string actorUserId,
        CancellationToken ct = default);
}

/// <summary>Outcome of a snapshot operation.</summary>
public sealed record ResultArchiveResult(
    bool Success,
    string? Error,
    Guid? ArchiveId,
    Guid? FileToken);
