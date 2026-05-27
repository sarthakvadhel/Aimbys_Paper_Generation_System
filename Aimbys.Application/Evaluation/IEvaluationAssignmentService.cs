namespace Aimbys.Application.Evaluation;

public interface IEvaluationAssignmentService
{
    Task<int> AssignPendingAsync(Guid examId, CancellationToken ct = default);
}
