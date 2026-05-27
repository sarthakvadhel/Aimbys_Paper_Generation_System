using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Users;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Users;

/// <summary>
/// Implements user invite, profile update, suspend/reactivate, and
/// paginated user listing for the Institute Admin user-management screen.
/// </summary>
public sealed class UserManagementService : IUserManagementService
{
    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuditWriter _audit;
    private readonly DomainEventCollector _events;

    public UserManagementService(
        AppDbContext db,
        UserManager<IdentityUser> userManager,
        IAuditWriter audit,
        DomainEventCollector events)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
        _events = events;
    }

    public async Task<UserInviteResult> InviteAsync(
        UserInviteRequest request,
        ClaimsPrincipal actor,
        CancellationToken ct = default)
    {
        // Validate email not already taken
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return new UserInviteResult(false, "A user with this email already exists.");

        // Validate role
        var role = request.Role switch
        {
            Roles.Teacher => Roles.Teacher,
            Roles.Student => Roles.Student,
            Roles.InstituteAdmin => Roles.InstituteAdmin,
            _ => null
        };

        if (role is null)
            return new UserInviteResult(false, $"Invalid role: {request.Role}");

        // Create IdentityUser with random password
        var newUser = new IdentityUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = false
        };

        var password = GenerateRandomPassword();
        var createResult = await _userManager.CreateAsync(newUser, password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return new UserInviteResult(false, errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(newUser, role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(newUser);
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            return new UserInviteResult(false, errors);
        }

        Guid profileId;

        if (role == Roles.Teacher || role == Roles.InstituteAdmin)
        {
            var profile = new TeacherProfile
            {
                UserId = newUser.Id,
                InstituteId = request.InstituteId,
                DisplayName = request.DisplayName,
                DepartmentId = request.DepartmentId,
                Status = ProfileStatus.Active
            };
            _db.TeacherProfiles.Add(profile);
            profileId = profile.Id;
        }
        else // Student
        {
            if (request.ClassBatchId is null)
            {
                await _userManager.DeleteAsync(newUser);
                return new UserInviteResult(false, "ClassBatchId is required for student invites.");
            }

            var profile = new StudentProfile
            {
                UserId = newUser.Id,
                InstituteId = request.InstituteId,
                ClassBatchId = request.ClassBatchId.Value,
                DisplayName = request.DisplayName,
                Status = ProfileStatus.Active
            };
            _db.StudentProfiles.Add(profile);
            profileId = profile.Id;
        }

        await _audit.WriteAsync(
            "User.Invited",
            entityType: "User",
            entityId: newUser.Id,
            actorUserId: _userManager.GetUserId(actor),
            detailsJson: JsonSerializer.Serialize(new { request.Email, request.Role, request.InstituteId }),
            cancellationToken: ct);

        _events.Enqueue(new UserInvitedEvent
        {
            UserId = newUser.Id,
            Email = request.Email,
            Role = role,
            InstituteId = request.InstituteId
        });

        await _db.SaveChangesAsync(ct);

        return new UserInviteResult(true, ProfileId: profileId, TemporaryPassword: password);
    }

    public async Task<bool> UpdateProfileAsync(
        Guid profileId,
        UserProfileUpdateRequest request,
        ClaimsPrincipal actor,
        CancellationToken ct = default)
    {
        var profile = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.Id == profileId, ct);
        if (profile is null) return false;

        if (request.DisplayName is not null) profile.DisplayName = request.DisplayName;
        if (request.Designation is not null) profile.Designation = request.Designation;
        if (request.DepartmentId is not null) profile.DepartmentId = request.DepartmentId;

        // Permission flags — only update non-null values
        if (request.CanCreateQuestions.HasValue) profile.CanCreateQuestions = request.CanCreateQuestions.Value;
        if (request.CanManageQuestionBank.HasValue) profile.CanManageQuestionBank = request.CanManageQuestionBank.Value;
        if (request.CanApproveQuestions.HasValue) profile.CanApproveQuestions = request.CanApproveQuestions.Value;
        if (request.CanGeneratePaper.HasValue) profile.CanGeneratePaper = request.CanGeneratePaper.Value;
        if (request.CanManageBlueprints.HasValue) profile.CanManageBlueprints = request.CanManageBlueprints.Value;
        if (request.CanEvaluate.HasValue) profile.CanEvaluate = request.CanEvaluate.Value;
        if (request.CanModerate.HasValue) profile.CanModerate = request.CanModerate.Value;
        if (request.CanReviewCodingQuestions.HasValue) profile.CanReviewCodingQuestions = request.CanReviewCodingQuestions.Value;
        if (request.CanScheduleExam.HasValue) profile.CanScheduleExam = request.CanScheduleExam.Value;
        if (request.CanPublishResults.HasValue) profile.CanPublishResults = request.CanPublishResults.Value;
        if (request.CanAssignEvaluators.HasValue) profile.CanAssignEvaluators = request.CanAssignEvaluators.Value;
        if (request.CanProctor.HasValue) profile.CanProctor = request.CanProctor.Value;
        if (request.CanManageAnalytics.HasValue) profile.CanManageAnalytics = request.CanManageAnalytics.Value;

        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            "User.ProfileUpdated",
            entityType: "TeacherProfile",
            entityId: profileId.ToString(),
            actorUserId: _userManager.GetUserId(actor),
            detailsJson: JsonSerializer.Serialize(new { request.DisplayName, request.Designation }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SuspendAsync(
        Guid profileId,
        string? reason,
        ClaimsPrincipal actor,
        CancellationToken ct = default)
    {
        var actorUserId = _userManager.GetUserId(actor) ?? string.Empty;

        // Try teacher first
        var teacher = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.Id == profileId, ct);
        if (teacher is not null)
        {
            teacher.Status = ProfileStatus.Suspended;
            teacher.UpdatedAtUtc = DateTime.UtcNow;

            await _audit.WriteAsync(
                "User.Suspended",
                entityType: "TeacherProfile",
                entityId: profileId.ToString(),
                actorUserId: actorUserId,
                detailsJson: JsonSerializer.Serialize(new { reason }),
                cancellationToken: ct);

            _events.Enqueue(new UserSuspendedEvent
            {
                SuspendedUserId = teacher.UserId,
                SuspendedByUserId = actorUserId,
                Reason = reason ?? string.Empty
            });

            await _db.SaveChangesAsync(ct);
            return true;
        }

        // Try student
        var student = await _db.StudentProfiles.FirstOrDefaultAsync(s => s.Id == profileId, ct);
        if (student is not null)
        {
            student.Status = ProfileStatus.Suspended;
            student.UpdatedAtUtc = DateTime.UtcNow;

            await _audit.WriteAsync(
                "User.Suspended",
                entityType: "StudentProfile",
                entityId: profileId.ToString(),
                actorUserId: actorUserId,
                detailsJson: JsonSerializer.Serialize(new { reason }),
                cancellationToken: ct);

            _events.Enqueue(new UserSuspendedEvent
            {
                SuspendedUserId = student.UserId,
                SuspendedByUserId = actorUserId,
                Reason = reason ?? string.Empty
            });

            await _db.SaveChangesAsync(ct);
            return true;
        }

        return false;
    }

    public async Task<bool> ReactivateAsync(
        Guid profileId,
        ClaimsPrincipal actor,
        CancellationToken ct = default)
    {
        var actorUserId = _userManager.GetUserId(actor);

        // Try teacher first
        var teacher = await _db.TeacherProfiles.FirstOrDefaultAsync(t => t.Id == profileId, ct);
        if (teacher is not null)
        {
            teacher.Status = ProfileStatus.Active;
            teacher.UpdatedAtUtc = DateTime.UtcNow;

            await _audit.WriteAsync(
                "User.Reactivated",
                entityType: "TeacherProfile",
                entityId: profileId.ToString(),
                actorUserId: actorUserId,
                cancellationToken: ct);

            await _db.SaveChangesAsync(ct);
            return true;
        }

        // Try student
        var student = await _db.StudentProfiles.FirstOrDefaultAsync(s => s.Id == profileId, ct);
        if (student is not null)
        {
            student.Status = ProfileStatus.Active;
            student.UpdatedAtUtc = DateTime.UtcNow;

            await _audit.WriteAsync(
                "User.Reactivated",
                entityType: "StudentProfile",
                entityId: profileId.ToString(),
                actorUserId: actorUserId,
                cancellationToken: ct);

            await _db.SaveChangesAsync(ct);
            return true;
        }

        return false;
    }

    public async Task<UserPageResult> GetPageAsync(UserPageFilter filter, CancellationToken ct = default)
    {
        // Build teacher query
        var teacherQuery = _db.TeacherProfiles
            .Where(t => t.InstituteId == filter.InstituteId)
            .AsQueryable();

        // Build student query
        var studentQuery = _db.StudentProfiles
            .Where(s => s.InstituteId == filter.InstituteId)
            .AsQueryable();

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(filter.StatusFilter)
            && Enum.TryParse<ProfileStatus>(filter.StatusFilter, true, out var statusEnum))
        {
            teacherQuery = teacherQuery.Where(t => t.Status == statusEnum);
            studentQuery = studentQuery.Where(s => s.Status == statusEnum);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(filter.SearchQuery))
        {
            var search = filter.SearchQuery.Trim().ToLower();
            teacherQuery = teacherQuery.Where(t => t.DisplayName.ToLower().Contains(search));
            studentQuery = studentQuery.Where(s => s.DisplayName.ToLower().Contains(search));
        }

        // Apply role filter
        var includeTeachers = string.IsNullOrWhiteSpace(filter.RoleFilter)
            || filter.RoleFilter.Equals(Roles.Teacher, StringComparison.OrdinalIgnoreCase)
            || filter.RoleFilter.Equals(Roles.InstituteAdmin, StringComparison.OrdinalIgnoreCase);
        var includeStudents = string.IsNullOrWhiteSpace(filter.RoleFilter)
            || filter.RoleFilter.Equals(Roles.Student, StringComparison.OrdinalIgnoreCase);

        var items = new List<UserListItem>();
        int totalTeachers = 0;
        int totalStudents = 0;

        if (includeTeachers)
        {
            totalTeachers = await teacherQuery.CountAsync(ct);
        }

        if (includeStudents)
        {
            totalStudents = await studentQuery.CountAsync(ct);
        }

        var totalCount = totalTeachers + totalStudents;
        var pageSize = filter.PageSize > 0 ? filter.PageSize : 20;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var page = Math.Clamp(filter.Page, 1, Math.Max(1, totalPages));
        var skip = (page - 1) * pageSize;

        // Project teachers with email join
        if (includeTeachers)
        {
            var teacherItems = await teacherQuery
                .OrderBy(t => t.DisplayName)
                .Select(t => new
                {
                    t.Id,
                    t.UserId,
                    t.DisplayName,
                    t.Status,
                    Department = t.Department != null ? t.Department.Name : null
                })
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(ct);

            foreach (var t in teacherItems)
            {
                var user = await _userManager.FindByIdAsync(t.UserId);
                var email = user?.Email ?? string.Empty;
                var roles = user != null ? await _userManager.GetRolesAsync(user) : Array.Empty<string>();
                var role = roles.Contains(Roles.InstituteAdmin) ? Roles.InstituteAdmin : Roles.Teacher;

                items.Add(new UserListItem(
                    ProfileId: t.Id,
                    UserId: t.UserId,
                    DisplayName: t.DisplayName,
                    Email: email,
                    Role: role,
                    Department: t.Department,
                    Status: t.Status.ToString(),
                    IsTeacher: true));
            }
        }

        // If we haven't filled the page, get students
        var remaining = pageSize - items.Count;
        if (includeStudents && remaining > 0)
        {
            var studentSkip = includeTeachers ? Math.Max(0, skip - totalTeachers) : skip;

            var studentItems = await studentQuery
                .OrderBy(s => s.DisplayName)
                .Select(s => new
                {
                    s.Id,
                    s.UserId,
                    s.DisplayName,
                    s.Status
                })
                .Skip(studentSkip)
                .Take(remaining)
                .ToListAsync(ct);

            foreach (var s in studentItems)
            {
                var user = await _userManager.FindByIdAsync(s.UserId);
                var email = user?.Email ?? string.Empty;

                items.Add(new UserListItem(
                    ProfileId: s.Id,
                    UserId: s.UserId,
                    DisplayName: s.DisplayName,
                    Email: email,
                    Role: Roles.Student,
                    Department: null,
                    Status: s.Status.ToString(),
                    IsTeacher: false));
            }
        }

        return new UserPageResult(items, totalCount, page, pageSize);
    }

    private static string GenerateRandomPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();

        char Pick(string set)
        {
            var b = new byte[1];
            rng.GetBytes(b);
            return set[b[0] % set.Length];
        }

        var chars = new char[12];
        chars[0] = Pick(upper);
        chars[1] = Pick(lower);
        chars[2] = Pick(digits);
        for (int i = 3; i < chars.Length; i++)
        {
            chars[i] = Pick(upper + lower + digits);
        }
        return new string(chars);
    }
}
