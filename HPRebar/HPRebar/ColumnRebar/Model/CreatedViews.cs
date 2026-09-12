using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace HPRebar.ColumnRebar.Model;

/// <summary>The views the annotation pass produced.</summary>
public sealed record CreatedViews
{
    /// <summary>Elevation looking along the column's width.</summary>
    public ViewSection? DetailX { get; init; }

    /// <summary>Elevation looking along the column's depth.</summary>
    public ViewSection? DetailY { get; init; }

    /// <summary>One cross-section per column segment, bottom to top.</summary>
    public IReadOnlyList<ViewSection> Sections { get; init; } = new List<ViewSection>();

    public int Total => (DetailX is null ? 0 : 1) + (DetailY is null ? 0 : 1) + Sections.Count;
}
