using System;
using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// A segmented stirrup distribution zone along a clear span (support dense zone vs midspan sparse zone).
/// </summary>
public sealed record KataStirrupZoneResult
{
    /// <summary>0-based index of the span containing this stirrup zone.</summary>
    public int SpanIndex { get; init; }

    /// <summary>0 = Left support dense zone, 1 = Midspan sparse zone, 2 = Right support dense zone.</summary>
    public int ZoneIndex { get; init; }

    /// <summary>Informative name of the zone (e.g. "Gối trái", "Giữa nhịp", "Gối phải").</summary>
    public string ZoneName { get; init; } = "";

    /// <summary>Start station X along the beam axis in mm.</summary>
    public double StartStationX { get; init; }

    /// <summary>End station X along the beam axis in mm.</summary>
    public double EndStationX { get; init; }

    /// <summary>Center-to-center spacing between stirrups in mm.</summary>
    public double Spacing { get; init; }

    /// <summary>Calculated number of stirrups placed in this zone.</summary>
    public int Count { get; init; }

    /// <summary>Individual longitudinal station X positions for each stirrup in mm.</summary>
    public IReadOnlyList<double> Stations { get; init; } = Array.Empty<double>();

    /// <summary>Outer width of the stirrup loop in mm (BeamWidth - 2 * CoverStirrup).</summary>
    public double OutToOutWidth { get; init; }

    /// <summary>Outer height of the stirrup loop in mm (BeamHeight - 2 * CoverStirrup).</summary>
    public double OutToOutHeight { get; init; }

    /// <summary>Stirrup shape type (Closed hoop, Cap U, Cross tie C).</summary>
    public KataStirrupShapeType StirrupType { get; init; } = KataStirrupShapeType.ClosedHoop;

    /// <summary>Kata standard bar shape code (e.g. "41" for closed hoop, "45" for cap U, "24a" for cross tie).</summary>
    public string ShapeCode => StirrupType switch
    {
        KataStirrupShapeType.ClosedHoop => "41",
        KataStirrupShapeType.CapStirrup => "45",
        KataStirrupShapeType.CrossTie => "24a",
        _ => "41"
    };

    /// <summary>Bar schedule mark / item designation (e.g. "d1", "d2").</summary>
    public string BarMark { get; init; } = "";

    /// <summary>Fabrication segment dimension A in mm (Width out-to-out).</summary>
    public double DimA => OutToOutWidth;

    /// <summary>Fabrication segment dimension B in mm (Height out-to-out).</summary>
    public double DimB => OutToOutHeight;
}
