using Aimbys.Application.Questions;
using Aimbys.Domain.Entities.Questions;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Questions;

/// <summary>
/// Records question exposure events and surfaces usage/risk summaries
/// from the analytics tables populated by the nightly aggregator.
/// </summary>
public sealed class QuestionAnalyticsService : IQuestionAnalyticsService
{
    private readonly AppDbContext _db;

    public QuestionAnalyticsService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task RecordExposureAsync(Guid questionId, Guid paperId, Guid instituteId, CancellationToken ct = default)
    {
        var log = new QuestionExposureLog
        {
            QuestionId = questionId,
            PaperId = paperId,
            InstituteId = instituteId,
            ExposedAtUtc = DateTime.UtcNow
        };

        _db.QuestionExposureLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<QuestionUsageSummary?> GetUsageSummaryAsync(Guid questionId, CancellationToken ct = default)
    {
        var row = await _db.QuestionUsageAnalytics
            .Where(a => a.QuestionId == questionId)
            .OrderByDescending(a => a.ComputedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (row is null) return null;

        return new QuestionUsageSummary(
            PapersUsedIn: row.PapersUsedIn,
            AttemptsCount: row.AttemptsCount,
            PValue: row.PValue,
            DiscriminationIndex: row.DiscriminationIndex,
            MeanTimeSeconds: row.MeanTimeSeconds,
            ComputedAtUtc: row.ComputedAtUtc);
    }

    /// <inheritdoc />
    public async Task<QuestionExposureRisk> GetExposureRiskAsync(Guid questionId, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-12);

        var count = await _db.QuestionExposureLogs
            .Where(e => e.QuestionId == questionId && e.ExposedAtUtc >= cutoff)
            .CountAsync(ct);

        var risk = count > 5 ? "High"
                 : count >= 3 ? "Medium"
                 : "Low";

        return new QuestionExposureRisk(ExposureCount: count, RiskLevel: risk);
    }
}
