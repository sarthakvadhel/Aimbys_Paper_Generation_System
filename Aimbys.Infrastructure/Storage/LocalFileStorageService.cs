using System.Security.Claims;
using System.Security.Cryptography;
using Aimbys.Application.Storage;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aimbys.Infrastructure.Storage;

/// <summary>
/// Local-disk implementation of <see cref="IFileStorageService"/>. Streams
/// uploads through SHA-256 to a fixed area-keyed directory, persists a
/// <see cref="FileAsset"/> audit row, and resolves downloads by token.
/// </summary>
public class LocalFileStorageService : ILocalFileStorageService
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _rootPath;

    public LocalFileStorageService(
        AppDbContext db,
        UserManager<IdentityUser> userManager,
        IOptions<LocalFileStorageOptions> options,
        ILogger<LocalFileStorageService> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
        _rootPath = options.Value.RootPath
            ?? throw new InvalidOperationException(
                "FileStorage:RootPath is not configured. The DI bootstrap should have defaulted it.");
    }

    // ---- Save ----------------------------------------------------------

    public async Task<FileSaveResult> SaveAsync(
        FileArea area,
        string ownerKey,
        IFormFile file,
        IReadOnlyCollection<string> mimeAllowList,
        long maxBytes,
        Guid? instituteId,
        ClaimsPrincipal uploader,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            throw new FileStorageException("No file was uploaded or the file is empty.");
        }

        if (string.IsNullOrWhiteSpace(ownerKey))
        {
            throw new FileStorageException("ownerKey is required.");
        }

        if (mimeAllowList is null || mimeAllowList.Count == 0)
        {
            throw new FileStorageException("MIME allow-list is required and must be non-empty.");
        }

        if (file.Length > maxBytes)
        {
            throw new FileStorageException(
                $"File is {file.Length} bytes; the maximum is {maxBytes} bytes.");
        }

        if (!mimeAllowList.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new FileStorageException(
                $"Content type '{file.ContentType}' is not in the caller's allow-list.");
        }

        var uploaderId = _userManager.GetUserId(uploader);
        if (string.IsNullOrEmpty(uploaderId))
        {
            throw new FileStorageException("Uploader is not authenticated.");
        }

        var areaFolder = Path.Combine(_rootPath, FileFolders.SubDirectoryFor(area));
        Directory.CreateDirectory(areaFolder); // belt-and-braces; startup also pre-creates

        var guidPart = Guid.NewGuid().ToString("N"); // 32 hex chars, no dashes
        var ext = SanitiseExtension(file.FileName);
        var tempPath = Path.Combine(areaFolder, $"{guidPart}.tmp");

        string sha256Hex;
        try
        {
            using var sha = SHA256.Create();
            await using var fs = new FileStream(
                tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 81920, useAsync: true);
            await using var hashStream = new CryptoStream(fs, sha, CryptoStreamMode.Write);
            await using (var src = file.OpenReadStream())
            {
                await src.CopyToAsync(hashStream, cancellationToken);
            }
            await hashStream.FlushFinalBlockAsync(cancellationToken);
            sha256Hex = Convert.ToHexStringLower(sha.Hash!);
        }
        catch (Exception)
        {
            TryDelete(tempPath);
            throw;
        }

        var hashPrefix = sha256Hex[..8];
        var storedFileName = $"{guidPart}{hashPrefix}{ext}";
        var finalPath = Path.Combine(areaFolder, storedFileName);

        try
        {
            File.Move(tempPath, finalPath, overwrite: false);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }

        var asset = new FileAsset
        {
            Id = Guid.NewGuid(),
            Token = Guid.NewGuid(),
            Area = area,
            OwnerKey = ownerKey,
            InstituteId = instituteId,
            OriginalFileName = SanitiseDisplayName(file.FileName),
            StoredFileName = storedFileName,
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            Sha256 = sha256Hex,
            UploadedByUserId = uploaderId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };

        _db.FileAssets.Add(asset);
        await _db.SaveChangesAsync(cancellationToken);

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = uploaderId,
            Action = "File.Saved",
            EntityType = nameof(FileAsset),
            EntityId = asset.Id.ToString(),
            DetailsJson = $"{{\"area\":\"{area}\",\"sizeBytes\":{asset.SizeBytes},\"sha256\":\"{sha256Hex}\"}}"
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "File saved. Token={Token} Area={Area} OwnerKey={OwnerKey} Bytes={Bytes} Sha256={Sha256}",
            asset.Token, area, ownerKey, asset.SizeBytes, sha256Hex);

        return new FileSaveResult(asset.Token, asset);
    }

    // ---- Get / OpenRead ------------------------------------------------

    public Task<FileAsset?> GetAsync(Guid token, CancellationToken cancellationToken = default)
    {
        return _db.FileAssets
            .AsNoTracking()
            .Where(a => a.Token == token && !a.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<FileOpenResult?> OpenReadAsync(Guid token, CancellationToken cancellationToken = default)
    {
        var asset = await GetAsync(token, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var path = Path.Combine(_rootPath, FileFolders.SubDirectoryFor(asset.Area), asset.StoredFileName);
        if (!File.Exists(path))
        {
            _logger.LogWarning(
                "File row exists but bytes are missing on disk. Token={Token} Path={Path}",
                token, path);
            return null;
        }

        var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, useAsync: true);

        return new FileOpenResult(stream, asset);
    }

    // ---- Soft delete ---------------------------------------------------

    public async Task<bool> SoftDeleteAsync(
        Guid token,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        var asset = await _db.FileAssets
            .Where(a => a.Token == token && !a.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (asset is null)
        {
            return false;
        }

        asset.IsDeleted = true;
        asset.DeletedAtUtc = DateTime.UtcNow;

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = _userManager.GetUserId(actor),
            Action = "File.Deleted",
            EntityType = nameof(FileAsset),
            EntityId = asset.Id.ToString()
        });

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ---- Helpers -------------------------------------------------------

    /// <summary>
    /// Returns a safe lowercase extension (with leading dot) or
    /// <c>".bin"</c> if the original is missing or unsuitable. Strips any
    /// non-alphanumeric characters so a hostile filename like
    /// <c>foo.exe%00.txt</c> can never end up on disk.
    /// </summary>
    private static string SanitiseExtension(string? originalFileName)
    {
        var raw = Path.GetExtension(originalFileName ?? string.Empty);
        if (string.IsNullOrEmpty(raw)) return ".bin";

        var trimmed = raw.TrimStart('.').ToLowerInvariant();
        var clean = new string(trimmed.Where(char.IsLetterOrDigit).ToArray());
        if (clean.Length == 0 || clean.Length > 12) return ".bin";

        return "." + clean;
    }

    /// <summary>
    /// Sanitises an original filename for use in the
    /// <c>Content-Disposition</c> header. Strips control characters and
    /// path separators; truncates to 255 chars.
    /// </summary>
    private static string SanitiseDisplayName(string? originalFileName)
    {
        if (string.IsNullOrWhiteSpace(originalFileName)) return "download.bin";

        var name = Path.GetFileName(originalFileName); // strips any path component
        var safe = new string(name.Where(c =>
            !char.IsControl(c) && c != '"' && c != '\\' && c != '/').ToArray()).Trim();

        if (safe.Length == 0) return "download.bin";
        return safe.Length <= 255 ? safe : safe[..255];
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
    }
}
