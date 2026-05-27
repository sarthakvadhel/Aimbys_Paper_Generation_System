using System.Security.Claims;

namespace Aimbys.Application.Exams;

public interface IExamSchedulingService
{
    Task<ExamScheduleResult> ScheduleAsync(ExamScheduleRequest request, ClaimsPrincipal actor, CancellationToken ct = default);
    Task<ExamGoLiveResult> GoLiveAsync(Guid examId, ClaimsPrincipal actor, CancellationToken ct = default);
}
