using Autodesk.AutoCAD.DatabaseServices;
using HPAutoCad.Aec.Geometry;
using AcadException = Autodesk.AutoCAD.Runtime.Exception;

namespace HPAutoCad.Aec.Cad;

/// <summary>
///     The curve sampling, block/attribute reading and style helpers behind <see cref="EntityShapeReader.Read"/>:
///     generic curves through AutoCAD's own sample points, arc step counts from the chord tolerance, dynamic block
///     names, attribute values, and the colour text. Kept apart from the entity switch so each half stays readable.
/// </summary>
public sealed partial class EntityShapeReader
{
    /// <summary>Samples per drawing-unit-independent curve: enough for predicates, bounded so a long spline cannot explode a response.</summary>
    private const int MinCurveSamples = 8;
    private const int MaxCurveSamples = 512;

    /// <summary>The effective (dynamic) block name a user knows the block by; the reference's own name is the anonymous *U-name for dynamic blocks.</summary>
    public string EffectiveBlockName(BlockReference block) => BlockName(block);

    /// <summary>Attribute values only — cheaper than a full record when a text filter has to look inside many block references.</summary>
    public IEnumerable<string> AttributeValues(BlockReference block) => ReadAttributes(block) is { } attributes ? attributes.Values : Enumerable.Empty<string>();

    /// <summary>An arc through AutoCAD's own WCS sample points (correct for any OCS normal), the exact length beside it.</summary>
    private PlanShape SampleArc(Arc arc)
    {
        var sweep = Math.Abs(arc.EndAngle - arc.StartAngle);
        if (sweep < 0) sweep += 2 * Math.PI;
        using var geCurve = arc.GetGeCurve();
        var samples = geCurve.GetSamplePoints(ArcSteps(Mm(arc.Radius), sweep) + 1);
        return new PlanShape(samples.Select(s => ToPt(s.Point)).ToArray(), false, Mm(arc.Length), approximate: true);
    }

    private PlanShape? SampleCurve(Curve curve)
    {
        double lengthDu;
        try { lengthDu = curve.GetDistanceAtParameter(curve.EndParam) - curve.GetDistanceAtParameter(curve.StartParam); }
        catch (AcadException) { lengthDu = 0; }
        var lengthMm = Mm(Math.Abs(lengthDu));
        if (lengthMm <= _tol.PointEquality) return PlanShape.Point(ToPt(curve.StartPoint));

        using var geCurve = curve.GetGeCurve();
        var samples = geCurve.GetSamplePoints(Math.Clamp((int)(lengthMm / Math.Max(_tol.ChordError * 20, 1)), MinCurveSamples, MaxCurveSamples));
        var points = samples.Select(s => ToPt(s.Point)).ToList();
        var closed = curve.Closed && points.Count >= 3;
        if (closed && points[0].AlmostEqualsXY(points[^1], _tol.PointEquality)) points.RemoveAt(points.Count - 1);
        double? area = null;
        if (closed)
        {
            try { area = Mm(curve.Area) * _units.MmPerUnit; } catch (AcadException) { area = null; }
        }

        return new PlanShape(points, closed, lengthMm, area, approximate: true);
    }

    private int ArcSteps(double radiusMm, double sweepRadians)
    {
        if (radiusMm <= 0) return 4;
        var maxStep = _tol.ChordError >= radiusMm ? Math.PI / 2 : 2 * Math.Acos(1 - _tol.ChordError / radiusMm);
        return Math.Clamp((int)Math.Ceiling(sweepRadians / Math.Max(maxStep, 1e-6)), 4, PlanShape.MaxArcSteps);
    }

    private string BlockName(BlockReference block)
    {
        try
        {
            var id = block.IsDynamicBlock ? block.DynamicBlockTableRecord : block.BlockTableRecord;
            return ((BlockTableRecord)_tr.GetObject(id, OpenMode.ForRead)).Name;
        }
        catch (AcadException)
        {
            return block.Name;
        }
    }

    private Dictionary<string, string>? ReadAttributes(BlockReference block)
    {
        Dictionary<string, string>? attributes = null;
        foreach (ObjectId attributeId in block.AttributeCollection)
        {
            try
            {
                if (_tr.GetObject(attributeId, OpenMode.ForRead) is not AttributeReference attribute) continue;
                attributes ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                attributes[attribute.Tag] = attribute.TextString;
            }
            catch (AcadException)
            {
                // an erased attribute reference: skip it, the block is still worth reporting
            }
        }

        return attributes;
    }

    private static string ColorOf(Entity entity)
    {
        var color = entity.Color;
        if (color.IsByLayer) return "ByLayer";
        if (color.IsByBlock) return "ByBlock";
        return color.IsByAci ? color.ColorIndex.ToString() : color.ColorNameForDisplay;
    }
}
