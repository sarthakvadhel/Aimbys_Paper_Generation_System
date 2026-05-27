using System.Security.Claims;

namespace Aimbys.Application.OrgTree;

/// <summary>
/// Service contract for managing the institute organisational tree:
/// streams, majors, chapters, academic-year currency, and department lifecycle.
/// </summary>
public interface IOrgTreeService
{
    /// <summary>Creates a new stream within an institute. Returns the new stream ID.</summary>
    Task<Guid> CreateStreamAsync(Guid instituteId, string name, string? description, CancellationToken ct);

    /// <summary>Creates a new major within a stream. Returns the new major ID.</summary>
    Task<Guid> CreateMajorAsync(Guid instituteId, Guid streamId, string name, string? description, CancellationToken ct);

    /// <summary>Creates a new chapter within a subject. Returns the new chapter ID.</summary>
    Task<Guid> CreateChapterAsync(Guid subjectId, string title, string? description, CancellationToken ct);

    /// <summary>Reorders chapters for a subject. Returns true on success.</summary>
    Task<bool> ReorderChaptersAsync(Guid subjectId, IReadOnlyList<Guid> orderedChapterIds, CancellationToken ct);

    /// <summary>
    /// Sets the specified academic year as current for the institute (at-most-one invariant).
    /// Returns true on success.
    /// </summary>
    Task<bool> SetCurrentAcademicYearAsync(Guid instituteId, Guid yearId, CancellationToken ct);

    /// <summary>
    /// Deactivates a department if it has no active children (subjects/classbatches).
    /// Returns (true, null) on success or (false, error message) if active children exist.
    /// </summary>
    Task<(bool Success, string? Error)> DeactivateDepartmentAsync(Guid departmentId, ClaimsPrincipal actor, CancellationToken ct);
}
