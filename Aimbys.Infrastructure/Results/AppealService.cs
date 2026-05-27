using System.Security.Claims;
using Aimbys.Application.Audit;
using Aimbys.Application.Results;
using Aimbys.Domain.Entities.Results;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Results;

/// <summary>
/// V1 appeal service. Handles filing, upholding, and adjusting appeals
/// against published scores.
/// </summary>
public class AppealService : IAppealService
{
    private readonly AppDbContext _db;
    private readonly IAuditWriter _audit;
    private readonly DomainEventCollector _events;

    public AppealService(
        AppDbContext db,
        IAuditWriter audit,
        DomainEventCollector events)
    {
        _db = db;
        _audit = audit;
        _events = events;
    }

    public async Task<AppealResult> FileAppealAsync(
        Guid attemptAnswerId, string studentUserId, string reason, CancellationToken ct = default)
    {
        // Validate no existing open appeal for the same answer.
        var existingOpen = await _db.ResultAppeals.AnyAsync(
            a => a.ExamAttemptAnswerId == attemptAnswerId
                 && (a.Status == AppealStatus.Open || a.Status == AppealStatus.UnderReview),
            ct);

        if (existingOpen)
            return new AppealResult(false, "An open appeal already exists for this answer.", null);

        // Resolve StudentProfileId from userId.
        var studentProfile = await _db.StudentProfiles
            .FirstOrDefaultAsync(sp => sp.UserId == studentUserId, ct);

        if (studentProfile is null)
            return new AppealResult(false, "Student profile not found.", null);

        var appeal = new ResultAppeal
        {
            ExamAttemptAnswerId = attemptAnswerId,
            StudentProfileId = studentProfile.Id,
            Reason = reason,
            Status = AppealStatus.Open,
            FiledAtUtc = DateTime.UtcNow
        };

        _db.ResultAppeals.Add(appeal);

        _events.Enqueue(new AppealFiledEvent
        {
            AppealId = appeal.Id,
            ExamAttemptAnswerId = attemptAnswerId,
            StudentProfileId = studentProfile.Id
        });

        await _audit.WriteAsync(
            "Appeal.Filed",
            "ResultAppeal",
            appeal.Id.ToString(),
            studentUserId,
            System.Text.Json.JsonSerializer.Serialize(new { attemptAnswerId, reason }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        return new AppealResult(true, null, appeal.Id);
    }

    public async Task<AppealResult> UpholdAsync(
        Guid appealId, ClaimsPrincipal actor, string? comment, CancellationToken ct = default)
    {
        var actorUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier);

        var appeal = await _db.ResultAppeals.FindAsync(new object[] { appealId }, ct);
        if (appeal is null)
            return new AppealResult(false, "Appeal not found.", null);

        appeal.Status = AppealStatus.UpheldOriginal;
        appeal.ResolvedAtUtc = DateTime.UtcNow;

        await _audit.WriteAsync(
            "Appeal.Upheld",
            "ResultAppeal",
            appealId.ToString(),
            actorUserId,
            System.Text.Json.JsonSerializer.Serialize(new { comment }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        return new AppealResult(true, null, appealId);
    }

    public async Task<AppealResult> AdjustAsync(
        Guid appealId, ClaimsPrincipal actor, decimal newScore, string reason, CancellationToken ct = default)
    {
        var actorUserId = actor.FindFirstValue(ClaimTypes.NameIdentifier);

        var appeal = await _db.ResultAppeals.FindAsync(new object[] { appealId }, ct);
        if (appeal is null)
            return new AppealResult(false, "Appeal not found.", null);

        appeal.Status = AppealStatus.Adjusted;
        appeal.ResolvedAtUtc = DateTime.UtcNow;

        // Create new FinalPublishedScore version.
        var latestVersion = await _db.FinalPublishedScores
            .Where(fps => fps.ExamAttemptAnswerId == appeal.ExamAttemptAnswerId)
            .MaxAsync(fps => (int?)fps.Version, ct) ?? 0;

        var newPublishedScore = new FinalPublishedScore
        {
            ExamAttemptAnswerId = appeal.ExamAttemptAnswerId,
            PointsAwarded = newScore,
            MaxPoints = 100, // V1 placeholder
            Source = ScoreSource.Overridden,
            ComputedAtUtc = DateTime.UtcNow,
            Version = latestVersion + 1
        };

        _db.FinalPublishedScores.Add(newPublishedScore);

        await _audit.WriteAsync(
            "Appeal.Adjusted",
            "ResultAppeal",
            appealId.ToString(),
            actorUserId,
            System.Text.Json.JsonSerializer.Serialize(new { newScore, reason }),
            cancellationToken: ct);

        await _db.SaveChangesAsync(ct);

        return new AppealResult(true, null, appealId);
    }
}
