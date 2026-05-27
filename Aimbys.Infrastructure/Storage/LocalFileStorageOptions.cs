namespace Aimbys.Infrastructure.Storage;

/// <summary>
/// Bound from the <c>FileStorage</c> configuration section. The single
/// option is the absolute path of the storage root; the area subdirectory
/// names are fixed (see <see cref="FileFolders"/>) so callers can't inject
/// arbitrary paths.
/// </summary>
public class LocalFileStorageOptions
{
    /// <summary>Configuration section name: <c>FileStorage</c>.</summary>
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Absolute path of the storage root. When unset, the service defaults
    /// to <c>{ContentRoot}/uploads</c> at startup so a fresh checkout works
    /// without configuration. In production this should always be an
    /// explicit absolute path on a backed-up volume.
    /// </summary>
    public string? RootPath { get; set; }
}
