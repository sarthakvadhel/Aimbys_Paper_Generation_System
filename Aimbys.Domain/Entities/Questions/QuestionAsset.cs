namespace Aimbys.Domain.Entities.Questions;

public class QuestionAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public Guid FileAssetId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
