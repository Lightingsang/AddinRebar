using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataExport.ColumnStack;
using Xunit;

namespace HPRebar.Core.Tests.KataExport.ColumnStack;

/// <summary>
/// The stack of Kata's own sample (sheet "Cot" of Kata.xlsm, drawn by Kata's VC command into the reference
/// drawing): pile cap 1.5 m from -5.500, five 1000 x 800 storeys of 3.0 / 3.5 / 3.9 / 4.6 / 3.3 m drawn as storeys,
/// and FLOOR 5, the row Kata draws only as the stub above them.
/// </summary>
public sealed class KataColumnSheetBuilderTests
{
    private static readonly string[] Levels = { "BASEMENT", "FLOOR 1", "FLOOR 2", "FLOOR 3", "FLOOR 4", "FLOOR 5" };
    private static readonly double[] HeightsMm = { 3000, 3500, 3900, 4600, 3300 };
    private static readonly double[] BeamDepthsMm = { 600, 600, 400, 400, 600 };

    [Fact]
    public void Build_KataSample_WritesTheSampleCells()
    {
        var input = SampleStack(_ => KataColumnSection.Rectangle(0, 1000, 0, 800));

        var sheet = KataColumnSheetBuilder.Build(input);

        Assert.Equal(800.0, sheet.Header["C1"]);
        Assert.Equal(200.0, sheet.Header["C2"]);
        Assert.Equal("C1", sheet.Header["C3"]);
        Assert.Equal(1, sheet.Header["C4"]);
        Assert.Equal(-5.5, sheet.Header["C5"]);
        Assert.Equal(1.5, sheet.Header["B9"]);
        Assert.Equal(Levels, sheet.Storeys.Select(r => r.Name.Value));
        Assert.Equal(new[] { 3.0, 3.5, 3.9, 4.6, 3.3, 3.3 }, sheet.Storeys.Select(r => r.HeightM));
        Assert.All(sheet.Storeys, r => Assert.Equal((1000.0, 800.0), (r.SizeB, r.SizeH)));
        Assert.Equal(new[] { 600.0, 600, 400, 400, 600, 600 }, sheet.Storeys.Select(r => r.BeamDepthMm));
        Assert.Equal(new[] { false, false, false, false, false, true }, sheet.Storeys.Select(r => r.IsCapRow));
        Assert.Empty(sheet.Warnings);
    }

    [Theory]
    [InlineData(0, 800, -800)]    // keeps the left face: drawn 0..800 by VC (variant C13 = -800)
    [InlineData(100, 900, 800)]   // centred: drawn 100..900 by VC (variant C13 = +800)
    [InlineData(0, 500, -500)]    // large one-sided step: drawn 0..500 by VC (variant C13 = -500)
    public void Build_StepAtFloor3_SignFollowsKataPlacement(double minX, double maxX, double expectedB)
    {
        var input = SampleStack(i => i == 3
            ? KataColumnSection.Rectangle(minX, maxX, 0, 800)
            : KataColumnSection.Rectangle(0, 1000, 0, 800));

        var sheet = KataColumnSheetBuilder.Build(input);

        Assert.Equal(expectedB, sheet.Storeys[3].SizeB);
        Assert.DoesNotContain(sheet.Warnings, w => w.StartsWith("FLOOR 3", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_WiderStoreyKeepingTheLeftFace_WarnsBecauseKataNeverDrewIt()
    {
        var input = SampleStack(i => i == 3
            ? KataColumnSection.Rectangle(0, 800, 0, 800)
            : KataColumnSection.Rectangle(0, 1000, 0, 800));

        var sheet = KataColumnSheetBuilder.Build(input);

        Assert.Equal(-1000.0, sheet.Storeys[4].SizeB);
        Assert.Contains(sheet.Warnings, w => w.StartsWith("FLOOR 4: larger than the storey below", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_TwoStoreysOnOneLevel_SuffixesTheUpperName()
    {
        var input = SampleStack(_ => KataColumnSection.Rectangle(0, 1000, 0, 800));
        var storeys = input.Storeys.ToList();
        storeys[2] = storeys[2] with { LevelName = "FLOOR 1" };

        var sheet = KataColumnSheetBuilder.Build(input with { Storeys = storeys });

        Assert.Equal("FLOOR 1 (2)", sheet.Storeys[2].Name.Value);
        Assert.Contains(sheet.Warnings, w => w.Contains("FLOOR 1 (2)"));
    }

    [Fact]
    public void Build_MeasuredFractionalLevels_HeightsAddUpToTheRoundedLevels()
    {
        var storeys = new[]
        {
            new KataColumnStorey("L1", 0.4, 3000.4, KataColumnSection.Rectangle(0, 500, 0, 500), 0),
            new KataColumnStorey("L2", 3000.4, 6000.6, KataColumnSection.Rectangle(0, 500, 0, 500), 0),
        };

        var sheet = KataColumnSheetBuilder.Build(new KataColumnStackInput("C", 1, storeys, null));

        Assert.Equal(new[] { 3.0, 3.001, 3.001 }, sheet.Storeys.Select(r => r.HeightM));
        Assert.Equal("L2 (đỉnh)", sheet.Storeys[2].Name.Value);
    }

    [Fact]
    public void Build_StoreyAboveAOneSidedStep_IsCentredOnTheStepAsKataDraws()
    {
        var sections = new Dictionary<int, KataColumnSection>
        {
            [3] = KataColumnSection.Rectangle(0, 800, 0, 800),
            [4] = KataColumnSection.Rectangle(-100, 900, 0, 800),
            [5] = KataColumnSection.Rectangle(-100, 900, 0, 800),
        };
        var input = SampleStack(i => sections.TryGetValue(i, out var s) ? s : KataColumnSection.Rectangle(0, 1000, 0, 800));

        var sheet = KataColumnSheetBuilder.Build(input);

        Assert.Equal(new[] { 1000.0, 1000.0, 1000.0, -800.0, 1000.0, 1000.0 }, sheet.Storeys.Select(r => r.SizeB));
        Assert.Empty(sheet.Warnings);
    }

    [Fact]
    public void Build_EccentricStep_WritesTheNearerPlacementAndWarns()
    {
        var input = SampleStack(i => i == 3
            ? KataColumnSection.Rectangle(150, 950, 0, 800)
            : KataColumnSection.Rectangle(0, 1000, 0, 800));

        var sheet = KataColumnSheetBuilder.Build(input);

        Assert.Equal(800.0, sheet.Storeys[3].SizeB);
        Assert.Contains(sheet.Warnings, w => w.StartsWith("FLOOR 3: b is 50 mm away", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_RoundStoreyBetweenRectangles_WritesDiameterAndZeroH()
    {
        var input = SampleStack(i => i == 3
            ? KataColumnSection.Circle(500, 400, 800)
            : KataColumnSection.Rectangle(0, 1000, 0, 800));

        var sheet = KataColumnSheetBuilder.Build(input);

        Assert.Equal((800.0, 0.0), (sheet.Storeys[3].SizeB, sheet.Storeys[3].SizeH));
        Assert.Equal((1000.0, 800.0), (sheet.Storeys[4].SizeB, sheet.Storeys[4].SizeH));
        Assert.Empty(sheet.Warnings);
    }

    [Fact]
    public void Build_RoundStoreyOffCentre_Warns()
    {
        var input = SampleStack(i => i == 3
            ? KataColumnSection.Circle(560, 400, 800)
            : KataColumnSection.Rectangle(0, 1000, 0, 800));

        var sheet = KataColumnSheetBuilder.Build(input);

        Assert.Contains(sheet.Warnings, w => w.StartsWith("FLOOR 3: the round column is 60 mm off", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_NoPileCap_UsesFirstBaseAndZeroCapHeightWithWarning()
    {
        var input = SampleStack(_ => KataColumnSection.Rectangle(0, 1000, 0, 800)) with { PileCap = null };

        var sheet = KataColumnSheetBuilder.Build(input);

        Assert.Equal(-4.0, sheet.Header["C5"]);
        Assert.Equal(0.0, sheet.Header["B9"]);
        Assert.Contains(sheet.Warnings, w => w.StartsWith("No pile cap", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_PileCapBelowFirstBase_StretchesB9SoLevelsStayRight()
    {
        var input = SampleStack(_ => KataColumnSection.Rectangle(0, 1000, 0, 800)) with { PileCap = new KataPileCap(-5500, 1200) };

        var sheet = KataColumnSheetBuilder.Build(input);

        Assert.Equal(1.5, sheet.Header["B9"]);
        Assert.Single(sheet.Warnings);
    }

    [Fact]
    public void Build_UnknownBeamAndSlab_KeepsTheUserCells()
    {
        var input = SampleStack(_ => KataColumnSection.Rectangle(0, 1000, 0, 800)) with
        {
            CrossingBeamWidthMm = null,
            SlabThicknessMm = null,
        };

        var sheet = KataColumnSheetBuilder.Build(input);

        Assert.Null(sheet.Header["C1"]);
        Assert.Null(sheet.Header["C2"]);
    }

    [Fact]
    public void Build_GapBetweenStoreys_Throws()
    {
        var storeys = SampleStack(_ => KataColumnSection.Rectangle(0, 1000, 0, 800)).Storeys.ToList();
        storeys[2] = storeys[2] with { BaseElevationMm = storeys[2].BaseElevationMm + 100 };
        var input = SampleStack(_ => KataColumnSection.Rectangle(0, 1000, 0, 800)) with { Storeys = storeys };

        var error = Assert.Throws<ArgumentException>(() => KataColumnSheetBuilder.Build(input));

        Assert.Contains("FLOOR 2 starts 100 mm from the top of FLOOR 1", error.Message);
    }

    [Fact]
    public void Build_EmptyStack_Throws()
    {
        var input = new KataColumnStackInput("C1", 1, Array.Empty<KataColumnStorey>(), null);

        Assert.Throws<ArgumentException>(() => KataColumnSheetBuilder.Build(input));
    }

    [Fact]
    public void Map_SteppedStack_TellsThePagePlacementAndShape()
    {
        var input = SampleStack(i => i switch
        {
            3 => KataColumnSection.Rectangle(0, 800, 0, 800),
            4 => KataColumnSection.Circle(400, 400, 800),
            _ => KataColumnSection.Rectangle(0, 1000, 0, 800),
        });

        var preview = KataColumnPreviewMapper.Map(input, KataColumnSheetBuilder.Build(input));

        Assert.Equal(
            new[] { "first", "centred", "centred", "leftFace", "centred", "stub" },
            preview.Rows.Select(r => r.Placement));
        Assert.Equal("circle", preview.Rows[4].Shape);
        Assert.Equal((-5.5, 1.5), (preview.PileCapBottomM, preview.PileCapHeightM));
    }

    [Fact]
    public void The_page_fixtures_match_the_mapper()
    {
        var sample = SampleStack(_ => KataColumnSection.Rectangle(0, 1000, 0, 800));
        var stepped = SampleStack(i => i switch
        {
            3 => KataColumnSection.Rectangle(0, 800, 0, 800),
            4 => KataColumnSection.Circle(460, 400, 800),
            _ => KataColumnSection.Rectangle(0, 1000, 0, 800),
        }) with { PileCap = null, SlabThicknessMm = null };
        var fixtures = new Dictionary<string, object>
        {
            ["kata-column-sample.json"] = KataColumnPreviewMapper.Map(sample, KataColumnSheetBuilder.Build(sample)),
            ["kata-column-stepped.json"] = KataColumnPreviewMapper.Map(stepped, KataColumnSheetBuilder.Build(stepped)),
        };

        KataPageFixtures.AssertUpToDate("kataColumn", fixtures, "KataColumnPreviewMapper");
    }

    private static KataColumnStackInput SampleStack(Func<int, KataColumnSection> section)
    {
        var storeys = new List<KataColumnStorey>();
        double baseMm = -4000;
        for (int i = 0; i < HeightsMm.Length; i++)
        {
            storeys.Add(new KataColumnStorey(Levels[i], baseMm, baseMm + HeightsMm[i], section(i), BeamDepthsMm[i], Levels[i + 1]));
            baseMm += HeightsMm[i];
        }

        return new KataColumnStackInput("C1", 1, storeys, new KataPileCap(-5500, 1500), 800, 200);
    }
}
