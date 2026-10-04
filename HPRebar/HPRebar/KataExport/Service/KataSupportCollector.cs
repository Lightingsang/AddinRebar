using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using HPRebar.KataExport.Model;
using RevitView = Autodesk.Revit.DB.View;

namespace HPRebar.KataExport.Service;

/// <summary>
/// Finds what the run bears on — columns, structural walls, foundations and crossing beams — by running
/// probe lines along the beam centre line through their solids, which measures each support's full width
/// along the axis (also past the beam end), and finds the column standing on each support.
/// </summary>
public static class KataSupportCollector
{
    private const double ProbeInsetMm = 20.0;
    private const double ProbeReachMm = 1500.0;
    private const double SearchMarginFt = 5.0;

    /// <summary>Above the highest beam top: past any slab, still inside a column of the storey above.</summary>
    private const double UpperProbeMm = 500.0;

    /// <summary>A crossing beam whose soffit is not higher than this above the run's soffit carries the run.</summary>
    private const double CarryingSoffitToleranceMm = 25.0;

    /// <summary>
    /// How far short of the run's centre line a crossing beam may stop and still frame into a column of the run: a
    /// beam coming in from one side ends at the column, which may be half its depth from the line (perimeter columns).
    /// </summary>
    private const double OneSidedReachMm = 600.0;

    /// <summary>Width given to a one-sided crossing beam whose type has no width parameter.</summary>
    private const double DefaultCrossingWidthMm = 200.0;

    /// <summary>A column or wall whose bottom is not lower than the soffit minus this stands on the beam.</summary>
    private const double StandingToleranceMm = 20.0;

    /// <summary>A wall this close to the run direction runs along the beam and is not a point support.</summary>
    private const double ParallelWallDegrees = 30.0;

    /// <summary>
    /// Foundation probes reach this far past the run so a footing or pile cap as wide as the strip limit is measured
    /// whole at a run end, and a raft under a short run still measures longer than the limit.
    /// </summary>
    private const double FoundationReachMm = KataSupportRules.MaxFoundationSupportMm;

    private static readonly BuiltInCategory[] Categories =
    {
        BuiltInCategory.OST_StructuralColumns,
        BuiltInCategory.OST_StructuralFoundation,
        BuiltInCategory.OST_Walls,
        BuiltInCategory.OST_StructuralFraming
    };

    public static (IReadOnlyList<KataSupport> Supports, IReadOnlyList<string> Warnings) Collect(Document doc, RevitView view, KataRunGeometry run)
    {
        var scan = new Scan(run);
        var selected = new HashSet<ElementId>(run.Pieces.Select(p => p.Element.Id));

        foreach (var element in KataCandidateCollector.Near(doc, view, run, SearchMarginFt, 1.0, 2.5, Categories))
        {
            if (selected.Contains(element.Id)) continue;

            switch (element.Category?.BuiltInCategory)
            {
                case BuiltInCategory.OST_StructuralColumns:
                    scan.Column(element);
                    break;
                case BuiltInCategory.OST_Walls when IsStructural(element):
                    scan.Wall(element);
                    break;
                case BuiltInCategory.OST_StructuralFoundation:
                    scan.Foundation(element);
                    break;
                case BuiltInCategory.OST_StructuralFraming when element is FamilyInstance crossing:
                    scan.CrossingBeam(crossing);
                    break;
            }
        }

        return scan.Finish();
    }

    private static bool IsStructural(Element wall) =>
        wall.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT)?.AsInteger() == 1;

    /// <summary>State of one support scan; the probes are built once and reused for every candidate.</summary>
    private sealed class Scan
    {
        private readonly KataRunGeometry _run;
        private readonly IReadOnlyList<Line> _probes;
        private readonly IReadOnlyList<Line> _upperProbes;
        private readonly IReadOnlyList<Line> _foundationProbes;
        private readonly List<KataSupport> _supports = new();
        private readonly List<Interval1D> _uppers = new();
        private readonly List<(Interval1D Extent, double SoffitFt, string Key, string Section)> _beams = new();
        private readonly List<(Interval1D Extent, string Key, string Section)> _oneSided = new();
        private readonly List<Interval1D> _standing = new();
        private int _broken, _parallelWalls, _stripFoundations, _carriedBeams;

        public Scan(KataRunGeometry run)
        {
            _run = run;
            double inset = RevitUnits.MmToFt(ProbeInsetMm);
            _probes = run.Pieces
                .SelectMany(p => new[] { p.TopFt - inset, (p.TopFt + p.BottomFt) / 2.0, p.BottomFt + inset, p.BottomFt - inset }
                    .Select(z => Probe(p, z)))
                .ToList();
            double upperZ = run.Pieces.Max(p => p.TopFt) + RevitUnits.MmToFt(UpperProbeMm);
            _upperProbes = run.Pieces.Select(p => Probe(p, upperZ)).ToList();

            // Only just under the soffit: a foundation is a support when the beam bears on it or sits in it, not
            // when a slab drawn as a foundation merely surrounds the beam's upper part (a ground slab).
            _foundationProbes = run.Pieces.Select(p => Probe(p, p.BottomFt - inset, FoundationReachMm)).ToList();
        }

        public void Column(Element column)
        {
            var solids = KataSolidReader.GetSolids(column);
            _uppers.AddRange(Intervals(solids, _upperProbes));
            if (StandsOnRun(column)) AddStanding(column);
            else Add(KataSupportKind.Column, column, Intervals(solids, _probes));
        }

        public void Wall(Element wall)
        {
            if (wall.Location is LocationCurve { Curve: Line line } && _run.Frame.IsParallel(line.Direction, ParallelWallDegrees)) _parallelWalls++;
            else if (StandsOnRun(wall)) AddStanding(wall);
            else Add(KataSupportKind.Column, wall, Intervals(KataSolidReader.GetSolids(wall), _probes));
        }

        public void Foundation(Element foundation)
        {
            var solids = KataSolidReader.GetSolids(foundation);

            // Footings and pile caps drawn as foundation slabs are supports; lean concrete drawn the same way is not.
            // Lean concrete touches nearly every tie beam, so it is dropped without a warning.
            if (foundation is Floor slab && SlabThicknessMm(slab, solids) is { } thickness && KataSupportRules.IsLeanConcrete(thickness)) return;

            var under = Intervals(solids, _foundationProbes).Where(i => i.Overlaps(_run.Extent)).ToList();
            if (under.Count == 0) return;

            // A wall footing, or any foundation running on under the beam, is a strip or raft: never a point support.
            if (foundation is WallFoundation || under.Any(KataSupportRules.IsStripOrRaft))
            {
                _stripFoundations++;
                return;
            }

            Add(KataSupportKind.Foundation, foundation, under);
        }

        public void CrossingBeam(FamilyInstance crossing)
        {
            if (crossing.Location is not LocationCurve { Curve: Line line }
                || _run.Frame.PlanCos(line.Direction) > Math.Cos(Math.PI / 4)) return;

            // Uncut geometry: a crossing beam cut back to a column face still overlaps that column here.
            var solids = KataSolidReader.GetOriginalSolids(crossing);
            var range = KataSolidReader.VerticalRange(solids);
            double? width = KataRunReader.TypeLength(crossing, KataRunReader.WidthParameter);
            double height = KataRunReader.TypeLength(crossing, KataRunReader.HeightParameter)
                            ?? (range is { } r ? RevitUnits.FtToMm(r.TopFt - r.BottomFt) : 0.0);

            var crossed = Intervals(solids, _probes).Where(e => e.Overlaps(_run.Extent)).ToList();
            foreach (var extent in crossed)
            {
                // Across the run, a crossing beam's width is what the probe measured along the axis.
                _beams.Add((extent, range?.BottomFt ?? double.MaxValue, crossing.UniqueId, KataFormat.Section(width ?? extent.Length, height)));
            }

            if (crossed.Count == 0 && OneSidedStation(line) is { } station)
            {
                double half = (width ?? DefaultCrossingWidthMm) / 2.0;
                _oneSided.Add((new Interval1D(station - half, station + half), crossing.UniqueId, KataFormat.Section(width ?? DefaultCrossingWidthMm, height)));
            }
        }

        /// <summary>
        /// Where a crossing beam that stops short of the run's centre line would meet it, when its near end is within
        /// <see cref="OneSidedReachMm"/> of the line (a beam framing into a column of the run from one side).
        /// </summary>
        private double? OneSidedStation(Line line)
        {
            double o0 = _run.Frame.Offset(line.GetEndPoint(0)) - _run.CenterOffsetMm;
            double o1 = _run.Frame.Offset(line.GetEndPoint(1)) - _run.CenterOffsetMm;
            if (Math.Min(Math.Abs(o0), Math.Abs(o1)) > OneSidedReachMm || Math.Abs(o1 - o0) < 1.0) return null;

            double t = -o0 / (o1 - o0);
            var meet = line.GetEndPoint(0) + (line.GetEndPoint(1) - line.GetEndPoint(0)) * t;
            double station = _run.Frame.Station(meet);
            return _run.Extent.Contains(station, 1.0) ? station : null;
        }

        public (IReadOnlyList<KataSupport>, IReadOnlyList<string>) Finish()
        {
            var hard = _supports.ToList();
            foreach (var beam in _beams)
            {
                bool atSupport = hard.Any(s => s.Extent.Overlaps(beam.Extent));
                bool carries = beam.SoffitFt <= NearestPiece(beam.Extent.Mid).BottomFt + RevitUnits.MmToFt(CarryingSoffitToleranceMm);
                if (atSupport || carries) _supports.Add(new KataSupport(KataSupportKind.Beam, beam.Extent, beam.Key, beam.Section));
                else _carriedBeams++;
            }

            // A beam framing in from one side does not reach the run: it only counts where it meets a column.
            foreach (var beam in _oneSided.Where(b => hard.Any(s => s.Extent.Contains(b.Extent.Mid, 1.0))))
                _supports.Add(new KataSupport(KataSupportKind.Beam, beam.Extent, beam.Key, beam.Section));

            var warnings = new List<string>();
            if (_broken > 0) warnings.Add($"{_broken} solid(s) near the run could not be evaluated by Revit and were ignored.");
            if (_parallelWalls > 0) warnings.Add($"{_parallelWalls} wall(s) running along the beam were not taken as supports.");
            var pointSupports = _supports.Where(s => s.Kind != KataSupportKind.Beam).Select(s => s.Extent).ToList();
            int standing = KataSupportRules.CountStandingOffSupports(_standing, pointSupports);
            if (standing > 0) warnings.Add($"{standing} column(s)/wall(s) stand on the beam between supports (transfer); they are not supports.");
            int shared = KataSupportRules.CountSupportsWithSeveralColumnsAbove(pointSupports, _uppers);
            if (shared > 0) warnings.Add($"{shared} support(s) carry more than one column above; row 19 shows the widest overlap only.");
            if (_stripFoundations > 0) warnings.Add($"{_stripFoundations} strip/raft foundation(s) under the beam were not taken as supports.");
            if (_carriedBeams > 0) warnings.Add($"{_carriedBeams} crossing beam(s) shallower than the run bear on it and were not taken as supports.");

            var supports = _supports
                .Select(s => s.Kind == KataSupportKind.Beam ? s : s with { Upper = UpperOver(s.Extent) })
                .ToList();
            return (supports, warnings);
        }

        /// <summary>
        /// Records a column or wall resting at or above the soffit, but only when its plan box sits on the beam —
        /// across the run's centre line and within its length. The search box is wider than the beam, so columns
        /// beside the run or past its end are found too, and those stand on something else.
        /// </summary>
        private void AddStanding(Element element)
        {
            var box = element.get_BoundingBox(null);
            if (box is null) return;

            var corners = new[] { box.Min, box.Max, new XYZ(box.Min.X, box.Max.Y, box.Min.Z), new XYZ(box.Max.X, box.Min.Y, box.Min.Z) };
            var stations = new Interval1D(corners.Min(_run.Frame.Station), corners.Max(_run.Frame.Station));
            var offsets = new Interval1D(corners.Min(_run.Frame.Offset), corners.Max(_run.Frame.Offset));
            if (stations.Overlaps(_run.Extent, 0.0) && offsets.Contains(_run.CenterOffsetMm, 0.0)) _standing.Add(stations);
        }

        /// <summary>
        /// Thickness of a foundation slab from its type (the thickness Revit reports for sloped or shape-edited slabs
        /// too), then the instance and compound structure, and only then the height of its solid.
        /// </summary>
        private static double? SlabThicknessMm(Floor slab, IReadOnlyList<Solid> solids)
        {
            var type = slab.Document.GetElement(slab.GetTypeId()) as FloorType;
            foreach (var parameter in new[]
                     {
                         type?.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM),
                         slab.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM)
                     })
            {
                if (parameter is { HasValue: true, StorageType: StorageType.Double } && parameter.AsDouble() > 0)
                    return RevitUnits.FtToMm(parameter.AsDouble());
            }

            double? width = type?.GetCompoundStructure()?.GetWidth();
            if (width is > 0) return RevitUnits.FtToMm(width.Value);

            return KataSolidReader.VerticalRange(solids) is { } range ? RevitUnits.FtToMm(range.TopFt - range.BottomFt) : null;
        }

        private Line Probe(KataBeamGeometry piece, double z, double reachMm = ProbeReachMm) =>
            _run.Frame.Probe(piece.Stations.Start - reachMm, piece.Stations.End + reachMm, _run.CenterOffsetMm, z);

        private IReadOnlyList<Interval1D> Intervals(IReadOnlyList<Solid> solids, IReadOnlyList<Line> probes)
        {
            var (intervals, skipped) = KataSolidReader.ProbeIntervals(solids, probes, _run.Frame);
            _broken += skipped;
            return intervals;
        }

        private void Add(KataSupportKind kind, Element element, IEnumerable<Interval1D> intervals)
        {
            foreach (var extent in intervals.Where(e => e.Overlaps(_run.Extent)))
                _supports.Add(new KataSupport(kind, extent, element.UniqueId));
        }

        /// <summary>A column or wall whose bottom is at or above the soffit sits on the beam (transfer or upturned beam).</summary>
        private bool StandsOnRun(Element element)
        {
            var box = element.get_BoundingBox(null);
            return box is not null && box.Min.Z >= _run.Pieces.Min(p => p.BottomFt) - RevitUnits.MmToFt(StandingToleranceMm);
        }

        private Interval1D? UpperOver(Interval1D support) =>
            _uppers
                .Where(u => u.Overlaps(support, 0.0))
                .OrderByDescending(u => Math.Min(u.End, support.End) - Math.Max(u.Start, support.Start))
                .Select(u => (Interval1D?)u)
                .FirstOrDefault();

        private KataBeamGeometry NearestPiece(double station) =>
            _run.Pieces
                .OrderBy(p => p.Stations.Contains(station, 0.0) ? 0.0 : Math.Min(Math.Abs(p.Stations.Start - station), Math.Abs(p.Stations.End - station)))
                .First();
    }
}
