namespace Aimbys.Application.Coding;

public interface ICodeExecutionService
{
    Task<CodeExecutionResult> RunSampleAsync(CodeRunRequest request, CancellationToken ct = default);
    Task<CodeExecutionResult> RunFullAsync(CodeRunRequest request, CancellationToken ct = default);
    Task<CodeExecutionResult?> GetResultAsync(Guid submissionId, CancellationToken ct = default);
}
