namespace Aimbys.Application.Coding;

public sealed record TestCaseResultItem(
    int Index,
    bool Passed,
    string? ActualOutput,
    string ExpectedOutput,
    int ExecutionTimeMs,
    string? ErrorOutput,
    bool IsHidden);
