using System.Security.Claims;
using Aimbys.Application.OrgTree;
using Aimbys.Domain.Entities;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.OrgTree;

/// <summary>
/// Implements <see cref="IOrgTreeService"/> for managing the institute
/// organisational tree (streams, majors, chapters, academic-year currency,
/// department lifecycle).
/// </summary>
public sealed class OrgTreeService : IOrgTreeService
{
    private readonly AppDbContext _db;

    public OrgTreeService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateStreamAsync(Guid instituteId, string name, string? description, CancellationToken ct)
    {
        // Validate unique (InstituteId, Name)
        var exists = await _db.Streams
            .AnyAsync(s => s.InstituteId == instituteId && s.Name == name, ct);

        if (exists)
            throw new InvalidOperationException($"A stream with name '{name}' already exists in this institute.");

        var stream = new Domain.Entities.Stream
        {
            InstituteId = instituteId,
            Name = name,
            Description = description
        };

        _db.Streams.Add(stream);
        await _db.SaveChangesAsync(ct);
        return stream.Id;
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateMajorAsync(Guid instituteId, Guid streamId, string name, string? description, CancellationToken ct)
    {
        // Validate unique (InstituteId, StreamId, Name)
        var exists = await _db.Majors
            .AnyAsync(m => m.InstituteId == instituteId && m.StreamId == streamId && m.Name == name, ct);

        if (exists)
            throw new InvalidOperationException($"A major with name '{name}' already exists in this stream.");

        var major = new Major
        {
            InstituteId = instituteId,
            StreamId = streamId,
            Name = name,
            Description = description
        };

        _db.Majors.Add(major);
        await _db.SaveChangesAsync(ct);
        return major.Id;
    }

    /// <inheritdoc/>
    public async Task<Guid> CreateChapterAsync(Guid subjectId, string title, string? description, CancellationToken ct)
    {
        // Auto-assign SortOrder as (max existing + 10)
        var maxSortOrder = await _db.Chapters
            .Where(c => c.SubjectId == subjectId)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync(ct) ?? 0;

        var chapter = new Chapter
        {
            SubjectId = subjectId,
            Title = title,
            Description = description,
            SortOrder = maxSortOrder + 10
        };

        _db.Chapters.Add(chapter);
        await _db.SaveChangesAsync(ct);
        return chapter.Id;
    }

    /// <inheritdoc/>
    public async Task<bool> ReorderChaptersAsync(Guid subjectId, IReadOnlyList<Guid> orderedChapterIds, CancellationToken ct)
    {
        var chapters = await _db.Chapters
            .Where(c => c.SubjectId == subjectId && orderedChapterIds.Contains(c.Id))
            .ToListAsync(ct);

        for (var i = 0; i < orderedChapterIds.Count; i++)
        {
            var chapter = chapters.FirstOrDefault(c => c.Id == orderedChapterIds[i]);
            if (chapter != null)
            {
                chapter.SortOrder = (i + 1) * 10;
                chapter.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> SetCurrentAcademicYearAsync(Guid instituteId, Guid yearId, CancellationToken ct)
    {
        // Set all years for the institute to IsCurrent = false
        var allYears = await _db.AcademicYears
            .Where(y => y.InstituteId == instituteId)
            .ToListAsync(ct);

        foreach (var year in allYears)
        {
            year.IsCurrent = year.Id == yearId;
            year.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> DeactivateDepartmentAsync(
        Guid departmentId, ClaimsPrincipal actor, CancellationToken ct)
    {
        // Check for active subjects referencing the department
        var hasActiveSubjects = await _db.Subjects
            .AnyAsync(s => s.DepartmentId == departmentId && s.IsActive, ct);

        if (hasActiveSubjects)
            return (false, "Cannot deactivate department: active subjects still reference it.");

        // Check for active class batches referencing the department
        var hasActiveClassBatches = await _db.ClassBatches
            .AnyAsync(cb => cb.DepartmentId == departmentId, ct);

        if (hasActiveClassBatches)
            return (false, "Cannot deactivate department: active class batches still reference it.");

        var department = await _db.Departments.FindAsync(new object[] { departmentId }, ct);
        if (department == null)
            return (false, "Department not found.");

        department.IsActive = false;
        department.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }
}
