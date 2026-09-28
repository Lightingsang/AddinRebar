using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.KataRebar.Service;

/// <summary>
/// Creates the continuous top and bottom bars of a plan, one Rebar per bar, hosted on the framing element
/// under the middle of the bar and drawn with their anchorage bends in the curves.
/// </summary>
public static class KataRebarCreationService
{
    public static int CreateMainBars(
        Document doc,
        KataRebarPlan plan,
        KataBeamPlacement placement,
        IReadOnlyDictionary<double, RebarBarType> barTypes)
    {
        int created = 0;
        foreach (var bar in plan.Layout.MainTopBars.Concat(plan.Layout.MainBottomBars))
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
