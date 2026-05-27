namespace Aimbys.Infrastructure.Workflow;

/// <summary>
/// In-memory registry of parsed workflow definitions. Loaded once on
/// application start from embedded JSON resources; consumed by
/// <c>WorkflowEngine</c> to resolve transition rules without re-parsing
/// JSON on every call.
/// </summary>
public interface IWorkflowDefinitionRegistry
{
    /// <summary>
    /// Returns the definition for <paramref name="key"/>, or <c>null</c>
    /// if no such definition is registered.
    /// </summary>
    WorkflowDefinitionDocument? Get(string key);

    /// <summary>Returns every registered definition. Used at startup for the database upsert.</summary>
    IReadOnlyList<WorkflowDefinitionDocument> All();
}
