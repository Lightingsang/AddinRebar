using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace HPRebar.ColumnRebar.Model;

/// <summary>
///     The Revit-side handles for one column segment. Everything numeric lives in the matching
///     <see cref="HPRebar.Core.ColumnRebar.Models.ColumnSection"/>; this record keeps only what the creation
///     services need to talk back to Revit.
/// </summary>
public sealed record ColumnFaces
{
    public Element Element { get; init; } = null!;

    public PlanarFace Top { get; init; } = null!;

    public PlanarFace Bottom { get; init; } = null!;

    /// <summary>Null on a circular column, which has no flat sides.</summary>
    public PlanarFace? South { get; init; }

    public PlanarFace? North { get; init; }

    public PlanarFace? West { get; init; }

    public PlanarFace? East { get; init; }

    public IReadOnlyList<CylindricalFace> Cylindricals { get; init; } = new List<CylindricalFace>();

    /// <summary>Insertion point, used to place circular sections. Null on a rectangular column.</summary>
    public XYZ? LocationPoint { get; init; }

    public Level? TopLevel { get; init; }

    public Level? BottomLevel { get; init; }

    /// <summary>Beams framing into this segment's head, in document order.</summary>
    public IReadOnlyList<Element> BeamsAtTop { get; init; } = new List<Element>();
}
