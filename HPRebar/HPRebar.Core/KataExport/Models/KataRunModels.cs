using System.Collections.Generic;

namespace HPRebar.Core.KataExport.Models;

/// <summary>What carries the beam at a support. Lower values win when supports overlap.</summary>
public enum KataSupportKind
{
    Column = 0,
    Foundation = 1,
    Beam = 2
}

/// <summary>
/// One structural framing element of the run, projected onto the beam axis.
/// </summary>
/// <param name="Extent">Stations covered by the element's location line (mm).</param>
/// <param name="WidthMm">Section width b.</param>
/// <param name="HeightMm">Section height h.</param>
/// <param name="ZOffsetMm">The element's "z Offset Value".</param>
/// <param name="ElementKey">Revit UniqueId, used for tracing only.</param>
public sealed record KataBeamPiece(Interval1D Extent, double WidthMm, double HeightMm, double ZOffsetMm, string ElementKey);

/// <summary>
/// A column, foundation or crossing beam that the run bears on, projected onto the beam axis.
/// </summary>
/// <param name="SectionText">"b x h" of a supporting beam, written in place of a width.</param>
/// <param name="Upper">Stations covered by the column standing on this support, when there is one.</param>
/// <param name="CrossingBeamStationMm">
/// Centre line of a beam framing into this column across the run; set when a crossing beam merges into a
/// column or foundation support.
/// </param>
public sealed record KataSupport(
    KataSupportKind Kind,
    Interval1D Extent,
    string ElementKey,
    string? SectionText = null,
    Interval1D? Upper = null,
    double? CrossingBeamStationMm = null);

/// <summary>A grid line crossing the beam axis at <paramref name="StationMm"/>.</summary>
public sealed record KataGridCrossing(string Name, double StationMm);

/// <summary>
/// Values of the first element of the run that go to cells B3..B10.
/// </summary>
/// <param name="Name">Value of the parameter chosen as "Name" (text or number, written as-is).</param>
/// <param name="Count">Value of the parameter chosen as "Count" (text or number, written as-is).</param>
/// <param name="LevelElevationMm">Elevation of the element's reference level.</param>
/// <param name="SlabThicknessMm">Floor on top of the beam (B7); null when none was found.</param>
/// <param name="AxisGridName">Grid the beam runs along (B8); null when none was found.</param>
/// <param name="AxisOffsetMm">
/// Signed distance from the beam centre line to that grid (B9), positive to the left of the run direction.
/// </param>
public sealed record KataHeader(
    object? Name,
    object? Count,
    double HeightMm,
    double WidthMm,
    double LevelElevationMm,
    double? SlabThicknessMm = null,
    string? AxisGridName = null,
    double? AxisOffsetMm = null);

/// <summary>A value Excel must keep as text instead of parsing it (e.g. "+3.300").</summary>
public sealed record KataText(string Value)
{
    public override string ToString() => Value;
}

/// <summary>
/// Everything read from Revit for one straight beam run, in millimetres along an axis oriented
/// so that its dominant plan component is positive (left to right, or bottom to top).
/// </summary>
public sealed record KataRunInput(
    IReadOnlyList<KataBeamPiece> Pieces,
    IReadOnlyList<KataSupport> Supports,
    IReadOnlyList<KataGridCrossing> Grids,
    KataHeader Header);

/// <summary>Choices made in the export window.</summary>
public sealed record KataBuildOptions
{
    /// <summary>Write the run from its far end: order reversed and offsets negated.</summary>
    public bool Reverse { get; init; }

    /// <summary>Put a zero-width support where two framing elements meet without a support.</summary>
    public bool InsertJoints { get; init; } = true;
}
