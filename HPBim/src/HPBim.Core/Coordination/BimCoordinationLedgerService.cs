namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service implementation managing the BIM Coordination Ledger.
/// Enforces the 7-step lifecycle state machine, prevents step skipping,
/// and applies FailFastGate when transitioning to Revit modeling.
/// </summary>
public sealed partial class BimCoordinationLedgerService : IBimCoordinationLedgerService
{
    private readonly IFailFastGate _failFastGate;
    private readonly Dictionary<string, CoordinationElementRecord> _elements;
    private readonly List<RfiRecord> _rfis;
    private BimCoordinationLedger _ledger;

    public BimCoordinationLedger Ledger => SyncLedgerState();

    public BimCoordinationLedgerService(IFailFastGate? failFastGate = null)
    {
        _failFastGate = failFastGate ?? new FailFastGate();
        _elements = new Dictionary<string, CoordinationElementRecord>(StringComparer.OrdinalIgnoreCase);
        _rfis = new List<RfiRecord>();
        _ledger = new BimCoordinationLedger();
    }

    public void Initialize(string project, string? primeDirective = null)
    {
        if (string.IsNullOrWhiteSpace(project))
        {
            throw new ArgumentException("Project name cannot be null or whitespace.", nameof(project));
        }

        _elements.Clear();
        _rfis.Clear();
        _mappings.Clear();
        _ledger = new BimCoordinationLedger
        {
            Project = project,
            UpdatedAt = DateTime.UtcNow,
            PrimeDirective = primeDirective ?? _ledger.PrimeDirective,
            ActivePhase = "PHAN_MONG",
            PhaseStatus = PhaseStatus.InProgress,
            QualityGate = "GATE_1_MODEL_CLEANUP",
            Tolerances = new CoordinationTolerances(),
            Summary = new CoordinationProgressSummary(),
            Elements = Array.Empty<CoordinationElementRecord>(),
            Rfis = Array.Empty<RfiRecord>()
        };
    }

    public BimCoordinationLedger LoadFromFile(string filePath)
    {
        var loaded = BimCoordinationLedgerSerializer.DeserializeFromFile(filePath);
        ApplyLoadedLedger(loaded);
        return Ledger;
    }

    public async Task<BimCoordinationLedger> LoadFromFileAsync(string filePath, CancellationToken ct = default)
    {
        var loaded = await BimCoordinationLedgerSerializer.DeserializeFromFileAsync(filePath, ct).ConfigureAwait(false);
        ApplyLoadedLedger(loaded);
        return Ledger;
    }

    public void SaveToFile(string filePath)
    {
        var current = SyncLedgerState();
        BimCoordinationLedgerSerializer.SerializeToFile(current, filePath, true);
    }

    public async Task SaveToFileAsync(string filePath, CancellationToken ct = default)
    {
        var current = SyncLedgerState();
        await BimCoordinationLedgerSerializer.SerializeToFileAsync(current, filePath, true, ct).ConfigureAwait(false);
    }

    public CoordinationElementRecord RegisterElement(CoordinationElementRecord element)
    {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (string.IsNullOrWhiteSpace(element.Id))
        {
            throw new ArgumentException("Element Id cannot be null or empty.", nameof(element));
        }

        _elements[element.Id] = element;
        return element;
    }

    public CoordinationElementRecord ClassifyElement(
        string elementId, ElementTechnicalGroup group, string actor, string comment)
    {
        var existing = GetExistingElement(elementId);
        var updated = existing with
        {
            TechnicalGroup = group,
            Timestamp = DateTime.UtcNow
        };

        _elements[elementId] = updated;
        return updated;
    }

    public CoordinationElementRecord TransitionStep(
        string elementId, CoordinationLifecycleStep targetStep, string actor, string comment)
    {
        var existing = GetExistingElement(elementId);
        ValidateStepTransition(existing, targetStep);

        var transition = new StepTransition
        {
            FromStep = existing.Step,
            ToStep = targetStep,
            Timestamp = DateTime.UtcNow,
            Actor = actor ?? "System",
            Comment = comment ?? string.Empty
        };

        var history = new List<StepTransition>(existing.History) { transition };
        var updated = existing with
        {
            Step = targetStep,
            Timestamp = DateTime.UtcNow,
            History = history.AsReadOnly()
        };

        _elements[elementId] = updated;
        return updated;
    }

    public CoordinationElementRecord UpdateRevitTarget(string elementId, RevitTargetSpec targetSpec)
    {
        var existing = GetExistingElement(elementId);
        var updated = existing with
        {
            RevitTargetSpec = targetSpec,
            Timestamp = DateTime.UtcNow
        };
        _elements[elementId] = updated;
        return updated;
    }

    public CoordinationElementRecord RecordAudit(string elementId, AuditRecord audit)
    {
        var existing = GetExistingElement(elementId);
        var updated = existing with
        {
            AuditRecord = audit,
            Timestamp = DateTime.UtcNow
        };
        _elements[elementId] = updated;
        return updated;
    }

    public RfiRecord CreateRfi(
        string title, string conflictDescription, IEnumerable<string> elementIds, string proposedAction)
    {
        var rfi = new RfiRecord
        {
            RfiId = $"RFI-{_rfis.Count + 1:D3}",
            Title = title ?? string.Empty,
            ConflictDescription = conflictDescription ?? string.Empty,
            ElementIds = elementIds?.ToList().AsReadOnly() ?? (IReadOnlyList<string>)Array.Empty<string>(),
            ProposedAction = proposedAction ?? string.Empty,
            Status = RfiStatus.PendingChiefEngineer,
            CreatedAt = DateTime.UtcNow
        };

        _rfis.Add(rfi);
        return rfi;
    }

    public CoordinationProgressSummary CalculateSummary()
    {
        var all = _elements.Values.ToList();
        return new CoordinationProgressSummary
        {
            TotalElements = all.Count,
            Step1_ReadCad = all.Count(e => e.Step == CoordinationLifecycleStep.Step1_ReadCad),
            Step2_CheckAndClassify = all.Count(e => e.Step == CoordinationLifecycleStep.Step2_CheckAndClassify),
            Step3_ModelRevit = all.Count(e => e.Step == CoordinationLifecycleStep.Step3_ModelRevit),
            Step4_QaqcAudit = all.Count(e => e.Step == CoordinationLifecycleStep.Step4_QaqcAudit),
            Step5_RouteFix = all.Count(e => e.Step == CoordinationLifecycleStep.Step5_RouteFix),
            Step6_ReAudit = all.Count(e => e.Step == CoordinationLifecycleStep.Step6_ReAudit),
            Step7_ConfirmedPass = all.Count(e => e.Step == CoordinationLifecycleStep.Step7_ConfirmedPass),
            Group1_ReadyToModel = all.Count(e => e.TechnicalGroup == ElementTechnicalGroup.Group1_ReadyToModel),
            Group2_ConflictingInfo = all.Count(e => e.TechnicalGroup == ElementTechnicalGroup.Group2_ConflictingInfo),
            Group3_MissingInfo = all.Count(e => e.TechnicalGroup == ElementTechnicalGroup.Group3_MissingInfo),
            UnresolvedHalt = all.Count(e => e.TechnicalGroup != ElementTechnicalGroup.Group1_ReadyToModel)
        };
    }

    private CoordinationElementRecord GetExistingElement(string elementId)
    {
        if (string.IsNullOrWhiteSpace(elementId))
        {
            throw new ArgumentException("Element Id cannot be null or empty.", nameof(elementId));
        }

        if (!_elements.TryGetValue(elementId, out var element))
        {
            throw new KeyNotFoundException($"Element '{elementId}' not found in coordination ledger.");
        }

        return element;
    }

    private void ValidateStepTransition(CoordinationElementRecord element, CoordinationLifecycleStep targetStep)
    {
        var current = element.Step;
        bool valid = current switch
        {
            CoordinationLifecycleStep.Step1_ReadCad =>
                targetStep == CoordinationLifecycleStep.Step2_CheckAndClassify,

            CoordinationLifecycleStep.Step2_CheckAndClassify =>
                targetStep == CoordinationLifecycleStep.Step3_ModelRevit,

            CoordinationLifecycleStep.Step3_ModelRevit =>
                targetStep == CoordinationLifecycleStep.Step4_QaqcAudit,

            CoordinationLifecycleStep.Step4_QaqcAudit =>
                targetStep == CoordinationLifecycleStep.Step5_RouteFix ||
                targetStep == CoordinationLifecycleStep.Step7_ConfirmedPass,

            CoordinationLifecycleStep.Step5_RouteFix =>
                targetStep == CoordinationLifecycleStep.Step6_ReAudit,

            CoordinationLifecycleStep.Step6_ReAudit =>
                targetStep == CoordinationLifecycleStep.Step5_RouteFix ||
                targetStep == CoordinationLifecycleStep.Step7_ConfirmedPass,

            CoordinationLifecycleStep.Step7_ConfirmedPass => false,

            _ => false
        };

        if (!valid)
        {
            throw new InvalidOperationException(
                $"Invalid lifecycle step transition from {current} to {targetStep} for element '{element.Id}'. Step skipping is prohibited.");
        }

        if (targetStep == CoordinationLifecycleStep.Step3_ModelRevit)
        {
            if (element.TechnicalGroup != ElementTechnicalGroup.Group1_ReadyToModel)
            {
                throw new InvalidOperationException(
                    $"Element '{element.Id}' cannot transition to Step3_ModelRevit because it belongs to {element.TechnicalGroup}. Only Group1 elements can be modeled in Revit.");
            }

            _failFastGate.AssertReadyForRevit(element);
        }
    }

    private void ApplyLoadedLedger(BimCoordinationLedger loaded)
    {
        _elements.Clear();
        _rfis.Clear();
        _mappings.Clear();
        _ledger = loaded;

        if (loaded.Elements != null)
        {
            foreach (var el in loaded.Elements) _elements[el.Id] = el;
        }

        if (loaded.Rfis != null) _rfis.AddRange(loaded.Rfis);

        HydrateMappings(loaded);
        SyncLedgerState();
    }

    private BimCoordinationLedger SyncLedgerState()
    {
        _ledger = _ledger with
        {
            UpdatedAt = DateTime.UtcNow,
            Summary = CalculateSummary(),
            Elements = _elements.Values.ToList().AsReadOnly(),
            Rfis = _rfis.ToList().AsReadOnly(),
            Mappings = new Dictionary<string, LedgerElementMapping>(_mappings, StringComparer.OrdinalIgnoreCase)
        };
        return _ledger;
    }
}
