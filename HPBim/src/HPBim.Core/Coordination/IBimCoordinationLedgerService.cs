namespace HPBim.Core.Coordination;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service interface managing the BIM Coordination Ledger and lifecycle state machine.
/// </summary>
public interface IBimCoordinationLedgerService
{
    BimCoordinationLedger Ledger { get; }
    void Initialize(string project, string? primeDirective = null);
    BimCoordinationLedger LoadFromFile(string filePath);
    Task<BimCoordinationLedger> LoadFromFileAsync(string filePath, CancellationToken ct = default);
    void SaveToFile(string filePath);
    Task SaveToFileAsync(string filePath, CancellationToken ct = default);
    CoordinationElementRecord RegisterElement(CoordinationElementRecord element);
    CoordinationElementRecord ClassifyElement(string elementId, ElementTechnicalGroup group, string actor, string comment);
    CoordinationElementRecord TransitionStep(string elementId, CoordinationLifecycleStep targetStep, string actor, string comment);
    CoordinationElementRecord UpdateRevitTarget(string elementId, RevitTargetSpec targetSpec);
    CoordinationElementRecord RecordAudit(string elementId, AuditRecord audit);
    RfiRecord CreateRfi(string title, string conflictDescription, IEnumerable<string> elementIds, string proposedAction);
    CoordinationProgressSummary CalculateSummary();
}
