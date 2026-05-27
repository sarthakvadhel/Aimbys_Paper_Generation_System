using Aimbys.Domain.SoftDelete;

namespace Aimbys.Domain.Entities;

public class Stream : ISoftDelete
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstituteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Institute? Institute { get; set; }
    public ICollection<Major> Majors { get; set; } = new List<Major>();
}
