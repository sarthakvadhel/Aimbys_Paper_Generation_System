namespace Aimbys.Domain.Entities.Coding;

public class CodingTestCaseResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SubmissionId { get; set; }
    public Guid TestCaseId { get; set; }
    public bool Passed { get; set; }
    public string? ActualOutput { get; set; }
    public string ExpectedOutput { get; set; } = string.Empty;
    public int ExecutionTimeMs { get; set; }
    public int MemoryUsedKb { get; set; }
    public string? ErrorOutput { get; set; }
    public CodingSubmission? Submission { get; set; }
}
