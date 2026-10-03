using System.Collections.Generic;
using Autodesk.Revit.DB;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.ColumnRebar.Model;

/// <summary>
///     A validated run of stacked columns, ready to reinforce. <see cref="Sections"/> and
///     <see cref="Faces"/> are index-aligned and ordered bottom to top.
/// </summary>
public sealed record ColumnStack
{
    public ColumnSectionStyle Style { get; init; }

    /// <summary>Pure numbers, millimetres, measured from <see cref="DatumFace"/>.</summary>
    public IReadOnlyList<ColumnSection> Sections { get; init; } = new List<ColumnSection>();

    public IReadOnlyList<ColumnFaces> Faces { get; init; } = new List<ColumnFaces>();

    /// <summary>
    ///     The face every vertical position is measured from: the top of whatever supports the bottom
    ///     column, falling back to the bottom column's own base.
    /// </summary>
    public PlanarFace DatumFace { get; init; } = null!;

    /// <summary>South face of the bottom column — the Y datum. Null on a circular stack.</summary>
    public PlanarFace? SouthDatum { get; init; }

    /// <summary>West face of the bottom column — the X datum. Null on a circular stack.</summary>
    public PlanarFace? WestDatum { get; init; }

    /// <summary>Insertion point of the bottom column — the plan datum for a circular stack.</summary>
    public XYZ? PointDatum { get; init; }

    /// <summary>Faces the dimension pass will hang witness lines off, bottom to top.</summary>
    public IReadOnlyList<PlanarFace> DimensionFaces { get; init; } = new List<PlanarFace>();
}
