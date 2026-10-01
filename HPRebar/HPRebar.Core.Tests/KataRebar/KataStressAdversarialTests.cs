using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Adversarial stress tests for KataBarNotationParser and KataRebarCalculator.
/// Tests extreme notations, geometric boundary conditions, deep/shallow beams,
/// multi-layer vertical offsets, and polyline simplification integrity.
/// </summary>
public class KataStressAdversarialTests
{
    #region 1. KataBarNotationParser Stress Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-")]
    [InlineData("*")]
    [InlineData("a")]
    [InlineData("f")]
    [InlineData("d")]
    [InlineData("x")]
    [InlineData("!")]
    [InlineData("9")]
    [InlineData("2")]
    [InlineData("20")]
    [InlineData("200")]
    [InlineData("2f")]
    [InlineData("3d")]
    [InlineData("4phi")]
    [InlineData("2f.")]
    [InlineData("2f..2")]
    [InlineData("2f-18")]
    [InlineData("2f0")]
    [InlineData("-2f18")]
    [InlineData("xyz20")]
    [InlineData("20f")]
    public void ParseSingleBar_ExtremeAndMalformedInputs_ReturnsNullSafely(string? text)
    {
        var item = KataBarNotationParser.ParseSingleBar(text);
        Assert.Null(item);
    }

    [Theory]
    [InlineData("2f18", 2, 18.0)]
    [InlineData("f10", 1, 10.0)]
    [InlineData("d12", 1, 12.0)]
    [InlineData("3d20", 3, 20.0)]
    [InlineData("4phi22", 4, 22.0)]
    [InlineData("2Ø16", 2, 16.0)]
    [InlineData("2ø16", 2, 16.0)]
    [InlineData("2Φ25", 2, 25.0)]
    [InlineData("2φ25", 2, 25.0)]
    [InlineData("2%%c14", 2, 14.0)]
    [InlineData("10f32", 10, 32.0)]
    [InlineData("2 f 18", 2, 18.0)]
    [InlineData("3  d  20", 3, 20.0)]
    [InlineData("2f18.5", 2, 18.5)]
    public void ParseSingleBar_ValidOrTolerantNotations_ParsesCorrectly(string text, int expCount, double expDia)
    {
        var item = KataBarNotationParser.ParseSingleBar(text);
        Assert.NotNull(item);
        Assert.Equal(expCount, item!.Count);
        Assert.Equal(expDia, item.Diameter);
    }

    [Fact]
    public void ParseSingleBar_HugeCountOverflow_FallsBackToOne()
    {
        // Count exceeds int.MaxValue
        var item = KataBarNotationParser.ParseSingleBar("9999999999999999999999999999f20");
        Assert.NotNull(item);
        Assert.Equal(1, item!.Count);
        Assert.Equal(20.0, item.Diameter);
    }

    [Theory]
    [InlineData(";;;2f18,,,3f20+++4d25;;;", 3)]
    [InlineData("2f18;+;,-;0;3f20", 2)]
    [InlineData(" 2f20 ; 3f22 + 4f25 , 1f18 ", 4)]
    [InlineData("2f18;invalid;0;-;3f20", 2)]
    [InlineData(";;;;", 0)]
    [InlineData("+++", 0)]
    [InlineData(",,,", 0)]
    public void ParseBarList_ComplexMixedSeparators_RobustlyExtractsValidItems(string text, int expectedCount)
    {
        var items = KataBarNotationParser.ParseBarList(text);
        Assert.Equal(expectedCount, items.Count);
    }

    [Theory]
    [InlineData("a", 150.0, 200.0, null)]
    [InlineData("a0", 150.0, 200.0, null)]
    [InlineData("a-100", 150.0, 200.0, null)]
    [InlineData("150", 150.0, 150.0, null)]
    [InlineData("@150", 150.0, 150.0, null)]
    [InlineData("a150", 150.0, 150.0, null)]
    [InlineData("a100/200", 100.0, 200.0, null)]
    [InlineData("100/200", 100.0, 200.0, null)]
    [InlineData("a100/200/50", 100.0, 200.0, 50.0)]
    [InlineData("100/200/50", 100.0, 200.0, 50.0)]
    [InlineData("a100/0", 100.0, 200.0, null)]
    [InlineData("a-100/200", 150.0, 200.0, null)]
    [InlineData("a100/200/50/300/400", 100.0, 200.0, 50.0)]
    [InlineData("///", 150.0, 200.0, null)]
    [InlineData("   ", 150.0, 200.0, null)]
    [InlineData(null, 150.0, 200.0, null)]
    [InlineData("abc/def", 150.0, 200.0, null)]
    [InlineData("@", 150.0, 200.0, null)]
    public void ParseStirrupSpacing_AdversarialInputs_ReturnsExpectedOrFallback(
        string? text, double expDense, double expSparse, double? expEnd)
    {
        var (s1, s2, s3) = KataBarNotationParser.ParseStirrupSpacing(text, defaultDense: 150.0, defaultSparse: 200.0);
        Assert.Equal(expDense, s1);
        Assert.Equal(expSparse, s2);
        Assert.Equal(expEnd, s3);
    }

    [Theory]
    [InlineData("-50;5f20", -50.0, 1)]
    [InlineData("100;5f25", 100.0, 1)]
    [InlineData("-100; 2f20 , 3f16", -100.0, 2)]
    [InlineData("-50", -50.0, 0)]
    [InlineData("100", 100.0, 0)]
    [InlineData("-0", 0.0, 0)]
    [InlineData("+50", 50.0, 0)]
    [InlineData("-35.5;2f18", -35.5, 1)]
    [InlineData("abc;-50;xyz;2f20", -50.0, 1)]
    [InlineData("-50;;;;5f20", -50.0, 1)]
    [InlineData("", 0.0, 0)]
    [InlineData("   ", 0.0, 0)]
    [InlineData(null, 0.0, 0)]
    public void ParseOffsetAndBars_AdversarialInputs_ParsesOffsetAndBarsCorrectly(
        string? text, double expOffset, int expBarCount)
    {
        var (offset, bars) = KataBarNotationParser.ParseOffsetAndBars(text);
        Assert.Equal(expOffset, offset);
        Assert.Equal(expBarCount, bars.Count);
    }

    [Theory]
    [InlineData("50/25", 50.0, 25.0)]
    [InlineData("30/20", 30.0, 20.0)]
    [InlineData("30", 30.0, 0.0)]
    [InlineData("10/10", 10.0, 10.0)]
    [InlineData("100/50", 100.0, 50.0)]
    [InlineData("0", 0.0, 0.0)]
    [InlineData("-10", 0.0, 0.0)]
    [InlineData("-10/-20", 0.0, 0.0)]
    [InlineData("0/0", 0.0, 0.0)]
    [InlineData("abc/def", 0.0, 0.0)]
    [InlineData("///", 0.0, 0.0)]
    [InlineData("", 0.0, 0.0)]
    [InlineData(null, 0.0, 0.0)]
    public void ParseCover_AdversarialInputs_ReturnsGivenNumbersOrZero(
        string? text, double expMain, double expStirrup)
    {
        var (cMain, cStirrup) = KataBarNotationParser.ParseCover(text);
        Assert.Equal(expMain, cMain);
        Assert.Equal(expStirrup, cStirrup);
    }

    [Theory]
    [InlineData("400", 400.0, 0.0)]
    [InlineData("300x500", 300.0, 500.0)]
    [InlineData("300*500", 300.0, 500.0)]
    [InlineData("300/500", 300.0, 500.0)]
    [InlineData("300 x 500", 300.0, 500.0)]
    [InlineData("0", 0.0, 0.0)]
    [InlineData("-300", -300.0, 0.0)]
    [InlineData("abc", 0.0, 0.0)]
    [InlineData("", 0.0, 0.0)]
    [InlineData(null, 0.0, 0.0)]
    public void ParseSupportDimension_AdversarialInputs_ReturnsExpected(
        string? text, double expW, double expH)
    {
        var (w, h) = KataBarNotationParser.ParseSupportDimension(text);
        Assert.Equal(expW, w);
        Assert.Equal(expH, h);
    }

    #endregion

    #region 2. KataRebarCalculator Boundary & Geometric Stress Tests

    [Fact]
    public void Calculate_ZeroSpans_ReturnsWarningWithoutThrowing()
    {
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_ZERO_SPANS",
            Width = 300.0,
            Height = 600.0,
            Spans = Array.Empty<KataSpanRebarSpec>()
        };

        var result = KataRebarCalculator.Calculate(spec);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Warnings);
        Assert.Empty(result.MainTopBars);
        Assert.Empty(result.MainBottomBars);
    }

    [Theory]
    [InlineData(0.0, 500.0)]
    [InlineData(-300.0, 500.0)]
    [InlineData(300.0, 0.0)]
    [InlineData(300.0, -500.0)]
    [InlineData(-300.0, -500.0)]
    public void Calculate_ZeroOrNegativeDimensions_ReturnsWarningWithoutThrowing(double width, double height)
    {
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_INVALID_DIMS",
            Width = width,
            Height = height,
            Spans = new[] { new KataSpanRebarSpec { SpanIndex = 0, Length = 5000.0 } }
        };

        var result = KataRebarCalculator.Calculate(spec);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public void Calculate_TwelveSpansContinuousBeam_CalculatesAllReinforcementsWithoutErrors()
    {
        // 12 spans: from L=3m to L=9m
        int numSpans = 12;
        var spans = new List<KataSpanRebarSpec>();
        var supports = new List<KataSupportRebarSpec>();

        for (int i = 0; i < numSpans; i++)
        {
            double len = 3000.0 + ((i % 5) * 1500.0); // 3000 to 9000 mm
            spans.Add(new KataSpanRebarSpec
            {
                SpanIndex = i,
                Length = len,
                BottomExtraLayer1 = new[] { new KataBarItem(2, 20.0) }
            });
        }

        for (int i = 0; i <= numSpans; i++)
        {
            supports.Add(new KataSupportRebarSpec
            {
                SupportIndex = i,
                ColumnWidth = 400.0 + ((i % 3) * 100.0), // 400, 500, 600 mm
                TopExtraLayer1 = new[] { new KataBarItem(3, 22.0) }
            });
        }

        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_12_SPANS",
            Width = 400.0,
            Height = 750.0,
            TopContinuous = new KataBarItem(4, 25.0),
            BottomContinuous = new KataBarItem(4, 25.0),
            GlobalStirrup = new KataStirrupSpec
            {
                Diameter = 10.0,
                SupportSpacing = 150.0,
                MidspanSpacing = 200.0
            },
            Spans = spans,
            Supports = supports
        };

        var result = KataRebarCalculator.Calculate(spec);

        Assert.Empty(result.Blocking);
        Assert.Contains(result.Warnings, w => w.Contains("vượt chiều dài cây thép 11700"));
        Assert.Equal(4, result.MainTopBars.Count);
        Assert.Equal(4, result.MainBottomBars.Count);

        // 13 supports x 3 extra bars = 39 extra top bars
        Assert.Equal(39, result.ExtraTopBars.Count);

        // 12 spans x 2 extra bars = 24 extra bottom bars
        Assert.Equal(24, result.ExtraBottomBars.Count);

        // Stirrup zones: 3 per span. In the three 3000 mm spans the dense zones (2h = 1500 each) fill the span:
        // 10 a150 from each face leave 200 in the middle, closed by one dense stirrup (a100) instead of the
        // sparse zone. No station twice, no gap wider than the zone spacings allow.
        Assert.Equal(36, result.StirrupZones.Count);
        foreach (var span in result.StirrupZones.GroupBy(z => z.SpanIndex))
        {
            var stations = span.SelectMany(z => z.Stations).OrderBy(x => x).ToList();
            double widest = spans[span.Key].Length <= 3000.0 ? 150.0 : 200.0;
            for (int i = 1; i < stations.Count; i++)
            {
                double gap = stations[i] - stations[i - 1];
                Assert.True(gap > 50.0 && gap <= widest + 1e-6, $"span {span.Key}: {stations[i - 1]} / {stations[i]}");
            }
        }

        // Verify total steel weight is positive, non-NaN, and finite
        Assert.True(result.TotalSteelWeightKg > 0.0);
        Assert.False(double.IsNaN(result.TotalSteelWeightKg));
        Assert.False(double.IsInfinity(result.TotalSteelWeightKg));

        // Verify polyline simplification on all generated curves
        AssertAllCurvesSimplifiedAndValid(result);
    }

    [Fact]
    public void Calculate_ExtremeUnequalSpans_2mVs12m_CalculatesCutoffUsingMaxSpan()
    {
        // L1 = 2000 mm, L2 = 12000 mm (ratio 1:6)
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_2M_VS_12M",
            Width = 350.0,
            Height = 800.0,
            TopCutoffRatioLayer1 = 0.25,
            CutoffOriginLayer1 = KataCutoffOrigin.FromColumnFace,
            TopContinuous = new KataBarItem(3, 22.0),
            BottomContinuous = new KataBarItem(3, 22.0),
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
                new KataSupportRebarSpec
                {
                    SupportIndex = 1,
                    ColumnWidth = 500.0,
                    TopExtraLayer1 = new[] { new KataBarItem(3, 25.0) }
                },
                new KataSupportRebarSpec { SupportIndex = 2, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 2000.0 },
                new KataSpanRebarSpec { SpanIndex = 1, Length = 12000.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        AssertOnlyAnchorageWarnings(result);

        // Interior Support 1 extra top bars:
        // Lcutoff = max(2000, 12000) * 0.25 = 3000 mm from column faces!
        // Support 1 left face = 400 + 2000 = 2400 mm. Right face = 2900 mm.
        // Xstart = 2400 - 3000 = -600 mm would leave the beam: the bar stops at Support 0's far face, its
        // centre 25 + 10 + 22/2 = 46 mm inside. Xend = 2900 + 3000 = 5900 mm.
        var supp1Bars = result.ExtraTopBars.Where(b => b.HostSupportIndex == 1).ToList();
        Assert.Equal(3, supp1Bars.Count);
        Assert.Equal(46.0, supp1Bars[0].Polyline.Points[0].X, 6);
        Assert.Equal(5900.0, supp1Bars[0].Polyline.Points[1].X, 6);
        Assert.Equal(5854.0, supp1Bars[0].Polyline.TotalLength, 6);

        AssertAllCurvesSimplifiedAndValid(result);
    }

    [Fact]
    public void Calculate_ShallowBeam_h200mm_ClampsHooksSafelyWithoutCrashing()
    {
        // Shallow beam: h = 200 mm, b = 300 mm, c = 25 mm
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_SHALLOW_200",
            Width = 300.0,
            Height = 200.0,
            CoverStirrup = 25.0,
            TopContinuous = new KataBarItem(2, 16.0),
            BottomContinuous = new KataBarItem(2, 16.0),
            GlobalStirrup = new KataStirrupSpec { Diameter = 8.0, SupportSpacing = 100.0, MidspanSpacing = 150.0 },
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 300.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 300.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 3500.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        // Bar centres 25 + 8 + 8 = 41 mm from both faces leave 200 - 82 = 118 mm for a leg: every leg is
        // clamped there and each end reports its anchorage shortfall.
        Assert.Equal(4, result.Warnings.Count);
        AssertOnlyAnchorageWarnings(result);

        var topBar = result.MainTopBars[0];
        Assert.Equal(118.0, topBar.StartHookLength, 6);
        Assert.Equal(118.0, topBar.EndHookLength, 6);

        var botBar = result.MainBottomBars[0];
        Assert.Equal(118.0, botBar.StartHookLength, 6);
        Assert.Equal(118.0, botBar.EndHookLength, 6);

        AssertAllCurvesSimplifiedAndValid(result);
    }

    [Theory]
    [InlineData(50.0)]
    [InlineData(10.0)]
    public void Calculate_ExtremeCovers_AppliesCoverCorrectly(double cover)
    {
        var spec = new KataBeamRebarSpec
        {
            BeamName = $"B_COVER_{cover}",
            Width = 400.0,
            Height = 600.0,
            CoverStirrup = cover,
            TopContinuous = new KataBarItem(3, 20.0),
            BottomContinuous = new KataBarItem(3, 20.0),
            GlobalStirrup = new KataStirrupSpec { Diameter = 10.0, SupportSpacing = 150.0, MidspanSpacing = 200.0 },
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 5000.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);

        AssertOnlyAnchorageWarnings(result);

        // Stirrup out-to-out dimensions must match Width - 2*cover, Height - 2*cover
        var stZone = result.StirrupZones[0];
        Assert.Equal(400.0 - (2.0 * cover), stZone.OutToOutWidth);
        Assert.Equal(600.0 - (2.0 * cover), stZone.OutToOutHeight);

        // The bars stop at the far column faces with their centre cover + Ø10 + Ø20/2 inside.
        double centre = cover + 10.0 + 10.0;
        Assert.Equal(centre, result.MainTopBars[0].Polyline.Points[1].X, 6);
        Assert.Equal(5800.0 - centre, result.MainTopBars[0].Polyline.Points[2].X, 6);

        AssertAllCurvesSimplifiedAndValid(result);
    }

    [Fact]
    public void Calculate_4LayersTopExtraBars_StrictZHierarchyAndNonOverlap()
    {
        // 4 top extra bar layers over interior Support 1
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_4LAYERS_Z",
            Width = 500.0,
            Height = 900.0,
            CoverStirrup = 25.0,
            TopContinuous = new KataBarItem(4, 25.0),
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

        double zCont = result.MainTopBars[0].Polyline.Points[1].Z;
        double z1 = layer1[0].Polyline.Points[0].Z;
        double z2 = layer2[0].Polyline.Points[0].Z;
        double z3 = layer3[0].Polyline.Points[0].Z;
        double z4 = layer4[0].Polyline.Points[0].Z;

        // Row 13 shares the main bars' level; each lower row keeps a clear gap of max(30, d) to the one above.
        Assert.Equal(zCont, z1, 6);
        Assert.Equal(25.0 / 2 + 30.0 + 25.0 / 2, z1 - z2, 6);
        Assert.Equal(25.0 / 2 + 30.0 + 20.0 / 2, z2 - z3, 6);
        Assert.Equal(20.0 / 2 + 30.0 + 20.0 / 2, z3 - z4, 6);

        AssertAllCurvesSimplifiedAndValid(result);
    }

    [Fact]
    public void Calculate_ExtremeCantileverOverhangs_HandlesAllVariations()
    {
        // 1. Left cantilever only
        var specLeft = new KataBeamRebarSpec
        {
            BeamName = "B_CANT_LEFT",
            Width = 300.0,
            Height = 500.0,
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 0.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 2, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 2500.0 },
                new KataSpanRebarSpec { SpanIndex = 1, Length = 5000.0 }
            },
            TopContinuous = new KataBarItem(2, 20.0),
            BottomContinuous = new KataBarItem(2, 20.0)
        };
        var resLeft = KataRebarCalculator.Calculate(specLeft);
        AssertOnlyAnchorageWarnings(resLeft);
        AssertAllCurvesSimplifiedAndValid(resLeft);

        // 2. Right cantilever only
        var specRight = new KataBeamRebarSpec
        {
            BeamName = "B_CANT_RIGHT",
            Width = 300.0,
            Height = 500.0,
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 2, ColumnWidth = 0.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 5000.0 },
                new KataSpanRebarSpec { SpanIndex = 1, Length = 2500.0 }
            },
            TopContinuous = new KataBarItem(2, 20.0),
            BottomContinuous = new KataBarItem(2, 20.0)
        };
        var resRight = KataRebarCalculator.Calculate(specRight);
        AssertOnlyAnchorageWarnings(resRight);
        AssertAllCurvesSimplifiedAndValid(resRight);

        // 3. Double cantilever (Both ends)
        var specBoth = new KataBeamRebarSpec
        {
            BeamName = "B_CANT_BOTH",
            Width = 300.0,
            Height = 500.0,
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 0.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 2, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 3, ColumnWidth = 0.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 2000.0 },
                new KataSpanRebarSpec { SpanIndex = 1, Length = 6000.0 },
                new KataSpanRebarSpec { SpanIndex = 2, Length = 2000.0 }
            },
            TopContinuous = new KataBarItem(2, 20.0),
            BottomContinuous = new KataBarItem(2, 20.0)
        };
        var resBoth = KataRebarCalculator.Calculate(specBoth);
        AssertOnlyAnchorageWarnings(resBoth);
        AssertAllCurvesSimplifiedAndValid(resBoth);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(10)]
    public void ComputeTransverseYPositions_VariousBarCounts_MaintainsSymmetry(int count)
    {
        double width = 500.0;
        double cover = 25.0;
        double stirrup = 10.0;
        double barDia = 25.0;

        var yPos = KataRebarCalculator.ComputeTransverseYPositions(width, cover, stirrup, barDia, count);
        Assert.Equal(count, yPos.Count);

        for (int i = 0; i < count; i++)
        {
            Assert.Equal(-yPos[count - 1 - i], yPos[i], precision: 5);
        }

        if (count % 2 != 0)
        {
            Assert.Equal(0.0, yPos[count / 2], precision: 5);
        }
    }

    [Fact]
    public void ComputeTransverseYPositions_OvercrowdedNarrowBeam_FallsBackToCenterSafely()
    {
        // width = 100 mm, cover = 50 mm, stirrup = 10 mm, bar = 25 mm -> y0 >= yn
        var yPos = KataRebarCalculator.ComputeTransverseYPositions(100.0, 50.0, 10.0, 25.0, 5);
        Assert.Equal(5, yPos.Count);
        foreach (double y in yPos)
        {
            Assert.Equal(0.0, y);
        }
    }

    [Fact]
    public void ParseBarList_ExtremelyLongInput_CompletesPromptlyWithoutHanging()
    {
        // 5000 valid tokens separated by commas (~35,000 characters)
        string hugeInput = string.Join(",", Enumerable.Repeat("2f20;3f18", 2500));
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var items = KataBarNotationParser.ParseBarList(hugeInput);
        sw.Stop();

        Assert.Equal(5000, items.Count);
        Assert.True(sw.ElapsedMilliseconds < 500, $"Parsing took too long: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Calculate_SingleSpanWithBothColumnsZero_HandlesGracefullyWithoutThrowing()
    {
        // Pathological case: 1 span where both supports have ColumnWidth = 0
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_FLOATING_SPAN",
            Width = 300.0,
            Height = 500.0,
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 0.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 0.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 4000.0 }
            },
            TopContinuous = new KataBarItem(2, 20.0),
            BottomContinuous = new KataBarItem(2, 20.0)
        };

        var result = KataRebarCalculator.Calculate(spec);
        AssertOnlyAnchorageWarnings(result);
        AssertAllCurvesSimplifiedAndValid(result);
    }

    [Fact]
    public void Calculate_NegativeColumnWidthsAndSpans_ClampedToZeroWithoutCrashing()
    {
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_NEGATIVE_SUBS",
            Width = 300.0,
            Height = 500.0,
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = -400.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = -500.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 4000.0 }
            },
            TopContinuous = new KataBarItem(2, 18.0),
            BottomContinuous = new KataBarItem(2, 18.0)
        };

        var result = KataRebarCalculator.Calculate(spec);
        AssertOnlyAnchorageWarnings(result);
        AssertAllCurvesSimplifiedAndValid(result);
    }

    [Fact]
    public void Calculate_ExtremeParameters_ZeroCoversAndNegativeMultipliers_HandledSafely()
    {
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_EXTREME_PARAMS",
            Width = 350.0,
            Height = 600.0,
            CoverStirrup = 0.0, // should default to 25
            CoverMain = -10.0,
            CompressionLapMultiplier = -5.0, // should clamp hook length to >= 200
            TopCutoffRatioLayer1 = -0.5, // should clamp ratio to default 0.25
            TopContinuous = new KataBarItem(2, 20.0),
            BottomContinuous = new KataBarItem(2, 20.0),
            Supports = new[]
            {
                new KataSupportRebarSpec
                {
                    SupportIndex = 0,
                    ColumnWidth = 400.0,
                    TopExtraLayer1 = new[] { new KataBarItem(2, 20.0) }
                },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 5000.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);
        AssertOnlyAnchorageWarnings(result);
        AssertAllCurvesSimplifiedAndValid(result);
    }

    [Fact]
    public void Calculate_InterleavedShortAndLongSpans_AllStirrupAndBarPolylinesValid()
    {
        // Interleaved: 1000mm, 8000mm, 1200mm, 11000mm
        var spec = new KataBeamRebarSpec
        {
            BeamName = "B_INTERLEAVED",
            Width = 400.0,
            Height = 700.0,
            TopContinuous = new KataBarItem(4, 25.0),
            BottomContinuous = new KataBarItem(4, 25.0),
            Supports = new[]
            {
                new KataSupportRebarSpec { SupportIndex = 0, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 1, ColumnWidth = 500.0 },
                new KataSupportRebarSpec { SupportIndex = 2, ColumnWidth = 400.0 },
                new KataSupportRebarSpec { SupportIndex = 3, ColumnWidth = 600.0 },
                new KataSupportRebarSpec { SupportIndex = 4, ColumnWidth = 400.0 }
            },
            Spans = new[]
            {
                new KataSpanRebarSpec { SpanIndex = 0, Length = 1000.0 },
                new KataSpanRebarSpec { SpanIndex = 1, Length = 8000.0 },
                new KataSpanRebarSpec { SpanIndex = 2, Length = 1200.0 },
                new KataSpanRebarSpec { SpanIndex = 3, Length = 11000.0 }
            }
        };

        var result = KataRebarCalculator.Calculate(spec);
        AssertOnlyAnchorageWarnings(result);
        AssertAllCurvesSimplifiedAndValid(result);
    }

    #endregion

    #region Helper Assertion

    /// <summary>The only warnings allowed are anchorage shortfalls, extra bar cutoffs, main bar length > 11.7 m, or a deep beam without side bars.</summary>
    private static void AssertOnlyAnchorageWarnings(KataRebarLayoutResult result) =>
        Assert.All(result.Warnings, w => Assert.True(w.StartsWith("Neo thép") || w.StartsWith("Thép gia cường") || w.StartsWith("Thép chủ") || w.Contains("thiếu cốt giá"), w));

    private static void AssertAllCurvesSimplifiedAndValid(KataRebarLayoutResult result)
    {
        var allCurves = result.MainTopBars
            .Concat(result.MainBottomBars)
            .Concat(result.ExtraTopBars)
            .Concat(result.ExtraBottomBars)
            .Concat(result.SideBars)
            .Concat(result.IndividualStirrups);

        foreach (var curve in allCurves)
        {
            var pts = curve.Polyline.Points;

            // Must have at least 2 vertices for open or closed curves
            Assert.True(pts.Count >= 2, $"Curve BarId {curve.BarId} role {curve.Role} has fewer than 2 points ({pts.Count}).");

            // Segment length check: no segment < 1.0mm
            for (int i = 0; i < pts.Count - 1; i++)
            {
                double dist = pts[i].DistanceTo(pts[i + 1]);
                Assert.True(dist >= 1.0 - 1e-9,
                    $"Curve BarId {curve.BarId} role {curve.Role}: Segment {i}->{i + 1} length {dist:F3}mm is shorter than 1.0mm tolerance.");
            }

            // Coordinates must not be NaN or Infinity
            foreach (var pt in pts)
            {
                Assert.False(double.IsNaN(pt.X) || double.IsNaN(pt.Y) || double.IsNaN(pt.Z),
                    $"Curve BarId {curve.BarId} contains NaN vertex ({pt.X}, {pt.Y}, {pt.Z})");
                Assert.False(double.IsInfinity(pt.X) || double.IsInfinity(pt.Y) || double.IsInfinity(pt.Z),
                    $"Curve BarId {curve.BarId} contains Infinite vertex ({pt.X}, {pt.Y}, {pt.Z})");
            }
        }
    }

    #endregion
}
