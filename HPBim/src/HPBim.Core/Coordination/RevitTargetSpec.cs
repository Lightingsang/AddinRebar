namespace HPBim.Core.Coordination;

using System;

/// <summary>
/// Target element parameters modeled in Revit, including two-phase self-check measurements.
/// </summary>
public sealed record RevitTargetSpec
{
    public long? ElementId { get; init; }
    public string UniqueId { get; init; } = string.Empty;
    public string FamilyName { get; init; } = string.Empty;
    public string TypeName { get; init; } = string.Empty;
    public string LevelName { get; init; } = string.Empty;
    public double? MeasuredWidth { get; init; }
    public double? MeasuredHeight { get; init; }
    public double? MeasuredTopElevation { get; init; }
    public bool TwoPhaseSelfCheckPassed { get; init; }
    public DateTime? ModeledAt { get; init; }
}
