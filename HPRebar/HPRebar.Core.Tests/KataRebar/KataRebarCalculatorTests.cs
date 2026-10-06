using System;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public class KataRebarCalculatorTests
{
    [Fact]
    public void Calculate_NullSpec_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => KataRebarCalculator.Calculate(null!));
    }

    [Fact]
    public void Calculate_EmptySpans_ReturnsInvalidLayoutWithWarning()
    {
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_EMPTY",
            Width = 300,
            Height = 500,
            Spans = Array.Empty<KataSpanRebarSpec>()
        };

        var result = KataRebarCalculator.Calculate(spec);

        Assert.False(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("no spans"));
    }

    [Fact]
    public void Calculate_NonPositiveDimensions_ReturnsWarning()
    {
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_ZERO",
            Width = 0,
            Height = -500,
            Spans = new[] { new KataSpanRebarSpec { SpanIndex = 0, Length = 4000 } }
        };

        var result = KataRebarCalculator.Calculate(spec);

        Assert.False(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("must be strictly positive"));
    }

    [Fact]
    public void Calculate_SingleSpanBeam_GeneratesContinuousBarsAndStirrupsCorrectly()
    {
        // 1 span beam: Column 400mm + Clear Span 6000mm + Column 400mm = 6800mm total
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_SINGLE",
            Width = 300.0,
            Height = 600.0,
            CoverStirrup = 25.0,
            TopContinuous = new KataBarItem(3, 20.0), // 3f20 top
            BottomContinuous = new KataBarItem(3, 20.0), // 3f20 bottom
            GlobalStirrup = new KataStirrupSpec
            {
                Diameter = 8.0,
                SupportSpacing = 100.0,
                MidspanSpacing = 200.0
            },
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 6000.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        Assert.True(result.IsValid);
        Assert.Equal("B_SINGLE", result.BeamName);

        // 1. Continuous Top Bars
        Assert.Equal(3, result.MainTopBars.Count);
        foreach (var bar in result.MainTopBars)
        {
            Assert.Equal(KataBarRole.MainTop, bar.Role);
            Assert.Equal(20.0, bar.Diameter);
            Assert.Equal(HookAngle.Hook90, bar.StartHookAngle);
            Assert.Equal(HookAngle.Hook90, bar.EndHookAngle);

            // Polyline points: 4 vertices (HookDown -> TopCornerStart -> TopCornerEnd -> HookDown)
            Assert.Equal(4, bar.Polyline.Points.Count);
            var pts = bar.Polyline.Points;

            // The 400 mm column cannot hold 40d = 800 straight: the bar runs to the far face (centre 50 mm
            // from it, never less in an end column) and bends down with a leg supplying the rest: 800 − 350 → 450.
            Assert.Equal(50.0, pts[0].X);
            Assert.Equal(50.0, pts[1].X);
            Assert.Equal(6750.0, pts[2].X);
            Assert.Equal(6750.0, pts[3].X);

            // Z: top elevation is 0. Z bar = 0 - 25 - 8 - 10 = -43 mm
            Assert.Equal(-43.0, pts[1].Z);
            Assert.Equal(-43.0, pts[2].Z);
            // Hook bends downwards
            Assert.True(pts[0].Z < pts[1].Z);
            Assert.True(pts[3].Z < pts[2].Z);

            Assert.Equal(450.0, bar.StartHookLength, 6);
            Assert.Equal(450.0, bar.EndHookLength, 6);
        }

        // 2. Transverse Y centering of 3 top bars
        // y0 = -150 + 25 + 8 + 10 = -107 mm, yn = +107 mm, middle = 0 mm
        Assert.Equal(-107.0, result.MainTopBars[0].TransverseY);
        Assert.Equal(0.0, result.MainTopBars[1].TransverseY);
        Assert.Equal(107.0, result.MainTopBars[2].TransverseY);

        // 3. Continuous Bottom Bars
        Assert.Equal(3, result.MainBottomBars.Count);
        foreach (var bar in result.MainBottomBars)
        {
            Assert.Equal(KataBarRole.MainBottom, bar.Role);
            Assert.Equal(HookAngle.Hook90, bar.StartHookAngle);
            Assert.Equal(HookAngle.Hook90, bar.EndHookAngle);
            Assert.Equal(4, bar.Polyline.Points.Count);
            var pts = bar.Polyline.Points;

            // Z: bot elevation is -600. Z bar = -600 + 25 + 8 + 10 = -557 mm
            Assert.Equal(-557.0, pts[1].Z);
            Assert.Equal(-557.0, pts[2].Z);
            // Hook bends upwards
            Assert.True(pts[0].Z > pts[1].Z);
            Assert.True(pts[3].Z > pts[2].Z);

            // 30d = 600 needs a 300 mm leg (15d) that overlaps the 450 mm top leg (room 514), so the bottom leg
            // moves inboard by (20 + 20)/2 + 25 = 45: 600 − (400 − 50 − 45) = 295, raised to the 15d = 300 minimum.
            Assert.Equal(95.0, pts[0].X, 6);
            Assert.Equal(6705.0, pts[3].X, 6);
            Assert.Equal(300.0, bar.StartHookLength, 6);
            Assert.Equal(300.0, bar.EndHookLength, 6);
        }

        // 4. 3-Zone Stirrups
        // Ln = 6000 mm. L1 = 1500 mm, L3 = 1500 mm, L2 = 3000 mm.
        Assert.Equal(3, result.StirrupZones.Count);
        var z1 = result.StirrupZones[0];
        var z2 = result.StirrupZones[1];
        var z3 = result.StirrupZones[2];

        Assert.Equal(0, z1.ZoneIndex);
        Assert.Equal("Gối trái", z1.ZoneName);
        Assert.Equal(100.0, z1.LabelSpacing);
        // Start station = 400 + 50 = 450 mm
        Assert.Equal(450.0, z1.StartStationX);
        Assert.True(z1.Count > 0);

        Assert.Equal(1, z2.ZoneIndex);
        Assert.Equal("Giữa nhịp", z2.ZoneName);
        Assert.Equal(200.0, z2.LabelSpacing);

        Assert.Equal(2, z3.ZoneIndex);
        Assert.Equal("Gối phải", z3.ZoneName);
        Assert.Equal(100.0, z3.LabelSpacing);
        // End station = 6400 - 50 = 6350 mm
        Assert.Equal(6350.0, z3.EndStationX);

        // Out to out dimensions: 300 - 50 = 250, 600 - 50 = 550
        Assert.Equal(250.0, z1.OutToOutWidth);
        Assert.Equal(550.0, z1.OutToOutHeight);

        // Individual stirrup curves generated
        Assert.NotEmpty(result.IndividualStirrups);
        Assert.Equal(z1.Count + z2.Count + z3.Count, result.IndividualStirrups.Count);

        // Total steel weight must be positive
        Assert.True(result.TotalSteelWeightKg > 0.0);
    }

    [Fact]
    public void Calculate_MultiSpanContinuousBeam_B01KataSample_CalculatesAllReinforcements()
    {
        // Construct B01 specification matching the parsed sample from sheet Dam
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B01",
            Width = 500.0,
            Height = 1100.0,
            CoverStirrup = 25.0,
            TopCutoffRatioLayer1 = 0.25,
            TopCutoffRatioLayer2 = 0.20,
            CutoffOriginLayer1 = KataCutoffOrigin.FromColumnFace,
            CutoffOriginLayer2 = KataCutoffOrigin.FromColumnCenter,
            TopContinuous = new KataBarItem(6, 25.0),
            BottomContinuous = new KataBarItem(6, 25.0),
            GlobalStirrup = new KataStirrupSpec
            {
                Diameter = 10.0,
                SupportSpacing = 150.0,
                MidspanSpacing = 200.0
            },
            GlobalSideBars = new[]
            {
                new KataBarItem(2, 12.0, 1),
                new KataBarItem(2, 12.0, 2)
            },
            Supports = new[]
            {
                new KataSupportRebarSpec
                {
                    SupportIndex = 0,
                    ColumnWidth = 400.0,
                    TopExtraLayer1 = new[] { new KataBarItem(6, 25.0) },
                    TopExtraLayer2 = new[] { new KataBarItem(6, 20.0) }
                },
                new KataSupportRebarSpec
                {
                    SupportIndex = 1,
                    ColumnWidth = 400.0,
                    TopExtraLayer1 = new[] { new KataBarItem(6, 25.0) },
                    TopExtraLayer2 = new[] { new KataBarItem(6, 20.0) },
                    TopExtraLayer3 = new[] { new KataBarItem(6, 20.0) }
                },
                new KataSupportRebarSpec
                {
                    SupportIndex = 2,
                    ColumnWidth = 400.0,
                    TopExtraLayer1 = new[] { new KataBarItem(2, 20.0), new KataBarItem(2, 16.0) }
                }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec
                {
                    SpanIndex = 0,
                    Length = 10400.0,
                    BottomExtraLayer1 = new[] { new KataBarItem(6, 25.0) },
                    BottomExtraLayer2 = new[] { new KataBarItem(2, 20.0) },
                    SideBars = new[] { new KataBarItem(0, 12.0) }, // override to 0 side bars
                    StirrupOverride = new KataStirrupSpec
                    {
                        Diameter = 10.0,
                        SupportSpacing = 100.0,
                        MidspanSpacing = 200.0
                    }
                },
                new KataSpanRebarSpec
                {
                    SpanIndex = 1,
                    Length = 6500.0,
                    BottomExtraLayer1 = new[] { new KataBarItem(2, 20.0) }
                }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        AssertOnlyAnchorageWarnings(result);

        // 1. Continuous Main Bars
        Assert.Equal(6, result.MainTopBars.Count);
        Assert.Equal(6, result.MainBottomBars.Count);

        // Total beam length: 400 + 10400 + 400 + 6500 + 400 = 18,100 mm. The 400 mm end columns cannot hold
        // 40d straight, so the bars stop at the far faces with their centre 50 inside (25 + 10 + 25/2 = 47.5 is less).
        double expectedStart = 50.0;
        double expectedEnd = 18100.0 - 50.0;
        Assert.Equal(expectedStart, result.MainTopBars[0].Polyline.Points[1].X);
        Assert.Equal(expectedEnd, result.MainTopBars[0].Polyline.Points[2].X);

        // 2. Extra Top Bars over Support 0 (Exterior), Support 1 (Interior), Support 2 (Exterior)
        // Support 0: 6f25 (Layer 1) + 6f20 (Layer 2) = 12 bars
        // Support 1: 6f25 (Layer 1) + 6f20 (Layer 2) + 6f20 (Layer 3) = 18 bars
        // Support 2: 2f20 + 2f16 = 4 bars
        // Total extra top bars = 12 + 18 + 4 = 34 bars
        Assert.Equal(34, result.ExtraTopBars.Count);

        // Check Support 0 exterior hooks: start hook angle is Hook90, end hook is None
        var supp0Bars = result.ExtraTopBars.Where(b => b.HostSupportIndex == 0).ToList();
        Assert.Equal(12, supp0Bars.Count);
        foreach (var b in supp0Bars)
        {
            Assert.Equal(HookAngle.Hook90, b.StartHookAngle);
            Assert.Equal(HookAngle.None, b.EndHookAngle);
            Assert.Equal(3, b.Polyline.Points.Count);
        }

        // Check Support 1 interior straight bars: both start and end hooks are None
        var supp1Bars = result.ExtraTopBars.Where(b => b.HostSupportIndex == 1).ToList();
        Assert.Equal(18, supp1Bars.Count);
        foreach (var b in supp1Bars)
        {
            Assert.Equal(HookAngle.None, b.StartHookAngle);
            Assert.Equal(HookAngle.None, b.EndHookAngle);
            Assert.Equal(2, b.Polyline.Points.Count); // straight segment
        }

        // Support 1 (10800-11200): every layer reaches H5 0.25 × its side's span from the face — 2600 into the
        // 10400 span (8200), 1625 → 1650 into the 6500 span (12850) — then the stagger adds 500 per outer layer:
        // layer 3 8200 / 12850, layer 2 7700 / 13350, layer 1 7200 / 13850.
        var supp1Layer1 = supp1Bars.First(b => b.Layer == 1);
        Assert.Equal(7200.0, supp1Layer1.Polyline.Points[0].X);
        Assert.Equal(13850.0, supp1Layer1.Polyline.Points[1].X);
        Assert.Equal(6650.0, supp1Layer1.Polyline.TotalLength);
        var supp1Layer2 = supp1Bars.First(b => b.Layer == 2);
        Assert.Equal((7700.0, 13350.0), (supp1Layer2.Polyline.Points[0].X, supp1Layer2.Polyline.Points[1].X));
        var supp1Layer3 = supp1Bars.First(b => b.Layer == 3);
        Assert.Equal((8200.0, 12850.0), (supp1Layer3.Polyline.Points[0].X, supp1Layer3.Polyline.Points[1].X));

        // 3. Extra Bottom Bars in Spans
        // Span 0: 6f25 (L1) + 2f20 (L2) = 8 bars
        // Span 1: 2f20 (L1) = 2 bars
        // Total extra bottom bars = 10
        Assert.Equal(10, result.ExtraBottomBars.Count);
        var span0Bot = result.ExtraBottomBars.Where(b => b.HostSpanIndex == 0).ToList();
        Assert.Equal(8, span0Bot.Count);

        // Span 0 clear length = 10400. Row 17 keeps min(H3 0.2 × 10400 = 2080 → 2100 from the centre = 1900 from
        // the face, 10400 / 6 = 1733 → nearest 50: 1750) = 1750 free; row 18 (span0Bot[0]) G1 nearer: 1250.
        // Start face = 400. End face = 10800.
        double expectedBotStart = 400.0 + 1250.0;
        double expectedBotEnd = 10800.0 - 1250.0;
        Assert.Equal(expectedBotStart, span0Bot[0].Polyline.Points[0].X, precision: 1);
        Assert.Equal(expectedBotEnd, span0Bot[0].Polyline.Points[1].X, precision: 1);

        // 4. Side Bars
        // Span 0 has override "0f12" -> 0 side bars in Span 0!
        // Span 1 inherits GlobalSideBars (2 layers x 2 bars = 4 side bars)
        var span0Side = result.SideBars.Where(b => b.HostSpanIndex == 0).ToList();
        Assert.Empty(span0Side);
        var span1Side = result.SideBars.Where(b => b.HostSpanIndex == 1).ToList();
        Assert.Equal(4, span1Side.Count);
        // Side bar extends 10*dia = 120 mm into left and right supports (400 mm columns) per Kata settings
        Assert.Equal(11080.0, span1Side[0].Polyline.Points[0].X);
        Assert.Equal(17820.0, span1Side[0].Polyline.Points[1].X);
        Assert.Equal(6740.0, span1Side[0].DimA); // 6500 + 2 * 120

        // 5. Stirrup Zones
        // 2 spans, 3 zones each = 6 zones
        Assert.Equal(6, result.StirrupZones.Count);
        // Span 0 has spacing override a100/200
        Assert.Equal(100.0, result.StirrupZones[0].LabelSpacing);
        Assert.Equal(200.0, result.StirrupZones[1].LabelSpacing);
        Assert.Equal(100.0, result.StirrupZones[2].LabelSpacing);

        // Total bar count check
        Assert.True(result.TotalBarCount > 50);
        Assert.True(result.TotalSteelWeightKg > 100.0);
    }

    [Fact]
    public void Calculate_CantileverOverhangBeam_AnchorsTopAndLapsBottomIntoTheConsole()
    {
        // Left cantilever: Support 0 ColumnWidth = 0.
        // Cantilever Span 0 = 2000 mm.
        // Interior Column Support 1 = 400 mm.
        // Span 1 = 5000 mm.
        // Right Column Support 2 = 400 mm.
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_CANTILEVER",
            Width = 300.0,
            Height = 500.0,
            CoverStirrup = 25.0,
            TopContinuous = new KataBarItem(2, 20.0),
            BottomContinuous = new KataBarItem(2, 20.0),
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 0.0 }, // Cantilever tip
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 2, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 2000.0 }, // Cantilever span
                new KataSpanRebarSpec { SpanIndex = 1, Length = 5000.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        AssertOnlyAnchorageWarnings(result);

        // Top Continuous: stops a (25 + 10 + 10 = 45) short of the cantilever tip and hooks down (Kata B01 console)
        Assert.Equal(2, result.MainTopBars.Count);
        var topBar = result.MainTopBars[0];
        Assert.Equal(45.0, topBar.Polyline.Points[0].X);
        Assert.Equal(HookAngle.Hook90, topBar.StartHookAngle);
        Assert.Equal(HookAngle.Hook90, topBar.EndHookAngle);

        // Bottom Continuous: laps G3·d = 600 into the console from the far face of Support 1 (X = 1800 mm), no hook at
        // start; the console has its own bars from a inside the tip to a inside Support 1's far face, bent up 10d there.
        Assert.Equal(4, result.MainBottomBars.Count);
        var console = result.MainBottomBars.Where(b => b.Polyline.Points.Min(p => p.X) < 100).ToList();
        Assert.Equal(2, console.Count);
        Assert.All(console, b => { Assert.Equal(45.0, b.Polyline.Points.Min(p => p.X), 1); Assert.Equal(2400.0 - 45.0, b.Polyline.Points.Max(p => p.X), 1); Assert.Equal(HookAngle.Hook90, b.EndHookAngle); });
        var botBar = result.MainBottomBars.First(b => b.Polyline.Points.Min(p => p.X) > 1000);
        Assert.Equal(1800.0, botBar.Polyline.Points[0].X);
        Assert.Equal(HookAngle.None, botBar.StartHookAngle);
        Assert.Equal(HookAngle.Hook90, botBar.EndHookAngle); // Right end column has hook

        // Cantilever Stirrup Zone (Span 0) is Console uniform spacing
        var span0Stirrups = result.StirrupZones.Where(z => z.SpanIndex == 0).ToList();
        Assert.Single(span0Stirrups);
        Assert.Equal("Console", span0Stirrups[0].ZoneName);
    }

    [Fact]
    public void Calculate_MultiLayerTopAdditionalBars_OffsetsElevationsAndCalculatesRatios()
    {
        // 4 top extra bar layers over Support 1 (interior column)
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_LAYERS",
            Width = 400.0,
            Height = 800.0,
            CoverStirrup = 25.0,
            TopContinuous = new KataBarItem(4, 25.0),
            TopCutoffRatioLayer1 = 0.333,
            TopCutoffRatioLayer2 = 0.250,
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
                new KataSupportRebarSpec
                {
                    SupportIndex = 1,
                    ColumnWidth = 500.0,
                    TopExtraLayer1 = new[] { new KataBarItem(4, 25.0) },
                    TopExtraLayer2 = new[] { new KataBarItem(4, 25.0) },
                    TopExtraLayer3 = new[] { new KataBarItem(4, 20.0) },
                    TopExtraLayer4 = new[] { new KataBarItem(4, 20.0) }
                },
                new KataSupportRebarSpec { SupportIndex = 2, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 6000.0 },
                new KataSpanRebarSpec { SpanIndex = 1, Length = 6000.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        AssertOnlyAnchorageWarnings(result);

        // 4 layers x 4 bars = 16 extra top bars over Support 1
        var supp1Bars = result.ExtraTopBars.Where(b => b.HostSupportIndex == 1).ToList();
        Assert.Equal(16, supp1Bars.Count);

        var layer1 = supp1Bars.Where(b => b.Layer == 1).ToList();
        var layer2 = supp1Bars.Where(b => b.Layer == 2).ToList();
        var layer3 = supp1Bars.Where(b => b.Layer == 3).ToList();
        var layer4 = supp1Bars.Where(b => b.Layer == 4).ToList();

        Assert.Equal(4, layer1.Count);
        Assert.Equal(4, layer2.Count);
        Assert.Equal(4, layer3.Count);
        Assert.Equal(4, layer4.Count);

        // Vertical Z elevations strictly decrease downwards: Layer 1 > Layer 2 > Layer 3 > Layer 4
        double z1 = layer1[0].Polyline.Points[0].Z;
        double z2 = layer2[0].Polyline.Points[0].Z;
        double z3 = layer3[0].Polyline.Points[0].Z;
        double z4 = layer4[0].Polyline.Points[0].Z;

        // Row 13 shares the main bars' level; rows 14-16 stack below at a clear gap of max(30, d).
        Assert.Equal(result.MainTopBars[0].Polyline.Points[1].Z, z1, 6);
        Assert.Equal(25.0 / 2 + 30.0 + 25.0 / 2, z1 - z2, 6);
        Assert.Equal(25.0 / 2 + 30.0 + 20.0 / 2, z2 - z3, 6);
        Assert.Equal(20.0 / 2 + 30.0 + 20.0 / 2, z3 - z4, 6);

        // Cutoff extension lengths: Layer 1 (ratio = 0.333) should be longer than Layer 2 (ratio = 0.25)
        Assert.True(layer1[0].Polyline.TotalLength > layer2[0].Polyline.TotalLength);
    }

    [Fact]
    public void Calculate_InnerStirrups_FollowTheOuterZonesAsFlatBarSets()
    {
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_STIRRUP_TYPES",
            Width = 400.0,
            Height = 600.0,
            CoverStirrup = 25.0,
            TopContinuous = new KataBarItem(4, 20.0),
            BottomContinuous = new KataBarItem(4, 20.0),
            // I8 = 1: the inner stirrups go beside every outer hoop.
            GlobalStirrup = new KataStirrupSpec { Diameter = 10.0, SupportSpacing = 150.0, MidspanSpacing = 200.0, TieSpacingMode = KataTieSpacingMode.LikeHoops },
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec
                {
                    SpanIndex = 0,
                    Length = 6000.0,
                    InnerStirrups = new[]
                    {
                        new KataStirrupBranchSpec(KataStirrupShapeType.ClosedHoop, "1-4", "C25"), // the outer hoop itself
                        new KataStirrupBranchSpec(KataStirrupShapeType.CapStirrup, "2-3", "C26"),
                        new KataStirrupBranchSpec(KataStirrupShapeType.CrossTie, "2", "C27")
                    }
                }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        Assert.True(result.IsValid, string.Join(" | ", result.Warnings));
        Assert.Equal(3, result.StirrupZones.Count);
        Assert.All(result.StirrupZones, z => Assert.Equal(KataStirrupShapeType.ClosedHoop, z.StirrupType));

        var caps = result.BarSets.Where(b => b.Role == KataBarRole.StirrupCap).ToList();
        var ties = result.BarSets.Where(b => b.Role == KataBarRole.CrossTie).ToList();
        Assert.Equal(3, caps.Count);
        Assert.Equal(3, ties.Count);
        Assert.Equal(result.StirrupZones.Sum(z => z.Count), caps.Sum(c => c.Count));
        // The U is open at the top, its legs turned in and down (bends, no hooks); the C hooks round its bars.
        Assert.Equal(8, caps[0].Shape.Points.Count);
        Assert.Equal(2, ties[0].Shape.Points.Count);
        Assert.Equal((0, 180), (caps[0].HookAngle, ties[0].HookAngle));
    }

    [Fact]
    public void Calculate_AllPolylinesSimplified_ProtectsAgainstRevitShortCurveCrashes()
    {
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_SIMPLIFY",
            Width = 300.0,
            Height = 500.0,
            TopContinuous = new KataBarItem(2, 20.0),
            BottomContinuous = new KataBarItem(2, 20.0),
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 4000.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        // Check all bar polylines across all categories
        var allCurves = result.MainTopBars
            .Concat(result.MainBottomBars)
            .Concat(result.ExtraTopBars)
            .Concat(result.ExtraBottomBars)
            .Concat(result.SideBars)
            .Concat(result.IndividualStirrups);

        foreach (var curve in allCurves)
        {
            var pts = curve.Polyline.Points;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                double dist = pts[i].DistanceTo(pts[i + 1]);
                Assert.True(dist >= 1.0, $"Segment from vertex {i} to {i + 1} is shorter than 1.0mm: {dist}");
            }
        }
    }

    [Fact]
    public void Calculate_RightCantileverOverhangBeam_AnchorsTopAndLapsBottomIntoTheConsole()
    {
        // Span 0 normal (5000 mm), Span 1 cantilever (1800 mm)
        // Support 0: Column 400 mm
        // Support 1: Column 400 mm
        // Support 2: Column 0 mm (cantilever tip)
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_RIGHT_CANT",
            Width = 300.0,
            Height = 500.0,
            CoverStirrup = 25.0,
            TopContinuous = new KataBarItem(3, 20.0),
            BottomContinuous = new KataBarItem(3, 20.0),
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 2, ColumnWidth = 0.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 5000.0 },
                new KataSpanRebarSpec { SpanIndex = 1, Length = 1800.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        AssertOnlyAnchorageWarnings(result);

        // Top Continuous: reaches right cantilever end with 90° hook down
        // Total length = 400 + 5000 + 400 + 1800 + 0 = 7600 mm
        Assert.Equal(3, result.MainTopBars.Count);
        // Left end in a 400 mm column: bar centre 50 from the far face (25 + 10 + 10 = 45 is less); the console tip keeps 45.
        var topBar = result.MainTopBars[0];
        Assert.Equal(50.0, topBar.Polyline.Points[1].X);
        Assert.Equal(7600.0 - 45.0, topBar.Polyline.Points[2].X);
        Assert.Equal(HookAngle.Hook90, topBar.StartHookAngle);
        Assert.Equal(HookAngle.Hook90, topBar.EndHookAngle);

        // Bottom Continuous: starts at Support 0 (exterior hook 90°), stops at Support 1 right face (X = 5800 mm, no hook);
        // the console has bottom bars of its own, from inside Support 1 (leg up 10d) to a short of the tip.
        Assert.Equal(6, result.MainBottomBars.Count);
        var console = result.MainBottomBars.Where(b => b.Polyline.Points.Min(p => p.X) > 5000).ToList();
        Assert.Equal(3, console.Count);
        Assert.All(console, b => { Assert.Equal(5400.0 + 45.0, b.Polyline.Points.Min(p => p.X), 1); Assert.Equal(7600.0 - 45.0, b.Polyline.Points.Max(p => p.X), 1); });
        // Its leg would overlap the top-bar leg, so it moves inboard by (20 + 20)/2 + 25 = 45 mm.
        var botBar = result.MainBottomBars[0];
        Assert.Equal(95.0, botBar.Polyline.Points[1].X);
        Assert.Equal(6000.0, botBar.Polyline.Points[2].X); // 5400 + G3·d 600 into the console
        Assert.Equal(HookAngle.Hook90, botBar.StartHookAngle);
        Assert.Equal(HookAngle.None, botBar.EndHookAngle);
    }

    [Fact]
    public void Calculate_BothSidesCantilever_HandlesDoubleOverhang()
    {
        // Support 0 (0mm, cant) -> Span 0 (1500mm cant) -> Support 1 (400mm) -> Span 1 (6000mm) -> Support 2 (400mm) -> Span 2 (1500mm cant) -> Support 3 (0mm, cant)
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_DOUBLE_CANT",
            Width = 350.0,
            Height = 600.0,
            CoverStirrup = 25.0,
            TopContinuous = new KataBarItem(2, 22.0),
            BottomContinuous = new KataBarItem(2, 22.0),
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 0.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 2, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 3, ColumnWidth = 0.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 1500.0 },
                new KataSpanRebarSpec { SpanIndex = 1, Length = 6000.0 },
                new KataSpanRebarSpec { SpanIndex = 2, Length = 1500.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        // Consoles as deep as the span: the lapped bottom bars lie on each other, which is said.
        Assert.Empty(result.Blocking);
        Assert.Equal(2, result.Warnings.Count(w => w.Contains("gần cùng đáy")));

        // Top Continuous: runs from a (25 + 10 + 11 = 46) inside the left tip to a inside the right one
        // Total = 0 + 1500 + 400 + 6000 + 400 + 1500 + 0 = 9800 mm
        var top = result.MainTopBars[0];
        Assert.Equal(46.0, top.Polyline.Points[1].X);
        Assert.Equal(9754.0, top.Polyline.Points[2].X);
        Assert.Equal(HookAngle.Hook90, top.StartHookAngle);
        Assert.Equal(HookAngle.Hook90, top.EndHookAngle);

        // Bottom Continuous: only spans between interior Support 1 left face (X = 1500) and Support 2 right face (X = 7900)
        // With NO hooks at either end
        var bot = result.MainBottomBars.First(b => b.Polyline.Points[0].X > 1000 && b.Polyline.Points[0].X < 2000);
        Assert.Equal(1240.0, bot.Polyline.Points[0].X); // 1900 − G3·d 660 into the left console
        Assert.Equal(8560.0, bot.Polyline.Points[1].X); // 7900 + 660 into the right one
        Assert.Equal(HookAngle.None, bot.StartHookAngle);
        Assert.Equal(HookAngle.None, bot.EndHookAngle);
    }

    [Fact]
    public void Calculate_UnequalAdjacentSpans_InteriorSupportTopCutoffUsesEachSidesSpan()
    {
        // Support 1 between Span 0 (8000 mm) and Span 1 (4000 mm)
        // Each side reaches 0.25 × its own span (Kata drawing of T2-DY7): 2000 left, 1000 right
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_UNEQUAL",
            Width = 300.0,
            Height = 600.0,
            TopCutoffRatioLayer1 = 0.25,
            CutoffOriginLayer1 = KataCutoffOrigin.FromColumnFace,
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
                new KataSupportRebarSpec
                {
                    SupportIndex = 1,
                    ColumnWidth = 400.0,
                    TopExtraLayer1 = new[] { new KataBarItem(2, 20.0) }
                },
                new KataSupportRebarSpec { SupportIndex = 2, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 8000.0 },
                new KataSpanRebarSpec { SpanIndex = 1, Length = 4000.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        Assert.True(result.IsValid);
        var supp1Bars = result.ExtraTopBars.Where(b => b.HostSupportIndex == 1).ToList();
        Assert.Equal(2, supp1Bars.Count);

        // Support 1 left face = 400 + 8000 = 8400. Right face = 8800.
        // Xstart = 8400 - 2000 = 6400 mm. Xend = 8800 + 1000 = 9800 mm.
        var bar = supp1Bars[0];
        Assert.Equal(6400.0, bar.Polyline.Points[0].X);
        Assert.Equal(9800.0, bar.Polyline.Points[1].X);
        Assert.Equal(3400.0, bar.Polyline.TotalLength); // 2000 + 400 + 2000
    }

    [Fact]
    public void Calculate_TransverseYCentering_GuaranteesSymmetryAcrossWidth()
    {
        // Test counts 1, 2, 3, 4, 5, 6
        double width = 400.0;
        double cover = 25.0;
        double stirrup = 8.0;
        double barDia = 20.0;

        for (int count = 1; count <= 6; count++)
        {
            var positions = KataRebarCalculator.ComputeTransverseYPositions(width, cover, stirrup, barDia, count);
            Assert.Equal(count, positions.Count);

            // Symmetrical: Y[i] + Y[n - 1 - i] == 0 within tolerance
            for (int i = 0; i < count; i++)
            {
                Assert.Equal(-positions[count - 1 - i], positions[i], precision: 6);
            }

            // If count is odd, middle element must be exactly 0
            if (count % 2 != 0)
            {
                Assert.Equal(0.0, positions[count / 2], precision: 6);
            }
        }
    }

    [Fact]
    public void Calculate_ShortSpanStirrupDistribution_ValidAndNonOverlapping()
    {
        // Short clear span of 1200 mm
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_SHORT",
            Width = 250.0,
            Height = 400.0,
            GlobalStirrup = new KataStirrupSpec
            {
                Diameter = 8.0,
                SupportSpacing = 100.0,
                MidspanSpacing = 150.0
            },
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 300.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 300.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 1200.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.StirrupZones);

        // Check that all stirrup stations are strictly monotonically increasing
        var allStations = result.StirrupZones
            .SelectMany(z => z.Stations)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        for (int i = 0; i < allStations.Count - 1; i++)
        {
            double spacing = allStations[i + 1] - allStations[i];
            Assert.True(spacing > 0.0, $"Stirrup stations overlapped at {allStations[i]}");
        }
    }

    [Fact]
    public void Calculate_MainBarsLongerThanAStockBar_StayWholeWithoutAWarning()
    {
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_LONG_CONTINUOUS",
            Width = 300.0,
            Height = 600.0,
            CoverStirrup = 25.0,
            TopContinuous = new KataBarItem(2, 20.0),
            BottomContinuous = new KataBarItem(2, 20.0),
            GlobalStirrup = new KataStirrupSpec { Diameter = 8.0, SupportSpacing = 100.0, MidspanSpacing = 200.0 },
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 2, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 6000.0 },
                new KataSpanRebarSpec { SpanIndex = 1, Length = 6000.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        Assert.Empty(result.Blocking);
        Assert.NotEmpty(result.MainTopBars);
        Assert.NotEmpty(result.MainBottomBars);
        Assert.True(result.MainTopBars[0].TotalLength > 11700.0);
        Assert.True(result.MainBottomBars[0].TotalLength > 11700.0);
        // A design model: one bar from end to end, the laps belong to the shop drawings.
        Assert.All(result.MainTopBars.Concat(result.MainBottomBars), b => Assert.Equal(4, b.Polyline.Points.Count));
        Assert.DoesNotContain(result.Warnings, w => w.Contains("cây thép") || w.Contains("nối chồng"));
    }

    [Fact]
    public void Calculate_SpanSideBarOverride_2f12_GivesTwoLayers()
    {
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_SIDE_OVERRIDE",
            Width = 300.0,
            Height = 750.0,
            TopContinuous = new KataBarItem(2, 20.0),
            BottomContinuous = new KataBarItem(2, 20.0),
            GlobalSideBars = new[] { new KataBarItem(2, 12.0), new KataBarItem(2, 12.0) }, // 2 layers
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec
                {
                    SpanIndex = 0,
                    Length = 6000.0,
                    SideBars = new[] { new KataBarItem(2, 12.0) } // row 20 "2f12": two layers, as Kata draws "2x2Ø12"
                }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        Assert.True(result.IsValid);
        // 2 layers, a bar on each face
        Assert.Equal(4, result.SideBars.Count);
        Assert.Equal(new[] { 1, 1, 2, 2 }, result.SideBars.Select(b => b.Layer).OrderBy(l => l).ToArray());
    }

    /// <summary>
    /// The allowed warnings are anchorage shortfalls of shallow beams, extra bar cutoffs, or a deep beam drawn
    /// without side bars.
    /// </summary>
    private static void AssertOnlyAnchorageWarnings(KataRebarLayoutResult result) =>
        Assert.All(result.Warnings, w => Assert.True(w.StartsWith("Neo thép") || w.StartsWith("Thép gia cường") || w.StartsWith("Thép chủ") || w.Contains("thiếu cốt giá"), w));
}
