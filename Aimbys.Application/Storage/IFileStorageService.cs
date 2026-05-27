using System.Security.Claims;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Aimbys.Application.Storage;

/// <summary>
/// Backend-agnostic file storage abstraction. The current implementation is
/// the local-disk <c>LocalFileStorageService</c> (Chunk 9). A future cloud
/// adapter (S3, Azure Blob, …) drops in by implementing this interface and
/// swapping the DI registration; nothing in <c>Aimbys.Application</c> or the
/// MVC controllers should depend on disk-specific behaviour.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Validates the upload (MIME against <paramref name="mimeAllowList"/>,
    /// size against <paramref name="maxBytes"/>), writes the bytes through
    /// a streaming SHA-256 to the area's storage location, and persists a
    /// <see cref="FileAsset"/> audit row.
    ///
    /// Throws <see cref="FileStorageException"/> on validation failure (the
    /// controller maps this to <c>400 Bad Request</c>); never silently
    /// truncates.
    /// </summary>
    /// <param name="area">Logical area; maps to a fixed sub-directory.</param>
    /// <param name="ownerKey">Free-form composite owner key (e.g. <c>Question:abc</c>).</param>
    /// <param name="file">The incoming form file.</param>
    /// <param name="mimeAllowList">Caller-controlled allow-list of MIME types.</param>
    /// <param name="maxBytes">Hard upper bound on the uploaded size.</param>
    /// <param name="instituteId">Tenancy boundary. <c>null</c> for platform-level uploads.</param>
    /// <param name="uploader">The current request principal; the uploader's id is captured.</param>
    Task<FileSaveResult> SaveAsync(
        FileArea area,
        string ownerKey,
        IFormFile file,
        IReadOnlyCollection<string> mimeAllowList,
        long maxBytes,
        Guid? instituteId,
        ClaimsPrincipal uploader,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a download token to its <see cref="FileAsset"/> metadata.
    /// Returns <c>null</c> for unknown / soft-deleted tokens. The caller
    /// (controller) is responsible for tenancy + permission checks before
    /// calling <see cref="OpenReadAsync"/>.
    /// </summary>
    Task<FileAsset?> GetAsync(Guid token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the token and opens a read stream. Returns <c>null</c> for
    /// unknown / soft-deleted tokens or when the on-disk file has gone
    /// missing (logs a warning in that case).
    /// </summary>
    Task<FileOpenResult?> OpenReadAsync(Guid token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes the asset (sets <c>IsDeleted = true</c>, stamps
    /// <c>DeletedAtUtc</c>). The on-disk file is left in place; orphan
    /// cleanup is a later hardening chunk.
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid token, ClaimsPrincipal actor, CancellationToken cancellationToken = default);
}
