using System;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// A second-layer "left;right" cell over a support between spans of different widths (B01 at K: span 400 | span
/// 300): whatever the counts, no two bars may lie on one line where both run.
/// </summary>
public sealed class KataSupportWidthChangeTests
{
    [Theory]
    [InlineData("2f20;2f16")]
    [InlineData("3f20;3f16")]
    [InlineData("1f20;1f16")]
    [InlineData("3f20;2f16")]
    public void Layer_two_bars_over_a_width_change_never_overlap(string cell)
    {
        var sheet = KataB01DrawingTests.Sheet();
        sheet.Set("K14", cell);
        var layout = KataRebarCalculator.Calculate(KataDamSheetParser.Parse(sheet));

        var bars = layout.ExtraTopBars.Where(b => b.HostSupportIndex >= 0).ToList();
        foreach (var (a, i) in bars.Select((b, i) => (b, i)))
        foreach (var b in bars.Skip(i + 1))
        {
            bool sameLine = Math.Abs(Y(a) - Y(b)) < 1.0 && Math.Abs(TopZ(a) - TopZ(b)) < 1.0;
            double overlap = Math.Min(MaxX(a), MaxX(b)) - Math.Max(MinX(a), MinX(b));
            Assert.False(sameLine && overlap > 1.0,
                $"{cell}: bars {a.BarId} and {b.BarId} share y {Y(a):0} z {TopZ(a):0} over {overlap:0} mm");
        }
    }

    private static double Y(KataRebarCurve bar) => bar.Polyline.Points.Average(p => p.Y);
    private static double TopZ(KataRebarCurve bar) => bar.Polyline.Points.Max(p => p.Z);
    private static double MinX(KataRebarCurve bar) => bar.Polyline.Points.Min(p => p.X);
    private static double MaxX(KataRebarCurve bar) => bar.Polyline.Points.Max(p => p.X);
}
