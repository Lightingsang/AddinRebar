using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.BeamRebar.Service;

namespace HPRebar.BeamRebar.Model;

/// <summary>
/// A validated run of continuous beam spans, ready for reinforcement.
/// <see cref="Spans"/> and <see cref="Faces"/> are index-aligned and ordered from start to end.
/// </summary>
public sealed record BeamStack
{
    /// <summary>Overall style across all spans in the continuous run.</summary>
    public BeamSectionStyle Style { get; init; } = BeamSectionStyle.Rectangle;

    /// <summary>Pure domain representation in millimetres with zero Revit references.</summary>
    public BeamContinuousStack ContinuousStack { get; init; } = new();

    /// <summary>Shorthand accessor to spans in the continuous stack.</summary>
    public IReadOnlyList<BeamSpan> Spans => ContinuousStack.Spans;

    /// <summary>Shorthand accessor to support nodes in the continuous stack.</summary>
    public IReadOnlyList<BeamSupportNode> Supports => ContinuousStack.Supports;

    /// <summary>Shorthand accessor to secondary beam intersections.</summary>
    public IReadOnlyList<SecondaryBeamIntersection> SecondaryIntersections => ContinuousStack.SecondaryIntersections;

    /// <summary>Left-most coordinate of the entire beam run (mm).</summary>
    public double OverallStartX => ContinuousStack.OverallStartX;

    /// <summary>Right-most coordinate of the entire beam run (mm).</summary>
    public double OverallEndX => ContinuousStack.OverallEndX;

    /// <summary>Overall continuous length (mm).</summary>
    public double TotalLength => ContinuousStack.TotalLength;

    /// <summary>Revit-side geometric faces, index-aligned with <see cref="Spans"/>.</summary>
    public IReadOnlyList<BeamFaces> Faces { get; init; } = new List<BeamFaces>();

    /// <summary>Alias for Faces.</summary>
    public IReadOnlyList<BeamFaces> SpanFaces => Faces;

    /// <summary>Coordinate transformation mapper from local (mm) to Revit world XYZ (ft).</summary>
    public PointMapper PointMapper { get; init; } = null!;

    /// <summary>Normalized unit vector along the continuous longitudinal beam axis.</summary>
    public XYZ BeamDirection { get; init; } = XYZ.BasisX;

    /// <summary>Normalized unit vector transverse to the beam axis (Z x BeamDirection).</summary>
    public XYZ TransverseDirection { get; init; } = XYZ.BasisY;

    /// <summary>Alias for TransverseDirection.</summary>
    public XYZ NormalDirection => TransverseDirection;

    /// <summary>Origin point in Revit world coordinates corresponding to s = 0 mm.</summary>
    public XYZ OriginPoint { get; init; } = XYZ.Zero;

    /// <summary>Alias for OriginPoint.</summary>
    public XYZ StartPoint => OriginPoint;

    /// <summary>Alias for BeamDirection.</summary>
    public XYZ BeamAxis => BeamDirection;

    /// <summary>Alias for TransverseDirection.</summary>
    public XYZ SideNormal => TransverseDirection;

    /// <summary>Datum face for longitudinal stationing (e.g. left exterior support face or first span start face).</summary>
    public PlanarFace DatumFace { get; init; } = null!;

    /// <summary>Top horizontal datum face of the continuous beam (typically top face of span 0).</summary>
    public PlanarFace TopDatum { get; init; } = null!;

    /// <summary>Bottom horizontal soffit datum face of span 0.</summary>
    public PlanarFace BottomDatum { get; init; } = null!;

    /// <summary>Faces used by the dimensioning service to generate elevation witness lines.</summary>
    public IReadOnlyList<PlanarFace> DimensionFaces { get; init; } = new List<PlanarFace>();

    /// <summary>Faces of supporting elements used for elevation dimensions.</summary>
    public IReadOnlyList<PlanarFace> SupportFaces { get; init; } = new List<PlanarFace>();

    /// <summary>Top elevation of span 0 in decimal feet.</summary>
    public double TopElevationFt => Faces.Count > 0 ? Faces[0].Top.Origin.Z : 0.0;

    /// <summary>Maximum cross-section height across all spans in millimetres.</summary>
    public double MaxHeightMm => ContinuousStack.MaxHeight;

    /// <summary>Maximum cross-section width across all spans in millimetres.</summary>
    public double MaxWidthMm => Spans.Count > 0 ? Spans.Max(s => s.Width) : 0.0;

    /// <summary>EndPoint in Revit world coordinates.</summary>
    public XYZ EndPoint => OriginPoint + (ContinuousStack.TotalLength / 304.8) * BeamDirection;

    /// <summary>Generates a human-readable summary of the continuous beam stack (all mm).</summary>
    public string Summary()
    {
        var culture = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.AppendLine($"BeamStack: {Spans.Count} span(s), {Supports.Count} support(s), TotalLength={ContinuousStack.TotalLength.ToString("0.#", culture)} mm.");

        for (int i = 0; i < Spans.Count; i++)
        {
            var span = Spans[i];
            sb.AppendLine($"  Span [{i + 1}]: Lc={span.LengthCenter:0.#} mm, Ln={span.LengthClear:0.#} mm, b={span.Width:0.#} mm, h={span.Height:0.#} mm, Cover={span.Cover:0.#} mm");
        }

        for (int i = 0; i < Supports.Count; i++)
        {
            var sup = Supports[i];
            sb.AppendLine($"  Support [{i}]: {sup.Name} ({sup.Type}), CenterX={sup.CenterX:0.#} mm, Width={sup.Width:0.#} mm, IsExterior={sup.IsExterior}");
        }

        if (SecondaryIntersections.Count > 0)
        {
            sb.AppendLine($"  Secondary Intersections ({SecondaryIntersections.Count}):");
            foreach (var sec in SecondaryIntersections)
            {
                sb.AppendLine($"    Span {sec.HostSpanIndex}: CenterX={sec.CenterX:0.#} mm, bs={sec.Width:0.#} mm, hs={sec.Height:0.#} mm, Side={sec.FramingSide}");
            }
        }

        return sb.ToString();
    }
}
