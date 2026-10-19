namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;
using HPBim.Core.Execution;

public sealed partial class BimCoordinationLedgerService
{
    private readonly Dictionary<string, LedgerElementMapping> _mappings =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, LedgerElementMapping> Mappings => _mappings;

    public bool TryGetTarget(CompositeSourceKey key, out string? uniqueId)
    {
        if (key != null && (_mappings.TryGetValue(key.ToString(), out var mapping) ||
                            _mappings.TryGetValue(key.PlainKey, out mapping)))
        {
            uniqueId = mapping.TargetUniqueId;
            return !string.IsNullOrEmpty(uniqueId);
        }

        uniqueId = null;
        return false;
    }

    public bool TryGetMapping(CompositeSourceKey key, out LedgerElementMapping? mapping)
    {
        if (key != null && (_mappings.TryGetValue(key.ToString(), out mapping) ||
                            _mappings.TryGetValue(key.PlainKey, out mapping)))
        {
            return true;
        }

        mapping = null;
        return false;
    }

    public LedgerElementMapping RegisterMapping(LedgerElementMapping mapping)
    {
        if (mapping == null) throw new ArgumentNullException(nameof(mapping));
        string canonicalKey = mapping.SourceKey.ToString();
        _mappings[canonicalKey] = mapping;
        _ledger = _ledger.RegisterMapping(mapping);
        return mapping;
    }

    public LedgerElementMapping UpdateMapping(
        CompositeSourceKey key,
        string uniqueId,
        string planHash,
        ElementExecutionStatus status,
        long? elementId = null)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        string keyStr = key.ToString();

        if (_mappings.TryGetValue(keyStr, out var existing) ||
            _mappings.TryGetValue(key.PlainKey, out existing))
        {
            var updated = existing.WithUpdate(uniqueId, elementId, planHash, status);
            _mappings.Remove(key.PlainKey);
            _mappings[keyStr] = updated;
            _ledger = _ledger.RegisterMapping(updated);
            return updated;
        }

        var created = LedgerElementMapping.Create(key, uniqueId, elementId, key.Category, key.PrimaryHandle, planHash);
        _mappings[keyStr] = created;
        _ledger = _ledger.RegisterMapping(created);
        return created;
    }

    public bool RemoveMapping(CompositeSourceKey key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        bool removed = _mappings.Remove(key.ToString());
        if (_mappings.Remove(key.PlainKey)) removed = true;
        if (removed)
        {
            _ledger = _ledger.RemoveMapping(key);
        }
        return removed;
    }

    public void SyncFromExecutionRecords(IEnumerable<ElementExecutionRecord> records, string planHash)
    {
        if (records == null) return;
        foreach (var rec in records)
        {
            if (rec.Status is ElementExecutionStatus.Created or ElementExecutionStatus.Updated)
            {
                if (!string.IsNullOrEmpty(rec.TargetUniqueId))
                {
                    UpdateMapping(rec.SourceKey, rec.TargetUniqueId!, planHash, rec.Status, rec.TargetElementId);
                }
            }
            else if (rec.Status is ElementExecutionStatus.NoOp)
            {
                if (_mappings.TryGetValue(rec.SourceKey.ToString(), out var existing) ||
                    _mappings.TryGetValue(rec.SourceKey.PlainKey, out existing))
                {
                    var noopUpdated = existing with
                    {
                        LastModifiedAtUtc = DateTime.UtcNow,
                        LastModifiedStatus = ElementExecutionStatus.NoOp
                    };
                    _mappings.Remove(rec.SourceKey.PlainKey);
                    _mappings[rec.SourceKey.ToString()] = noopUpdated;
                    _ledger = _ledger.RegisterMapping(noopUpdated);
                }
            }
        }
    }

    public void HydrateMappings(BimCoordinationLedger loaded)
    {
        if (loaded == null) return;

        if (loaded.Mappings != null)
        {
            foreach (var kvp in loaded.Mappings)
            {
                if (kvp.Value == null) continue;
                var mapping = kvp.Value;
                if (mapping.SourceKey == null || mapping.SourceKey == CompositeSourceKey.Empty)
                {
                    if (CompositeSourceKey.TryParse(kvp.Key, out var parsed, allowEmptyDoc: true) && parsed != null)
                    {
                        mapping = mapping with { SourceKey = parsed };
                    }
                }

                string canonical = mapping.SourceKey != null && mapping.SourceKey != CompositeSourceKey.Empty
                    ? mapping.SourceKey.ToString()
                    : kvp.Key;

                _mappings[canonical] = mapping;
            }
        }

        if (loaded.Elements != null)
        {
            foreach (var el in loaded.Elements)
            {
                if (el.RevitTargetSpec == null || string.IsNullOrWhiteSpace(el.RevitTargetSpec.UniqueId)) continue;
                string docId = el.CadSourceSpec?.DwgSheet ?? string.Empty;
                string handle = el.CadSourceSpec?.Handle ?? el.Id;
                var key = new CompositeSourceKey(docId, el.Category, handle);
                if (!_mappings.ContainsKey(key.ToString()) && !_mappings.ContainsKey(key.PlainKey))
                {
                    var mapping = LedgerElementMapping.Create(
                        key, el.RevitTargetSpec.UniqueId, el.RevitTargetSpec.ElementId, el.Category, el.Code ?? el.Id, string.Empty);
                    _mappings[key.ToString()] = mapping;
                }
            }
        }
    }
}
