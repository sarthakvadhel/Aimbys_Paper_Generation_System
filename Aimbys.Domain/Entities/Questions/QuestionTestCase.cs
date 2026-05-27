namespace Aimbys.Domain.Entities.Questions;

public class QuestionTestCase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VersionId { get; set; }
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public int TimeoutMs { get; set; } = 5000;
    public int MemoryLimitMb { get; set; } = 256;
    public int SortOrder { get; set; }

    public QuestionVersion? Version { get; set; }
}
