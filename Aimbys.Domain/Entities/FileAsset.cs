using Aimbys.Domain.Enums;
using Aimbys.Domain.SoftDelete;

namespace Aimbys.Domain.Entities;

/// <summary>
/// Audit row for a single uploaded file. The file bytes live on disk under
/// the configured storage root; this row carries the metadata that
/// <see cref="Aimbys.Application.Storage.IFileStorageService"/> uses to
/// resolve, authorise, audit, and (soft-)delete the upload.
///
/// <para>
/// File names on disk are <c>{newGuid}{contentHashPrefix}.{ext}</c> &mdash;
/// no user-supplied path component ever lands on the filesystem. The
/// original filename (sanitised) is preserved here only so the download
/// controller can set <c>Content-Disposition: attachment; filename=...</c>.
/// </para>
/// </summary>
public class FileAsset : IRestoreable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Public token used in the download URL (<c>/files/{token}</c>).
    /// Distinct from <see cref="Id"/> so internal IDs don't leak into URLs
    /// and so token rotation is possible later without changing the row id.
    /// </summary>
    public Guid Token { get; set; } = Guid.NewGuid();

    public FileArea Area { get; set; }

    /// <summary>
    /// Free-form composite owner key the caller assigns
    /// (e.g. <c>Question:6f9a-…</c>, <c>Paper:abcd-…</c>). Indexed for fast
    /// lookups when callers need "all files attached to this entity".
    /// </summary>
    public string OwnerKey { get; set; } = string.Empty;

    /// <summary>Tenancy boundary. Null means platform-level (Super Admin scope).</summary>
    public Guid? InstituteId { get; set; }

    /// <summary>
    /// Sanitised original filename (max 255). Used only for the
    /// <c>Content-Disposition</c> header on download.
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Filename actually written to disk: <c>{guid}{sha256-prefix}.{ext}</c>.
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>MIME content-type (validated against the caller's allow-list at save time).</summary>
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    /// <summary>SHA-256 of the file contents, lowercase hex (64 chars).</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>FK to <c>AspNetUsers.Id</c>.</summary>
    public string UploadedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft-delete flag. The on-disk file is left in place for a later
    /// orphan-cleanup job (Chunk 34) so a misclick can be recovered from.
    /// </summary>
    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Identity user id of the actor that soft-deleted the asset.
    /// Required by <see cref="IRestoreable"/>; populated by
    /// <c>ISoftDeleteService.DeleteAsync</c> in Chunk 12+.
    /// </summary>
    public string? DeletedByUserId { get; set; }

    /// <summary>UTC instant of the most recent restore (null on first delete).</summary>
    public DateTime? RestoredAtUtc { get; set; }
}
