namespace HPBim.Core.Execution;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HPBim.Core.Coordination;

/// <summary>
/// Immutable, single-source-of-truth Intermediate Representation (IR) defining
/// the exact topological sequence of operations to execute in Revit.
/// Shared identically by Preview (dry-run rollback) and Apply (commit).
/// </summary>
public sealed record BimExecutionPlan
{
    public Guid PlanId { get; init; } = Guid.NewGuid();

    private readonly string? _planHash;

    public string PlanHash
    {
        get => !string.IsNullOrEmpty(_planHash)
            ? _planHash!
            : ComputePlanHash(TargetDoc, Items, SourceModelName);
        init => _planHash = value;
    }
    public string SourceModelName { get; init; } = string.Empty;
    public string SchemaVersion { get; init; } = "1.0.0";
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    public DocumentIdentitySnapshot TargetDoc { get; init; } = new();
    public IReadOnlyList<ElementPlanItem> Items { get; init; } = Array.Empty<ElementPlanItem>();
    public ExecutionOptions Options { get; init; } = new();
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public int TotalElementCount => Items.Count;

    /// <summary>
    /// Computes a deterministic SHA-256 hash representing the full plan content and preconditions.
    /// Does not include random PlanId so the hash is 100% deterministic across rebuilds.
    /// </summary>
    public static string ComputePlanHash(
        DocumentIdentitySnapshot targetDoc,
        IReadOnlyList<ElementPlanItem> items,
        string? sourceModelName = null)
    {
        using var sha256 = SHA256.Create();
        var sb = new StringBuilder();
        sb.Append(sourceModelName ?? string.Empty);
        sb.Append('|').Append(targetDoc?.Title ?? string.Empty);
        sb.Append('|').Append(targetDoc?.PathName ?? string.Empty);
        sb.Append('|').Append(targetDoc != null ? targetDoc.VersionGuid.ToString("N") : string.Empty);

        if (items != null)
        {
            foreach (var item in items)
            {
                AppendItemCanonicalString(sb, item);
            }
        }

        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static void AppendItemCanonicalString(StringBuilder sb, ElementPlanItem item)
    {
        sb.Append(';').Append(item.SourceKey?.ToString() ?? string.Empty);
        sb.Append(':').Append(item.Category);
        sb.Append(':').Append(item.Mark);
        sb.Append(':').Append(item.TargetLevelName);
        sb.Append(':').Append(item.TargetTopLevelName ?? string.Empty);
        sb.Append(':').Append(item.HostElementKey ?? string.Empty);
        sb.Append(':').Append(item.IsStructural ? "1" : "0");
        sb.Append(':').Append(item.Resource?.GetCanonicalString() ?? string.Empty);
        sb.Append(':').Append(item.Geometry?.GetCanonicalString() ?? string.Empty);
        AppendParametersCanonicalString(sb, item.Parameters);
    }

    private static void AppendParametersCanonicalString(
        StringBuilder sb,
        IReadOnlyDictionary<string, object>? parameters)
    {
        if (parameters == null || parameters.Count == 0) return;
        sb.Append(":params[");
        bool first = true;
        foreach (var kvp in parameters.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (!first) sb.Append(',');
            string valStr = kvp.Value switch
            {
                double d => d.ToString("F2", CultureInfo.InvariantCulture),
                float f => f.ToString("F2", CultureInfo.InvariantCulture),
                decimal dec => dec.ToString("F2", CultureInfo.InvariantCulture),
                _ => Convert.ToString(kvp.Value, CultureInfo.InvariantCulture) ?? string.Empty
            };
            sb.Append(kvp.Key).Append('=').Append(valStr);
            first = false;
        }
        sb.Append(']');
    }

    /// <summary>
    /// Backward-compatible overload ignoring random planId to ensure deterministic hashing.
    /// </summary>
    public static string ComputePlanHash(
        Guid planId,
        DocumentIdentitySnapshot targetDoc,
        IReadOnlyList<ElementPlanItem> items) =>
        ComputePlanHash(targetDoc, items);
}
