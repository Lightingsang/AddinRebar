using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Creates the longitudinal bars of a plan — continuous top and bottom bars and the additional top bars over
/// the supports — one Rebar per bar, hosted on the framing element under the middle of the bar and drawn with
/// their anchorage bends in the curves.
/// </summary>
public static class KataRebarCreationService
{
    public static int CreateLongitudinalBars(
        Document doc,
        KataRebarPlan plan,
        KataBeamPlacement placement,
        IReadOnlyDictionary<double, RebarBarType> barTypes)
    {
        int created = 0;
        foreach (var bar in plan.Layout.LongitudinalBars)
        {
            var points = bar.Polyline.Points;
            var host = placement.HostAt((points[0].X + points[points.Count - 1].X) / 2.0);
            var rebar = KataRebarCurveFactory.Create(
                doc,
                RebarStyle.Standard,
                barTypes[bar.Diameter],
                host,
                placement.Mapper.AxisY,
                KataRebarCurveFactory.Curves(bar.Polyline, placement.Mapper));

            KataRebarStamp.Apply(rebar, host, plan.Spec.BeamName, bar.BarMark);
            created++;
        }

        return created;
    }
}
