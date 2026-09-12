using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// A calculated contiguous run of stirrups with uniform spacing.
/// Coordinates are in local millimetres along the continuous beam axis.
/// </summary>
public sealed record StirrupZone
{
    /// <summary>Zone sequence index within the span (0 = Left, 1 = Mid, 2 = Right).</summary>
    public int ZoneIndex { get; init; }

    /// <summary>Parent span index (-1 if support node zone).</summary>
    public int HostSpanIndex { get; init; } = -1;

    /// <summary>Zone classification label (e.g., "Dense Support", "Midspan").</summary>
    public string ZoneName { get; init; } = string.Empty;

    /// <summary>Longitudinal coordinate X of the zone start station (mm).</summary>
    public double StartX { get; init; }

    /// <summary>Longitudinal coordinate X of the zone end station (mm).</summary>
    public double EndX { get; init; }

    /// <summary>Zone longitudinal length (mm).</summary>
    public double Length => EndX - StartX;

    /// <summary>Uniform spacing between stirrups in this zone (mm).</summary>
    public double Spacing { get; init; }

    /// <summary>Number of stirrup positions in this zone.</summary>
    public int Count { get; init; }

    /// <summary>Exact longitudinal coordinates X for each individual stirrup (mm).</summary>
    public IReadOnlyList<double> Positions { get; init; } = Array.Empty<double>();

    /// <summary>Out-to-out stirrup width (b - 2*Cover) (mm).</summary>
    public double Width { get; init; }

    /// <summary>Out-to-out stirrup height (h - 2*Cover) (mm).</summary>
    public double Height { get; init; }

    /// <summary>Top elevation of stirrup top outer bar edge (Z_top - Cover) (mm).</summary>
    public double TopElevation { get; init; }
}
