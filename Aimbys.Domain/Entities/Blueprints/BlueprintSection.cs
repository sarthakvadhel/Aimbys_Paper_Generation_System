namespace Aimbys.Domain.Entities.Blueprints;

public class BlueprintSection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Marks { get; set; }
    public int QuestionCount { get; set; }
    public string? TypeMix { get; set; } // JSON: {"MCQ": 5, "Descriptive": 3}
    public int SortOrder { get; set; }

    // Navigation
    public BlueprintVersion? Version { get; set; }
}
