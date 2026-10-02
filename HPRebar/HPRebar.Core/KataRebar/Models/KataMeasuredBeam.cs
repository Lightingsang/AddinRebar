using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>What carries the beam at a measured support.</summary>
public enum KataMeasuredSupportKind
{
    /// <summary>Not a support: a span.</summary>
    None = 0,
    Column = 1,
    Foundation = 2,
    Beam = 3,

    /// <summary>Two framing elements meeting with nothing under them.</summary>
    Joint = 4
}

/// <summary>One support or span of the beam run as measured in Revit, in axis order.</summary>
/// <param name="LengthMm">Width of the support or length of the span along the beam axis.</param>
/// <param name="HeightMm">Depth of the beam over a span (0 when unknown, and for supports).</param>
public sealed record KataMeasuredSegment(KataMeasuredSupportKind SupportKind, double LengthMm, double HeightMm = 0.0)
{
    public bool IsSupport => SupportKind != KataMeasuredSupportKind.None;
}

/// <summary>
/// The beam run as Revit models it, in the axis order Revit reads it (left to right or bottom to top).
/// The sheet is compared against it and it replaces the sheet's own numbers when the bars are laid out.
/// </summary>
/// <param name="PieceCount">Number of framing elements in the run.</param>
public sealed record KataMeasuredBeam(
    double WidthMm,
    double HeightMm,
    IReadOnlyList<KataMeasuredSegment> Segments,
    int PieceCount);
