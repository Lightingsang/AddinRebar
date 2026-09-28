using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.BeamRebar.Service;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;

namespace HPRebar.KataRebar.Service;

/// <summary>Free-form bars from a layout polyline: the bends are drawn in the curves, no hook types are used.</summary>
public static class KataRebarCurveFactory
{
    /// <summary>Shortest segment kept, in feet (about 0.6 mm), below Revit's short-curve tolerance.</summary>
    private const double ShortestSegmentFt = 2.0e-3;

    public static Rebar Create(Document doc, RebarStyle style, RebarBarType barType, Element host, XYZ normal, IList<Curve> curves)
    {
        // Multi-version: rebar terminations — Revit 2026 adds BarTerminationsData (the only overload left in
        // 2027); earlier versions take the hook types and orientations directly.
#if REVIT2026_OR_GREATER
        var rebar = Rebar.CreateFromCurves(doc, style, barType, host, normal, curves, new BarTerminationsData(doc), true, true);
#else
        var rebar = Rebar.CreateFromCurves(
            doc, style, barType, null, null, host, normal, curves,
            RebarHookOrientation.Right, RebarHookOrientation.Right, true, true);
#endif
        return rebar ?? throw new InvalidOperationException($"Revit không tạo được thanh thép '{barType.Name}' từ {curves.Count} đoạn cong.");
    }

    /// <summary>Model curves of a local polyline (mm), dropping segments Revit would refuse as too short.</summary>
    public static IList<Curve> Curves(Polyline3 polyline, PointMapper mapper)
    {
        var curves = new List<Curve>();
        foreach (var (from, to) in KataPolylineSegments.Of(polyline))
        {
            var p0 = mapper.ToXyz(from);
            var p1 = mapper.ToXyz(to);
            if (p0.DistanceTo(p1) > ShortestSegmentFt)
                curves.Add(Line.CreateBound(p0, p1));
        }

        return curves.Count > 0 ? curves : throw new InvalidOperationException("Đường tim thanh thép không còn đoạn nào dài hơn dung sai Revit.");
    }
}
