using Aimbys.Domain.Enums;

namespace Aimbys.Domain.Entities.Blueprints;

public class BlueprintConstraint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VersionId { get; set; }
    public Guid ChapterId { get; set; }
    public Guid? CompetencyId { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; }
    public QuestionType QuestionType { get; set; }
    public int Marks { get; set; }
    public int Count { get; set; }

    // Navigation
    public BlueprintVersion? Version { get; set; }
}
