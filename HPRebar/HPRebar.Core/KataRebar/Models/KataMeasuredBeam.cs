using System.Collections.Generic;
using System.Linq;

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
/// <param name="Pieces">The framing elements along a span, in axis order; empty when unknown.</param>
/// <param name="Loads">Beams framing into the span and stub columns standing on it; empty when none.</param>
public sealed record KataMeasuredSegment(
    KataMeasuredSupportKind SupportKind,
    double LengthMm,
    double HeightMm = 0.0,
    IReadOnlyList<KataMeasuredPiece>? Pieces = null,
    IReadOnlyList<KataMeasuredLoad>? Loads = null)
{
    public bool IsSupport => SupportKind != KataMeasuredSupportKind.None;

    public IReadOnlyList<KataMeasuredPiece> PieceList => Pieces ?? System.Array.Empty<KataMeasuredPiece>();

    public IReadOnlyList<KataMeasuredLoad> LoadList => Loads ?? System.Array.Empty<KataMeasuredLoad>();

    /// <summary>The segment read from the run's other end: its pieces and loads in reverse, measured from the other side.</summary>
    public KataMeasuredSegment Mirrored() => this with
    {
        Pieces = PieceList.Reverse().Select(p => p with { StartMm = LengthMm - p.StartMm - p.LengthMm }).ToList(),
        Loads = LoadList.Reverse().Select(l => l with { StartMm = LengthMm - l.StartMm - l.WidthMm }).ToList()
    };
}

/// <summary>
/// A beam framing into a span or a stub column standing on it, as Revit models it: where it starts (mm from the span's
/// start, in the run's own axis order), its width along the beam, and a crossing beam's soffit below the run's top.
/// </summary>
public sealed record KataMeasuredLoad(double StartMm, double WidthMm, double SoffitBelowTopMm, bool IsColumn);

/// <summary>
/// One framing element over (part of) a span: where it starts (mm from the span's start, in the run's own axis
/// order), its section and its top relative to its level (row 19).
/// </summary>
public sealed record KataMeasuredPiece(double StartMm, double LengthMm, double WidthMm, double HeightMm, double TopMm)
{
    public double MidMm => StartMm + LengthMm / 2.0;
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
