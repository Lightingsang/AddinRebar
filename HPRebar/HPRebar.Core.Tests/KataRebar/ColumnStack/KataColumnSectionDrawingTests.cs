using System;
using System.Linq;
using HPRebar.Core.KataRebar.ColumnStack;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar.ColumnStack;

public sealed class KataColumnSectionDrawingTests
{
    [Fact]
    public void Build_Sample_OneSectionPerStorey_WithTheStoreysOwnBars()
    {
        var (spec, design, sections) = Sample();

        Assert.Equal(design.Storeys.Count, sections.Count);
        foreach (var storey in design.Storeys)
        {
            var section = sections[storey.Storey];
            var own = design.Bars.Where(b => b.Run.Storey == storey.Storey && b.Run.Kind != KataColumnBarKind.Starter).ToList();
            Assert.Equal(storey.Name, section.Level);
            Assert.Equal(own.Count, section.Bars.Count);
            Assert.Equal(own.Select(b => b.Number).Distinct().OrderBy(n => n), section.Bars.Select(b => b.Number).Distinct().OrderBy(n => n));
            Assert.Equal(storey.HoopNumber, section.HoopNumber);
            Assert.Equal(spec.CoverMm + spec.Hoops.DiameterMm / 2, section.HoopOffsetMm);
        }
    }

    [Fact]
    public void Build_Sample_CutPassesThroughEveryOwnBarOfTheStorey()
    {
        var (_, design, sections) = Sample();

        foreach (var storey in design.Storeys)
        {
            double cut = sections[storey.Storey].CutElevationMm;
            var own = design.Bars.Where(b => b.Run.Storey == storey.Storey && b.Run.Kind != KataColumnBarKind.Starter).ToList();
            Assert.All(own, b => Assert.InRange(cut, b.Run.Points.Min(p => p.Z), b.Run.Points.Max(p => p.Z)));
        }
    }

    [Fact]
    public void Build_Sample_BarsSitInsideTheHoop()
    {
        var (_, _, sections) = Sample();

        Assert.All(sections, s => Assert.All(s.Bars, b =>
        {
            Assert.InRange(b.X, s.HoopOffsetMm, s.WidthMm - s.HoopOffsetMm);
            Assert.InRange(b.Y, s.HoopOffsetMm, s.DepthMm - s.HoopOffsetMm);
        }));
    }

    [Fact]
    public void Build_Sample_BarsSitOnTheirSectionGrid_NotOnACrank()
    {
        var (spec, design, sections) = Sample();

        foreach (var storey in design.Storeys)
        {
            var grid = storey.Section.Bars;
            Assert.All(sections[storey.Storey].Bars, b =>
                Assert.Contains(grid, g => Math.Abs(g.X - b.X) < 0.5 && Math.Abs(g.Y - b.Y) < 0.5));
        }
    }

    [Fact]
    public void Build_Sample_InnerTiesWrapTheBarsOfTheirGridLines()
    {
        var (_, design, sections) = Sample();

        var withTies = sections.Where(s => s.Ties.Count > 0).ToList();
        Assert.NotEmpty(withTies);
        Assert.All(withTies, s => Assert.All(s.Ties.Where(t => t.Kind == "closed"), t =>
        {
            // Bars on the tie's corners lie inside its outline by half a bar Ø + half a tie Ø.
            double wrap = s.Bars.Max(b => b.DiameterMm) / 2 + s.TieDiameterMm / 2;
            Assert.Contains(s.Bars, b => Math.Abs(b.X - (t.X1 + wrap)) < 30 && Math.Abs(b.Y - (t.Y1 + wrap)) < 30);
        }));
        Assert.All(withTies, s => Assert.Equal(design.Storeys[sections.IndexOf(s)].TieSet!.Ties.Count, s.Ties.Count));
    }

    [Fact]
    public void Dims_Rectangle_BarsOfTheBAndHFacesThenOverall()
    {
        var (_, _, sections) = Sample();
        var s = sections[0];

        var chains = KataColumnSectionDims.Chains(s);

        Assert.Equal(4, chains.Count);
        int bottomBars = s.Bars.Count(b => b.Y <= s.Bars.Min(x => x.Y) + 1);
        Assert.Equal(bottomBars + 2, chains[0].Stations.Count);
        Assert.False(chains[0].Vertical);
        Assert.True(chains[0].LineAt < 0);
        Assert.Equal((0.0, s.WidthMm), (chains[2].Stations[0].Along, chains[2].Stations[^1].Along));
        Assert.True(chains[3].Vertical);
        Assert.Equal((0.0, s.DepthMm), (chains[3].Stations[0].Along, chains[3].Stations[^1].Along));
        Assert.All(chains, c => Assert.True(c.Stations.Zip(c.Stations.Skip(1), (a, b) => b.Along > a.Along).All(x => x)));
    }

    [Fact]
    public void Dims_Round_DiameterOnly()
    {
        var round = new KataColumnSectionDrawing("R", 0, true, 400, 400, 40, 10, 100, 1, 8, 400, Array.Empty<KataColumnSectionBar>(), Array.Empty<KataColumnSectionTie>());

        var chain = Assert.Single(KataColumnSectionDims.Chains(round));
        Assert.Equal(400, chain.Stations[^1].Along - chain.Stations[0].Along);
    }

    [Fact]
    public void Elevation_Sample_LevelsLapsAndZones()
    {
        var (spec, design, _) = Sample();
        var storeys = KataColumnTestSheet.Storeys();

        var elevation = KataColumnElevationDims.Build(spec, storeys, design);

        Assert.Equal(4, elevation.Chains.Count);
        Assert.All(elevation.Chains, c => Assert.True(c.Vertical));
        var levels = elevation.Chains[0];
        Assert.Equal(storeys.Count + 1, levels.Stations.Count);
        Assert.Equal(storeys[^1].TopMm, levels.Stations[^1].Along);
        // Group A on the left, B on the right, each lap measured from the bar above's start to the bar below's end:
        // starter → storey 0 … → top storey = one lap per storey.
        Assert.True(elevation.Chains[1].LineAt < elevation.LeftMm);
        Assert.Equal(2 * storeys.Count, elevation.Chains[1].Stations.Count);
        Assert.Equal(2 * storeys.Count, elevation.Chains[2].Stations.Count);
        Assert.True(elevation.Chains[3].LineAt > elevation.Chains[2].LineAt);
        Assert.All(elevation.Chains, c => Assert.True(c.Stations.Zip(c.Stations.Skip(1), (x, y) => y.Along > x.Along).All(ok => ok)));
        Assert.Equal(storeys[0].BaseMm - spec.PileCapHeightMm, elevation.BottomMm);
    }

    [Fact]
    public void Build_RoundStack_BarsOnOneCircle_NoInnerTies()
    {
        var cells = KataColumnTestSheet.Sample(j: 500);
        for (int row = 10; row <= 15; row++)
        {
            cells.Set($"D{row}", 0.0);
        }

        var spec = KataColumnSpecReader.Read(cells).Spec!;
        var storeys = KataColumnTestSheet.Storeys();
        var design = KataColumnRebarDesigner.Design(spec, storeys, new KataColumnBarRules());

        var sections = KataColumnSectionDrawings.Build(spec, storeys, design);

        Assert.Empty(design.Errors);
        Assert.Equal(storeys.Count, sections.Count);
        Assert.All(sections, s =>
        {
            Assert.True(s.IsRound);
            Assert.Empty(s.Ties);
            Assert.NotEmpty(s.Bars);
            // On the circle; a bar lapped beside the one below (along b, one Ø) sits up to one Ø inside it.
            double outer = s.Bars.Max(b => b.DiameterMm);
            double radius = s.WidthMm / 2 - s.HoopOffsetMm - s.HoopDiameterMm / 2 - outer / 2;
            Assert.All(s.Bars, b => Assert.InRange(Math.Sqrt(Math.Pow(b.X - s.WidthMm / 2, 2) + Math.Pow(b.Y - s.WidthMm / 2, 2)), radius - outer - 0.5, radius + 0.5));
            Assert.True(s.Bars.Count(b => Math.Abs(Math.Sqrt(Math.Pow(b.X - s.WidthMm / 2, 2) + Math.Pow(b.Y - s.WidthMm / 2, 2)) - radius) < 0.5) >= s.Bars.Count / 2);
        });
    }

    [Fact]
    public void Elevation_OneStorey_StarterLapOnlyAndOneLevelSpan()
    {
        var cells = KataColumnTestSheet.Sample(j: 500);
        foreach (char column in "ABCDEFGHIJKL")
        {
            for (int row = 12; row <= 15; row++)
            {
                cells.Set($"{column}{row}", null);
            }
        }

        var spec = KataColumnSpecReader.Read(cells).Spec!;
        var storeys = KataColumnTestSheet.Storeys().Take(1).ToList();
        var design = KataColumnRebarDesigner.Design(spec, storeys, new KataColumnBarRules());

        var elevation = KataColumnElevationDims.Build(spec, storeys, design);

        Assert.Empty(design.Errors);
        Assert.Single(KataColumnSectionDrawings.Build(spec, storeys, design));
        Assert.Equal(new[] { storeys[0].BaseMm, storeys[0].TopMm }, elevation.Chains[0].Stations.Select(s => s.Along));
        // Group A and B: the starter's lap into the one storey only.
        Assert.Equal(2, elevation.Chains[1].Stations.Count);
        Assert.Equal(2, elevation.Chains[2].Stations.Count);
    }

    [Fact]
    public void Build_RefusedSheet_NoSections()
    {
        var spec = KataColumnSpecReader.Read(KataColumnTestSheet.Sample()).Spec!;
        var design = KataColumnRebarDesigner.Design(spec, KataColumnTestSheet.Storeys(), new KataColumnBarRules());

        Assert.NotEmpty(design.Errors);
        Assert.Empty(KataColumnSectionDrawings.Build(spec, KataColumnTestSheet.Storeys(), design));
    }

    private static (KataColumnSpec Spec, KataColumnRebarDesign Design, System.Collections.Generic.List<KataColumnSectionDrawing> Sections) Sample()
    {
        var spec = KataColumnSpecReader.Read(KataColumnTestSheet.Sample(j: 500)).Spec!;
        var design = KataColumnRebarDesigner.Design(spec, KataColumnTestSheet.Storeys(), new KataColumnBarRules());
        return (spec, design, KataColumnSectionDrawings.Build(spec, KataColumnTestSheet.Storeys(), design).ToList());
    }
}
