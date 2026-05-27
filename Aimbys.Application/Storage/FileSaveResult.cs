using Aimbys.Domain.Entities;

namespace Aimbys.Application.Storage;

/// <summary>
/// Returned by <see cref="IFileStorageService.SaveAsync"/>. The
/// <see cref="Token"/> goes into URLs (<c>/files/{token}</c>); the
/// <see cref="Asset"/> is the audit row for the upload.
/// </summary>
public sealed record FileSaveResult(Guid Token, FileAsset Asset);
