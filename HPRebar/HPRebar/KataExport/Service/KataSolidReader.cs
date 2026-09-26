using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.KataExport.Service;

/// <summary>
/// Solids of an element and where probe lines along the run pass through them. Family instances that are
/// not cut or joined keep their solids inside a <see cref="GeometryInstance"/>, so both levels are read —
/// reading only the top level loses every such column and footing. Geometry Revit cannot evaluate (open or
/// imported solids) is skipped and counted instead of stopping the export.
/// </summary>
public static class KataSolidReader
{
    private const double MinVolume = 1.0e-6;

    public static IReadOnlyList<Solid> GetSolids(Element element) =>
        Extract(element.get_Geometry(new Options { DetailLevel = ViewDetailLevel.Fine }));

    /// <summary>
    /// Framing geometry before joins, cut-backs and coping, placed in the model. A beam joined to a floor or
    /// cut back to a column face keeps its full section and length here, which is what Kata dimensions.
    /// </summary>
    public static IReadOnlyList<Solid> GetOriginalSolids(FamilyInstance instance)
    {
        try
        {
            var original = instance.GetOriginalGeometry(new Options { DetailLevel = ViewDetailLevel.Fine });
            var solids = Extract(original?.GetTransformed(instance.GetTransform()));
            return solids.Count > 0 ? solids : GetSolids(instance);
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException)
        {
            return GetSolids(instance);
        }
    }

    /// <summary>Station intervals where any probe line runs inside the solids, and how many solids could not be probed.</summary>
    public static (IReadOnlyList<Interval1D> Intervals, int Skipped) ProbeIntervals(IReadOnlyList<Solid> solids, IReadOnlyList<Line> probes, KataAxisFrame frame)
    {
        var intervals = new List<Interval1D>();
        var options = new SolidCurveIntersectionOptions { ResultType = SolidCurveIntersectionMode.CurveSegmentsInside };
        var broken = new HashSet<Solid>();

        foreach (var solid in solids)
        {
            foreach (var probe in probes)
            {
                try
                {
                    var hit = solid.IntersectWithCurve(probe, options);
                    for (int i = 0; i < hit.SegmentCount; i++)
                    {
                        var segment = hit.GetCurveSegment(i);
                        intervals.Add(new Interval1D(frame.Station(segment.GetEndPoint(0)), frame.Station(segment.GetEndPoint(1))));
                    }
                }
                catch (Autodesk.Revit.Exceptions.ApplicationException)
                {
                    broken.Add(solid);
                    break;
                }
            }
        }

        return (Merge(intervals), broken.Count);
    }

    /// <summary>Lowest and highest elevation (feet) of the solids' vertices.</summary>
    public static (double BottomFt, double TopFt)? VerticalRange(IReadOnlyList<Solid> solids)
    {
        var zs = Vertices(solids).Select(p => p.Z).ToList();
        return zs.Count == 0 ? null : (zs.Min(), zs.Max());
    }

    public static IEnumerable<XYZ> Vertices(IReadOnlyList<Solid> solids) =>
        solids.SelectMany(s => s.Edges.Cast<Edge>()).SelectMany(e => e.Tessellate());

    private static IReadOnlyList<Solid> Extract(GeometryElement? geometry)
    {
        var solids = new List<Solid>();
        if (geometry is null) return solids;

        foreach (var obj in geometry)
        {
            switch (obj)
            {
                case Solid solid when HasVolume(solid):
                    solids.Add(solid);
                    break;
                case GeometryInstance instance:
                    solids.AddRange(instance.GetInstanceGeometry().OfType<Solid>().Where(HasVolume));
                    break;
            }
        }

        return solids;
    }

    private static bool HasVolume(Solid solid)
    {
        try
        {
            return solid.Volume > MinVolume;
        }
        catch (Autodesk.Revit.Exceptions.ApplicationException)
        {
            return false;
        }
    }

    private static IReadOnlyList<Interval1D> Merge(List<Interval1D> intervals)
    {
        var merged = new List<Interval1D>();
        foreach (var interval in intervals.OrderBy(i => i.Start))
        {
            if (merged.Count > 0 && merged[merged.Count - 1].Overlaps(interval))
                merged[merged.Count - 1] = merged[merged.Count - 1].Union(interval);
            else
                merged.Add(interval);
        }

        return merged;
    }
}
