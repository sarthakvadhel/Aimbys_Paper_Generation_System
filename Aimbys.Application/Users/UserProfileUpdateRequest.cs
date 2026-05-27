namespace Aimbys.Application.Users;

public sealed record UserProfileUpdateRequest(
    string? DisplayName,
    string? Designation,
    Guid? DepartmentId,
    // 13 teacher permission flags (nullable = don't change)
    bool? CanCreateQuestions,
    bool? CanManageQuestionBank,
    bool? CanApproveQuestions,
    bool? CanGeneratePaper,
    bool? CanManageBlueprints,
    bool? CanEvaluate,
    bool? CanModerate,
    bool? CanReviewCodingQuestions,
    bool? CanScheduleExam,
    bool? CanPublishResults,
    bool? CanAssignEvaluators,
    bool? CanProctor,
    bool? CanManageAnalytics);
