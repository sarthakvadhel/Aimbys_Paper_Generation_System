using Microsoft.AspNetCore.Mvc.Rendering;

namespace Aimbys.Web.ViewModels.Users;

public class UserEditViewModel
{
    public Guid ProfileId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsTeacher { get; set; }

    // 13 permission flags (only relevant for teachers)
    public bool CanCreateQuestions { get; set; }
    public bool CanManageQuestionBank { get; set; }
    public bool CanApproveQuestions { get; set; }
    public bool CanGeneratePaper { get; set; }
    public bool CanManageBlueprints { get; set; }
    public bool CanEvaluate { get; set; }
    public bool CanModerate { get; set; }
    public bool CanReviewCodingQuestions { get; set; }
    public bool CanScheduleExam { get; set; }
    public bool CanPublishResults { get; set; }
    public bool CanAssignEvaluators { get; set; }
    public bool CanProctor { get; set; }
    public bool CanManageAnalytics { get; set; }

    // Dropdowns
    public SelectList? Departments { get; set; }
}
