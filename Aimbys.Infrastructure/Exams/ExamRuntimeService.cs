using System.Text.Json;
using Aimbys.Application.Analytics;
using Aimbys.Application.Evaluation;
using Aimbys.Application.Exams;
using Aimbys.Application.Results;
using Aimbys.Domain.Entities.Evaluation;
using Aimbys.Domain.Entities.Exams;
using Aimbys.Domain.Entities.Results;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Analytics;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Aimbys.Infrastructure.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Exams;

/// <summary>
/// Manages the runtime lifecycle of an exam attempt. Every mutating
/// method that takes a <c>studentUserId</c> enforces ownership at the
/// service boundary; <see cref="AutoSubmitAsync"/> is the system-only
/// counterpart used by the auto-submit job and the suspicion-driven
/// force-submit path.
/// </summary>
public sealed class ExamRuntimeService : IExamRuntimeService
{
    private static readonly HashSet<QuestionType> AutoTypes = new()
    {
        QuestionType.MCQ,
        QuestionType.MultiSelect,
        QuestionType.TrueFalse,
        QuestionType.FillBlanks,
    };

    private readonly AppDbContext _db;
    private readonly IEvaluationAssignmentService _assignmentService;
    private readonly IAnalyticsAggregationService _analytics;
    private readonly IResultPublicationService _publication;
    private readonly DomainEventCollector _events;
    private readonly ILogger<ExamRuntimeService> _logger;

    public ExamRuntimeService(
        AppDbContext db,
        IEvaluationAssignmentService assignmentService,
        IAnalyticsAggregationService analytics,
        IResultPublicationService publication,
        DomainEventCollector events,
        ILogger<ExamRuntimeService> logger)
    {
        _db = db;
        _assignmentService = assignmentService;
        _analytics = analytics;
        _publication = publication;
        _events = events;
        _logger = logger;
    }

    // =========================================================================
    // Start
    // =========================================================================

    public async Task<ExamAttemptResult> StartAttemptAsync(
        Guid examId, Guid studentProfileId, CancellationToken ct = default)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.Id == examId, ct);
        if (exam == null)
            return new ExamAttemptResult(false, "Exam not found.");

        if (exam.Status != ExamStatus.Live && exam.Status != ExamStatus.Scheduled)
            return new ExamAttemptResult(false, "Exam is not available.");

        var studentInBatch = await _db.StudentProfiles
            .AnyAsync(sp => sp.Id == studentProfileId && sp.ClassBatchId == exam.ClassBatchId, ct);
        if (!studentInBatch)
            return new ExamAttemptResult(false, "Student is not in the exam's class batch.");

        var existingAttempt = await _db.ExamAttempts
            .AnyAsync(a => a.ExamId == examId && a.StudentProfileId == studentProfileId, ct);
        if (existingAttempt)
            return new ExamAttemptResult(false, "An attempt already exists for this student.");

        var attempt = new ExamAttempt
        {
            ExamId = examId,
            StudentProfileId = studentProfileId,
            Status = AttemptStatus.InProgress,
            StartedAtUtc = DateTime.UtcNow
        };
        _db.ExamAttempts.Add(attempt);

        var paperQuestions = await _db.PaperQuestions
            .Where(pq => pq.VersionId == exam.PaperVersionId)
            .OrderBy(pq => pq.SortOrder)
            .ToListAsync(ct);

        foreach (var pq in paperQuestions)
        {
            _db.ExamAttemptAnswers.Add(new ExamAttemptAnswer
            {
                AttemptId = attempt.Id,
                QuestionId = pq.QuestionId,
                QuestionVersionId = pq.QuestionVersionId,
                LastSavedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Attempt {AttemptId} started for student {StudentProfileId} on exam {ExamId}.",
            attempt.Id, studentProfileId, examId);

        return new ExamAttemptResult(true, AttemptId: attempt.Id);
    }

    // =========================================================================
    // SaveAnswer / FlagQuestion (HTTP-owned)
    // =========================================================================

    public async Task<SaveAnswerResult> SaveAnswerAsync(
        Guid attemptId, Guid questionId, string? answerJson, string studentUserId,
        CancellationToken ct = default)
    {
        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

        if (attempt?.Exam == null)
            return new SaveAnswerResult(false, "Attempt not found.");

        // Ownership: tenant-/student-scoped at the service boundary so a
        // hostile caller cannot save into another student's attempt by
        // guessing GUIDs.
        if (!OwnerMatches(attempt, studentUserId))
            return new SaveAnswerResult(false, "forbidden");

        if (attempt.Status != AttemptStatus.InProgress)
            return new SaveAnswerResult(false, "Attempt is not in progress.");

        if (attempt.StartedAtUtc.HasValue)
        {
            var deadline = attempt.StartedAtUtc.Value.AddMinutes(attempt.Exam.DurationMinutes);
            if (DateTime.UtcNow > deadline)
                return new SaveAnswerResult(false, "timer_expired");
        }

        var answer = await _db.ExamAttemptAnswers
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == questionId, ct);

        if (answer == null)
        {
            // Resolve the current question version so the answer is bound
            // to the right version (the previous code reused questionId,
            // which left answers pointing at a non-existent version).
            var currentVersionId = await _db.QuestionVersions
                .AsNoTracking()
                .Where(v => v.QuestionId == questionId && v.IsCurrentVersion)
                .Select(v => v.Id)
                .FirstOrDefaultAsync(ct);

            answer = new ExamAttemptAnswer
            {
                AttemptId = attemptId,
                QuestionId = questionId,
                QuestionVersionId = currentVersionId == Guid.Empty ? questionId : currentVersionId,
                AnswerJson = answerJson,
                LastSavedAtUtc = DateTime.UtcNow
            };
            _db.ExamAttemptAnswers.Add(answer);
        }
        else
        {
            answer.AnswerJson = answerJson;
            answer.LastSavedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return new SaveAnswerResult(true);
    }

    public async Task<bool> FlagQuestionAsync(
        Guid attemptId, Guid questionId, bool flagged, string studentUserId,
        CancellationToken ct = default)
    {
        var attempt = await _db.ExamAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

        if (attempt is null || !OwnerMatches(attempt, studentUserId))
            return false;

        var answer = await _db.ExamAttemptAnswers
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == questionId, ct);

        if (answer == null)
        {
            var currentVersionId = await _db.QuestionVersions
                .AsNoTracking()
                .Where(v => v.QuestionId == questionId && v.IsCurrentVersion)
                .Select(v => v.Id)
                .FirstOrDefaultAsync(ct);

            answer = new ExamAttemptAnswer
            {
                AttemptId = attemptId,
                QuestionId = questionId,
                QuestionVersionId = currentVersionId == Guid.Empty ? questionId : currentVersionId,
                IsFlagged = flagged,
                LastSavedAtUtc = DateTime.UtcNow
            };
            _db.ExamAttemptAnswers.Add(answer);
        }
        else
        {
            answer.IsFlagged = flagged;
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    // =========================================================================
    // Submit (HTTP-owned + system AutoSubmit share core)
    // =========================================================================

    public async Task<SubmitResult> SubmitAsync(
        Guid attemptId, string studentUserId, CancellationToken ct = default)
    {
        var attempt = await LoadAttemptForSubmitAsync(attemptId, ct);
        if (attempt is null)
            return new SubmitResult(false, "Attempt not found.");

        if (!OwnerMatches(attempt, studentUserId))
            return new SubmitResult(false, "forbidden");

        if (attempt.Status != AttemptStatus.InProgress)
            return new SubmitResult(false, "Attempt is not in progress.");

        return await CompleteSubmissionAsync(attempt, isAutoSubmit: false, ct);
    }

    public async Task<SubmitResult> AutoSubmitAsync(
        Guid attemptId, CancellationToken ct = default)
    {
        var attempt = await LoadAttemptForSubmitAsync(attemptId, ct);
        if (attempt is null)
            return new SubmitResult(false, "Attempt not found.");

        if (attempt.Status != AttemptStatus.InProgress)
        {
            // Idempotent: already submitted attempts return success so
            // the background job can keep running without churn.
            return new SubmitResult(true, TotalAutoScore: attempt.TotalAutoScore ?? 0);
        }

        return await CompleteSubmissionAsync(attempt, isAutoSubmit: true, ct);
    }

    private Task<ExamAttempt?> LoadAttemptForSubmitAsync(Guid attemptId, CancellationToken ct) =>
        _db.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

    /// <summary>
    /// Shared submit implementation. Auto-evaluates objective answers,
    /// writes <see cref="FinalPublishedScore"/> rows, then either
    /// publishes the attempt (auto-only papers) or routes manual
    /// answers to the evaluation queue.
    /// </summary>
    private async Task<SubmitResult> CompleteSubmissionAsync(
        ExamAttempt attempt, bool isAutoSubmit, CancellationToken ct)
    {
        if (attempt.Exam is null)
            return new SubmitResult(false, "Exam not found.");

        // Load question types and version data for all answers in one query.
        var questionIds = attempt.Answers.Select(a => a.QuestionId).ToList();
        var questionVersionIds = attempt.Answers.Select(a => a.QuestionVersionId).ToList();

        var questions = await _db.Questions
            .Where(q => questionIds.Contains(q.Id))
            .Select(q => new { q.Id, q.Type })
            .ToDictionaryAsync(q => q.Id, ct);

        var questionVersions = await _db.QuestionVersions
            .Include(v => v.Options)
            .Where(v => questionVersionIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);

        var paperQuestions = await _db.PaperQuestions
            .Where(pq => pq.VersionId == attempt.Exam.PaperVersionId)
            .ToDictionaryAsync(pq => pq.QuestionId, ct);

        attempt.Status = AttemptStatus.Submitted;
        attempt.SubmittedAtUtc = DateTime.UtcNow;
        attempt.AutoSubmitted = isAutoSubmit;

        decimal totalAutoScore = 0m;
        decimal totalMaxScore = 0m;
        bool hasManualQuestions = false;
        var finalPublishedScores = new List<FinalPublishedScore>();

        foreach (var answer in attempt.Answers)
        {
            if (!questions.TryGetValue(answer.QuestionId, out var q)) continue;
            questionVersions.TryGetValue(answer.QuestionVersionId, out var qv);
            paperQuestions.TryGetValue(answer.QuestionId, out var pq);

            var maxMarks = pq?.MarksOverride ?? qv?.Marks ?? 0m;
            totalMaxScore += maxMarks;

            if (AutoTypes.Contains(q.Type))
            {
                var awarded = EvaluateAutoQuestion(q.Type, answer.AnswerJson, qv, maxMarks);
                answer.AutoMarksAwarded = awarded;
                totalAutoScore += awarded;

                finalPublishedScores.Add(new FinalPublishedScore
                {
                    ExamAttemptAnswerId = answer.Id,
                    PointsAwarded = awarded,
                    MaxPoints = maxMarks,
                    Source = ScoreSource.Auto,
                    ComputedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                hasManualQuestions = true;
            }
        }

        attempt.TotalAutoScore = totalAutoScore;

        var result = new Result
        {
            ExamAttemptId = attempt.Id,
            TotalScore = totalAutoScore,
            MaxScore = totalMaxScore,
            Percentage = totalMaxScore > 0 ? (double)(totalAutoScore / totalMaxScore * 100) : 0,
            State = hasManualQuestions ? ResultState.UnderEvaluation : ResultState.AutoEvaluated,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Results.Add(result);

        _db.FinalPublishedScores.AddRange(finalPublishedScores);

        // Record a Submitted-class event so the institute admin's exam
        // timeline shows whether the student submitted manually or the
        // system auto-submitted on timeout / suspicion.
        _db.ExamEvents.Add(new ExamEvent
        {
            AttemptId = attempt.Id,
            EventType = isAutoSubmit ? ExamEventType.AutoSubmitted : ExamEventType.ManualSubmitted,
            OccurredAtUtc = DateTime.UtcNow
        });

        // Persist the auto-evaluation work first; the publication path
        // re-reads FinalPublishedScores and the Result row.
        await _db.SaveChangesAsync(ct);

        if (hasManualQuestions)
        {
            // Route manual answers to the evaluator inbox; result will be
            // published later by the institute admin.
            await _assignmentService.AssignPendingAsync(attempt.ExamId, ct);
        }
        else
        {
            // Auto-only paper: publish this attempt's result through the
            // single sanctioned publication path so audit, recipient
            // notification, ranks, and leaderboard are all kept in sync.
            var publishResult = await _publication.PublishAttemptAsync(
                attempt.Id, ResultPublicationService.SystemActorId, ct);
            if (!publishResult.Success)
            {
                _logger.LogError(
                    "Auto-publish failed for attempt {AttemptId}: {Error}",
                    attempt.Id, publishResult.Error);
            }
        }

        _events.Enqueue(new ExamSubmittedEvent
        {
            AttemptId = attempt.Id,
            ExamId = attempt.ExamId,
            StudentProfileId = attempt.StudentProfileId,
            HasManualQuestions = hasManualQuestions,
            TotalAutoScore = totalAutoScore
        });

        _logger.LogInformation(
            "Attempt {AttemptId} submitted (auto={IsAuto}). AutoScore={Score}, HasManual={HasManual}.",
            attempt.Id, isAutoSubmit, totalAutoScore, hasManualQuestions);

        return new SubmitResult(true, TotalAutoScore: totalAutoScore);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Compares the attempt's owner with the caller-supplied user id.
    /// Treats <c>null</c> / empty as a non-match so a hostile body cannot
    /// pass an empty string and slip through.
    /// </summary>
    private static bool OwnerMatches(ExamAttempt attempt, string? studentUserId) =>
        !string.IsNullOrEmpty(studentUserId)
        && string.Equals(attempt.StudentUserId, studentUserId, StringComparison.Ordinal);

    // ----- auto-evaluation helpers ------------------------------------------

    private static decimal EvaluateAutoQuestion(
        QuestionType type,
        string? answerJson,
        Domain.Entities.Questions.QuestionVersion? qv,
        decimal maxMarks)
    {
        if (string.IsNullOrWhiteSpace(answerJson) || qv == null) return 0m;
        try
        {
            return type switch
            {
                QuestionType.MCQ => EvaluateMcq(answerJson, qv, maxMarks),
                QuestionType.TrueFalse => EvaluateTrueFalse(answerJson, qv, maxMarks),
                QuestionType.MultiSelect => EvaluateMultiSelect(answerJson, qv, maxMarks),
                QuestionType.FillBlanks => EvaluateFillBlanks(answerJson, qv, maxMarks),
                _ => 0m
            };
        }
        catch { return 0m; }
    }

    private static decimal EvaluateMcq(
        string answerJson,
        Domain.Entities.Questions.QuestionVersion qv,
        decimal maxMarks)
    {
        string? selected = null;
        try
        {
            using var doc = JsonDocument.Parse(answerJson);
            selected = doc.RootElement.ValueKind == JsonValueKind.String
                ? doc.RootElement.GetString()
                : doc.RootElement.TryGetProperty("selected", out var prop)
                    ? prop.GetString()
                    : null;
        }
        catch { selected = answerJson.Trim('"'); }

        if (selected == null) return 0m;
        var correct = qv.Options.FirstOrDefault(o => o.IsCorrect);
        return string.Equals(correct?.Label, selected, StringComparison.OrdinalIgnoreCase) ? maxMarks : 0m;
    }

    private static decimal EvaluateTrueFalse(
        string answerJson,
        Domain.Entities.Questions.QuestionVersion qv,
        decimal maxMarks)
    {
        bool? selected = null;
        try
        {
            using var doc = JsonDocument.Parse(answerJson);
            if (doc.RootElement.ValueKind == JsonValueKind.True) selected = true;
            else if (doc.RootElement.ValueKind == JsonValueKind.False) selected = false;
            else if (doc.RootElement.ValueKind == JsonValueKind.String)
                selected = bool.TryParse(doc.RootElement.GetString(), out var b) ? b : null;
        }
        catch { return 0m; }

        if (selected == null) return 0m;
        var correct = qv.Options.FirstOrDefault(o => o.IsCorrect);
        if (correct == null) return 0m;
        var correctBool = string.Equals(correct.Label, "True", StringComparison.OrdinalIgnoreCase)
                          || correct.Label == "1";
        return selected == correctBool ? maxMarks : 0m;
    }

    private static decimal EvaluateMultiSelect(
        string answerJson,
        Domain.Entities.Questions.QuestionVersion qv,
        decimal maxMarks)
    {
        List<string>? selected = null;
        try
        {
            using var doc = JsonDocument.Parse(answerJson);
            var arr = doc.RootElement.ValueKind == JsonValueKind.Array
                ? doc.RootElement
                : doc.RootElement.TryGetProperty("selected", out var prop) && prop.ValueKind == JsonValueKind.Array
                    ? prop
                    : (JsonElement?)null;
            if (arr == null) return 0m;
            selected = arr.Value.EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty)
                .ToList();
        }
        catch { return 0m; }

        if (selected == null || selected.Count == 0) return 0m;
        var correctLabels = qv.Options.Where(o => o.IsCorrect).Select(o => o.Label)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var selectedSet = selected.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return correctLabels.SetEquals(selectedSet) ? maxMarks : 0m;
    }

    private static decimal EvaluateFillBlanks(
        string answerJson,
        Domain.Entities.Questions.QuestionVersion qv,
        decimal maxMarks)
    {
        string? typed = null;
        try
        {
            using var doc = JsonDocument.Parse(answerJson);
            typed = doc.RootElement.ValueKind == JsonValueKind.String
                ? doc.RootElement.GetString()
                : null;
        }
        catch { typed = answerJson.Trim('"'); }

        if (typed == null) return 0m;
        var correct = qv.Options.FirstOrDefault(o => o.IsCorrect);
        return string.Equals(correct?.Text, typed, StringComparison.OrdinalIgnoreCase) ? maxMarks : 0m;
    }
}
