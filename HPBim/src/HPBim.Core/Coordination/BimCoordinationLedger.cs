namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;
using HPBim.Core.Execution;

/// <summary>
/// Root coordination ledger tracking all elements across the 7 lifecycle steps,
/// with persistent CompositeSourceKey -> TargetUniqueId mappings for idempotency.
/// </summary>
public sealed record BimCoordinationLedger
{
    public string Project { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public string PrimeDirective { get; init; } =
        "Khi chưa đủ cơ sở để kết luận, hãy dừng lại và báo chưa đủ thông tin. Không được suy đoán để hoàn thành công việc. Thà để một cấu kiện chưa dựng còn hơn dựng sai nhưng báo đúng.";
    public string ActivePhase { get; init; } = "PHAN_MONG";
    public PhaseStatus PhaseStatus { get; init; } = PhaseStatus.InProgress;
    public string QualityGate { get; init; } = "GATE_1_MODEL_CLEANUP";
    public CoordinationTolerances Tolerances { get; init; } = new();
    public CoordinationProgressSummary Summary { get; init; } = new();
    public IReadOnlyList<CoordinationElementRecord> Elements { get; init; } = Array.Empty<CoordinationElementRecord>();
    public IReadOnlyList<RfiRecord> Rfis { get; init; } = Array.Empty<RfiRecord>();

    public IReadOnlyDictionary<string, LedgerElementMapping> Mappings { get; init; } =
        new Dictionary<string, LedgerElementMapping>(StringComparer.OrdinalIgnoreCase);

    public bool TryGetTarget(CompositeSourceKey key, out string? uniqueId)
    {
        if (key != null && Mappings.TryGetValue(key.ToString(), out var mapping))
        {
            uniqueId = mapping.TargetUniqueId;
            return !string.IsNullOrEmpty(uniqueId);
        }

        uniqueId = null;
        return false;
    }

    public bool TryGetMapping(CompositeSourceKey key, out LedgerElementMapping? mapping)
    {
        if (key != null && Mappings.TryGetValue(key.ToString(), out mapping))
        {
            return true;
        }

        mapping = null;
        return false;
    }

    private Dictionary<string, LedgerElementMapping> CloneMappings()
    {
        var dict = new Dictionary<string, LedgerElementMapping>(StringComparer.OrdinalIgnoreCase);
        if (Mappings != null)
        {
            foreach (var kvp in Mappings) dict[kvp.Key] = kvp.Value;
        }
        return dict;
    }

    public BimCoordinationLedger RegisterMapping(LedgerElementMapping mapping)
    {
        if (mapping == null) throw new ArgumentNullException(nameof(mapping));
        var dict = CloneMappings();
        dict[mapping.SourceKey.ToString()] = mapping;
        return this with { Mappings = dict, UpdatedAt = DateTime.UtcNow };
    }

    public BimCoordinationLedger UpdateMapping(
        CompositeSourceKey key,
        string uniqueId,
        string planHash,
        ElementExecutionStatus status,
        long? elementId = null)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        string keyStr = key.ToString();
        var dict = CloneMappings();

        if (dict.TryGetValue(keyStr, out var existing))
        {
            dict[keyStr] = existing.WithUpdate(uniqueId, elementId, planHash, status);
        }
        else
        {
            dict[keyStr] = LedgerElementMapping.Create(key, uniqueId, elementId, key.Category, key.PrimaryHandle, planHash);
        }

        return this with { Mappings = dict, UpdatedAt = DateTime.UtcNow };
    }

    public BimCoordinationLedger RemoveMapping(CompositeSourceKey key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        string keyStr = key.ToString();
        if (!Mappings.ContainsKey(keyStr)) return this;

        var dict = CloneMappings();
        dict.Remove(keyStr);
        return this with { Mappings = dict, UpdatedAt = DateTime.UtcNow };
    }
}
