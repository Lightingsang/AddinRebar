using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace HPRebar.BeamRebar.Model;

/// <summary>
/// Revit-side geometry handles and references for a single beam span.
/// All numeric dimensions reside in the matching <see cref="HPRebar.Core.BeamRebar.Models.BeamSpan"/>.
/// </summary>
public sealed record BeamFaces
{
    /// <summary>The Revit FamilyInstance of category OST_StructuralFraming.</summary>
    public Element Element { get; init; } = null!;

    /// <summary>Top horizontal planar face (+Z direction).</summary>
    public PlanarFace Top { get; init; } = null!;

    /// <summary>Bottom horizontal soffit planar face (-Z direction).</summary>
    public PlanarFace Bottom { get; init; } = null!;

    /// <summary>Left vertical lateral face (+Y_beam direction).</summary>
    public PlanarFace? Left { get; init; }

    /// <summary>Right vertical lateral face (-Y_beam direction).</summary>
    public PlanarFace? Right { get; init; }

    /// <summary>Start vertical end cut face (-X_beam direction).</summary>
    public PlanarFace? StartFace { get; init; }

    /// <summary>End vertical end cut face (+X_beam direction).</summary>
    public PlanarFace? EndFace { get; init; }

    /// <summary>Reference level assigned to the beam.</summary>
    public Level? Level { get; init; }

    /// <summary>Zero-based index of the span in the continuous stack.</summary>
    public int SpanIndex { get; init; }

    /// <summary>Supporting structural elements (columns, walls, girders) touching or below this span.</summary>
    public IReadOnlyList<Element> IntersectingSupports { get; init; } = new List<Element>();

    /// <summary>Secondary framing beams framing into the web of this span.</summary>
    public IReadOnlyList<Element> IntersectingSecondaryBeams { get; init; } = new List<Element>();

    // --- Property Aliases for Ergonomics & Cross-Plan Compatibility ---
    public PlanarFace TopFace => Top;
    public PlanarFace BottomFace => Bottom;
    public PlanarFace? LeftFace => Left;
    public PlanarFace? RightFace => Right;
    public PlanarFace? LeftVertical => Left;
    public PlanarFace? RightVertical => Right;
    public PlanarFace TopHorizontal => Top;
    public PlanarFace BottomHorizontal => Bottom;
}
