using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Bulk;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Enums;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Bulk;

/// <summary>
/// Default <see cref="IBulkOperationService"/>. V1 surfaces full
/// implementations of the three methods the platform can already
/// exercise &mdash; <see cref="ImportStudentsAsync"/>,
/// <see cref="ActivateDeactivateBulkAsync"/>,
/// <see cref="NotifyBulkAsync"/> &mdash; and stubs the rest until
/// their underlying aggregates ship.
///
/// <para>
/// All methods enforce the InstituteAdmin / SuperAdmin role
/// themselves; controllers can still annotate
/// <c>[Authorize(Roles=...)]</c> for a clearer 403 path, but the
/// service is the source of truth.
/// </para>
/// </summary>
public sealed class BulkOperationService : IBulkOperationService
{
    /// <summary>
    /// Maximum rows persisted in a single <c>SaveChanges</c>. Keeps
    /// the change-tracker bounded and the SQL statement size sane.
    /// </summary>
    public const int BatchSize = 100;

    private readonly AppDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuditWriter _audit;
    private readonly ILogger<BulkOperationService> _logger;

    public BulkOperationService(
        AppDbContext db,
        UserManager<IdentityUser> userManager,
        IAuditWriter audit,
        ILogger<BulkOperationService> logger)
    {
        _db = db;
        _userManager = userManager;
        _audit = audit;
        _logger = logger;
    }

    // ===== ImportStudentsAsync =========================================

    public async Task<BulkOperationResult> ImportStudentsAsync(
        Guid instituteId,
        System.IO.Stream csvStream,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        EnsureInstituteOrSuperAdmin(actor);

        var rows = ParseStudentCsv(csvStream, out var parseErrors);
        var errors = new List<BulkOperationError>(parseErrors);

        var classBatches = await _db.ClassBatches
            .Where(c => c.InstituteId == instituteId)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);

        var batchByName = classBatches.ToDictionary(
            c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

        var existingAdmissions = await _db.StudentProfiles
            .IgnoreQueryFilters()
            .Where(s => s.InstituteId == instituteId && s.AdmissionNumber != null)
            .Select(s => s.AdmissionNumber!)
            .ToListAsync(cancellationToken);

        var admissionSet = new HashSet<string>(existingAdmissions, StringComparer.OrdinalIgnoreCase);

        int succeeded = 0;
        var credentials = new List<(string Email, string Password)>();
        var pendingProfiles = new List<StudentProfile>(BatchSize);
        var pendingPolicies = new List<Domain.Entities.UserPasswordPolicy>(BatchSize);

        for (int i = 0; i < rows.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = rows[i];
            var rowNumber = row.RowNumber;

            if (!IsValidEmail(row.Email))
            {
                errors.Add(new BulkOperationError(rowNumber, "row.email_invalid", "Email is not a valid address.", "Email"));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.DisplayName))
            {
                errors.Add(new BulkOperationError(rowNumber, "row.display_name_required", "DisplayName is required.", "DisplayName"));
                continue;
            }

            if (!batchByName.TryGetValue(row.ClassBatchName, out var classBatchId))
            {
                errors.Add(new BulkOperationError(rowNumber, "row.class_batch_not_found",
                    $"Class batch '{row.ClassBatchName}' not found in institute.", "ClassBatchName"));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(row.AdmissionNumber)
                && !admissionSet.Add(row.AdmissionNumber))
            {
                errors.Add(new BulkOperationError(rowNumber, "row.admission_number_duplicate",
                    $"Admission number '{row.AdmissionNumber}' already exists.", "AdmissionNumber"));
                continue;
            }

            var existingUser = await _userManager.FindByEmailAsync(row.Email);
            if (existingUser is not null)
            {
                errors.Add(new BulkOperationError(rowNumber, "row.email_exists",
                    $"A user with email '{row.Email}' already exists.", "Email"));
                continue;
            }

            // Use CSV password if provided, otherwise generate one
            var password = !string.IsNullOrWhiteSpace(row.Password)
                ? row.Password.Trim()
                : GenerateRandomPassword();

            var newUser = new IdentityUser
            {
                UserName = row.Email,
                Email = row.Email,
                EmailConfirmed = false
            };

            var created = await _userManager.CreateAsync(newUser, password);
            if (!created.Succeeded)
            {
                errors.Add(new BulkOperationError(rowNumber, "row.identity_create_failed",
                    string.Join("; ", created.Errors.Select(e => e.Description)), "Email"));
                continue;
            }

            var roleResult = await _userManager.AddToRoleAsync(newUser, Roles.Student);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(newUser);
                errors.Add(new BulkOperationError(rowNumber, "row.role_assignment_failed",
                    string.Join("; ", roleResult.Errors.Select(e => e.Description))));
                continue;
            }

            credentials.Add((row.Email, password));

            pendingProfiles.Add(new StudentProfile
            {
                UserId = newUser.Id,
                InstituteId = instituteId,
                ClassBatchId = classBatchId,
                DisplayName = row.DisplayName,
                AdmissionNumber = string.IsNullOrWhiteSpace(row.AdmissionNumber) ? null : row.AdmissionNumber,
                RollNumber = string.IsNullOrWhiteSpace(row.RollNumber) ? null : row.RollNumber,
                Status = ProfileStatus.Active
            });

            // Mark as must-change-password
            pendingPolicies.Add(new Domain.Entities.UserPasswordPolicy
            {
                UserId = newUser.Id,
                MustChangePassword = true
            });

            if (pendingProfiles.Count >= BatchSize)
            {
                _db.StudentProfiles.AddRange(pendingProfiles);
                _db.UserPasswordPolicies.AddRange(pendingPolicies);
                await _db.SaveChangesAsync(cancellationToken);
                succeeded += pendingProfiles.Count;
                pendingProfiles.Clear();
                pendingPolicies.Clear();
            }
        }

        if (pendingProfiles.Count > 0)
        {
            _db.StudentProfiles.AddRange(pendingProfiles);
            _db.UserPasswordPolicies.AddRange(pendingPolicies);
            await _db.SaveChangesAsync(cancellationToken);
            succeeded += pendingProfiles.Count;
        }

        await _audit.WriteAsync(
            "Bulk.StudentsImported",
            entityType: "Institute",
            entityId: instituteId.ToString(),
            actorUserId: _userManager.GetUserId(actor),
            detailsJson: JsonSerializer.Serialize(new { succeeded, failed = errors.Count }),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "ImportStudentsAsync(institute={InstituteId}) imported {Succeeded} students, {Failed} errors.",
            instituteId, succeeded, errors.Count);

        var result = BulkOperationResult.Create(succeeded, errors);
        result.Credentials.AddRange(credentials.Select(c => new BulkCredential(c.Email, c.Password)));
        return result;
    }

    // ===== ActivateDeactivateBulkAsync ================================

    public async Task<BulkOperationResult> ActivateDeactivateBulkAsync(
        IReadOnlyList<Guid> profileIds,
        bool active,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        EnsureInstituteOrSuperAdmin(actor);

        var errors = new List<BulkOperationError>();
        int succeeded = 0;
        var newStatus = active ? ProfileStatus.Active : ProfileStatus.Inactive;

        // Batch through the input list. Each batch loads the matching
        // profiles in a single query so 1000 ids becomes 10 round-trips.
        for (int offset = 0; offset < profileIds.Count; offset += BatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchIds = profileIds.Skip(offset).Take(BatchSize).ToArray();

            var teacherHits = await _db.TeacherProfiles
                .Where(t => batchIds.Contains(t.Id))
                .ToListAsync(cancellationToken);

            var studentHits = await _db.StudentProfiles
                .Where(s => batchIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var foundIds = new HashSet<Guid>(
                teacherHits.Select(t => t.Id).Concat(studentHits.Select(s => s.Id)));

            // Per-row "not found" errors first so the row numbering stays stable.
            for (int i = 0; i < batchIds.Length; i++)
            {
                var id = batchIds[i];
                if (!foundIds.Contains(id))
                {
                    errors.Add(new BulkOperationError(
                        offset + i + 1,
                        "row.profile_not_found",
                        $"Profile {id} was not found.",
                        "ProfileId"));
                }
            }

            foreach (var t in teacherHits)
            {
                t.Status = newStatus;
                t.UpdatedAtUtc = DateTime.UtcNow;
            }
            foreach (var s in studentHits)
            {
                s.Status = newStatus;
                s.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            succeeded += teacherHits.Count + studentHits.Count;
        }

        await _audit.WriteAsync(
            "Bulk.ProfilesActivationToggled",
            entityType: "Profile",
            entityId: $"count={profileIds.Count}",
            actorUserId: _userManager.GetUserId(actor),
            detailsJson: JsonSerializer.Serialize(new { active, succeeded, failed = errors.Count }),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return BulkOperationResult.Create(succeeded, errors);
    }

    // ===== NotifyBulkAsync ============================================

    public async Task<BulkOperationResult> NotifyBulkAsync(
        IReadOnlyList<string> recipientUserIds,
        string title,
        string template,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        EnsureInstituteOrSuperAdmin(actor);

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("title is required.", nameof(title));

        var errors = new List<BulkOperationError>();
        int succeeded = 0;

        for (int offset = 0; offset < recipientUserIds.Count; offset += BatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = recipientUserIds.Skip(offset).Take(BatchSize).ToArray();
            var notifications = new List<Notification>(batch.Length);

            for (int i = 0; i < batch.Length; i++)
            {
                var userId = batch[i];
                if (string.IsNullOrWhiteSpace(userId))
                {
                    errors.Add(new BulkOperationError(
                        offset + i + 1,
                        "row.recipient_blank",
                        "RecipientUserId is required.",
                        "RecipientUserId"));
                    continue;
                }

                notifications.Add(new Notification
                {
                    RecipientUserId = userId,
                    Title = title,
                    Body = template,
                    Severity = NotificationSeverity.Information,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            if (notifications.Count > 0)
            {
                _db.Notifications.AddRange(notifications);
                await _db.SaveChangesAsync(cancellationToken);
                succeeded += notifications.Count;
            }
        }

        await _audit.WriteAsync(
            "Bulk.NotificationsSent",
            entityType: "Notification",
            entityId: $"count={recipientUserIds.Count}",
            actorUserId: _userManager.GetUserId(actor),
            detailsJson: JsonSerializer.Serialize(new { succeeded, failed = errors.Count }),
            cancellationToken: cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);

        return BulkOperationResult.Create(succeeded, errors);
    }

    // ===== Stubs (persistence ships when underlying aggregates land) ==

    public Task<BulkOperationResult> AssignTeachersAsync(
        Guid instituteId,
        IReadOnlyList<BulkTeacherAssignment> assignments,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        EnsureInstituteOrSuperAdmin(actor);
        return Task.FromResult(StubResult(
            "AssignTeachersAsync",
            $"Implementation lands when the teacher-assignment entity ships. Received {assignments.Count} rows."));
    }

    public Task<BulkOperationResult> ScheduleExamsBulkAsync(
        Guid instituteId,
        IReadOnlyList<BulkExamSchedule> schedules,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        EnsureInstituteOrSuperAdmin(actor);
        return Task.FromResult(StubResult(
            "ScheduleExamsBulkAsync",
            $"Implementation lands when the Exam aggregate ships. Received {schedules.Count} rows."));
    }

    public Task<BulkOperationResult> PublishResultsBulkAsync(
        IReadOnlyList<Guid> examIds,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        EnsureInstituteOrSuperAdmin(actor);
        return Task.FromResult(StubResult(
            "PublishResultsBulkAsync",
            $"Implementation lands when the Result aggregate ships. Received {examIds.Count} rows."));
    }

    public Task<BulkOperationResult> AssignPapersBulkAsync(
        IReadOnlyList<BulkPaperAssignment> assignments,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        EnsureInstituteOrSuperAdmin(actor);
        return Task.FromResult(StubResult(
            "AssignPapersBulkAsync",
            $"Implementation lands when the Paper aggregate ships. Received {assignments.Count} rows."));
    }

    // ===== helpers ====================================================

    private static BulkOperationResult StubResult(string method, string note)
    {
        return BulkOperationResult.Create(
            succeeded: 0,
            errors: new List<BulkOperationError>
            {
                new(0, "method.not_implemented",
                    $"{method}: {note}",
                    Field: null)
            });
    }

    private static void EnsureInstituteOrSuperAdmin(ClaimsPrincipal actor)
    {
        if (actor.IsInRole(Roles.SuperAdmin) || actor.IsInRole(Roles.InstituteAdmin))
        {
            return;
        }

        throw new UnauthorizedAccessException(
            "Bulk operations require the SuperAdmin or InstituteAdmin role.");
    }

    private static string GenerateRandomPassword()
    {
        // Generates a password meeting the platform's policy:
        // upper, lower, digit, length >= 8.
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

    /// <summary>
    /// Trivial RFC-5321 sanity check; matches "looks like a@b.c" without
    /// the false-positives of a fully RFC-compliant regex. Good enough
    /// for a bulk-import gate; deeper validation runs at user creation.
    /// </summary>
    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var at = email.IndexOf('@');
        if (at <= 0 || at == email.Length - 1) return false;
        var dot = email.IndexOf('.', at);
        return dot > at && dot < email.Length - 1;
    }

    /// <summary>
    /// Parses the import CSV into typed rows. Tolerant of trailing
    /// whitespace and quoted fields; rows that don't have the
    /// expected column count become a parse-level error rather than
    /// throwing, so the import surface returns a structured result
    /// even on malformed input.
    /// </summary>
    private static List<StudentImportRow> ParseStudentCsv(
        System.IO.Stream csvStream,
        out List<BulkOperationError> parseErrors)
    {
        parseErrors = new List<BulkOperationError>();
        var rows = new List<StudentImportRow>();

        using var reader = new StreamReader(csvStream, leaveOpen: true);
        var headerLine = reader.ReadLine();
        if (headerLine is null)
        {
            parseErrors.Add(new BulkOperationError(0, "csv.empty",
                "CSV stream contains no header row.", null));
            return rows;
        }

        var header = ParseCsvLine(headerLine).Select(h => h.Trim()).ToArray();
        var required = new[] { "Email", "DisplayName", "AdmissionNumber", "RollNumber", "ClassBatchName" };
        if (header.Length < required.Length
            || !header.Take(required.Length).SequenceEqual(required, StringComparer.OrdinalIgnoreCase))
        {
            parseErrors.Add(new BulkOperationError(0, "csv.header_mismatch",
                $"Expected header: {string.Join(",", required)} (Password column optional)", null));
            return rows;
        }
        // Password is optional 6th column
        bool hasPasswordCol = header.Length > 5
            && string.Equals(header[5], "Password", StringComparison.OrdinalIgnoreCase);

        int rowNumber = 1; // header was 1, first data row is 2
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = ParseCsvLine(line);
            if (fields.Count < required.Length)
            {
                parseErrors.Add(new BulkOperationError(rowNumber, "csv.column_count",
                    $"Row has {fields.Count} columns, expected at least {required.Length}.", null));
                continue;
            }

            rows.Add(new StudentImportRow(
                RowNumber: rowNumber,
                Email: fields[0].Trim(),
                DisplayName: fields[1].Trim(),
                AdmissionNumber: fields[2].Trim(),
                RollNumber: fields[3].Trim(),
                ClassBatchName: fields[4].Trim(),
                Password: hasPasswordCol && fields.Count > 5 ? fields[5].Trim() : null));
        }

        return rows;
    }

    /// <summary>
    /// Minimal CSV line parser supporting double-quoted fields with
    /// embedded commas and "" escapes. Adequate for a bulk-import
    /// surface; switch to CsvHelper if richer dialects become needed.
    /// </summary>
    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '"' && sb.Length == 0)
                {
                    inQuotes = true;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        fields.Add(sb.ToString());
        return fields;
    }

    private sealed record StudentImportRow(
        int RowNumber,
        string Email,
        string DisplayName,
        string AdmissionNumber,
        string RollNumber,
        string ClassBatchName,
        string? Password = null);
}
