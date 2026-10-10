using System;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.BeamRebar.Service;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// The long section of a beam run, like one elevation of Kata's drawing: a section view looking across the beam with
/// the first support on the left, its cut plane in front of the beam's front face (a projection of the whole beam, as
/// Kata draws it; the cut marks and dimension ticks lie on that plane) and its far clip past the back face, cropped to
/// the bars plus a margin — and to Kata's whole elevation drawing when the drawings are made —, at 1:25, without a view
/// template (<see cref="KataSectionViews"/>).
/// </summary>
internal static class KataLongSectionView
{
    private const double MarginMm = 400.0;

    /// <summary>The cut plane in front of the front face and the far clip behind the back face, each this far off it.</summary>
    private const double ClearOfFaceMm = 300.0;

    /// <summary>Local y of the plane the section cuts on (in front of the beam): the marks are drawn on it.</summary>
    public static double CutPlaneY(PointMapper mapper, double widthMm) =>
        -LookAlong(mapper).DotProduct(mapper.AxisY.Normalize()) * (HalfWidth(widthMm) + ClearOfFaceMm);

    /// <param name="drawing">Kata's elevation drawing, whose dimensions and flags must be inside the crop; null when not drafted.</param>
    public static KataSectionViews.Result FindOrCreate(Document doc, string runKey, KataRebarPlan plan, PointMapper mapper, string viewName,
        KataElevationDrawing? drawing = null)
    {
        var (left, right, bottom, top) = Extents(plan, drawing);
        var frame = new KataSectionViews.Frame(
            mapper.ToXyz(new Point3(0.0, CutPlaneY(mapper, plan.Spec.Width), 0.0)), mapper.AxisX, LookAlong(mapper),
            left, right, bottom, top, 2.0 * (HalfWidth(plan.Spec.Width) + ClearOfFaceMm));
        return KataSectionViews.FindOrCreate(doc, runKey, KataDraftingStorage.SectionKind, frame, viewName);
    }

    /// <summary>
    /// The crop in local millimetres (x along the run, z up from its top): the bars with a margin, and Kata's whole
    /// elevation drawing when it is drafted.
    /// </summary>
    public static (double Left, double Right, double Bottom, double Top) Extents(KataRebarPlan plan, KataElevationDrawing? drawing)
    {
        var points = plan.Layout.LongitudinalBars.SelectMany(b => b.Polyline.Points).ToList();
        if (points.Count == 0) throw new InvalidOperationException("Không có thép dọc để dựng mặt cắt dọc dầm.");
        double left = points.Min(p => p.X) - MarginMm, right = points.Max(p => p.X) + MarginMm;
        double bottom = points.Min(p => p.Z) - MarginMm, top = Math.Max(0.0, points.Max(p => p.Z)) + MarginMm;
        return drawing is null
            ? (left, right, bottom, top)
            : (Math.Min(left, drawing.MinX), Math.Max(right, drawing.MaxX), Math.Min(bottom, drawing.Bottom), Math.Max(top, drawing.Top));
    }

    /// <summary>Looking across the beam from its front with local X to the screen's right (right = look × up).</summary>
    private static XYZ LookAlong(PointMapper mapper) => mapper.AxisX.Normalize().Negate().CrossProduct(XYZ.BasisZ);

    private static double HalfWidth(double widthMm) => widthMm > 0.0 ? widthMm / 2.0 : 500.0;
}
