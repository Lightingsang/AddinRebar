namespace HPBim.Core.Coordination;

/// <summary>
/// Strict engineering tolerance thresholds in millimeters for structural coordination.
/// </summary>
public sealed record CoordinationTolerances
{
    public double SectionDimensionMm { get; init; } = 0.0;
    public double ElevationMm { get; init; } = 1.0;
    public double PlanPositionMm { get; init; } = 1.0;
    public double PileCenterMm { get; init; } = 2.0;
}
