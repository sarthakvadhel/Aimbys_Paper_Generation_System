using Aimbys.Application.Coding;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Coding;

/// <summary>
/// V1 mock implementation of <see cref="ICodeExecutionService"/>.
/// Real sandboxed execution requires OS-level process isolation that is
/// not available in this build environment.
/// </summary>
public sealed class CodeExecutionService : ICodeExecutionService
{
    private readonly ILogger<CodeExecutionService> _logger;

    public CodeExecutionService(ILogger<CodeExecutionService> logger)
    {
        _logger = logger;
    }

    public Task<CodeExecutionResult> RunSampleAsync(CodeRunRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Code execution infrastructure not wired — returning mock result");

        var results = new List<TestCaseResultItem>
        {
            new(Index: 1, Passed: true, ActualOutput: "Hello, World!", ExpectedOutput: "Hello, World!", ExecutionTimeMs: 12, ErrorOutput: null, IsHidden: false),
            new(Index: 2, Passed: true, ActualOutput: "42", ExpectedOutput: "42", ExecutionTimeMs: 8, ErrorOutput: null, IsHidden: false),
        };

        var result = new CodeExecutionResult(
            Success: true,
            Error: null,
            TotalTestCases: results.Count,
            PassedTestCases: results.Count,
            Results: results);

        return Task.FromResult(result);
    }

    public Task<CodeExecutionResult> RunFullAsync(CodeRunRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Code execution infrastructure not wired — returning mock result (full run)");

        var results = new List<TestCaseResultItem>
        {
            new(Index: 1, Passed: true, ActualOutput: "Hello, World!", ExpectedOutput: "Hello, World!", ExecutionTimeMs: 12, ErrorOutput: null, IsHidden: false),
            new(Index: 2, Passed: true, ActualOutput: "42", ExpectedOutput: "42", ExecutionTimeMs: 8, ErrorOutput: null, IsHidden: false),
            new(Index: 3, Passed: true, ActualOutput: "OK", ExpectedOutput: "OK", ExecutionTimeMs: 5, ErrorOutput: null, IsHidden: true),
        };

        var result = new CodeExecutionResult(
            Success: true,
            Error: null,
            TotalTestCases: results.Count,
            PassedTestCases: results.Count,
            Results: results);

        return Task.FromResult(result);
    }

    public Task<CodeExecutionResult?> GetResultAsync(Guid submissionId, CancellationToken ct = default)
    {
        // No persistence in V1 mock.
        return Task.FromResult<CodeExecutionResult?>(null);
    }
}
