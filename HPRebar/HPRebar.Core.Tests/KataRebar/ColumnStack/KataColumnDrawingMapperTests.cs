using System;
using System.Linq;
using HPRebar.Core.KataRebar.ColumnStack;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar.ColumnStack;

/// <summary>The elevation the page draws, laid out as Kata's VC elevation (plans/261103-1207-kata-column/reports/variants/v00-base.json).</summary>
public sealed class KataColumnDrawingMapperTests
{
    [Fact]
    public void Map_Sample_LevelsFromCapBottomToTop_WithTheSheetNames()
    {
        var (_, storeys, _, elevation) = Sample();

        Assert.Equal("PILECAPS", elevation.Levels[0].Name);
        Assert.Equal(elevation.BottomMm, elevation.Levels[0].ZMm);
        Assert.Equal(new[] { "BASEMENT", "FLOOR 1", "FLOOR 2", "FLOOR 3", "FLOOR 4", "FLOOR 5" }, elevation.Levels.Skip(1).Select(l => l.Name));
        Assert.Equal(storeys[^1].TopMm, elevation.Levels[^1].ZMm);
        Assert.Equal(storeys.Count, elevation.Storeys.Count);
        Assert.Equal((600.0, 600.0), (elevation.Storeys[0].BeamDepthMm, elevation.Storeys[1].BeamDepthMm));
    }

    [Fact]
    public void Map_Sample_GroupALeftAndBRight_EveryStoreyAndLayer_StartersIncluded()
    {
        var (_, storeys, design, elevation) = Sample();

        int layers(int storey) => design.Bars.Where(b => b.Run.Storey == storey && b.Run.Kind != KataColumnBarKind.Starter).Select(b => b.Run.Layer).Distinct().Count();
        for (int k = 0; k < storeys.Count; k++)
        {
            var own = elevation.Bars.Where(b => b.Storey == k && b.Kind != KataColumnBarKind.Starter).ToList();
            Assert.Equal(2 * layers(k), own.Count);
            var a = own.Single(b => b.Group == KataColumnBarGroup.A && b.Layer == 1);
            var bBar = own.Single(b => b.Group == KataColumnBarGroup.B && b.Layer == 1);
            Assert.True(a.Points.Min(p => p.X) < 0 && bBar.Points.Max(p => p.X) > 0);
        }

        var starters = elevation.Bars.Where(b => b.Kind == KataColumnBarKind.Starter).ToList();
        Assert.NotEmpty(starters);
        Assert.All(starters, s => Assert.True(s.Points.Min(p => p.Z) < storeys[0].BaseMm));
    }

    [Fact]
    public void Map_Sample_LabelCountsTheGroupsBarsOfOneDiameter_AndTheLengthIsTheBars()
    {
        var (_, _, design, elevation) = Sample();

        // One label per group and Ø, as VC writes "8Ø20" and "12Ø25": the labels of a storey add up to its bars, two
        // layers of one Ø included (BASEMENT: 40Ø25 on two layers = 20Ø25 + 20Ø25, not four labels).
        var keys = elevation.Bars.Select(b => b.Kind == KataColumnBarKind.Starter ? -1 : b.Storey).Distinct();
        Assert.All(keys, key =>
        {
            var labelled = elevation.Bars.Where(b => (b.Kind == KataColumnBarKind.Starter ? -1 : b.Storey) == key && b.Labelled);
            Assert.Equal(design.Bars.Count(b => KataColumnElevationDims.StoreyKey(b.Run) == key), labelled.Sum(b => b.Count));
        });
        Assert.All(elevation.Bars, b =>
        {
            var run = design.Bars.First(d => d.Number == b.Number).Run;
            Assert.InRange(b.LengthMm, run.Points.Max(p => p.Z) - run.Points.Min(p => p.Z), double.MaxValue);
        });
    }

    [Fact]
    public void Map_Sample_HoopZonesInsideTheCover_AndOneCutPerStoreyInsideIt()
    {
        var (spec, storeys, design, elevation) = Sample();

        Assert.Equal(design.Storeys.Sum(s => s.HoopZones.Count), elevation.HoopZones.Count);
        Assert.All(elevation.HoopZones, z =>
        {
            Assert.Equal(elevation.Storeys[z.Storey].LeftMm + spec.CoverMm + spec.Hoops.DiameterMm / 2, z.LeftMm, 6);
            Assert.True(z.FromMm < z.ToMm);
        });
        Assert.Equal(Enumerable.Range(1, storeys.Count).Select(k => $"{k}-{k}"), elevation.Cuts.Select(c => c.Name));
        Assert.All(elevation.Cuts, c => Assert.InRange(c.ZMm, storeys[c.Storey].BaseMm, storeys[c.Storey].TopMm));
        Assert.NotEmpty(elevation.Chains);
    }

    [Fact]
    public void Map_NoPileCap_NoCapLevel_AndTheStartersStayInside()
    {
        var cells = KataColumnTestSheet.Sample(j: 500);
        cells.Set("B9", 0.0);
        var reading = KataColumnSpecReader.Read(cells);
        Assert.Empty(reading.Errors);
        var storeys = KataColumnTestSheet.Storeys();
        var design = KataColumnRebarDesigner.Design(reading.Spec!, storeys, new KataColumnBarRules());

        var elevation = KataColumnRebarSummaryMapper.Map(reading.Spec!, design, storeys).Elevation!;

        Assert.DoesNotContain(elevation.Levels, l => l.Name == KataColumnDrawingMapper.PileCapLevel);
        Assert.Equal((elevation.Storeys[0].LeftMm, elevation.Storeys[0].RightMm), (elevation.PileCapLeftMm, elevation.PileCapRightMm));
        Assert.All(elevation.Bars, b => Assert.All(b.Points, p => Assert.InRange(p.Z, elevation.BottomMm, elevation.TopMm)));
    }

    [Fact]
    public void Map_RefusedSheet_Null()
    {
        var spec = KataColumnSpecReader.Read(KataColumnTestSheet.Sample()).Spec!;
        var storeys = KataColumnTestSheet.Storeys();
        var design = KataColumnRebarDesigner.Design(spec, storeys, new KataColumnBarRules());

        Assert.NotEmpty(design.Errors);
        Assert.Null(KataColumnRebarSummaryMapper.Map(spec, design, storeys).Elevation);
    }

    private static (KataColumnSpec Spec, System.Collections.Generic.IReadOnlyList<KataColumnStoreyGeometry> Storeys, KataColumnRebarDesign Design, KataColumnElevationDrawing Elevation) Sample()
    {
        var spec = KataColumnSpecReader.Read(KataColumnTestSheet.Sample(j: 500)).Spec!;
        var storeys = KataColumnTestSheet.Storeys();
        var design = KataColumnRebarDesigner.Design(spec, storeys, new KataColumnBarRules());
        return (spec, storeys, design, KataColumnRebarSummaryMapper.Map(spec, design, storeys).Elevation!);
    }
}
