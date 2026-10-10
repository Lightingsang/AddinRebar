using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Service;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using Serilog;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// One cross section per section flag of Kata's elevation (<see cref="KataSectionCuts"/>), named "&lt;B3&gt; n-n" — two
/// flags cutting the same bars share the number, each still gets its view as Kata draws every section —, looking
/// along the run (+local X: the run's left side, +Y, on the left as Kata draws it), cropped to Kata's whole section
/// drawing, the far clip 300 mm on. On each: Kata's break lines through the slab, the circles round inner-layer bars
/// and Kata's dimensions (<see cref="KataDrawingDims"/>); the bars themselves are the model's.
/// </summary>
internal static class KataCrossSections
{
    private const double MarginMm = 50.0;
    private const double FarClipMm = 300.0;

    public sealed record Placed(ViewSection View, KataSectionDrawing Drawing, KataSectionCut Cut);

    public sealed record Outcome(IReadOnlyList<Placed> Views, int Dims, int RefusedDims, IReadOnlyList<string> Superseded, IReadOnlyList<string> Skipped);

    public static Outcome Create(Document doc, KataRebarPlan plan, PointMapper mapper, string runKey, string beamName,
        IReadOnlyList<KataSectionCut> cuts, IReadOnlyList<Rebar> bars)
    {
        var placed = new List<Placed>();
        var superseded = new List<string>();
        var skipped = new List<string>();
        int dims = 0, refused = 0;
        for (int i = 0; i < cuts.Count; i++)
        {
            var cut = cuts[i];
            KataSectionDrawing drawing;
            try
            {
                drawing = KataSectionDrawingBuilder.Build(plan.Spec, plan.Layout, plan.Rules, cut, mirror: false);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                Log.Warning(ex, "Kata Rebar: section {Number}-{Number} at {X:0} not drawn", cut.Number, cut.Number, cut.X);
                skipped.Add($"{cut.Number}-{cut.Number}");
                continue;
            }

            XYZ ToModel(double x, double z) => mapper.ToXyz(new Point3(cut.X, -x, z));
            var frame = new KataSectionViews.Frame(
                mapper.ToXyz(new Point3(cut.X, 0.0, 0.0)), mapper.AxisY.Negate(), mapper.AxisX,
                drawing.MinX - MarginMm, drawing.MaxX + MarginMm, drawing.Bottom - MarginMm, drawing.Top + MarginMm, FarClipMm);
            var result = KataSectionViews.FindOrCreate(doc, runKey, KataDraftingStorage.CrossKind(i), frame, $"{beamName} {cut.Number}-{cut.Number}");
            if (result.Superseded is not null) superseded.Add(result.Superseded);

            var view = result.View;
            foreach (var rebar in bars) rebar.SetUnobscuredInView(view, true);
            DrawLines(doc, view, runKey, drawing, ToModel);
            var (made, no) = KataDrawingDims.Create(doc, view, runKey, KataDimChains.From(drawing.Dims), ToModel);
            dims += made;
            refused += no;
            placed.Add(new Placed(view, drawing, cut));
        }

        return new Outcome(placed, dims, refused, superseded, skipped);
    }

    /// <summary>Kata's break lines through the slab and the circles round the bars of an inner layer.</summary>
    private static void DrawLines(Document doc, ViewSection view, string runKey, KataSectionDrawing drawing, Func<double, double, XYZ> toModel)
    {
        var thin = KataLineStyles.Thin(doc);
        foreach (var polyline in drawing.Lines.Where(l => l.Pen == KataDrawingPen.Thin))
        {
            var v = polyline.Vertices;
            for (int k = 1; k < v.Count; k++)
            {
                var a = toModel(v[k - 1].X, v[k - 1].Z);
                var b = toModel(v[k].X, v[k].Z);
                if (a.DistanceTo(b) < RevitUnits.MmToFt(0.5)) continue;
                Tag(doc.Create.NewDetailCurve(view, Line.CreateBound(a, b)), thin, runKey);
            }
        }

        var fine = KataLineStyles.Fine(doc);
        foreach (var mark in drawing.Marks)
        {
            var centre = toModel(mark.X, mark.Z);
            var circle = Arc.Create(centre, RevitUnits.MmToFt(mark.Radius), 0.0, 2.0 * Math.PI, view.RightDirection, view.UpDirection);
            Tag(doc.Create.NewDetailCurve(view, circle), fine, runKey);
        }
    }

    private static void Tag(DetailCurve curve, GraphicsStyle style, string runKey)
    {
        curve.LineStyle = style;
        KataDraftingStorage.Write(curve, runKey, KataDraftingStorage.DraftingKind);
    }
}
