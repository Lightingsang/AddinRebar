using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataRebar.Models;
using HPRebar.KataExport.Model;

namespace HPRebar.KataRebar.Model;

/// <summary>
/// The picked beam run as Revit models it: the measurements the sheet is checked against, and the frame
/// and segments the bars are placed in. The Revit objects are only valid on the API thread that read them.
/// </summary>
public sealed class KataBeamMatchResult
{
    public bool IsSuccess { get; init; }

    public string Message { get; init; } = "";

    public IReadOnlyList<ElementId> BeamIds { get; init; } = Array.Empty<ElementId>();

    public KataMeasuredBeam? Measured { get; init; }

    /// <summary>Straight run of the picked framing: frame and pieces in axis order.</summary>
    public KataRunGeometry? Run { get; init; }

    /// <summary>Stations (mm) of each support / span in Revit axis order, matching <see cref="Measured"/>.</summary>
    public IReadOnlyList<Interval1D> SegmentExtents { get; init; } = Array.Empty<Interval1D>();

    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    public static KataBeamMatchResult Failed(string message) => new() { IsSuccess = false, Message = message };
}
