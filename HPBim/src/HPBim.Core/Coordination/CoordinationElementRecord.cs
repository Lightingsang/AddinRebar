namespace HPBim.Core.Coordination;

using System;
using System.Collections.Generic;

/// <summary>
/// Central coordination ledger element record tracking lifecycle across the 7 steps.
/// </summary>
public sealed record CoordinationElementRecord
{
    public string Id { get; init; } = string.Empty;
    public CoordinationCategory Category { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public CoordinationLifecycleStep Step { get; init; } = CoordinationLifecycleStep.Step1_ReadCad;
    public ElementTechnicalGroup TechnicalGroup { get; init; } = ElementTechnicalGroup.Group3_MissingInfo;
    public CadSourceSpec CadSourceSpec { get; init; } = new();
    public RevitTargetSpec? RevitTargetSpec { get; init; }
    public AuditRecord? AuditRecord { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<StepTransition> History { get; init; } = Array.Empty<StepTransition>();

    /// <summary>Alias for Id to support both naming styles.</summary>
    public string ElementId => Id;

    /// <summary>Alias for Code to support both naming styles.</summary>
    public string Mark => Code;
}
