using System;
using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Reinforcement and geometric specification at a support/column location (odd columns in sheet Dam).
/// </summary>
public sealed record KataSupportRebarSpec
{
    /// <summary>0-based index of this support along the beam run.</summary>
    public int SupportIndex { get; init; }

    /// <summary>1-based sheet column this entry was read from (3 = C); 0 when the spec was not read from a sheet.</summary>
    public int SheetColumn { get; init; }

    /// <summary>Column/support width along beam axis in mm (sheet Dam row 11). 0 indicates cantilever tip or free end.</summary>
    public double ColumnWidth { get; init; }

    /// <summary>Optional section description if bearing on another beam (e.g. "300x500").</summary>
    public string SupportSection { get; init; } = "";

    /// <summary>Depth of a crossing beam carrying the run ("200x350" in row 11 gives 350); 0 for a column.</summary>
    public double BeamDepth { get; init; }

    /// <summary>
    /// Row 11 says something other than a width: text, a negative number. A support of no width is written "0"; this
    /// one is neither, so the run cannot be drawn until the cell is fixed.
    /// </summary>
    public bool WidthUnreadable { get; init; }

    /// <summary>True if this support represents a cantilever tip or zero-width end joint.</summary>
    public bool IsCantilever => ColumnWidth <= 0.0;

    /// <summary>Name of grid line intersecting this support (sheet Dam row 22, e.g. "1", "A").</summary>
    public string GridName { get; init; } = "";

    /// <summary>Offset from support centerline to grid line in mm (sheet Dam row 23).</summary>
    public double GridOffset { get; init; }

    /// <summary>Width of column directly above this floor level in mm (sheet Dam row 19).</summary>
    public double UpperColumnWidth { get; init; }

    /// <summary>Offset of column directly above this floor level in mm (sheet Dam row 19).</summary>
    public double UpperColumnOffset { get; init; }

    /// <summary>Width of secondary intersecting beam bearing at this support in mm (sheet Dam row 20).</summary>
    public double CrossingBeamWidth { get; init; }

    /// <summary>Depth of the crossing beam ("400x500" in row 20 gives 500); 0 when row 20 gives its width only.</summary>
    public double CrossingBeamDepth { get; init; }

    /// <summary>Offset of secondary intersecting beam in mm (sheet Dam row 21).</summary>
    public double CrossingBeamOffset { get; init; }

    /// <summary>Top extra negative reinforcement bars - Layer 1 (sheet Dam row 13).</summary>
    public IReadOnlyList<KataBarItem> TopExtraLayer1 { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Top extra negative reinforcement bars - Layer 2 (sheet Dam row 14).</summary>
    public IReadOnlyList<KataBarItem> TopExtraLayer2 { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Top extra negative reinforcement bars - Layer 3 (sheet Dam row 15).</summary>
    public IReadOnlyList<KataBarItem> TopExtraLayer3 { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Top extra negative reinforcement bars - Layer 4 (sheet Dam row 16).</summary>
    public IReadOnlyList<KataBarItem> TopExtraLayer4 { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>
    /// Additional top bars of rows 13-16 split by side: a cell "left;right" gives the bars reaching into the
    /// span on each side, a cell without ';' the same bars on both sides. Index 0 = row 13.
    /// </summary>
    public IReadOnlyList<KataSideBars> TopExtraSides { get; init; } = Array.Empty<KataSideBars>();

    /// <summary>
    /// Row 24 "*": no joint stirrups at this support, and the span after it takes no inner stirrups from the span
    /// before (B01 K: span L has none; B03 G has no "*": span 3 keeps span 2's "Đai C 2").
    /// </summary>
    public bool Row24Star { get; init; }

    /// <summary>
    /// Row 17 over a support with a width, as written: "-" (the bars of row 17 run on through it), "-;0" (they end in
    /// it), or bars ("2f20", "left;right") laid over it; see <c>KataSupportBottomBarLayout</c>.
    /// </summary>
    public string BottomLayer2Text { get; init; } = "";

    /// <summary>Convenience accessor for all 4 top extra bar layers.</summary>
    public IReadOnlyList<IReadOnlyList<KataBarItem>> AllTopExtraLayers =>
        new[] { TopExtraLayer1, TopExtraLayer2, TopExtraLayer3, TopExtraLayer4 };
}
