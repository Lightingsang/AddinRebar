using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPRebar.Core.KataExport.Models;
using HPRebar.KataExport.Service;

namespace HPRebar.KataExport.Model;

/// <summary>One framing element of the run with everything the support scan and the sheet need.</summary>
public sealed class KataBeamGeometry
{
    public required FamilyInstance Element { get; init; }

    /// <summary>Stations of the location line (mm).</summary>
    public required Interval1D Stations { get; init; }

    /// <summary>Plan offset of the section centre from the run axis (mm).</summary>
    public required double CenterOffsetMm { get; init; }

    public required double BottomFt { get; init; }

    public required double TopFt { get; init; }

    public required double WidthMm { get; init; }

    public required double HeightMm { get; init; }

    public required double ZOffsetMm { get; init; }

    public Level? ReferenceLevel { get; init; }
}

/// <summary>A validated straight beam run: its plan frame and its elements in axis order.</summary>
public sealed class KataRunGeometry
{
    public required KataAxisFrame Frame { get; init; }

    public required IReadOnlyList<KataBeamGeometry> Pieces { get; init; }

    /// <summary>Stations from the start of the first element to the end of the last (mm).</summary>
    public required Interval1D Extent { get; init; }

    /// <summary>Lateral position of the run's centre line (mm), the line the probes run along.</summary>
    public required double CenterOffsetMm { get; init; }
}
