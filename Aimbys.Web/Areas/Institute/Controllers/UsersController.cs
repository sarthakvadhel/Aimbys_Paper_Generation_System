using System.Text;
using Aimbys.Application.Authorization;
using Aimbys.Application.Bulk;
using Aimbys.Application.Users;
using Aimbys.Domain.Entities;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Aimbys.Web.Models.UI;
using Aimbys.Web.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Web.Areas.Institute.Controllers;

[Area("Institute")]
[Authorize(Roles = Roles.InstituteAdmin)]
public class UsersController : Controller
{
    private readonly IUserManagementService _userService;
    private readonly IBulkOperationService _bulk;
    private readonly IInstituteScope _scope;
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly Microsoft.AspNetCore.Antiforgery.IAntiforgery _antiforgery;

    public UsersController(
        IUserManagementService userService,
        IBulkOperationService bulk,
        IInstituteScope scope,
        AppDbContext db,
        UserManager<IdentityUser> userManager,
        Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery)
    {
        _userService = userService;
        _bulk = bulk;
        _scope = scope;
        _db = db;
        _userManager = userManager;
        _antiforgery = antiforgery;
    }

    public async Task<IActionResult> Index(string? q, string? role, string? status, int page = 1, CancellationToken ct = default)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return NotFound();

        var filter = new UserPageFilter(
            InstituteId: instituteId.Value,
            SearchQuery: q,
            RoleFilter: role,
            StatusFilter: status,
            Page: page);

        var result = await _userService.GetPageAsync(filter, ct);

        // KPI counts
        var teacherCount = await _db.TeacherProfiles.CountAsync(t => t.InstituteId == instituteId.Value, ct);
        var studentCount = await _db.StudentProfiles.CountAsync(s => s.InstituteId == instituteId.Value, ct);

        var columns = new[]
        {
            new DataTableColumn("Name"),
            new DataTableColumn("Email"),
            new DataTableColumn("Role"),
            new DataTableColumn("Department"),
            new DataTableColumn("Status"),
            new DataTableColumn("Actions")
        };

        var rows = result.Items.Select(item =>
        {
            var roleBadge = RoleBadge(item.Role);
            var statusBadge = StatusBadge.Render(item.Status);
            var actions = BuildActions(item.ProfileId, item.Status, item.IsTeacher);
            return new DataTableRow(new DataTableCell[]
            {
                new(item.DisplayName),
                new(item.Email),
                new(roleBadge, IsHtml: true),
                new(item.Department ?? "-"),
                new(statusBadge, IsHtml: true),
                new(actions, IsHtml: true)
            });
        }).ToList();

        var roleOptions = new List<FilterBarOption>
        {
            new(Roles.Teacher, "Teacher"),
            new(Roles.Student, "Student"),
            new(Roles.InstituteAdmin, "Institute Admin")
        };

        var statusOptions = new List<FilterBarOption>
        {
            new("Active", "Active"),
            new("Suspended", "Suspended"),
            new("Inactive", "Inactive")
        };

        var model = new UserIndexViewModel
        {
            Table = new DataTableModel("Users", columns, rows),
            FilterBar = new FilterBarModel(
                SearchName: "q",
                SearchValue: q,
                SearchPlaceholder: "Search by name...",
                Filters: new[]
                {
                    new FilterBarSelect("Role", "role", role, roleOptions),
                    new FilterBarSelect("Status", "status", status, statusOptions)
                }),
            Pagination = new PaginationModel(result.Page, result.TotalPages),
            TeacherCount = teacherCount,
            StudentCount = studentCount,
            TotalCount = teacherCount + studentCount
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Invite(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return NotFound();

        var vm = await BuildInviteViewModel(instituteId.Value, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(UserInviteViewModel model, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return NotFound();

        if (!ModelState.IsValid)
        {
            var vm = await BuildInviteViewModel(instituteId.Value, ct);
            vm.DisplayName = model.DisplayName;
            vm.Email = model.Email;
            vm.Role = model.Role;
            vm.DepartmentId = model.DepartmentId;
            vm.ClassBatchId = model.ClassBatchId;
            return View(vm);
        }

        var request = new UserInviteRequest(
            InstituteId: instituteId.Value,
            DisplayName: model.DisplayName,
            Email: model.Email,
            Role: model.Role,
            DepartmentId: model.DepartmentId,
            ClassBatchId: model.ClassBatchId);

        var result = await _userService.InviteAsync(request, User, ct);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Failed to invite user.");
            var vm = await BuildInviteViewModel(instituteId.Value, ct);
            vm.DisplayName = model.DisplayName;
            vm.Email = model.Email;
            vm.Role = model.Role;
            vm.DepartmentId = model.DepartmentId;
            vm.ClassBatchId = model.ClassBatchId;
            return View(vm);
        }

        TempData["Success"] = $"User invited successfully. Temporary password: {result.TemporaryPassword}";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return NotFound();

        var teacher = await _db.TeacherProfiles
            .Include(t => t.Department)
            .FirstOrDefaultAsync(t => t.Id == id && t.InstituteId == instituteId.Value, ct);

        if (teacher is null) return NotFound();

        var user = await _userManager.FindByIdAsync(teacher.UserId);
        var roles = user != null ? await _userManager.GetRolesAsync(user) : Array.Empty<string>();
        var displayRole = roles.Contains(Roles.InstituteAdmin) ? Roles.InstituteAdmin : Roles.Teacher;

        var departments = await _db.Departments
            .Where(d => d.InstituteId == instituteId.Value)
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

        var vm = new UserEditViewModel
        {
            ProfileId = teacher.Id,
            DisplayName = teacher.DisplayName,
            Designation = teacher.Designation,
            DepartmentId = teacher.DepartmentId,
            Email = user?.Email ?? string.Empty,
            Role = displayRole,
            Status = teacher.Status.ToString(),
            IsTeacher = true,
            CanCreateQuestions = teacher.CanCreateQuestions,
            CanManageQuestionBank = teacher.CanManageQuestionBank,
            CanApproveQuestions = teacher.CanApproveQuestions,
            CanGeneratePaper = teacher.CanGeneratePaper,
            CanManageBlueprints = teacher.CanManageBlueprints,
            CanEvaluate = teacher.CanEvaluate,
            CanModerate = teacher.CanModerate,
            CanReviewCodingQuestions = teacher.CanReviewCodingQuestions,
            CanScheduleExam = teacher.CanScheduleExam,
            CanPublishResults = teacher.CanPublishResults,
            CanAssignEvaluators = teacher.CanAssignEvaluators,
            CanProctor = teacher.CanProctor,
            CanManageAnalytics = teacher.CanManageAnalytics,
            Departments = new SelectList(departments, "Id", "Name", teacher.DepartmentId)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return NotFound();

        var request = new UserProfileUpdateRequest(
            DisplayName: model.DisplayName,
            Designation: model.Designation,
            DepartmentId: model.DepartmentId,
            CanCreateQuestions: model.CanCreateQuestions,
            CanManageQuestionBank: model.CanManageQuestionBank,
            CanApproveQuestions: model.CanApproveQuestions,
            CanGeneratePaper: model.CanGeneratePaper,
            CanManageBlueprints: model.CanManageBlueprints,
            CanEvaluate: model.CanEvaluate,
            CanModerate: model.CanModerate,
            CanReviewCodingQuestions: model.CanReviewCodingQuestions,
            CanScheduleExam: model.CanScheduleExam,
            CanPublishResults: model.CanPublishResults,
            CanAssignEvaluators: model.CanAssignEvaluators,
            CanProctor: model.CanProctor,
            CanManageAnalytics: model.CanManageAnalytics);

        var success = await _userService.UpdateProfileAsync(model.ProfileId, request, User, ct);
        if (!success)
        {
            TempData["Error"] = "Profile not found.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(Guid id, string? reason, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return NotFound();

        var success = await _userService.SuspendAsync(id, reason, User, ct);
        if (!success)
            TempData["Error"] = "Profile not found.";
        else
            TempData["Success"] = "User suspended.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivate(Guid id, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return NotFound();

        var success = await _userService.ReactivateAsync(id, User, ct);
        if (!success)
            TempData["Error"] = "Profile not found.";
        else
            TempData["Success"] = "User reactivated.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return NotFound();

        // Find profile (teacher or student)
        string? userId = null;
        var teacher = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.Id == id && t.InstituteId == instituteId.Value, ct);
        if (teacher is not null) userId = teacher.UserId;
        else
        {
            var student = await _db.StudentProfiles.FirstOrDefaultAsync(s => s.Id == id && s.InstituteId == instituteId.Value, ct);
            if (student is not null) userId = student.UserId;
        }

        if (userId is null) return NotFound();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        // Generate a new temporary password
        var newPassword = "Temp@" + new Random().Next(10000, 99999).ToString();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            TempData["Error"] = "Failed to reset password: " + string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        // Set MustChangePassword flag
        var policy = await _db.UserPasswordPolicies.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (policy is null)
        {
            _db.UserPasswordPolicies.Add(new Domain.Entities.UserPasswordPolicy { UserId = user.Id, MustChangePassword = true });
        }
        else
        {
            policy.MustChangePassword = true;
            policy.UpdatedAtUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);

        TempData["Success"] = $"Password reset for {user.Email}. Temporary password: {newPassword} (user must change on next login)";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> BulkImport(CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return NotFound();

        return View(new BulkImportResultViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkImport(IFormFile? file, CancellationToken ct)
    {
        var instituteId = await _scope.GetCurrentInstituteIdAsync(User, ct);
        if (instituteId is null) return NotFound();

        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Please select a CSV file.";
            return View(new BulkImportResultViewModel());
        }

        using var stream = file.OpenReadStream();
        var result = await _bulk.ImportStudentsAsync(instituteId.Value, stream, User, ct);

        var vm = new BulkImportResultViewModel
        {
            HasResult = true,
            SuccessCount = result.Succeeded,
            Errors = result.Errors.Select(e => $"Row {e.RowNumber}: {e.Message}").ToList(),
            Credentials = result.Credentials
        };

        return View(vm);
    }

    // ===== Helpers =====================================================

    private async Task<UserInviteViewModel> BuildInviteViewModel(Guid instituteId, CancellationToken ct)
    {
        var departments = await _db.Departments
            .Where(d => d.InstituteId == instituteId)
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync(ct);

        var classBatches = await _db.ClassBatches
            .Where(c => c.InstituteId == instituteId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(ct);

        var roleOptions = new[]
        {
            new { Value = Roles.Teacher, Text = "Teacher" },
            new { Value = Roles.Student, Text = "Student" },
            new { Value = Roles.InstituteAdmin, Text = "Institute Admin" }
        };

        return new UserInviteViewModel
        {
            Roles = new SelectList(roleOptions, "Value", "Text"),
            Departments = new SelectList(departments, "Id", "Name"),
            ClassBatches = new SelectList(classBatches, "Id", "Name")
        };
    }

    private string BuildActions(Guid profileId, string status, bool isTeacher)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"d-flex gap-1 flex-wrap\">");

        if (isTeacher)
            sb.Append($"<a href=\"{Url.Action("Edit", "Users", new { area = "Institute", id = profileId })}\" class=\"btn btn-outline-primary btn-sm\">Edit</a>");

        // Reset Password button (works for both teachers and students)
        sb.Append($"<form method=\"post\" action=\"{Url.Action("ResetPassword", "Users", new { area = "Institute", id = profileId })}\">" +
            $"<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"{GetAntiForgeryToken()}\" />" +
            "<button type=\"submit\" class=\"btn btn-outline-secondary btn-sm\" onclick=\"return confirm('Reset password for this user?')\">Reset Password</button></form>");

        if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append($"<form method=\"post\" action=\"{Url.Action("Suspend", "Users", new { area = "Institute", id = profileId })}\">");
            sb.Append("<input type=\"hidden\" name=\"reason\" value=\"Administrative action\" />");
            sb.Append($"<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"{GetAntiForgeryToken()}\" />");
            sb.Append("<button type=\"submit\" class=\"btn btn-warning btn-sm\">Suspend</button></form>");
        }
        else if (status.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append($"<form method=\"post\" action=\"{Url.Action("Reactivate", "Users", new { area = "Institute", id = profileId })}\">");
            sb.Append($"<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"{GetAntiForgeryToken()}\" />");
            sb.Append("<button type=\"submit\" class=\"btn btn-success btn-sm\">Reactivate</button></form>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    private string GetAntiForgeryToken()
    {
        return _antiforgery.GetAndStoreTokens(HttpContext).RequestToken!;
    }

    private static string RoleBadge(string role)
    {
        var classes = role switch
        {
            Roles.InstituteAdmin => "text-bg-primary",
            Roles.Teacher => "text-bg-info",
            Roles.Student => "text-bg-success",
            _ => "text-bg-secondary"
        };
        var encoded = System.Net.WebUtility.HtmlEncode(role);
        return $"<span class=\"badge rounded-pill {classes}\" style=\"font-size: 0.7rem;\">{encoded}</span>";
    }
}
