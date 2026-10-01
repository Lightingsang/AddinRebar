using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public sealed class KataTieWrapTests
{
    [Fact]
    public void A_side_bar_tie_runs_one_bend_radius_under_the_bars_and_reaches_past_them()
    {
        // Side bars (±111, −386), Ø8 tie, bend radius 14 (Revit: 20 / 2 + 8 / 2).
        var set = new KataBarSet
        {
            Diameter = 8.0,
            Shape = new Polyline3(new System.Collections.Generic.List<Point3> { new(466, -111, -386), new(466, 111, -386) }),
            WrapEnds = true,
            WrapOffset = new Point3(0, 0, -1)
        };

        var (shape, start, end) = KataTieWrap.Lay(set, 14.0);

        Assert.Equal(new Point3(466, -129, -400), shape.Points[0]);
        Assert.Equal(new Point3(466, 129, -400), shape.Points[1]);
        Assert.Equal((new Point3(466, -111, -386), new Point3(466, 111, -386)), (start, end));
    }

    [Fact]
    public void An_upright_C_tie_stands_beside_its_bar_and_reaches_past_top_and_bottom()
    {
        var set = new KataBarSet
        {
            Diameter = 8.0,
            Shape = new Polyline3(new System.Collections.Generic.List<Point3> { new(0, 0, -43), new(0, 0, -557) }),
            WrapEnds = true,
            WrapOffset = new Point3(0, 1, 0)
        };

        var (shape, _, _) = KataTieWrap.Lay(set, 14.0);

        Assert.Equal(new Point3(0, 14, -25), shape.Points[0]);
        Assert.Equal(new Point3(0, 14, -575), shape.Points[1]);
    }
}
