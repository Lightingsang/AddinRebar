using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>The arrowhead block of a section leader (its size is <see cref="KataSectionLeader.ArrowSize"/>).</summary>
public enum KataLeaderArrow
{
    /// <summary>No arrowhead (the layer-2 leaders: their bars carry their own circles).</summary>
    None,

    /// <summary>_DotBlank: a circle half the arrow size round the point, the leader from its edge.</summary>
    DotBlank,

    /// <summary>_DotSmall: a small filled dot on the point.</summary>
    DotSmall,

    /// <summary>AutoCAD's closed filled arrow, its tip on the point.</summary>
    Closed
}

/// <summary>A polyline vertex; <paramref name="Bulge"/> turns the segment to the next vertex into an arc (AutoCAD's bulge, negative clockwise).</summary>
public sealed record KataBulgeVertex(double X, double Z, double Bulge = 0.0);

/// <summary>A polyline of the section (outline, break line, hoop, tie, a stub from a marking circle to its leader).</summary>
public sealed record KataSectionPolyline(KataDrawingPen Pen, IReadOnlyList<KataBulgeVertex> Vertices);

/// <summary>A bar cut by the section (kata_block_THEP at scale <paramref name="Diameter"/>), as Kata draws it.</summary>
public sealed record KataSectionBar(double X, double Z, double Diameter, int Number);

/// <summary>A LEADER from <c>Points[0]</c> (where the arrowhead sits) to its last point.</summary>
public sealed record KataSectionLeader(IReadOnlyList<(double X, double Z)> Points, KataLeaderArrow Arrow, double ArrowSize);

/// <summary>A thin circle marking a bar of an inner layer (kata_net manh).</summary>
public sealed record KataSectionMark(double X, double Z, double Radius);

/// <summary>
/// A tag (kata_block_KHT) inserted at (<paramref name="X"/>, <paramref name="Z"/>): its text on the leader before the
/// insertion, its number circles beyond it, on the side <paramref name="PointsRight"/> says. A stirrup or tie tag
/// writes its <paramref name="Spacing"/> ("a500") under the leader, the bars ("Ø8") on it.
/// </summary>
public sealed record KataSectionTag(double X, double Z, bool PointsRight, string Text, IReadOnlyList<int> Numbers, string Spacing = "")
{
    /// <summary>One line on the leader, or the bars on it and the spacing under it.</summary>
    public KataTagLayout Layout => Spacing.Length == 0 ? KataTagLayout.OneLine : KataTagLayout.SpacingBelow;

    /// <summary>Kata's visibility state of the block, e.g. P12.</summary>
    public string BlockState => KataTagState.Of(PointsRight, Numbers.Count, Layout);
}

/// <summary>
/// Kata's section n-n of a beam (T2-DY7.dwg, TL 1/25) in model millimetres: X across the beam from its centre line,
/// Z up from its top. Lines, bars, leaders, marking circles and tags as Kata draws them; dimensions and title in the
/// elevation's own records.
/// </summary>
/// <param name="MinX">Leftmost and rightmost extent of everything drawn (mm).</param>
/// <param name="Top">Highest and lowest extent of everything drawn (mm).</param>
public sealed record KataSectionDrawing(
    int Number,
    IReadOnlyList<KataSectionPolyline> Lines,
    IReadOnlyList<KataSectionBar> Bars,
    IReadOnlyList<KataSectionLeader> Leaders,
    IReadOnlyList<KataSectionMark> Marks,
    IReadOnlyList<KataSectionTag> Tags,
    IReadOnlyList<KataDrawingDim> Dims,
    KataDrawingTitle Title,
    double MinX,
    double MaxX,
    double Top,
    double Bottom);
