using System;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public class KataShapeCodeAndBarMarkTests
{
    [Fact]
    public void Calculate_AssignsStandardKataShapeCodesAndBarMarks()
    {
        // Continuous 2-span beam with full complement of bars
        var spec = new KataBeamRebarSpec
        {
            BeamName = "D1",
            Width = 300.0,
            Height = 600.0,
            CoverStirrup = 25.0,
            TopContinuous = new KataBarItem(2, 20.0), // 2f20
            BottomContinuous = new KataBarItem(2, 20.0), // 2f20
            GlobalStirrup = new KataStirrupSpec
            {
                Diameter = 8.0,
                SupportSpacing = 100.0,
                MidspanSpacing = 200.0
            },
            Supports = new[]
            {
                new KataSupportRebarSpec
                {
                    SupportIndex = 0,
                    ColumnWidth = 400.0,
                    TopExtraLayer1 = new[] { new KataBarItem(2, 18.0) }
                },
                new KataSupportRebarSpec
                {
                    SupportIndex = 1,
                    ColumnWidth = 400.0,
                    TopExtraLayer1 = new[] { new KataBarItem(2, 18.0) }
                },
                new KataSupportRebarSpec
                {
                    SupportIndex = 2,
                    ColumnWidth = 400.0,
                    TopExtraLayer1 = new[] { new KataBarItem(2, 18.0) }
                }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec
                {
                    SpanIndex = 0,
                    Length = 4000.0,
                    BottomExtraLayer1 = new[] { new KataBarItem(2, 18.0) },
                    InnerStirrups = new[]
                    {
                        new KataStirrupBranchSpec(KataStirrupShapeType.CapStirrup, "1-2", "C26"),
                        new KataStirrupBranchSpec(KataStirrupShapeType.CrossTie, "1", "C27")
                    }
                },
                new KataSpanRebarSpec
                {
                    SpanIndex = 1,
                    Length = 4000.0,
                    BottomExtraLayer1 = new[] { new KataBarItem(2, 18.0) }
                }
            },
            GlobalSideBars = new[] { new KataBarItem(2, 12.0) }
        };

        var layout = KataRebarCalculator.Calculate(spec);
        Assert.True(layout.IsValid);

        // 1. Main Top Bars
        Assert.NotEmpty(layout.MainTopBars);
        foreach (var bar in layout.MainTopBars)
        {
            Assert.Equal("15a", bar.ShapeCode);
            Assert.Equal("1", bar.BarMark);
            Assert.Equal("Thép chủ trên", bar.BarDescription);
            Assert.Equal(1, bar.SttCad);
            Assert.True(bar.DimA > 0);
            Assert.True(bar.DimB > 0);
            Assert.True(bar.DimC > 0);
            Assert.Equal(2.0 * bar.Diameter, bar.DimR);
        }

        // 2. Main Bottom Bars
        Assert.NotEmpty(layout.MainBottomBars);
        foreach (var bar in layout.MainBottomBars)
        {
            Assert.Equal("15a", bar.ShapeCode);
            Assert.Equal("2", bar.BarMark);
            Assert.Equal("Thép chủ dưới", bar.BarDescription);
            Assert.Equal(2, bar.SttCad);
            Assert.True(bar.DimA > 0);
            Assert.True(bar.DimB > 0);
            Assert.True(bar.DimC > 0);
            Assert.Equal(2.0 * bar.Diameter, bar.DimR);
        }

        // 3. Extra Top Bars: Gối biên (0, 2) có móc L (05a), Gối giữa (1) là thẳng (00)
        var topSupp0 = layout.ExtraTopBars.Where(b => b.HostSupportIndex == 0).ToList();
        Assert.NotEmpty(topSupp0);
        foreach (var bar in topSupp0)
        {
            Assert.Equal("05a", bar.ShapeCode);
            Assert.Equal("3.1.1", bar.BarMark);
            Assert.Contains("Gia cường gối 1", bar.BarDescription);
            Assert.Equal(3, bar.SttCad);
            Assert.True(bar.DimA > 0);
            Assert.True(bar.DimB > 0);
        }

        var topSupp1 = layout.ExtraTopBars.Where(b => b.HostSupportIndex == 1).ToList();
        Assert.NotEmpty(topSupp1);
        foreach (var bar in topSupp1)
        {
            Assert.Equal("00", bar.ShapeCode);
            Assert.Equal("3.2.1", bar.BarMark);
            Assert.Contains("Gia cường gối 2", bar.BarDescription);
            Assert.Equal(3, bar.SttCad);
            Assert.True(bar.DimA > 0);
            Assert.Equal(0.0, bar.DimB);
        }

        // 4. Extra Bottom Bars (Nhịp 1, Nhịp 2: Thanh thẳng Shape 00)
        var botSpan0 = layout.ExtraBottomBars.Where(b => b.HostSpanIndex == 0).ToList();
        Assert.NotEmpty(botSpan0);
        foreach (var bar in botSpan0)
        {
            Assert.Equal("00", bar.ShapeCode);
            Assert.Equal("4.1.1", bar.BarMark);
            Assert.Contains("Gia cường nhịp 1", bar.BarDescription);
            Assert.Equal(4, bar.SttCad);
            Assert.True(bar.DimA > 0);
        }

        // 5. Side Bars (Cốt sườn: Shape 00)
        Assert.NotEmpty(layout.SideBars);
        foreach (var bar in layout.SideBars)
        {
            Assert.Equal("00", bar.ShapeCode);
            Assert.Equal($"5.{bar.HostSpanIndex + 1}.1", bar.BarMark);
            // Spans with the same side bars share them: "nhịp 1–2" when they run on through a support.
            Assert.StartsWith($"Cốt giá nhịp {bar.HostSpanIndex + 1}", bar.BarDescription);
            Assert.EndsWith("lớp 1", bar.BarDescription);
            Assert.Equal(5, bar.SttCad);
            Assert.True(bar.DimA > 0);
        }

        // 6. Stirrup Zones
        Assert.NotEmpty(layout.StirrupZones);
        var closedZones = layout.StirrupZones.Where(z => z.StirrupType == KataStirrupShapeType.ClosedHoop).ToList();
        Assert.NotEmpty(closedZones);
        foreach (var z in closedZones)
        {
            Assert.Equal("41", z.ShapeCode);
            Assert.Equal("d1", z.BarMark);
            Assert.Equal(250.0, z.DimA); // 300 - 2*25
            Assert.Equal(550.0, z.DimB); // 600 - 2*25
        }

        var capSets = layout.BarSets.Where(b => b.Role == KataBarRole.StirrupCap).ToList();
        Assert.NotEmpty(capSets);
        Assert.All(capSets, b => Assert.StartsWith("d2.1.", b.BarMark));
        var tieSets = layout.BarSets.Where(b => b.Role == KataBarRole.CrossTie && b.ZoneName != "Cốt giá").ToList();
        Assert.NotEmpty(tieSets);
        Assert.All(tieSets, b => Assert.StartsWith("d3.1.", b.BarMark));

        // 7. Individual Stirrups
        var closedStirrups = layout.IndividualStirrups.Where(s => s.Role == KataBarRole.StirrupClosed).ToList();
        Assert.NotEmpty(closedStirrups);
        foreach (var s in closedStirrups)
        {
            Assert.Equal("41", s.ShapeCode);
            Assert.Equal("d1", s.BarMark);
            Assert.Equal("Đai chữ nhật kín", s.BarDescription);
            Assert.Equal(6, s.SttCad);
            Assert.Equal(250.0, s.DimA);
            Assert.Equal(550.0, s.DimB);
        }
    }
}
