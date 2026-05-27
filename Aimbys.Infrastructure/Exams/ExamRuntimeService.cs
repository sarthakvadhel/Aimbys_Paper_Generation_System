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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Exams;

/// <summary>
/// Manages the runtime lifecycle of an exam attempt: start, save answers,
/// flag questions, and submit. On submit, auto-evaluates objective questions
/// immediately and routes manual questions into the evaluator workflow.
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
    private readonly DomainEventCollector _events;
    private readonly ILogger<ExamRuntimeService> _logger;

    public ExamRuntimeService(
        AppDbContext db,
        IEvaluationAssignmentService assignmentService,
        IAnalyticsAggregationService analytics,
        DomainEventCollector events,
        ILogger<ExamRuntimeService> logger)
    {
        _db = db;
        _assignmentService = assignmentService;
        _analytics = analytics;
        _events = events;
        _logger = logger;
    }

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

        _logger.LogInformation("Attempt {AttemptId} started for student {StudentProfileId} on exam {ExamId}.",
            attempt.Id, studentProfileId, examId);

        return new ExamAttemptResult(true, AttemptId: attempt.Id);
    }

    public async Task<SaveAnswerResult> SaveAnswerAsync(
        Guid attemptId, Guid questionId, string? answerJson, string studentUserId,
        CancellationToken ct = default)
    {
        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

        if (attempt?.Exam == null)
            return new SaveAnswerResult(false, "Attempt not found.");

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
            answer = new ExamAttemptAnswer
            {
                AttemptId = attemptId,
                QuestionId = questionId,
                QuestionVersionId = questionId,
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
        var answer = await _db.ExamAttemptAnswers
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == questionId, ct);

        if (answer == null)
        {
            answer = new ExamAttemptAnswer
            {
                AttemptId = attemptId,
                QuestionId = questionId,
                QuestionVersionId = questionId,
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

    public async Task<SubmitResult> SubmitAsync(
        Guid attemptId, string studentUserId, CancellationToken ct = default)
    {
        var attempt = await _db.ExamAttempts
            .Include(a => a.Exam)
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);

        if (attempt?.Exam == null)
            return new SubmitResult(false, "Attempt not found.");

        if (attempt.Status != AttemptStatus.InProgress)
            return new SubmitResult(false, "Attempt is not in progress.");

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

        if (!hasManualQuestions)
        {
            // All questions are auto-evaluated — publish immediately.
            result.State = ResultState.Published;
            result.IsPublished = true;
            result.PublishedAtUtc = DateTime.UtcNow;
            result.PublishedByUserId = "system";
            result.Grade = ComputeGrade(result.Percentage);
            attempt.Status = AttemptStatus.Published;
        }

        await _db.SaveChangesAsync(ct);

        if (hasManualQuestions)
            await _assignmentService.AssignPendingAsync(attempt.ExamId, ct);
        else
            await _analytics.RecomputeLeaderboardAsync(attempt.ExamId, ct);

        _events.Enqueue(new ExamSubmittedEvent
        {
            AttemptId = attempt.Id,
            ExamId = attempt.ExamId,
            StudentProfileId = attempt.StudentProfileId,
            HasManualQuestions = hasManualQuestions,
            TotalAutoScore = totalAutoScore
        });

        _logger.LogInformation(
            "Attempt {AttemptId} submitted. AutoScore={Score}, HasManual={HasManual}.",
            attemptId, totalAutoScore, hasManualQuestions);

        return new SubmitResult(true, TotalAutoScore: totalAutoScore);
    }

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

    private static string ComputeGrade(double pct) => pct switch
    {
        >= 90 => "A+",
        >= 80 => "A",
        >= 70 => "B+",
        >= 60 => "B",
        >= 50 => "C",
        >= 40 => "D",
        _ => "F"
    };
}
