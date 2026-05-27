using System.Security.Claims;
using System.Text.Json;
using Aimbys.Application.Audit;
using Aimbys.Application.Authorization;
using Aimbys.Domain.Entities;
using Aimbys.Domain.Entities.Audit;
using Aimbys.Infrastructure.Identity;
using Aimbys.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Aimbys.Infrastructure.Audit;

/// <summary>
/// Default <see cref="IAuditVisibilityService"/>. Filters audit-log
/// rows against <see cref="AuditVisibilityRule"/> rows pulled once
/// per request (with a 1-minute cache); applies the rule's
/// role / permission gate, the compliance-mode gate, and field
/// masking inside <c>DetailsJson</c>.
///
/// <para>
/// SuperAdmin bypasses every layer &mdash; platform support must be
/// able to see the unredacted audit trail when investigating an
/// incident. The bypass is logged at the call site (the audit
/// viewer controller) rather than here.
/// </para>
/// </summary>
public sealed class AuditVisibilityService : IAuditVisibilityService
{
    /// <summary>Cache window for the visibility-rule snapshot.</summary>
    public static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(1);

    /// <summary>Replacement value written in place of masked fields.</summary>
    public const string MaskValue = "***";

    private const string CacheKey = "audit-visibility-rules";

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly IPermissionGuard _permissionGuard;

    public AuditVisibilityService(
        AppDbContext db,
        IMemoryCache cache,
        IPermissionGuard permissionGuard)
    {
        _db = db;
        _cache = cache;
        _permissionGuard = permissionGuard;
    }

    public async Task<AuditVisibilityResult> FilterAsync(
        IReadOnlyList<AuditLog> rows,
        ClaimsPrincipal actor,
        bool complianceMode = false,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
        {
            return new AuditVisibilityResult(Array.Empty<AuditLog>(), 0, 0);
        }

        // SuperAdmin bypass: see everything, masked nothing.
        if (actor.IsInRole(Roles.SuperAdmin))
        {
            return new AuditVisibilityResult(rows, HiddenCount: 0, MaskedCount: 0);
        }

        var ruleSnapshots = await GetRuleSnapshotsAsync(cancellationToken);
        if (ruleSnapshots.Count == 0)
        {
            // No rules configured → permissive default; render rows unchanged.
            return new AuditVisibilityResult(rows, HiddenCount: 0, MaskedCount: 0);
        }

        var visible = new List<AuditLog>(rows.Count);
        int hidden = 0;
        int masked = 0;

        foreach (var row in rows)
        {
            var rule = MatchRule(row.Action, ruleSnapshots);
            if (rule is null)
            {
                // No rule covers this action → permissive default.
                visible.Add(row);
                continue;
            }

            if (rule.RequiresComplianceMode && !complianceMode)
            {
                hidden++;
                continue;
            }

            if (!await ActorMatchesAsync(actor, rule, cancellationToken))
            {
                hidden++;
                continue;
            }

            if (rule.MaskFields.Count > 0
                && !string.IsNullOrEmpty(row.DetailsJson))
            {
                var redactedJson = RedactFields(row.DetailsJson, rule.MaskFields);
                if (!ReferenceEquals(redactedJson, row.DetailsJson))
                {
                    visible.Add(CloneWithMask(row, redactedJson));
                    masked++;
                    continue;
                }
            }

            visible.Add(row);
        }

        return new AuditVisibilityResult(visible, hidden, masked);
    }

    // ----- helpers ------------------------------------------------------

    private async Task<IReadOnlyList<RuleSnapshot>> GetRuleSnapshotsAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue<IReadOnlyList<RuleSnapshot>>(CacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        var rows = await _db.Set<AuditVisibilityRule>()
            .AsNoTracking()
            .ToListAsync(ct);

        var snapshots = rows
            .Select(r => new RuleSnapshot(
                Pattern: r.ActionPattern,
                IsWildcard: r.ActionPattern.EndsWith("*", StringComparison.Ordinal),
                Roles: ParseStringArray(r.VisibleToRolesJson),
                Permissions: ParseStringArray(r.VisibleToPermissionsJson),
                MaskFields: ParseStringArray(r.MaskFieldsJson),
                RequiresComplianceMode: r.RequiresComplianceMode))
            // Most-specific match first: longer patterns and non-wildcard rules win.
            .OrderByDescending(s => s.IsWildcard ? 0 : 1)
            .ThenByDescending(s => s.Pattern.Length)
            .ToList();

        _cache.Set(CacheKey, (IReadOnlyList<RuleSnapshot>)snapshots, CacheTtl);
        return snapshots;
    }

    private static RuleSnapshot? MatchRule(string action, IReadOnlyList<RuleSnapshot> rules)
    {
        foreach (var rule in rules)
        {
            if (rule.IsWildcard)
            {
                var prefix = rule.Pattern.TrimEnd('*');
                if (action.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }
            }
            else if (string.Equals(action, rule.Pattern, StringComparison.OrdinalIgnoreCase))
            {
                return rule;
            }
        }
        return null;
    }

    private async Task<bool> ActorMatchesAsync(
        ClaimsPrincipal actor,
        RuleSnapshot rule,
        CancellationToken ct)
    {
        // Empty arrays mean "no constraint on this dimension".
        if (rule.Roles.Count > 0
            && !rule.Roles.Any(r => actor.IsInRole(r)))
        {
            return false;
        }

        if (rule.Permissions.Count > 0)
        {
            foreach (var permission in rule.Permissions)
            {
                if (await _permissionGuard.HasAsync(actor, permission, ct))
                {
                    return true;
                }
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// Replaces each property in <paramref name="maskFields"/> with
    /// <see cref="MaskValue"/> at the top level of
    /// <paramref name="detailsJson"/>. Returns the input string
    /// unchanged when none of the fields are present (lets the caller
    /// skip the clone allocation).
    /// </summary>
    private static string RedactFields(string detailsJson, IReadOnlyList<string> maskFields)
    {
        try
        {
            using var document = JsonDocument.Parse(detailsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return detailsJson;
            }

            var maskSet = new HashSet<string>(maskFields, StringComparer.OrdinalIgnoreCase);
            bool anyHits = false;

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (maskSet.Contains(property.Name))
                    {
                        writer.WriteString(property.Name, MaskValue);
                        anyHits = true;
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }
                writer.WriteEndObject();
            }

            return anyHits
                ? System.Text.Encoding.UTF8.GetString(stream.ToArray())
                : detailsJson;
        }
        catch (JsonException)
        {
            // Malformed JSON: don't crash the viewer; redact wholesale.
            return JsonSerializer.Serialize(new { masked = MaskValue });
        }
    }

    private static AuditLog CloneWithMask(AuditLog source, string newDetailsJson)
    {
        return new AuditLog
        {
            Id = source.Id,
            ActorUserId = source.ActorUserId,
            Action = source.Action,
            EntityType = source.EntityType,
            EntityId = source.EntityId,
            DetailsJson = newDetailsJson,
            IpAddress = source.IpAddress,
            Severity = source.Severity,
            OccurredAtUtc = source.OccurredAtUtc
        };
    }

    private static IReadOnlyList<string> ParseStringArray(string json)
    {
        if (string.IsNullOrEmpty(json) || json == "[]")
        {
            return Array.Empty<string>();
        }
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json)
                ?? (IReadOnlyList<string>)Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// In-memory rendering of one <see cref="AuditVisibilityRule"/>
    /// row with the JSON columns parsed once per cache window.
    /// </summary>
    private sealed record RuleSnapshot(
        string Pattern,
        bool IsWildcard,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions,
        IReadOnlyList<string> MaskFields,
        bool RequiresComplianceMode);
}
