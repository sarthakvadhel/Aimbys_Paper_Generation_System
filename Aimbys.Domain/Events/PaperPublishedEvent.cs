namespace Aimbys.Domain.Events;

/// <summary>
/// Raised when an Institute Admin publishes an approved paper, making
/// it available for exam scheduling.
/// </summary>
public sealed record PaperPublishedEvent : DomainEventBase
{
    public Guid PaperId { get; init; }
    public string PaperTitle { get; init; } = string.Empty;
    public string PublishedByUserId { get; init; } = string.Empty;

    /// <summary>Identity user id of the paper's author (notification recipient).</summary>
    public string AuthorUserId { get; init; } = string.Empty;
}
