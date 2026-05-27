namespace Aimbys.Domain.Workflow;

/// <summary>
/// Single source of truth for the paper-approval workflow identity:
/// definition key, subject-type tag, and canonical state names. These
/// constants must be used wherever the engine API
/// (<c>IWorkflowService</c>) takes a string for definition / subject /
/// state &mdash; <strong>do not hardcode the strings inline</strong>.
///
/// <para>
/// State names mirror the entries in
/// <see cref="Aimbys.Domain.Enums.PaperStatus"/>; the JSON definition
/// at <c>Aimbys.Infrastructure/Workflow/Definitions/PaperApproval.json</c>
/// uses the same names and is loaded into the registry at startup.
/// </para>
/// </summary>
public static class PaperWorkflow
{
    /// <summary>Workflow definition key (matches <c>PaperApproval.json</c>).</summary>
    public const string DefinitionKey = "PaperApproval";

    /// <summary>Subject-type tag stored on every <c>WorkflowInstance</c>.</summary>
    public const string SubjectType = "Paper";

    /// <summary>Canonical state names. Mirror <c>PaperStatus</c> values.</summary>
    public static class States
    {
        public const string Draft = nameof(Draft);
        public const string SubmittedForApproval = nameof(SubmittedForApproval);
        public const string Approved = nameof(Approved);
        public const string Returned = nameof(Returned);
        public const string Published = nameof(Published);
        public const string Archived = nameof(Archived);
    }

    /// <summary>
    /// Maps a <see cref="Aimbys.Domain.Enums.PaperStatus"/> to its
    /// equivalent workflow state name.
    /// </summary>
    public static string StateForStatus(Aimbys.Domain.Enums.PaperStatus status) => status switch
    {
        Aimbys.Domain.Enums.PaperStatus.Draft                => States.Draft,
        Aimbys.Domain.Enums.PaperStatus.SubmittedForApproval => States.SubmittedForApproval,
        Aimbys.Domain.Enums.PaperStatus.Approved             => States.Approved,
        Aimbys.Domain.Enums.PaperStatus.Returned             => States.Returned,
        Aimbys.Domain.Enums.PaperStatus.Published            => States.Published,
        Aimbys.Domain.Enums.PaperStatus.Archived             => States.Archived,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
