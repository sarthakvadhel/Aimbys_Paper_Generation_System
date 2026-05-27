namespace Aimbys.Application.Coding;

public sealed record CodeExecutionResult(
    bool Success,
    string? Error,
    int TotalTestCases,
    int PassedTestCases,
    IReadOnlyList<TestCaseResultItem> Results);
