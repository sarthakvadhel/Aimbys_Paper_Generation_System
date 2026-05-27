using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Coding;

public class CodingSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExamAttemptAnswerId { get; set; }
    public string Language { get; set; } = string.Empty; // "javascript", "python", "java", "cpp", "csharp", "sql"
    public string SourceCode { get; set; } = string.Empty;
    public ExecutionStatus ExecutionStatus { get; set; } = ExecutionStatus.Pending;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public int? TotalTestCases { get; set; }
    public int? PassedTestCases { get; set; }
    public ICollection<CodingTestCaseResult> TestCaseResults { get; set; } = new List<CodingTestCaseResult>();
}
