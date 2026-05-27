using Aimbys.Domain.Enums;

namespace Aimbys.Infrastructure.Storage;

/// <summary>
/// Single source of truth for the area &harr; subdirectory mapping.
/// Callers cannot inject directory names; every upload flows through this
/// table.
/// </summary>
public static class FileFolders
{
    /// <summary>
    /// Subdirectory name (under the storage root) for a given
    /// <see cref="FileArea"/>.
    /// </summary>
    public static string SubDirectoryFor(FileArea area) => area switch
    {
        FileArea.Questions    => "questions",
        FileArea.Papers       => "papers",
        FileArea.Answers      => "answers",
        FileArea.Certificates => "certificates",
        FileArea.Reports      => "reports",
        FileArea.Exams        => "exams",
        FileArea.Coding       => "coding",
        FileArea.Temp         => "temp",
        _ => throw new ArgumentOutOfRangeException(nameof(area), area, "Unknown FileArea.")
    };

    /// <summary>
    /// Creates the storage root and every area subdirectory if they don't
    /// already exist. Idempotent; safe to call on every startup.
    /// </summary>
    public static void EnsureCreated(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Root path is required.", nameof(rootPath));
        }

        Directory.CreateDirectory(rootPath);
        foreach (FileArea area in Enum.GetValues<FileArea>())
        {
            Directory.CreateDirectory(Path.Combine(rootPath, SubDirectoryFor(area)));
        }
    }
}
