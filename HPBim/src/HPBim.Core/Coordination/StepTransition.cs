namespace HPBim.Core.Coordination;

using System;

/// <summary>
/// Audit trail record for an element transitioning between lifecycle steps.
/// </summary>
public sealed record StepTransition
{
    public CoordinationLifecycleStep? FromStep { get; init; }
    public CoordinationLifecycleStep ToStep { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string Actor { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
}
