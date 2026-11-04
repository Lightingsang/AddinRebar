using System;
using System.Collections.Generic;
using System.Linq;

namespace HPRebar.Core.KataRebar.ColumnStack;

/// <summary>Rules the plan uses beyond the sheet (user decisions of 2026-11-03 where Kata draws nothing).</summary>
/// <param name="TopContinues">A column goes on above the top storey: its bars lap into it like Kata's stub row.</param>
/// <param name="CrankLimitMm">Largest face step a bar may crank across (1:6 from the beam soffit); beyond it the bar is cut and anchored.</param>
/// <param name="CrankSlope">Rise per unit of horizontal crank; the Kata settings' crank slope when the caller has it.</param>
public sealed record KataColumnBarRules(
    bool TopContinues = false,
    double CrankLimitMm = 75,
    double LapFactor = 30,
    double StaggerExtraFactor = 10,
    double CrankSlope = 6);

/// <summary>
/// The longitudinal bars of a column stack, measured against Kata's VC drawings (2026-11-03): group A laps start at
/// the storey base + J, group B 40 diameters higher (lap 30 d + 10 d, d = I's second Ø); each bar runs from its lap in
/// one storey to the lap end in the storey above, inset one bar Ø beside the bar below over the lap and cranked back
/// over 6 d; starters stand on the pile-cap bottom + lift with an outward foot of F5 × d. Lap insets, feet and legs of corner
/// bars run along b (Kata's elevation direction), never along the diagonal: Revit re-fits a bent bar in a diagonal
/// plane and lengthens its leg, and a bar bent both ways becomes another shape — either way it would no longer share
/// a number with the face bars. User rules where Kata differs
/// or draws nothing: every lap is at least 30 × the bar's own Ø; a face step up to the crank limit is cranked from the
/// beam soffit, a larger one cuts the bars under the slab with an inward F5 × d leg; a bar with no bar below it (a
/// storey larger than the one below, or one cut below) is dowelled one lap into the storey below, or anchored in the
/// floor beam with an F5 × d foot where the storey below has no concrete; the top storey anchors under the roof.
/// </summary>
public static class KataColumnBarPlanner
{
    /// <summary>Faces read from Revit may differ by a few mm; smaller differences are not steps.</summary>
    public const double FaceToleranceMm = 2;

    /// <summary>An upper bar laps with a lower bar that ends this close to it (centre to centre).</summary>
    public const double PartnerReachMm = 60;

    /// <summary>Segments shorter than this are dropped (Revit refuses curves under its short-curve tolerance).</summary>
    private const double ShortestSegmentMm = 2;

    public static KataColumnBarPlan Plan(KataColumnSpec spec, IReadOnlyList<KataColumnStoreyGeometry> storeys, KataColumnBarRules rules)
    {
        var errors = Validate(spec, storeys, rules);
        if (errors.Count > 0)
        {
            return new KataColumnBarPlan(Array.Empty<KataColumnBarRun>(), Array.Empty<string>(), errors);
        }

        var context = new PlanContext(spec, storeys, rules);
        var bars = new List<KataColumnBarRun>();
        bars.AddRange(context.Starters());
        for (int k = 0; k < storeys.Count; k++)
        {
            bars.AddRange(context.MainBars(k));
        }

        var runs = bars.Select(Clean).ToList();
        context.Errors.AddRange(runs.Where(r => !RisesSteadily(r))
            .Select(r => $"J{spec.Rows[r.Storey].Row}: {spec.Rows[r.Storey].Name} has no room for its laps (a bar would turn downwards); lower J."));
        return context.Errors.Count > 0
            ? new KataColumnBarPlan(Array.Empty<KataColumnBarRun>(), context.Notes, context.Errors.Distinct().ToList())
            : new KataColumnBarPlan(runs, context.Notes, Array.Empty<string>());
    }

    private static List<string> Validate(KataColumnSpec spec, IReadOnlyList<KataColumnStoreyGeometry> storeys, KataColumnBarRules rules)
    {
        var errors = new List<string>();
        if (storeys.Count == 0 || spec.Rows.Count != storeys.Count + 1)
        {
            errors.Add($"Sheet Cot has {spec.Rows.Count} rows for {storeys.Count} storeys; it needs one row per storey plus the row above the top one.");
            return errors;
        }

        if (spec.TightInnerLayer)
        {
            errors.Add("G3 \"thép lớp 2 ghép sát\" is not supported yet; untick it.");
        }

        if (spec.DenseAtLaps)
        {
            errors.Add("H3 \"đai dày tại vị trí nối\" is not supported yet; untick it.");
        }

        int rowsNeedingJ = rules.TopContinues ? spec.Rows.Count : storeys.Count;
        errors.AddRange(spec.Rows.Take(rowsNeedingJ).Where(r => r.LapStartMm is null)
            .Select(r => $"J{r.Row}: no lap position ({r.Name}); type where the laps start above the storey base."));
        return errors;
    }

    /// <summary>Consecutive points closer than the shortest segment become one.</summary>
    private static KataColumnBarRun Clean(KataColumnBarRun run)
    {
        var points = new List<KataPoint3> { run.Points[0] };
        foreach (var p in run.Points.Skip(1))
        {
            if (Distance(points[points.Count - 1], p) >= ShortestSegmentMm)
            {
                points.Add(p);
            }
        }

        return run with { Points = points };
    }

    private static bool RisesSteadily(KataColumnBarRun run) =>
        run.Points.Count >= 2 && run.Points.Zip(run.Points.Skip(1), (a, b) => b.Z >= a.Z - 0.01).All(up => up);

    private static double Distance(KataPoint3 a, KataPoint3 b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z));

    /// <summary>Everything one plan run shares: sections in model coordinates, lap levels per storey and group.</summary>
    private sealed class PlanContext
    {
        private readonly KataColumnSpec _spec;
        private readonly IReadOnlyList<KataColumnStoreyGeometry> _storeys;
        private readonly KataColumnBarRules _rules;
        private readonly List<PlacedBar>[] _bars;
        private readonly bool[] _cutAbove;

        public PlanContext(KataColumnSpec spec, IReadOnlyList<KataColumnStoreyGeometry> storeys, KataColumnBarRules rules)
        {
            _spec = spec;
            _storeys = storeys;
            _rules = rules;
            _bars = new List<PlacedBar>[storeys.Count];
            _cutAbove = new bool[storeys.Count];
            for (int k = 0; k < storeys.Count; k++)
            {
                var section = KataColumnSectionPlanner.PlanOnFaces(spec.Rows[k], storeys[k], spec);
                Notes.AddRange(section.Notes.Select(n => $"{spec.Rows[k].Name}: {n}"));
                _bars[k] = Place(section, storeys[k], spec.Rows[k].IsRound);
                if (k > 0)
                {
                    _cutAbove[k - 1] = NeedsCut(k - 1);
                    Pair(k);
                }
            }
        }

        public List<string> Notes { get; } = new();

        public List<string> Errors { get; } = new();

        public IEnumerable<KataColumnBarRun> Starters()
        {
            var first = _storeys[0];
            bool onCap = _spec.PileCapHeightMm > 0;
            if (!onCap)
            {
                Notes.Add("No pile cap (B9 = 0): starters run one lap below the first storey, without a foot.");
            }

            double outerD = _spec.Rows[0].OuterDiameterMm;
            foreach (var bar in _bars[0])
            {
                double top = LapStart(0, bar.Group) + Lap(0, bar.Group, bar.DiameterMm);
                if (!onCap)
                {
                    yield return Run(KataColumnBarKind.Starter, 0, bar, At(bar, 0, first.BaseMm - Lap(0, bar.Group, bar.DiameterMm)), At(bar, 0, top));
                    continue;
                }

                // The cap bottom comes from the Revit storey and the cap height (B9), never from C5's absolute level;
                // inner-layer feet sit one outer Ø higher so they do not run through the outer feet.
                double bottom = first.BaseMm - _spec.PileCapHeightMm + _spec.StarterLiftMm + (bar.Layer == 2 ? outerD : 0);
                double foot = _spec.BendFactor * bar.DiameterMm;
                yield return Run(KataColumnBarKind.Starter, 0, bar, AtSide(bar, foot, bottom), At(bar, 0, bottom), At(bar, 0, top));
            }
        }

        public IEnumerable<KataColumnBarRun> MainBars(int k)
        {
            if (k < _storeys.Count - 1 && _cutAbove[k])
            {
                Notes.Add($"{_spec.Rows[k].Name} → {_spec.Rows[k + 1].Name}: face step over {_rules.CrankLimitMm} mm; bars cut under the slab and anchored, the bars above are dowelled.");
            }

            int dowels = 0;
            var runs = new List<KataColumnBarRun>();
            foreach (var bar in _bars[k])
            {
                var points = new List<KataPoint3>();
                (bar.SideX, bar.SideY) = SideOf(k, bar);
                dowels += AddBottom(points, k, bar) ? 1 : 0;
                var kind = AddTop(points, k, bar);
                runs.Add(new KataColumnBarRun(kind, k, bar.Group, bar.Layer, bar.DiameterMm, points));
            }

            if (dowels > 0 && !_cutAbove[k - 1 < 0 ? 0 : k - 1])
            {
                Notes.Add($"{_spec.Rows[k].Name}: {dowels} bar(s) have no bar below to lap with; they are dowelled into the storey below.");
            }

            return runs;
        }

        /// <returns>True when the bar is dowelled instead of lapped.</returns>
        private bool AddBottom(List<KataPoint3> points, int k, PlacedBar bar)
        {
            if (k > 0 && (bar.Partner is null || _cutAbove[k - 1]))
            {
                AddDowel(points, k, bar);
                return true;
            }

            double start = LapStart(k, bar.Group);
            double lapTop = start + Lap(k, bar.Group, bar.DiameterMm);
            points.Add(AtSide(bar, -bar.DiameterMm, start));
            points.Add(AtSide(bar, -bar.DiameterMm, lapTop));
            points.Add(At(bar, 0, lapTop + _rules.CrankSlope * bar.DiameterMm));
            return false;
        }

        /// <summary>
        /// A bar with nothing below to lap with: one lap down into the storey below where that storey has concrete under
        /// it, else an inward F5 × d foot in the floor beam.
        /// </summary>
        private void AddDowel(List<KataPoint3> points, int k, PlacedBar bar)
        {
            double lap = _rules.LapFactor * bar.DiameterMm;
            if (InsideBarZone(bar.X, bar.Y, k - 1, bar.DiameterMm))
            {
                points.Add(At(bar, 0, _storeys[k].BaseMm - lap));
                return;
            }

            double beam = _spec.Rows[k - 1].BeamDepthMm;
            double footZ = _storeys[k].BaseMm - Math.Max(beam, lap / 2) + Inset(bar.DiameterMm);
            double foot = _spec.BendFactor * bar.DiameterMm;
            points.Add(AtSide(bar, -foot, footZ));
            points.Add(At(bar, 0, footZ));
        }

        private KataColumnBarKind AddTop(List<KataPoint3> points, int k, PlacedBar bar)
        {
            var storey = _storeys[k];
            bool last = k == _storeys.Count - 1;
            if ((last && !_rules.TopContinues) || (!last && _cutAbove[k]))
            {
                // Inner-layer ends one outer Ø lower so their legs do not run through the outer bars' ends.
                double drop = bar.Layer == 2 ? _spec.Rows[k].OuterDiameterMm : 0;
                double end = storey.TopMm - Inset(bar.DiameterMm) - drop;
                double leg = _spec.BendFactor * bar.DiameterMm;
                points.Add(At(bar, 0, end));
                points.Add(AtSide(bar, -leg, end));
                return last ? KataColumnBarKind.Top : KataColumnBarKind.Main;
            }

            double lapEnd = LapStart(k + 1, bar.Group) + Lap(k + 1, bar.Group, bar.DiameterMm);
            var (dx, dy) = last ? (0.0, 0.0) : ShiftInto(bar, k + 1);
            if (dx == 0 && dy == 0)
            {
                points.Add(At(bar, 0, lapEnd));
                return KataColumnBarKind.Main;
            }

            double soffit = storey.TopMm - _spec.Rows[k].BeamDepthMm;
            double from = Math.Max(soffit, points[points.Count - 1].Z);
            double rise = _rules.CrankSlope * Math.Sqrt(dx * dx + dy * dy);
            points.Add(new KataPoint3(bar.X, bar.Y, from));
            points.Add(new KataPoint3(bar.X + dx, bar.Y + dy, from + rise));
            points.Add(new KataPoint3(bar.X + dx, bar.Y + dy, lapEnd));
            return KataColumnBarKind.Main;
        }

        /// <summary>Bars of storey k need cutting when any would move more than the crank limit to stay in storey k + 1.</summary>
        private bool NeedsCut(int k) =>
            _bars[k].Select(b => ShiftInto(b, k + 1)).Any(s => Math.Sqrt(s.Dx * s.Dx + s.Dy * s.Dy) > _rules.CrankLimitMm + FaceToleranceMm);

        /// <summary>
        /// Each bar of storey k laps with the nearest bar of storey k − 1 of its layer whose top lands within reach, and
        /// takes that bar's group, so laps of one group always pair up; bars with no partner alternate A/B.
        /// </summary>
        private void Pair(int k)
        {
            var unpaired = new List<PlacedBar>();
            foreach (var bar in _bars[k])
            {
                var partner = _cutAbove[k - 1] ? null : _bars[k - 1]
                    .Where(b => b.Layer == bar.Layer)
                    .Select(b => (Bar: b, Shift: ShiftInto(b, k)))
                    .Select(t => (t.Bar, Gap: Math.Sqrt(Math.Pow(t.Bar.X + t.Shift.Dx - bar.X, 2) + Math.Pow(t.Bar.Y + t.Shift.Dy - bar.Y, 2))))
                    .Where(t => t.Gap <= PartnerReachMm)
                    .OrderBy(t => t.Gap)
                    .Select(t => t.Bar)
                    .FirstOrDefault();
                if (partner is null)
                {
                    unpaired.Add(bar);
                    continue;
                }

                bar.Partner = partner;
                bar.Group = partner.Group;
            }

            for (int i = 0; i < unpaired.Count; i++)
            {
                unpaired[i].Group = i % 2 == 0 ? KataColumnBarGroup.A : KataColumnBarGroup.B;
            }
        }

        /// <summary>The move that brings a bar of the storey below inside the bar zone of storey <paramref name="upper"/>.</summary>
        private (double Dx, double Dy) ShiftInto(PlacedBar bar, int upper)
        {
            var g = _storeys[upper];
            double inset = Inset(bar.DiameterMm);
            if (_spec.Rows[upper].IsRound)
            {
                double cx = (g.MinXMm + g.MaxXMm) / 2, cy = (g.MinYMm + g.MaxYMm) / 2;
                double radius = (g.MaxXMm - g.MinXMm) / 2 - inset;
                double ox = bar.X - cx, oy = bar.Y - cy, distance = Math.Sqrt(ox * ox + oy * oy);
                return distance <= radius + FaceToleranceMm
                    ? (0, 0)
                    : (ox * radius / distance - ox, oy * radius / distance - oy);
            }

            double dx = Clamp(bar.X, g.MinXMm + inset, g.MaxXMm - inset) - bar.X;
            double dy = Clamp(bar.Y, g.MinYMm + inset, g.MaxYMm - inset) - bar.Y;
            return (Math.Abs(dx) <= FaceToleranceMm ? 0 : dx, Math.Abs(dy) <= FaceToleranceMm ? 0 : dy);
        }

        private bool InsideBarZone(double x, double y, int storey, double diameter)
        {
            var g = _storeys[storey];
            double inset = Inset(diameter) - FaceToleranceMm;
            return x >= g.MinXMm + inset && x <= g.MaxXMm - inset && y >= g.MinYMm + inset && y <= g.MaxYMm - inset;
        }

        private double Inset(double diameter) => _spec.CoverMm + _spec.Hoops.DiameterMm + diameter / 2;

        private double LapStart(int row, KataColumnBarGroup group)
        {
            var r = _spec.Rows[row];
            double baseMm = row < _storeys.Count ? _storeys[row].BaseMm : _storeys[_storeys.Count - 1].TopMm;
            double start = baseMm + (r.LapStartMm ?? 0);
            return group == KataColumnBarGroup.A ? start : start + (_rules.LapFactor + _rules.StaggerExtraFactor) * r.InnerDiameterMm;
        }

        /// <summary>
        /// Kata laps group A with the row's first Ø and group B with its second ("25/20"); the plan never laps a bar
        /// shorter than 30 × its own Ø (user decision).
        /// </summary>
        private double Lap(int row, KataColumnBarGroup group, double barDiameter)
        {
            var r = _spec.Rows[row];
            double kata = _rules.LapFactor * (group == KataColumnBarGroup.A ? r.OuterDiameterMm : r.InnerDiameterMm);
            return Math.Max(kata, _rules.LapFactor * barDiameter);
        }

        private static KataPoint3 At(PlacedBar bar, double outward, double z) =>
            new(bar.X + bar.OutX * outward, bar.Y + bar.OutY * outward, z);

        /// <summary>A lap inset, foot or leg, along the bar's side direction (<see cref="SideOf" />).</summary>
        private static KataPoint3 AtSide(PlacedBar bar, double outward, double z) =>
            new(bar.X + bar.SideX * outward, bar.Y + bar.SideY * outward, z);

        /// <summary>
        /// The plan direction of a bar's lap inset, feet and legs in storey k, chosen so the whole bar lies in one vertical
        /// plane: a face bar's outward normal; a corner bar's crank into the storey above when it has one (a step of
        /// both faces is a diagonal crank), else along b — never a diagonal it does not need (see the class).
        /// </summary>
        private (double X, double Y) SideOf(int k, PlacedBar bar)
        {
            if (bar.OutX == 0 || bar.OutY == 0)
            {
                return (bar.OutX, bar.OutY);
            }

            var (dx, dy) = k < _storeys.Count - 1 && !_cutAbove[k] ? ShiftInto(bar, k + 1) : (0.0, 0.0);
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length == 0)
            {
                return (Math.Sign(bar.OutX), 0);
            }

            var (ux, uy) = (dx / length, dy / length);
            return ux * bar.OutX + uy * bar.OutY < 0 ? (-ux, -uy) : (ux, uy);
        }

        private static KataColumnBarRun Run(KataColumnBarKind kind, int storey, PlacedBar bar, params KataPoint3[] points) =>
            new(kind, storey, bar.Group, bar.Layer, bar.DiameterMm, points);

        private static double Clamp(double value, double min, double max) => Math.Min(Math.Max(value, min), max);

        /// <summary>Bars in model coordinates; storey 0 alternates A/B around the section within each layer.</summary>
        private static List<PlacedBar> Place(KataColumnSection section, KataColumnStoreyGeometry g, bool round)
        {
            double cx = (g.MinXMm + g.MaxXMm) / 2, cy = (g.MinYMm + g.MaxYMm) / 2;
            double halfB = (g.MaxXMm - g.MinXMm) / 2, halfH = (g.MaxYMm - g.MinYMm) / 2;
            var placed = new List<PlacedBar>();
            foreach (var layer in section.Bars.GroupBy(b => b.Layer))
            {
                var ordered = layer.OrderBy(b => Math.Atan2(b.Y - halfH, b.X - halfB)).ToList();
                for (int i = 0; i < ordered.Count; i++)
                {
                    var bar = ordered[i];
                    double x = g.MinXMm + bar.X, y = g.MinYMm + bar.Y;
                    var (outX, outY) = round ? Radial(x - cx, y - cy) : Outward(x - cx, y - cy, halfB, halfH);
                    placed.Add(new PlacedBar(x, y, bar.DiameterMm, bar.Layer, outX, outY)
                    {
                        Group = i % 2 == 0 ? KataColumnBarGroup.A : KataColumnBarGroup.B,
                    });
                }
            }

            return placed;
        }

        private static (double X, double Y) Radial(double dx, double dy)
        {
            double length = Math.Sqrt(dx * dx + dy * dy);
            return length == 0 ? (0, 1) : (dx / length, dy / length);
        }

        /// <summary>Unit vector from the centre towards the nearest face (diagonal at a corner).</summary>
        private static (double X, double Y) Outward(double dx, double dy, double halfB, double halfH)
        {
            double toX = halfB - Math.Abs(dx), toY = halfH - Math.Abs(dy);
            double ux = toX <= toY + FaceToleranceMm ? Math.Sign(dx) : 0;
            double uy = toY <= toX + FaceToleranceMm ? Math.Sign(dy) : 0;
            double length = Math.Sqrt(ux * ux + uy * uy);
            return length == 0 ? (0, 1) : (ux / length, uy / length);
        }
    }

    /// <summary>A bar of a storey section in model coordinates; its group and lap partner are set while pairing.</summary>
    private sealed class PlacedBar
    {
        public PlacedBar(double x, double y, double diameterMm, int layer, double outX, double outY)
        {
            X = x;
            Y = y;
            DiameterMm = diameterMm;
            Layer = layer;
            OutX = outX;
            OutY = outY;
            // A starter's foot: along b at a corner, outward on a face; main bars set theirs per storey.
            (SideX, SideY) = outX != 0 && outY != 0 ? (Math.Sign(outX), 0.0) : (outX, outY);
        }

        public double X { get; }
        public double Y { get; }
        public double DiameterMm { get; }
        public int Layer { get; }
        public double OutX { get; }
        public double OutY { get; }

        /// <summary>Plan direction of the bar's lap inset, feet and legs.</summary>
        public double SideX { get; set; }

        public double SideY { get; set; }
        public KataColumnBarGroup Group { get; set; }
        public PlacedBar? Partner { get; set; }
    }
}
