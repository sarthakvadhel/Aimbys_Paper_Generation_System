namespace Aimbys.Domain.Events;

/// <summary>
/// Raised when a published paper is archived &mdash; it is no longer
/// usable for new exam events but historical attempts and results
/// remain intact.
/// </summary>
public sealed record PaperArchivedEvent : DomainEventBase
{
    public Guid PaperId { get; init; }
    public string PaperTitle { get; init; } = string.Empty;
    public string ArchivedByUserId { get; init; } = string.Empty;

    /// <summary>Identity user id of the paper's author (notification recipient).</summary>
    public string AuthorUserId { get; init; } = string.Empty;
}
