using System.Collections.Generic;

namespace HPRebar.Core.KataExport.Models;

public enum KataSegmentKind
{
    Support,
    Span,

    /// <summary>Zero-width support where two framing elements meet without anything under them.</summary>
    Joint
}

/// <summary>One column of the Kata sheet: a support, a span or a joint, in axis order.</summary>
/// <param name="Support">The (merged) support, for <see cref="KataSegmentKind.Support"/>.</param>
/// <param name="Piece">The framing element under the span's midpoint, for <see cref="KataSegmentKind.Span"/>.</param>
public sealed record KataSegment(KataSegmentKind Kind, Interval1D Extent, KataSupport? Support = null, KataBeamPiece? Piece = null);

/// <summary>Ordered segments of a run, whether each end sits on a support, and what was ignored or looks odd.</summary>
public sealed record KataSegmentation(
    IReadOnlyList<KataSegment> Segments,
    bool StartsWithSupport,
    bool EndsWithSupport,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Cell values for sheet "Dam": <see cref="HeaderColumn"/> goes to B3..B10, each row from column C.
/// Numbers are <see cref="double"/> (or the header parameter's own type), empty cells are "".
/// </summary>
public sealed record KataSheet(
    IReadOnlyList<object?> HeaderColumn,
    IReadOnlyList<object?> Row11,
    IReadOnlyList<object?> Row19,
    IReadOnlyList<object?> Row21,
    IReadOnlyList<object?> Row22,
    IReadOnlyList<object?> Row23,
    IReadOnlyList<string> Warnings)
{
    /// <summary>Columns C..BZ of the Kata sheet.</summary>
    public const int MaxColumns = 76;

    public int ColumnCount => Row11.Count;

    /// <summary>Rows in writing order with their Excel row number.</summary>
    public IEnumerable<(int Row, IReadOnlyList<object?> Values)> Rows()
    {
        yield return (11, Row11);
        yield return (19, Row19);
        yield return (21, Row21);
        yield return (22, Row22);
        yield return (23, Row23);
    }
}
