using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities;

/// <summary>
/// Profile row for a teacher inside an <see cref="Institute"/>. Login
/// credentials live on <c>AspNetUsers</c> (referenced by <see cref="UserId"/>);
/// this row carries the institutional metadata and the 13 operational
/// permission flags that an Institute Admin assigns dynamically (Chunk 17).
///
/// In the PARAKH model the only Identity roles are <c>SuperAdmin</c>,
/// <c>InstituteAdmin</c>, <c>Teacher</c>, <c>Student</c>. Capabilities like
/// "Evaluator", "Moderator", "Reviewer", "Proctor" are not roles &mdash; they
/// are the boolean flags below, checked by
/// <c>IPermissionGuard</c> / <c>[RequiresPermission(...)]</c>.
/// </summary>
public class TeacherProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK to <c>AspNetUsers.Id</c> (the Identity user). Unique.</summary>
    public string UserId { get; set; } = string.Empty;

    public Guid InstituteId { get; set; }
    public Guid? DepartmentId { get; set; }

    /// <summary>Cached display name, max 200.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Optional employee code, unique within the institute.</summary>
    public string? EmployeeCode { get; set; }

    /// <summary>Free-form designation (e.g. <c>"Senior Teacher"</c>, <c>"HoD"</c>), max 100.</summary>
    public string? Designation { get; set; }

    public ProfileStatus Status { get; set; } = ProfileStatus.Active;

    // --- Operational permission flags (the canonical 13) -----------------
    //
    // All default to false. The Institute Admin opts a teacher in to each
    // capability via the user-management screen (Chunk 17). Constants for
    // these names live in Aimbys.Domain.Permissions.TeacherPermissions so
    // [RequiresPermission(TeacherPermissions.CanEvaluate)] never disagrees
    // with the column read.

    /// <summary>Author and edit questions in the question bank.</summary>
    public bool CanCreateQuestions { get; set; }

    /// <summary>Compose papers from the bank or from a blueprint.</summary>
    public bool CanGeneratePaper { get; set; }

    /// <summary>Author and publish blueprints (chapter/competency matrix).</summary>
    public bool CanManageBlueprints { get; set; }

    /// <summary>Score student answers from the evaluation desk.</summary>
    public bool CanEvaluate { get; set; }

    /// <summary>Moderate evaluator scores (approve / require-changes / override).</summary>
    public bool CanModerate { get; set; }

    /// <summary>Publish examination results once moderation is complete.</summary>
    public bool CanPublishResults { get; set; }

    /// <summary>Schedule an exam against an approved paper version.</summary>
    public bool CanScheduleExam { get; set; }

    /// <summary>Review coding-question submissions and override auto-scoring.</summary>
    public bool CanReviewCodingQuestions { get; set; }

    /// <summary>Curate the institute-wide question bank (organise, retire, archive).</summary>
    public bool CanManageQuestionBank { get; set; }

    /// <summary>Assign evaluators to submissions in the moderation queue.</summary>
    public bool CanAssignEvaluators { get; set; }

    /// <summary>Open the analytics dashboards beyond the personal slice.</summary>
    public bool CanManageAnalytics { get; set; }

    /// <summary>Approve or reject submitted questions in the review queue.</summary>
    public bool CanApproveQuestions { get; set; }

    /// <summary>Proctor a live exam session.</summary>
    public bool CanProctor { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Institute? Institute { get; set; }
    public Department? Department { get; set; }
}
