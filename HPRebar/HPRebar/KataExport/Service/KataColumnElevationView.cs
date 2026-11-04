using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.KataRebar.ColumnStack;
using HPRebar.KataRebar.Service;

namespace HPRebar.KataExport.Service;

/// <summary>
/// The stack's elevation along b, as Kata draws it: one section view looking along h from in front of the stack, through
/// all of it, 1:50, from the pile cap to the roof; storey levels, the laps of group A (left) and B (right) and the hoop
/// zones dimensioned (<see cref="KataColumnElevationDims" />); per storey the outermost bar of each side and one hoop set
/// tagged. Found again on a re-run like the sections (<see cref="KataSectionViews" />).
/// </summary>
internal static class KataColumnElevationView
{
    public const int Scale = 50;
    private const string Kind = "ColumnElevation";
    private const double MarginMm = 400;
    private const double FrontMm = 100;
    private const double TagReachMm = 250;

    /// <returns>Dimensions created and refused.</returns>
    public static (int Dims, int Refused) Create(Document doc, KataColumnViewAnnotator annotator, string name,
        IReadOnlyList<KataColumnStoreyGeometry> storeys, KataColumnElevation elevation, IReadOnlyList<KataColumnCreatedRebar> rebars)
    {
        double minY = storeys.Min(s => s.MinYMm), maxY = storeys.Max(s => s.MaxYMm);
        // The section looks along Z × b (KataSectionViews.Box). In a mirrored column frame that is −h, so the view
        // stands behind the stack instead of in front of it.
        var look = XYZ.BasisZ.CrossProduct(annotator.AlongB);
        double plane = look.DotProduct(annotator.AlongH) >= 0 ? minY - FrontMm : maxY + FrontMm;
        double gap = KataColumnElevationDims.GapMm;
        var frame = new KataSectionViews.Frame(
            annotator.ToModel(elevation.CentreXMm, plane, elevation.BottomMm),
            annotator.AlongB,
            look,
            elevation.LeftMm - 2 * gap,
            elevation.RightMm + 4 * gap,
            -MarginMm,
            elevation.TopMm - elevation.BottomMm + MarginMm,
            maxY - minY + 2 * FrontMm);
        var view = KataSectionViews.FindOrCreate(doc, annotator.RunKey, Kind, frame, $"{name} – mặt đứng").View;
        view.Scale = Scale;
        annotator.ClearDrafting(view);
        foreach (var created in rebars)
        {
            created.Rebar.SetUnobscuredInView(view, true);
        }

        doc.Regenerate();
        XYZ At(double x, double z) => annotator.ToModel(elevation.CentreXMm + x, plane, z);
        var dims = KataDrawingDims.Create(doc, view, annotator.RunKey, elevation.Chains, At);
        Tag(view, annotator, storeys, elevation, rebars, At);
        return dims;
    }

    /// <summary>Each storey's outermost bar on the left and on the right, and its longest hoop zone.</summary>
    private static void Tag(ViewSection view, KataColumnViewAnnotator annotator, IReadOnlyList<KataColumnStoreyGeometry> storeys,
        KataColumnElevation elevation, IReadOnlyList<KataColumnCreatedRebar> rebars, Func<double, double, XYZ> at)
    {
        foreach (var group in rebars.GroupBy(r => r.Storey))
        {
            var g = storeys[group.Key];
            double mid = (g.BaseMm + g.TopMm) / 2;
            double X(KataColumnCreatedRebar r) => g.MinXMm + r.XMm - elevation.CentreXMm;
            var bars = group.Where(r => r.Role == KataColumnRebarRole.Bar).ToList();
            if (bars.Count > 0)
            {
                var left = bars.OrderBy(r => r.XMm).First();
                var right = bars.OrderByDescending(r => r.XMm).First();
                annotator.Tag(view, left.Rebar, at(X(left) - TagReachMm, mid));
                annotator.Tag(view, right.Rebar, at(X(right) + TagReachMm, mid + TagReachMm));
            }

            if (group.Where(r => r.Role == KataColumnRebarRole.Hoop).OrderByDescending(r => r.ToMm - r.FromMm).FirstOrDefault() is { } hoop)
            {
                annotator.Tag(view, hoop.Rebar, at(g.MaxXMm - elevation.CentreXMm + TagReachMm, (hoop.FromMm + hoop.ToMm) / 2));
            }
        }
    }
}
