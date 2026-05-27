using Aimbys.Application.Evaluation;
using Aimbys.Domain.Entities.Evaluation;
using Aimbys.Domain.Enums;
using Aimbys.Domain.Events;
using Aimbys.Infrastructure.Notifications;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aimbys.Infrastructure.Evaluation;

/// <summary>
/// Assigns pending manual-evaluation answers to teachers with CanEvaluate
/// permission using round-robin distribution.
/// </summary>
public class EvaluationAssignmentService : IEvaluationAssignmentService
{
    private static readonly HashSet<QuestionType> ManualTypes = new()
    {
        QuestionType.TITA,
        QuestionType.Descriptive,
        QuestionType.Coding,
        QuestionType.FileUpload,
        QuestionType.CaseStudy,
        QuestionType.Equation,
    };

    private readonly AppDbContext _db;
    private readonly DomainEventCollector _events;

    public EvaluationAssignmentService(AppDbContext db, DomainEventCollector events)
    {
        _db = db;
        _events = events;
    }

    public async Task<int> AssignPendingAsync(Guid examId, CancellationToken ct = default)
    {
        var evaluators = await _db.TeacherProfiles
            .Where(t => t.CanEvaluate && t.Status == ProfileStatus.Active)
            .OrderBy(t => t.Id)
            .ToListAsync(ct);

        if (evaluators.Count == 0) return 0;

        var attemptIds = await _db.ExamAttempts
            .Where(a => a.ExamId == examId
                        && (a.Status == AttemptStatus.Submitted || a.Status == AttemptStatus.Evaluated))
            .Select(a => a.Id)
            .ToListAsync(ct);

        if (attemptIds.Count == 0) return 0;

        var alreadyAssignedAnswerIds = await _db.Evaluations
            .Select(e => e.AttemptAnswerId)
            .ToListAsync(ct);

        var manualAnswers = await _db.ExamAttemptAnswers
            .Where(a => attemptIds.Contains(a.AttemptId)
                        && !alreadyAssignedAnswerIds.Contains(a.Id))
            .Join(_db.Questions,
                  ans => ans.QuestionId,
                  q => q.Id,
                  (ans, q) => new { Answer = ans, q.Type })
            .Where(x => ManualTypes.Contains(x.Type))
            .ToListAsync(ct);

        if (manualAnswers.Count == 0) return 0;

        int assigned = 0;
        int idx = 0;

        foreach (var item in manualAnswers)
        {
            var evaluator = evaluators[idx % evaluators.Count];
            idx++;

            var evaluation = new Domain.Entities.Evaluation.Evaluation
            {
                AttemptAnswerId = item.Answer.Id,
                EvaluatorTeacherProfileId = evaluator.Id,
                Status = EvaluationStatus.Pending,
                AssignedAtUtc = DateTime.UtcNow
            };
            _db.Evaluations.Add(evaluation);

            _events.Enqueue(new EvaluationAssignedEvent
            {
                EvaluationId = evaluation.Id,
                AssignedToUserId = evaluator.UserId,
                ExamTitle = string.Empty,
                StudentName = string.Empty
            });

            assigned++;
        }

        await _db.SaveChangesAsync(ct);
        return assigned;
    }
}
