using System.Security.Claims;
using Aimbys.Application.Authorization;
using Aimbys.Application.Blueprints;
using Aimbys.Domain.Entities.Blueprints;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Blueprints;

public class BlueprintAuthoringService : IBlueprintAuthoringService
{
    private readonly AppDbContext _db;
    private readonly IInstituteScope _scope;
    private readonly IBlueprintValidator _validator;

    public BlueprintAuthoringService(AppDbContext db, IInstituteScope scope, IBlueprintValidator validator)
    {
        _db = db;
        _scope = scope;
        _validator = validator;
    }

    public async Task<BlueprintResult> CreateAsync(BlueprintCreateRequest request, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(actor, ct);
        if (instituteId is null)
            return new BlueprintResult(false, "Institute not resolved.");

        // Resolve teacher profile
        var userId = actor.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return new BlueprintResult(false, "User not authenticated.");

        var teacherProfile = await _db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId && t.InstituteId == instituteId.Value, ct);
        if (teacherProfile is null)
            return new BlueprintResult(false, "Teacher profile not found.");

        var blueprint = new Blueprint
        {
            InstituteId = instituteId.Value,
            SubjectId = request.SubjectId,
            AssessmentDesignId = request.AssessmentDesignId,
            Name = request.Name,
            CreatedByTeacherProfileId = teacherProfile.Id,
            Status = BlueprintStatus.Draft
        };

        var version = new BlueprintVersion
        {
            BlueprintId = blueprint.Id,
            VersionNumber = 1,
            TotalMarks = request.TotalMarks,
            DurationMinutes = request.DurationMinutes
        };

        blueprint.CurrentVersionId = version.Id;
        blueprint.Versions.Add(version);

        _db.Blueprints.Add(blueprint);
        await _db.SaveChangesAsync(ct);

        return new BlueprintResult(true, BlueprintId: blueprint.Id, VersionId: version.Id);
    }

    public async Task<BlueprintResult> EditAsync(Guid blueprintId, BlueprintEditRequest request, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(actor, ct);
        if (instituteId is null)
            return new BlueprintResult(false, "Institute not resolved.");

        var blueprint = await _db.Blueprints
            .Include(b => b.Versions)
            .FirstOrDefaultAsync(b => b.Id == blueprintId && b.InstituteId == instituteId.Value, ct);

        if (blueprint is null)
            return new BlueprintResult(false, "Blueprint not found.");

        var currentVersion = blueprint.Versions
            .FirstOrDefault(v => v.Id == blueprint.CurrentVersionId);

        BlueprintVersion targetVersion;

        if (currentVersion is null || currentVersion.IsLocked || blueprint.Status == BlueprintStatus.Published)
        {
            // Create new version
            var maxVersion = blueprint.Versions.Max(v => v.VersionNumber);
            targetVersion = new BlueprintVersion
            {
                BlueprintId = blueprint.Id,
                VersionNumber = maxVersion + 1,
                TotalMarks = request.TotalMarks,
                DurationMinutes = request.DurationMinutes
            };
            _db.BlueprintVersions.Add(targetVersion);
            blueprint.CurrentVersionId = targetVersion.Id;
            blueprint.Status = BlueprintStatus.Draft;
        }
        else
        {
            targetVersion = currentVersion;
            targetVersion.TotalMarks = request.TotalMarks;
            targetVersion.DurationMinutes = request.DurationMinutes;

            // Remove existing children for re-creation
            var existingSections = await _db.BlueprintSections.Where(s => s.VersionId == targetVersion.Id).ToListAsync(ct);
            _db.BlueprintSections.RemoveRange(existingSections);

            var existingConstraints = await _db.BlueprintConstraints.Where(c => c.VersionId == targetVersion.Id).ToListAsync(ct);
            _db.BlueprintConstraints.RemoveRange(existingConstraints);

            var existingCohorts = await _db.BlueprintCohorts.Where(c => c.VersionId == targetVersion.Id).ToListAsync(ct);
            _db.BlueprintCohorts.RemoveRange(existingCohorts);
        }

        // Add sections
        foreach (var s in request.Sections)
        {
            _db.BlueprintSections.Add(new BlueprintSection
            {
                VersionId = targetVersion.Id,
                Name = s.Name,
                Marks = s.Marks,
                QuestionCount = s.QuestionCount,
                TypeMix = s.TypeMix,
                SortOrder = s.SortOrder
            });
        }

        // Add constraints
        foreach (var c in request.Constraints)
        {
            _db.BlueprintConstraints.Add(new BlueprintConstraint
            {
                VersionId = targetVersion.Id,
                ChapterId = c.ChapterId,
                CompetencyId = c.CompetencyId,
                DifficultyLevel = (DifficultyLevel)c.DifficultyLevel,
                QuestionType = (QuestionType)c.QuestionType,
                Marks = c.Marks,
                Count = c.Count
            });
        }

        // Add cohorts
        foreach (var co in request.Cohorts)
        {
            _db.BlueprintCohorts.Add(new BlueprintCohort
            {
                VersionId = targetVersion.Id,
                StreamId = co.StreamId,
                MajorId = co.MajorId,
                DepartmentId = co.DepartmentId,
                AcademicYearId = co.AcademicYearId,
                ClassBatchId = co.ClassBatchId
            });
        }

        blueprint.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new BlueprintResult(true, BlueprintId: blueprint.Id, VersionId: targetVersion.Id);
    }

    public async Task<BlueprintResult> PublishAsync(Guid blueprintId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(actor, ct);
        if (instituteId is null)
            return new BlueprintResult(false, "Institute not resolved.");

        var blueprint = await _db.Blueprints
            .Include(b => b.Versions)
            .FirstOrDefaultAsync(b => b.Id == blueprintId && b.InstituteId == instituteId.Value, ct);

        if (blueprint is null)
            return new BlueprintResult(false, "Blueprint not found.");

        var currentVersion = blueprint.Versions
            .FirstOrDefault(v => v.Id == blueprint.CurrentVersionId);

        if (currentVersion is null)
            return new BlueprintResult(false, "No current version found.");

        // Load sections for validation
        var sections = await _db.BlueprintSections
            .Where(s => s.VersionId == currentVersion.Id)
            .ToListAsync(ct);

        var constraints = await _db.BlueprintConstraints
            .Where(c => c.VersionId == currentVersion.Id)
            .ToListAsync(ct);

        var cohorts = await _db.BlueprintCohorts
            .Where(c => c.VersionId == currentVersion.Id)
            .ToListAsync(ct);

        var editRequest = new BlueprintEditRequest(
            currentVersion.TotalMarks,
            currentVersion.DurationMinutes,
            sections.Select(s => new BlueprintSectionDto(s.Name, s.Marks, s.QuestionCount, s.TypeMix, s.SortOrder)).ToList(),
            constraints.Select(c => new BlueprintConstraintDto(c.ChapterId, c.CompetencyId, (int)c.DifficultyLevel, (int)c.QuestionType, c.Marks, c.Count)).ToList(),
            cohorts.Select(c => new BlueprintCohortDto(c.StreamId, c.MajorId, c.DepartmentId, c.AcademicYearId, c.ClassBatchId)).ToList()
        );

        var validation = _validator.Validate(editRequest, currentVersion.TotalMarks);
        if (!validation.IsValid)
            return new BlueprintResult(false, string.Join("; ", validation.Errors));

        blueprint.Status = BlueprintStatus.Published;
        currentVersion.IsLocked = true;
        blueprint.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new BlueprintResult(true, BlueprintId: blueprint.Id, VersionId: currentVersion.Id);
    }

    public async Task<BlueprintResult> ArchiveAsync(Guid blueprintId, ClaimsPrincipal actor, CancellationToken ct = default)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(actor, ct);
        if (instituteId is null)
            return new BlueprintResult(false, "Institute not resolved.");

        var blueprint = await _db.Blueprints
            .FirstOrDefaultAsync(b => b.Id == blueprintId && b.InstituteId == instituteId.Value, ct);

        if (blueprint is null)
            return new BlueprintResult(false, "Blueprint not found.");

        blueprint.Status = BlueprintStatus.Archived;
        blueprint.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new BlueprintResult(true, BlueprintId: blueprint.Id);
    }
}
