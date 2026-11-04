using System;
using System.Linq;
using HPRebar.Core.KataRebar.ColumnStack;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar.ColumnStack;

/// <summary>
/// The longitudinal chain against Kata's VC drawing of the sample with J = 500 on every row (variant j-all500): in that
/// drawing 0 is the first storey base (−4.000), here model elevations are used, so every number is shifted by −4000.
/// </summary>
public sealed class KataColumnBarPlannerTests
{
    private const double Origin = -4000;

    [Fact]
    public void Plan_J500_GroupAChainMatchesKatasDrawing()
    {
        var plan = PlanSample(new KataColumnBarRules(TopContinues: true));

        Assert.Empty(plan.Errors);
        // Drawing, group A (left face): starter −1400→1250, then 500→4250, 3500→7750, 7000→11500, 10900→16100, 15500→19400.
        // One difference by user decision: the FLOOR 2 Ø25 bar laps 30 × 25 into FLOOR 3 (Kata: 30 × Ø20) → 11650.
        Assert.Equal(
            new[] { (-1400.0, 1250.0), (500, 4250), (3500, 7750), (7000, 11650), (10900, 16100), (15500, 19400) },
            Extents(plan, KataColumnBarGroup.A));
    }

    [Fact]
    public void Plan_J500_GroupBStaggersFortyDiametersHigher()
    {
        var plan = PlanSample(new KataColumnBarRules(TopContinues: true));

        // Drawing, group B (right face): starter −1400→2250, then 1500→5250, 4500→8400, 7800→12300, 11700→16900, 16300→20200.
        // Kata laps group B of "25/20" with Ø20 (24 d on a Ø25 bar); the plan keeps 30 × 25 (user decision): 8550, 12450.
        Assert.Equal(
            new[] { (-1400.0, 2250.0), (1500, 5250), (4500, 8550), (7800, 12450), (11700, 16900), (16300, 20200) },
            Extents(plan, KataColumnBarGroup.B));
    }

    [Fact]
    public void Plan_J500_StartersStandOnTheCapWithAFifteenDiameterFoot()
    {
        var plan = PlanSample(new KataColumnBarRules(TopContinues: true));

        var starter = plan.Bars.First(b => b.Kind == KataColumnBarKind.Starter && b.DiameterMm == 25);
        var foot = starter.Points[0];
        var corner = starter.Points[1];
        Assert.Equal(-5400, corner.Z);
        Assert.Equal(375, Math.Round(Math.Sqrt(Math.Pow(foot.X - corner.X, 2) + Math.Pow(foot.Y - corner.Y, 2))));
        Assert.Equal(40, plan.Bars.Count(b => b.Kind == KataColumnBarKind.Starter));
    }

    [Fact]
    public void Plan_LapInsetOneDiameterThenCrankOverSixDiameters()
    {
        var plan = PlanSample(new KataColumnBarRules(TopContinues: true));

        var bar = plan.Bars.First(b => b.Kind == KataColumnBarKind.Main && b.Storey == 1 && b.DiameterMm == 25);
        double inset = Math.Sqrt(Math.Pow(bar.Points[0].X - bar.Points[2].X, 2) + Math.Pow(bar.Points[0].Y - bar.Points[2].Y, 2));
        Assert.Equal(25, Math.Round(inset));
        Assert.Equal(150, bar.Points[2].Z - bar.Points[1].Z);
    }

    [Fact]
    public void Plan_RoofTop_AnchorsUnderTheSlabWithAFifteenDiameterLeg()
    {
        var plan = PlanSample(new KataColumnBarRules(TopContinues: false));

        var top = plan.Bars.Where(b => b.Kind == KataColumnBarKind.Top).ToList();
        Assert.Equal(40, top.Count);
        // FLOOR 4 top = 14300 − cover 30 − hoop 10 − Ø20/2; layer 2 one outer Ø lower so its legs pass under layer 1's.
        Assert.All(top, b => Assert.Equal(b.Layer == 1 ? 14250 : 14230, b.Points[^1].Z));
        Assert.All(top, b => Assert.Equal(300, Math.Round(Math.Sqrt(Math.Pow(b.Points[^1].X - b.Points[^2].X, 2) + Math.Pow(b.Points[^1].Y - b.Points[^2].Y, 2)))));
    }

    [Fact]
    public void Plan_CornerFeetAndLegs_RunAlongB_NeverDiagonal()
    {
        var plan = PlanSample(new KataColumnBarRules(TopContinues: false));

        // Revit lengthens a bent bar's leg in a diagonal plane, so corner bars would not share a number with face bars.
        var bent = plan.Bars.Where(b => b.Kind is KataColumnBarKind.Top or KataColumnBarKind.Starter).ToList();
        Assert.NotEmpty(bent);
        Assert.All(bent, b =>
        {
            var (a, z) = b.Kind == KataColumnBarKind.Top ? (b.Points[^2], b.Points[^1]) : (b.Points[1], b.Points[0]);
            bool alongB = Math.Abs(z.Y - a.Y) < 1e-6, alongH = Math.Abs(z.X - a.X) < 1e-6;
            Assert.True(alongB || alongH, $"{b.Kind} storey {b.Storey}: leg ({a.X},{a.Y}) → ({z.X},{z.Y}) is diagonal");
        });
        AssertEveryBarInOneVerticalPlane(plan);
        // The corner bars' legs run along b: some leg along b starts at a bar sitting on an h face too.
        Assert.Contains(bent, b => b.Kind == KataColumnBarKind.Top && Math.Abs(b.Points[^1].Y - b.Points[^2].Y) < 1e-6 && Math.Abs(b.Points[^1].X - b.Points[^2].X) > 1);
    }

    [Theory]
    [InlineData(25, 25)]   // both faces of FLOOR 2 shrink: a corner bar below cranks diagonally
    [InlineData(50, 0)]    // b faces only
    [InlineData(0, 50)]    // h faces only
    [InlineData(200, 0)]   // cut under the slab, legs and dowels
    public void Plan_Steps_EveryBarLiesInOneVerticalPlane(double shrinkB, double shrinkH)
    {
        var spec = KataColumnSpecReader.Read(KataColumnTestSheet.Sample(j: 500)).Spec!;
        var storeys = KataColumnTestSheet.Storeys().ToList();
        var s = storeys[2];
        storeys[2] = s with { MinXMm = s.MinXMm + shrinkB, MaxXMm = s.MaxXMm - shrinkB, MinYMm = s.MinYMm + shrinkH, MaxYMm = s.MaxYMm - shrinkH };

        var plan = KataColumnBarPlanner.Plan(spec, storeys, new KataColumnBarRules());

        Assert.Empty(plan.Errors);
        AssertEveryBarInOneVerticalPlane(plan);
    }

    /// <summary>Revit builds a bar bent in two plan directions as a free-form bar of another shape, numbered apart.</summary>
    private static void AssertEveryBarInOneVerticalPlane(KataColumnBarPlan plan) =>
        Assert.All(plan.Bars, b =>
        {
            var offsets = b.Points.Skip(1).Select(p => (X: p.X - b.Points[0].X, Y: p.Y - b.Points[0].Y)).Where(o => Math.Abs(o.X) + Math.Abs(o.Y) > 1e-6).ToList();
            Assert.True(offsets.All(o => Math.Abs(o.X * offsets[0].Y - o.Y * offsets[0].X) < 1e-3 * Math.Max(1, Math.Abs(o.X) + Math.Abs(o.Y))),
                $"{b.Kind} storey {b.Storey} at ({b.Points[0].X},{b.Points[0].Y}) bends in two plan directions");
        });

    [Fact]
    public void Plan_EmptyJ_RefusedWithTheCells()
    {
        var spec = KataColumnSpecReader.Read(KataColumnTestSheet.Sample()).Spec!;

        var plan = KataColumnBarPlanner.Plan(spec, KataColumnTestSheet.Storeys(), new KataColumnBarRules());

        Assert.Empty(plan.Bars);
        Assert.Equal(new[] { "J10", "J11", "J12", "J13", "J14" }, plan.Errors.Select(e => e.Substring(0, 3)));
    }

    [Fact]
    public void Plan_SmallStep_CranksFromTheSoffit_LargeStep_CutsAndAnchors()
    {
        var spec = KataColumnSpecReader.Read(KataColumnTestSheet.Sample(j: 500)).Spec!;
        var storeys = KataColumnTestSheet.Storeys().ToList();
        storeys[2] = storeys[2] with { MaxXMm = 950 };      // FLOOR 2 loses 50 mm on its right face → crank
        storeys[4] = storeys[4] with { MaxXMm = 800 };      // FLOOR 4 loses 200 mm → cut

        var plan = KataColumnBarPlanner.Plan(spec, storeys, new KataColumnBarRules());

        var cranked = plan.Bars.First(b => b.Storey == 1 && b.Points.Max(p => p.X) > 940 && b.Points.Count == 6);
        Assert.Equal(-50, Math.Round(cranked.Points[^1].X - cranked.Points[2].X));
        Assert.Contains(plan.Notes, n => n.StartsWith("FLOOR 3 → FLOOR 4", StringComparison.Ordinal));
        Assert.All(plan.Bars.Where(b => b.Storey == 4), b => Assert.Equal(storeys[4].BaseMm - 30 * b.DiameterMm, b.Points[0].Z));
    }

    [Theory]
    [InlineData(500, 0)]
    [InlineData(1550, 0)]
    [InlineData(500, 50)]
    [InlineData(1550, 50)]
    [InlineData(500, 200)]
    public void Plan_AnyJAndStep_EveryBarRisesWithoutShortSegments(double j, double step)
    {
        var spec = KataColumnSpecReader.Read(KataColumnTestSheet.Sample(j: j)).Spec!;
        var storeys = KataColumnTestSheet.Storeys().ToList();
        storeys[2] = storeys[2] with { MaxXMm = 1000 - step };

        var plan = KataColumnBarPlanner.Plan(spec, storeys, new KataColumnBarRules());

        Assert.True(plan.Errors.Count > 0 || plan.Bars.Count > 0);
        Assert.All(plan.Bars, b => Assert.True(b.Points.Zip(b.Points.Skip(1), (p, q) => q.Z >= p.Z - 0.01).All(x => x), $"{b.Kind} storey {b.Storey} turns down"));
        Assert.All(plan.Bars, b => Assert.All(b.Points.Zip(b.Points.Skip(1), (p, q) => Math.Sqrt(Math.Pow(q.X - p.X, 2) + Math.Pow(q.Y - p.Y, 2) + Math.Pow(q.Z - p.Z, 2))), d => Assert.True(d >= 2)));
    }

    [Fact]
    public void Plan_LargerUpperStorey_DowelsTheNewOuterBars()
    {
        var spec = KataColumnSpecReader.Read(KataColumnTestSheet.Sample(j: 500)).Spec!;
        var storeys = KataColumnTestSheet.Storeys().ToList();
        storeys[2] = storeys[2] with { MaxXMm = 1300 };

        var plan = KataColumnBarPlanner.Plan(spec, storeys, new KataColumnBarRules());

        Assert.Empty(plan.Errors);
        Assert.Contains(plan.Notes, n => n.StartsWith("FLOOR 2:", StringComparison.Ordinal) && n.Contains("dowelled"));
        Assert.Contains(plan.Bars, b => b.Storey == 2 && b.Points[0].Z < storeys[2].BaseMm);
    }

    [Fact]
    public void Plan_FewerBarsAbove_EveryLappedBarKeepsTheGroupOfTheBarBelowIt()
    {
        var cells = KataColumnTestSheet.Sample(j: 500);
        cells.Set("E12", 5.0);
        var spec = KataColumnSpecReader.Read(cells).Spec!;

        var plan = KataColumnBarPlanner.Plan(spec, KataColumnTestSheet.Storeys(), new KataColumnBarRules());

        // A lapped FLOOR 2 bar starts where its group's laps start (base + 500, or 40 d higher for group B).
        var floor2 = plan.Bars.Where(b => b.Storey == 2 && b.Kind == KataColumnBarKind.Main).ToList();
        double baseMm = KataColumnTestSheet.Storeys()[2].BaseMm;
        Assert.All(floor2.Where(b => b.Points[0].Z >= baseMm), b =>
            Assert.Equal(b.Group == KataColumnBarGroup.A ? baseMm + 500 : baseMm + 500 + 40 * 20, b.Points[0].Z));
    }

    [Fact]
    public void Plan_SquareToRound_KeepsBarsInsideTheCircle()
    {
        var cells = KataColumnTestSheet.Sample(j: 500);
        cells.Set("C12", 800.0);
        cells.Set("D12", 0.0);
        var spec = KataColumnSpecReader.Read(cells).Spec!;
        var storeys = KataColumnTestSheet.Storeys().ToList();
        storeys[2] = storeys[2] with { MinXMm = 100, MaxXMm = 900 };

        var plan = KataColumnBarPlanner.Plan(spec, storeys, new KataColumnBarRules());

        Assert.Contains(plan.Notes, n => n.StartsWith("FLOOR 1 → FLOOR 2", StringComparison.Ordinal));
        Assert.All(plan.Bars.Where(b => b.Storey == 1), b => Assert.True(b.Points[^1].Z <= storeys[1].TopMm));
    }

    [Fact]
    public void Plan_StoreysOnAnotherDatum_StartersFollowTheRevitStoreyNotC5()
    {
        var spec = KataColumnSpecReader.Read(KataColumnTestSheet.Sample(j: 500)).Spec!;
        var storeys = KataColumnTestSheet.Storeys().Select(g => g with { BaseMm = g.BaseMm + 10000, TopMm = g.TopMm + 10000 }).ToList();

        var plan = KataColumnBarPlanner.Plan(spec, storeys, new KataColumnBarRules());

        Assert.All(plan.Bars.Where(b => b.Kind == KataColumnBarKind.Starter && b.Layer == 1), b => Assert.Equal(storeys[0].BaseMm - 1500 + 100, b.Points[0].Z));
    }

    [Fact]
    public void Plan_TickedOptionsNotSupported_Refused()
    {
        var cells = KataColumnTestSheet.Sample(j: 500);
        cells.Set("G3", true);
        var spec = KataColumnSpecReader.Read(cells).Spec!;

        var plan = KataColumnBarPlanner.Plan(spec, KataColumnTestSheet.Storeys(), new KataColumnBarRules());

        Assert.Contains(plan.Errors, e => e.StartsWith("G3", StringComparison.Ordinal));
    }

    [Fact]
    public void Design_Sample_NumbersIdenticalBarsAlike_AndFeedsThePage()
    {
        var spec = KataColumnSpecReader.Read(KataColumnTestSheet.Sample(j: 500)).Spec!;
        var design = KataColumnRebarDesigner.Design(spec, KataColumnTestSheet.Storeys(), new KataColumnBarRules());

        Assert.Empty(design.Errors);
        Assert.Equal(5, design.Storeys.Count);
        // Every storey has the same 1000 × 800 section and hoop Ø → one hoop number.
        Assert.Single(design.Storeys.Select(s => s.HoopNumber).Distinct());
        Assert.True(design.Bars.Select(b => b.Number).Distinct().Count() < design.Bars.Count);

        var noJ = KataColumnSpecReader.Read(KataColumnTestSheet.Sample()).Spec!;
        var fixtures = new System.Collections.Generic.Dictionary<string, object>
        {
            ["kata-column-rebar-sample.json"] = KataColumnRebarSummaryMapper.Map(spec, design, KataColumnTestSheet.Storeys()),
            ["kata-column-rebar-errors.json"] = KataColumnRebarSummaryMapper.Map(noJ, KataColumnRebarDesigner.Design(noJ, KataColumnTestSheet.Storeys(), new KataColumnBarRules()), KataColumnTestSheet.Storeys()),
        };
        HPRebar.Core.Tests.KataExport.KataPageFixtures.AssertUpToDate("kataColumn", fixtures, "KataColumnRebarSummaryMapper");
    }

    private static KataColumnBarPlan PlanSample(KataColumnBarRules rules)
    {
        var spec = KataColumnSpecReader.Read(KataColumnTestSheet.Sample(j: 500)).Spec!;
        return KataColumnBarPlanner.Plan(spec, KataColumnTestSheet.Storeys(), rules);
    }

    /// <summary>(bottom, top) of the layer-1 bars of one group per storey, starter first, in the drawing's elevations.</summary>
    private static (double, double)[] Extents(KataColumnBarPlan plan, KataColumnBarGroup group) =>
        plan.Bars.Where(b => b.Group == group && b.Layer == 1)
            .GroupBy(b => (b.Kind == KataColumnBarKind.Starter ? -1 : b.Storey))
            .OrderBy(g => g.Key)
            .Select(g => g.Select(b => (b.Points.Min(p => p.Z) - Origin, b.Points.Max(p => p.Z) - Origin)).Distinct().Single())
            .ToArray();
}
