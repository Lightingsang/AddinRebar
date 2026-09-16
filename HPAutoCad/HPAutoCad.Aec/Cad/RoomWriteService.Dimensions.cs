using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using HPAutoCad.Aec.Architecture;
using HPAutoCad.Aec.Model;

namespace HPAutoCad.Aec.Cad;

/// <summary>The dimensions <c>arch_auto_dimension_plan</c> draws: one aligned dimension per plan on the requested layer and style.</summary>
public static partial class RoomWriteService
{
    /// <summary>Dimensions per call: each is ~320 B in the answer (item + plan) and one entity in the drawing; 120 stays under the 64 KB cap with 7-digit coordinates.</summary>
    public const int MaxDimensions = 120;

    public static EditResult WriteDimensions(EditContext cx, CancellationToken ct, IReadOnlyList<DimensionPlan> plans, string? layerName, string? dimStyle, string? space, bool dryDecisionsOnly)
    {
        var result = new EditResult();
        if (plans.Count > MaxDimensions) throw new ArgumentException($"{plans.Count} dimensions planned; the maximum per call is {MaxDimensions} — narrow the subjects (roomIds / filter) or the sides.");
        var spaceId = cx.SpaceId(space, out var spaceError);
        if (spaceId.IsNull) throw new ArgumentException(spaceError!.Message);
        var styleId = AnnotationService.DimStyle(cx, dimStyle, out var styleError);
        if (styleId.IsNull) throw new ArgumentException(styleError!.Message);
        var layer = cx.LayerForCreate(layerName, out var layerError, out var layerWarning);
        if (layer is null) return EditResult.Refused(layerError!, type: "DIMENSION");
        if (layerWarning is not null) result.Warn(layerWarning);

        if (dryDecisionsOnly)
        {
            result.Items = plans.Select((p, i) => new ItemOutcome(i, true, null, "DIMENSION", [$"{p.Rule}:{p.Subject}:{p.Side}", $"{p.MeasurementMm:0} mm"])).ToArray();
            result.Summary = Summary(plans, layer.Name, dimStyle, 0, listPlans: true);
            return result;
        }

        var target = (BlockTableRecord)cx.Tr.GetObject(spaceId, OpenMode.ForWrite);
        var outcomes = new ItemOutcome[plans.Count];
        for (var i = 0; i < plans.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var p = plans[i];
            var dimension = new AlignedDimension(Point(cx, p.P1Mm), Point(cx, p.P2Mm), Point(cx, p.DimLineMm), "", styleId);
            dimension.SetDatabaseDefaults(cx.Db);
            dimension.LayerId = layer.ObjectId;
            target.AppendEntity(dimension);
            cx.Tr.AddNewlyCreatedDBObject(dimension, true);
            var handle = dimension.Handle.ToString();
            result.Created(handle);
            outcomes[i] = new ItemOutcome(i, true, handle, "DIMENSION", [$"{p.Rule}:{p.Subject}:{p.Side}", $"{p.MeasurementMm:0} mm"]);
        }

        result.Settle(outcomes);
        result.Summary = Summary(plans, layer.Name, dimStyle, plans.Count, listPlans: false);
        return result;
    }

    private static Point3d Point(EditContext cx, Geometry.Pt p) => new(cx.ToDrawing(p.X), cx.ToDrawing(p.Y), 0);

    /// <summary>The plan is listed in a preview; after the write the items carry rule, subject, side and measurement, so the points are not echoed a second time.</summary>
    private static object Summary(IReadOnlyList<DimensionPlan> plans, string layer, string? dimStyle, int drawn, bool listPlans) => new
    {
        planned = plans.Count,
        drawn,
        layer,
        dimStyle = dimStyle ?? "(current)",
        byRule = plans.GroupBy(p => p.Rule).ToDictionary(g => g.Key, g => g.Count()),
        bySubject = plans.GroupBy(p => p.Subject).ToDictionary(g => g.Key, g => g.Count()),
        dimensions = listPlans ? plans.Select(p => new { rule = p.Rule, subject = p.Subject, side = p.Side, p1Mm = p.P1Mm.Rounded(), p2Mm = p.P2Mm.Rounded(), dimLineMm = p.DimLineMm.Rounded(), measurementMm = Math.Round(p.MeasurementMm, 1) }).ToArray() : null,
    };
}
