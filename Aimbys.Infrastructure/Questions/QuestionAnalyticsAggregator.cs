using Aimbys.Application.Scheduling;
using Aimbys.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Questions;

/// <summary>
/// Nightly aggregator that computes P-value, discrimination index, and
/// mean time per question from exam-attempt data. Detects difficulty
/// drift when observed P-value deviates significantly from the authored
/// difficulty band.
///
/// V1: no exam-attempt data exists yet (lands in Chunk 25+) so the job
/// logs a skip message and returns.
/// </summary>
public sealed class QuestionAnalyticsAggregator : IScheduledJobHandler
{
    public const string Key = "question.analytics.aggregate";
    public const string DefaultCron = "0 2 * * *"; // 02:00 UTC daily

    private readonly AppDbContext _db;
    private readonly ILogger<QuestionAnalyticsAggregator> _logger;

    public QuestionAnalyticsAggregator(AppDbContext db, ILogger<QuestionAnalyticsAggregator> logger)
    {
        _db = db;
        _logger = logger;
    }

    public string JobKey => Key;

    public Task ExecuteAsync(string? payload, CancellationToken cancellationToken)
    {
        // V1 implementation: since no exam-attempt data exists yet (Chunk 25+),
        // this is a no-op stub that logs and returns. The logic skeleton is:
        // 1. Find questions with new attempts since last run
        // 2. For each: compute P-value = correct/total, discrimination index
        // 3. Upsert QuestionUsageAnalytics row
        // 4. Compare P-value against authored difficulty:
        //    - Easy: expected P > 0.7; if P < 0.5 → drift=Harder
        //    - Medium: expected P 0.4-0.7; if P > 0.8 → drift=Easier, if P < 0.3 → drift=Harder
        //    - Hard: expected P < 0.4; if P > 0.6 → drift=Easier
        // 5. Write QuestionDifficultyAudit if drift detected

        _logger.LogInformation("QuestionAnalyticsAggregator: no exam-attempt data available yet (lands in Chunk 25+). Skipping.");
        return Task.CompletedTask;
    }
}
