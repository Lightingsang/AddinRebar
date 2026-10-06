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

    /// <summary>Center-to-center spacing between stirrups in mm, as placed (never more than <see cref="NominalSpacing"/>).</summary>
    public double Spacing { get; init; }

    /// <summary>
    /// The spacing the sheet asks for (G7 / G8 "a100"), written on the tags; the placed <see cref="Spacing"/> is that or a
    /// little less, so the zone ends where Kata ends it. 0 = same as <see cref="Spacing"/>.
    /// </summary>
    public double NominalSpacing { get; init; }

    /// <summary>The spacing written on the drawing: <see cref="NominalSpacing"/>, else <see cref="Spacing"/>.</summary>
    public double LabelSpacing => NominalSpacing > 0.0 ? NominalSpacing : Spacing;

    /// <summary>Bar diameter of this zone's stirrups (mm); 0 = the beam's stirrup diameter (G6). Joint stirrups have their own.</summary>
    public double Diameter { get; init; }

    /// <summary>The zone's stirrup diameter, <paramref name="beamStirrupDiameter"/> when it has none of its own.</summary>
    public double DiameterOr(double beamStirrupDiameter) => Diameter > 0.0 ? Diameter : beamStirrupDiameter;

    /// <summary>Calculated number of stirrups placed in this zone.</summary>
    public int Count { get; init; }

    /// <summary>Individual longitudinal station X positions for each stirrup in mm.</summary>
    public IReadOnlyList<double> Stations { get; init; } = Array.Empty<double>();

    /// <summary>Outer width of the stirrup loop in mm (BeamWidth - 2 * CoverStirrup).</summary>
    public double OutToOutWidth { get; init; }

    /// <summary>Outer height of the stirrup loop in mm (BeamHeight - 2 * CoverStirrup).</summary>
    public double OutToOutHeight { get; init; }

    /// <summary>Transverse coordinate of the stirrup's outer face on the -Y side in mm (beam centre = 0).</summary>
    public double BoxMinY { get; init; }

    /// <summary>Vertical coordinate of the stirrup's outer face at the bottom in mm (beam top = 0).</summary>
    public double BoxMinZ { get; init; }

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

    /// <summary>
    /// Kata's bar number ("số hiệu", the drawing's circled 1, 2, 3...): identical bars share it
    /// (<see cref="Calculators.KataBarNumbering"/>); 0 until numbered. The schedule mark in Revit.
    /// </summary>
    public int BarNumber { get; init; }

    /// <summary>Fabrication segment dimension A in mm (Width out-to-out).</summary>
    public double DimA => OutToOutWidth;

    /// <summary>Fabrication segment dimension B in mm (Height out-to-out).</summary>
    public double DimB => OutToOutHeight;
}
