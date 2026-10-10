using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>Kata's dimension segments grouped into the chains a Revit dimension draws.</summary>
public sealed class KataDimChainsTests
{
    [Fact]
    public void Touching_segments_on_one_line_make_one_chain_in_order()
    {
        var chains = KataDimChains.From(new[]
        {
            new KataDrawingDim(400, 0, 1800, 0, false, 300),
            new KataDrawingDim(0, 0, 400, 0, false, 300),
            new KataDrawingDim(1800, -50, 6000, -50, false, 300)
        });

        var chain = Assert.Single(chains);
        Assert.False(chain.Vertical);
        Assert.Equal(300, chain.LineAt);
        Assert.Equal(new[] { 0.0, 400, 1800, 6000 }, chain.Stations.Select(s => s.Along));
    }

    [Fact]
    public void A_gap_or_another_line_starts_a_new_chain_and_runs_are_left_out()
    {
        var chains = KataDimChains.From(new[]
        {
            new KataDrawingDim(0, 0, 400, 0, false, 300),
            new KataDrawingDim(1000, 0, 1400, 0, false, 300),
            new KataDrawingDim(0, 0, 400, 0, false, 600),
            new KataDrawingDim(0, -600, 0, 0, true, -375),
            new KataDrawingDim(0, 0, 900, 0, false, 300, KataDimStyle.Run)
        });

        Assert.Equal(4, chains.Count);
        Assert.Single(chains, c => c.Vertical && c.Stations.Select(s => s.Along).SequenceEqual(new[] { -600.0, 0.0 }));
    }

    [Theory]
    [MemberData(nameof(KataDwgElevationTests.Beams), MemberType = typeof(KataDwgElevationTests))]
    public void Every_Kata_dimension_of_the_elevation_is_measured_by_a_chain(string name)
    {
        var beam = KataDwgBeam.Named(name);
        var spec = KataDamSheetParser.Parse(beam.Sheet());
        var layout = KataRebarCalculator.Calculate(spec);
        var tags = KataBarTagBuilder.Build(spec, layout, 10.0);
        var drawing = KataElevationDrawingBuilder.Build(spec, layout, KataSectionCuts.Build(spec, layout), 10.0, KataBarTagBuilder.StirrupRowOf(tags));
        var chains = KataDimChains.From(drawing.Dims);

        foreach (var d in drawing.Dims.Where(d => d.Style == KataDimStyle.Kata && d.Value > 0.5))
        {
            double a = d.Vertical ? d.Z1 : d.X1, b = d.Vertical ? d.Z2 : d.X2;
            Assert.Contains(chains, c => c.Vertical == d.Vertical && System.Math.Abs(c.LineAt - d.LineAt) < 0.5
                                         && c.Stations.Any(s => System.Math.Abs(s.Along - a) < 0.5)
                                         && c.Stations.Any(s => System.Math.Abs(s.Along - b) < 0.5));
        }
    }
}
