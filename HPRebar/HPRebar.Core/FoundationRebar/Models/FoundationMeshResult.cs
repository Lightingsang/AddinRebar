using System;
using System.Collections.Generic;

namespace HPRebar.Core.FoundationRebar.Models;

/// <summary>
/// Summary and geometry result of foundation mesh reinforcement generation.
/// </summary>
public sealed record FoundationMeshResult
{
    /// <summary>Centerline polylines in world coordinates for Bottom Mat Direction X.</summary>
    public IReadOnlyList<Polyline3> BottomBarsX { get; init; } = Array.Empty<Polyline3>();

    /// <summary>Centerline polylines in world coordinates for Bottom Mat Direction Y.</summary>
    public IReadOnlyList<Polyline3> BottomBarsY { get; init; } = Array.Empty<Polyline3>();

    /// <summary>Centerline polylines in world coordinates for Top Mat Direction X.</summary>
    public IReadOnlyList<Polyline3> TopBarsX { get; init; } = Array.Empty<Polyline3>();

    /// <summary>Centerline polylines in world coordinates for Top Mat Direction Y.</summary>
    public IReadOnlyList<Polyline3> TopBarsY { get; init; } = Array.Empty<Polyline3>();

    /// <summary>Detailed bar representations with layer and diameter information.</summary>
    public IReadOnlyList<FoundationBar> Bars { get; init; } = Array.Empty<FoundationBar>();

    /// <summary>Summary metrics and statistics for the generated mesh.</summary>
    public FoundationMeshStatistics Statistics { get; init; } = new();

    /// <summary>Total number of reinforcement bars across all active layers.</summary>
    public int TotalBarCount => Statistics?.TotalBarCount ?? (BottomBarsX.Count + BottomBarsY.Count + TopBarsX.Count + TopBarsY.Count);

    /// <summary>Total cumulative length of all generated rebar curves in millimetres.</summary>
    public double TotalLengthMm => Statistics?.TotalLengthMm ?? 0.0;
}

/// <summary>
/// Quantitative metrics for reinforcement scheduling and validation.
/// </summary>
public sealed record FoundationMeshStatistics
{
    public int TotalBarCount { get; init; }
    public int BottomBarCountX { get; init; }
    public int BottomBarCountY { get; init; }
    public int TopBarCountX { get; init; }
    public int TopBarCountY { get; init; }
    public double TotalLengthMm { get; init; }
    public double BottomLengthMmX { get; init; }
    public double BottomLengthMmY { get; init; }
    public double TopLengthMmX { get; init; }
    public double TopLengthMmY { get; init; }

    /// <summary>Estimated steel weight in kilograms: sum(0.006165 * d^2 * L_m).</summary>
    public double EstimatedWeightKg { get; init; }
}
