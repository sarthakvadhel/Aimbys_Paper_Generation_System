namespace Aimbys.Application.Bulk;

/// <summary>
/// One row in <c>IBulkOperationService.AssignTeachersAsync</c>.
/// Concrete persistence lands when the teacher-subject /
/// teacher-class assignment entities ship in a future chunk; this
/// record only fixes the public input shape now.
/// </summary>
public sealed record BulkTeacherAssignment(
    Guid TeacherProfileId,
    Guid? SubjectId,
    Guid? ClassBatchId);

/// <summary>
/// One row in <c>IBulkOperationService.ScheduleExamsBulkAsync</c>.
/// Persistence lands when the Exam aggregate ships.
/// </summary>
public sealed record BulkExamSchedule(
    Guid PaperId,
    Guid ClassBatchId,
    DateTime ScheduledAtUtc,
    int DurationMinutes);

/// <summary>
/// One row in <c>IBulkOperationService.AssignPapersBulkAsync</c>.
/// Persistence lands when the Paper aggregate ships.
/// </summary>
public sealed record BulkPaperAssignment(
    Guid PaperId,
    string AssignedToUserId);
