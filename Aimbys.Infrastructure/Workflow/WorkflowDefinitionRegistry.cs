using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Workflow;

/// <summary>
/// Default <see cref="IWorkflowDefinitionRegistry"/>. Scans the
/// containing assembly for embedded resources under
/// <c>Aimbys.Infrastructure.Workflow.Definitions</c>, deserialises each
/// JSON document, validates its self-consistency, and exposes the
/// results as a keyed dictionary.
///
/// Validation rules applied per definition:
/// <list type="bullet">
///   <item><c>States</c> must be non-empty (initial state required).</item>
///   <item>Every transition's <c>fromState</c> and <c>toState</c> must
///         appear in <c>States</c>.</item>
///   <item>Every escalation rule's <c>state</c> must appear in
///         <c>States</c>.</item>
///   <item>Every <c>terminalState</c> must appear in <c>States</c>.</item>
/// </list>
/// A failed definition logs an error and is skipped &mdash; the
/// registry stays usable for the others.
/// </summary>
public sealed class WorkflowDefinitionRegistry : IWorkflowDefinitionRegistry
{
    /// <summary>
    /// Resource-name prefix the loader uses when filtering embedded
    /// resources. Held as a constant so tests can verify it stays in
    /// sync with the project's folder layout.
    /// </summary>
    public const string ResourcePrefix = "Aimbys.Infrastructure.Workflow.Definitions.";

    private readonly Dictionary<string, WorkflowDefinitionDocument> _byKey;

    public WorkflowDefinitionRegistry(ILogger<WorkflowDefinitionRegistry> logger)
    {
        _byKey = new Dictionary<string, WorkflowDefinitionDocument>(StringComparer.OrdinalIgnoreCase);

        var assembly = typeof(WorkflowDefinitionRegistry).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                        && n.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        foreach (var name in resourceNames)
        {
            try
            {
                using var stream = assembly.GetManifestResourceStream(name)
                    ?? throw new InvalidOperationException($"Embedded resource '{name}' could not be opened.");

                var doc = JsonSerializer.Deserialize<WorkflowDefinitionDocument>(stream, jsonOptions)
                    ?? throw new InvalidOperationException($"Definition '{name}' deserialised as null.");

                ValidateOrThrow(doc, name);

                if (_byKey.ContainsKey(doc.Key))
                {
                    logger.LogError(
                        "Workflow definition '{Key}' is registered twice. The first occurrence wins; ignoring '{Resource}'.",
                        doc.Key, name);
                    continue;
                }

                _byKey[doc.Key] = doc;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to load workflow definition resource '{Resource}'. Skipping.",
                    name);
            }
        }

        logger.LogInformation(
            "Loaded {Count} workflow definitions: {Keys}",
            _byKey.Count,
            string.Join(", ", _byKey.Keys));
    }

    public WorkflowDefinitionDocument? Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        return _byKey.TryGetValue(key, out var doc) ? doc : null;
    }

    public IReadOnlyList<WorkflowDefinitionDocument> All() => _byKey.Values.ToList();

    private static void ValidateOrThrow(WorkflowDefinitionDocument doc, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(doc.Key))
            throw new InvalidOperationException($"{sourceName}: 'key' is required.");

        if (doc.States.Count == 0)
            throw new InvalidOperationException($"{sourceName}: 'states' must contain at least one entry (the initial state).");

        var stateSet = new HashSet<string>(doc.States, StringComparer.OrdinalIgnoreCase);

        foreach (var t in doc.Transitions)
        {
            if (!stateSet.Contains(t.FromState))
                throw new InvalidOperationException($"{sourceName}: transition references unknown fromState '{t.FromState}'.");
            if (!stateSet.Contains(t.ToState))
                throw new InvalidOperationException($"{sourceName}: transition references unknown toState '{t.ToState}'.");
        }

        foreach (var e in doc.EscalationRules)
        {
            if (!stateSet.Contains(e.State))
                throw new InvalidOperationException($"{sourceName}: escalation rule references unknown state '{e.State}'.");
            if (e.MaxDurationMinutes <= 0)
                throw new InvalidOperationException($"{sourceName}: escalation rule for '{e.State}' must have maxDurationMinutes > 0.");
        }

        foreach (var s in doc.TerminalStates)
        {
            if (!stateSet.Contains(s))
                throw new InvalidOperationException($"{sourceName}: terminalState '{s}' is not declared in 'states'.");
        }
    }
}
