using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.ColumnStack;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar.ColumnStack;

/// <summary>
/// The section, hoop and tie rules against Kata's own VC drawings (plans/261103-1207-kata-column/reports/variants*):
/// numbers below are measured in those drawings, sheet = Kata.xlsm's sample (1000 × 800, cover 30, hoops d10).
/// </summary>
public sealed class KataColumnSectionPlannerTests
{
    [Fact]
    public void Read_KataSample_ReadsHeaderOptionsAndRows()
    {
        var reading = KataColumnSpecReader.Read(Sample());

        Assert.Empty(reading.Errors);
        var spec = reading.Spec!;
        Assert.Equal(("C1", 1, -5500.0, 1500.0, 30.0, 100.0, 15.0), (spec.Name, spec.Count, spec.PileCapBottomMm, spec.PileCapHeightMm, spec.CoverMm, spec.StarterLiftMm, spec.BendFactor));
        Assert.Equal(new KataColumnHoopSpec(10, 100, 200, 100), spec.Hoops);
        Assert.Equal(new KataColumnTieSpec(8, 400), spec.Ties);
        Assert.Equal((true, true, false, false), (spec.AutoTies, spec.HoopsThroughBeam, spec.DenseAtLaps, spec.TightInnerLayer));
        Assert.Equal(6, spec.Rows.Count);
        Assert.Equal((25.0, 20.0), (spec.Rows[2].OuterDiameterMm, spec.Rows[2].InnerDiameterMm));
        Assert.Equal((3500.0, 600.0), (spec.Rows[1].HeightMm, spec.Rows[1].BeamDepthMm));
    }

    [Fact]
    public void Read_StoreyWithoutBars_NamesTheCell()
    {
        var cells = Sample();
        cells.Set("E15", null);

        var reading = KataColumnSpecReader.Read(cells);

        Assert.Null(reading.Spec);
        Assert.Contains(reading.Errors, e => e.StartsWith("E15", StringComparison.Ordinal));
    }

    [Fact]
    public void Plan_SampleFloor1_PutsTheFortyBarsOfTheDrawing()
    {
        var spec = KataColumnSpecReader.Read(Sample()).Spec!;

        var section = KataColumnSectionPlanner.Plan(spec.Rows[1], spec);

        Assert.Equal(40, section.Bars.Count);
        Assert.Equal(new[] { 52.5, 201.7, 350.8, 500, 649.2, 798.3, 947.5 }, section.Bars.Select(b => Math.Round(b.X, 1)).Distinct().OrderBy(x => x));
        Assert.Equal(new[] { 52.5, 201.7, 350.8, 500, 649.2, 798.3, 947.5 }, RowAt(section, 168.3));
        Assert.Equal(new[] { 52.5, 201.7, 798.3, 947.5 }, RowAt(section, 400));
        Assert.Equal(16, section.Bars.Count(b => b.Layer == 2));
    }

    /// <summary>Every (F, H) pair drawn by VC.</summary>
    [Theory]
    [InlineData(0, 0, 24)]
    [InlineData(2, 2, 24)]
    [InlineData(3, 0, 26)]
    [InlineData(0, 3, 26)]
    [InlineData(3, 3, 26)]
    [InlineData(1, 1, 24)]
    [InlineData(2, 3, 26)]
    [InlineData(2, 4, 28)]
    [InlineData(3, 4, 26)]
    [InlineData(3, 5, 28)]
    [InlineData(4, 3, 28)]
    [InlineData(5, 3, 30)]
    [InlineData(4, 4, 28)]
    [InlineData(5, 5, 32)]
    [InlineData(6, 6, 36)]
    [InlineData(7, 7, 40)]
    public void Plan_InnerCounts_MatchTheBarCountsKataDraws(int f, int h, int expected)
    {
        var spec = KataColumnSpecReader.Read(Sample(f: f, h: h)).Spec!;

        var section = KataColumnSectionPlanner.Plan(spec.Rows[1], spec);

        Assert.Equal(expected, section.Bars.Count);
    }

    /// <summary>
    /// Layer-2 bars (1-based "column:row", bottom row 1) and ties of both directions in the s-f*-h* drawings
    /// (E = G = 7). F and H count their ends; with a layer-2 row, H's ends include the row's slots, so H = 3 or 4 adds
    /// nothing beside F ≥ 3 and that column is absent, ties included. F = 1 or 2 adds nothing either.
    /// </summary>
    [Theory]
    [InlineData(0, 0, "", "O3-5", "O3-5", false)]
    [InlineData(1, 1, "", "O3-5", "O3-5", true)]
    [InlineData(2, 2, "", "O3-5", "O3-5", true)]
    [InlineData(3, 0, "4:2 4:6", "O3-5", "O2-6 C4", false)]
    [InlineData(3, 3, "4:2 4:6", "O3-5", "O2-6 C4", true)]
    [InlineData(3, 4, "4:2 4:6", "O3-5", "O2-6 C4", true)]
    [InlineData(3, 5, "4:2 2:4 6:4 4:6", "O2-6 C4", "O2-6 C4", false)]
    [InlineData(0, 3, "2:4 6:4", "O2-6 C4", "O3-5", false)]
    [InlineData(2, 3, "2:4 6:4", "O2-6 C4", "O3-5", true)]
    [InlineData(2, 4, "2:2 6:2 2:6 6:6", "O2-6 C4", "O3-5", true)]
    [InlineData(4, 3, "2:2 6:2 2:6 6:6", "O3-5", "O2-6 C4", true)]
    [InlineData(4, 4, "2:2 6:2 2:6 6:6", "O3-5", "O2-6 C4", true)]
    [InlineData(5, 5, "2:2 4:2 6:2 2:4 6:4 2:6 4:6 6:6", "O2-6 C4", "O2-6 C4", false)]
    [InlineData(6, 6, "2:2 3:2 5:2 6:2 2:3 6:3 2:5 6:5 2:6 3:6 5:6 6:6", "O2-6 C4", "O2-6 C4", false)]
    public void Plan_InnerLines_BarsAndTiesAsKataDraws(int f, int h, string layer2, string alongB, string alongH, bool noted)
    {
        var spec = KataColumnSpecReader.Read(Sample(f: f, h: h)).Spec!;

        var section = KataColumnSectionPlanner.Plan(spec.Rows[1], spec);

        Assert.Equal(layer2, Layer2(section));
        Assert.Equal((alongB, alongH), (Ties(section, true), Ties(section, false)));
        Assert.Equal(noted, section.Notes.Count > 0);
    }

    /// <summary>The live check's sheet E = 4, F = 2, G = 5, H = 3, drawn by VC: H's centre only, C tie across b.</summary>
    [Fact]
    public void Plan_E4F2G5H3_AsKataDraws()
    {
        var cells = Sample(e: 4, f: 2, h: 3);
        cells.Set("G11", 5.0);
        var spec = KataColumnSpecReader.Read(cells).Spec!;

        var section = KataColumnSectionPlanner.Plan(spec.Rows[1], spec);

        Assert.Equal(16, section.Bars.Count);
        Assert.Equal("2:3 3:3", Layer2(section));
        Assert.Equal(("O2-3", "C3"), (Ties(section, true), Ties(section, false)));
    }

    private static string Layer2(KataColumnSection section) => string.Join(" ", section.Bars.Where(b => b.Layer == 2)
        .Select(b => $"{b.Slot.Column + 1}:{b.Slot.Row + 1}"));

    private static string Ties(KataColumnSection section, bool alongB) => string.Join(" ", section.Ties.Where(t => t.AlongB == alongB)
        .Select(t => t.Kind == KataColumnTieKind.Crosstie ? $"C{t.FirstSlot + 1}" : $"O{t.FirstSlot + 1}-{t.SecondSlot + 1}"));

    [Theory]
    [InlineData(7, 2, "0,6")]
    [InlineData(7, 3, "0,3,6")]
    [InlineData(7, 4, "0,1,5,6")]
    [InlineData(7, 5, "0,1,3,5,6")]
    [InlineData(7, 6, "0,1,2,4,5,6")]
    public void SymmetricSlots_TakesPairsFromOutsideAndTheCentre(int slots, int count, string expected)
    {
        Assert.Equal(expected, string.Join(",", KataColumnSectionPlanner.SymmetricSlots(slots, count)));
    }

    [Theory]
    [InlineData(3, 0, "")]
    [InlineData(4, 0, "")]
    [InlineData(5, 0, "C3")]
    [InlineData(6, 0, "O3-4")]
    [InlineData(7, 0, "O3-5")]
    [InlineData(8, 0, "O3-6")]
    [InlineData(9, 0, "O3-7 C5")]
    [InlineData(11, 0, "O3-5 O7-9")]
    [InlineData(7, 7, "O2-6 C4")]
    public void Plan_AutoTiesAlongB_MatchKatasDrawings(int barsAlongB, int innerAlongH, string expected)
    {
        var spec = KataColumnSpecReader.Read(Sample(e: barsAlongB, f: 0, h: innerAlongH)).Spec!;

        var section = KataColumnSectionPlanner.Plan(spec.Rows[1], spec);

        // 1-based slots as read off the drawings (slot 1 = the corner bar).
        var along = section.Ties.Where(t => t.AlongB).Select(t => t.Kind == KataColumnTieKind.Crosstie ? $"C{t.FirstSlot + 1}" : $"O{t.FirstSlot + 1}-{t.SecondSlot + 1}");
        Assert.Equal(expected, string.Join(" ", along));
    }

    [Theory]
    [InlineData(1000, 2900, 1000)]
    [InlineData(400, 6400, 1050)]
    [InlineData(400, 6600, 1100)]
    [InlineData(300, 2900, 500)]
    [InlineData(300, 3500, 600)]
    [InlineData(300, 4200, 700)]
    [InlineData(300, 2700, 450)]
    [InlineData(300, 2400, 450)]
    [InlineData(450, 3050, 500)]
    public void CriticalLength_MatchesTheDenseZonesKataDraws(double side, double clear, double expected)
    {
        Assert.Equal(expected, KataColumnHoopPlanner.CriticalLength(side, clear));
    }

    [Fact]
    public void Zones_SampleFloor1_DenseMiddleDenseAndThroughTheBeam()
    {
        var spec = KataColumnSpecReader.Read(Sample()).Spec!;

        var zones = KataColumnHoopPlanner.Zones(spec.Rows[1], spec);

        // Drawing, storey base 3000: 3050–4000 a100, 4200–4700 a200, 4900–5850 a100, 5950–6450 a100 (beam).
        Assert.Equal(
            new[] { (50.0, 1000.0, 100.0), (1200, 1700, 200), (1900, 2850, 100), (2950, 3450, 100) },
            zones.Select(z => (z.FromMm, z.ToMm, z.SpacingMm)));
    }

    [Fact]
    public void Zones_ShortBasement_OneDenseZone()
    {
        var spec = KataColumnSpecReader.Read(Sample()).Spec!;

        var zones = KataColumnHoopPlanner.Zones(spec.Rows[0], spec);

        Assert.Equal(new[] { (50.0, 2350.0), (2450, 2950) }, zones.Select(z => (z.FromMm, z.ToMm)));
    }

    [Fact]
    public void Plan_RoundStorey_PutsEPlusFBarsOnOneCircle()
    {
        var cells = Sample();
        cells.Set("C11", 800.0);
        cells.Set("D11", 0.0);
        var spec = KataColumnSpecReader.Read(cells).Spec!;

        var section = KataColumnSectionPlanner.Plan(spec.Rows[1], spec);

        Assert.Equal(14, section.Bars.Count);
        Assert.All(section.Bars, b => Assert.Equal(347.5, Math.Round(Math.Sqrt(Math.Pow(b.X - 400, 2) + Math.Pow(b.Y - 400, 2)), 1)));
        Assert.Empty(section.Ties);
    }

    [Fact]
    public void Plan_InnerBarsWithoutRoom_AreReportedNotDropped()
    {
        var cells = Sample(f: 4);
        cells.Set("G11", 3.0);
        var spec = KataColumnSpecReader.Read(cells).Spec!;

        var section = KataColumnSectionPlanner.Plan(spec.Rows[1], spec);

        Assert.Contains(section.Notes, n => n.StartsWith("F11 = 4 ignored", StringComparison.Ordinal));
    }

    [Fact]
    public void Zones_BeamDeeperThanTheStorey_OnlyTheJoint()
    {
        var spec = KataColumnSpecReader.Read(Sample()).Spec!;
        var row = spec.Rows[1] with { BeamDepthMm = 3500 };

        var zones = KataColumnHoopPlanner.Zones(row, spec);

        Assert.All(zones, z => Assert.True(z.ToMm > z.FromMm));
    }

    private static IEnumerable<double> RowAt(KataColumnSection section, double y) =>
        section.Bars.Where(b => Math.Abs(b.Y - y) < 0.1).Select(b => Math.Round(b.X, 1)).OrderBy(x => x);

    private static KataCellTable Sample(int e = 7, int f = 7, int h = 7) => KataColumnTestSheet.Sample(e, f, h);
}
