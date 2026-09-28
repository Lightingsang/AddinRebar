using System;
using System.Linq;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using Xunit;
using static HPRebar.Core.Tests.KataExport.TestKataData;

namespace HPRebar.Core.Tests.KataExport;

/// <summary>
/// The elevation drawn in the export window: one drawn column per sheet column, the same texts as the cells,
/// mirrored when the run is written in reverse.
/// </summary>
public sealed class KataElevationTests
{
    private static KataElevation Elevation(KataRunInput input, bool reverse = false, bool insertJoints = false)
    {
        var options = new KataBuildOptions { Reverse = reverse, InsertJoints = insertJoints };
        return KataElevationBuilder.Build(input, options, KataRowBuilder.Build(input, options));
    }

    [Fact]
    public void EveryColumnCarriesItsExcelLetterAndTheTextsOfItsCells()
    {
        var input = TwoSpansOnColumns();
        var sheet = KataRowBuilder.Build(input);

        var elevation = Elevation(input);

        Assert.Equal(sheet.ColumnCount, elevation.Columns.Count);
        Assert.Equal(new[] { "C", "D", "E", "F", "G" }, elevation.Columns.Select(c => c.Letter));
        Assert.Equal(sheet.Row11.Select(KataColumnLetters.CellText), elevation.Columns.Select(c => c.Row11));
        Assert.Equal(sheet.Row19.Select(KataColumnLetters.CellText), elevation.Columns.Select(c => c.Row19));
        Assert.Equal(sheet.Row21.Select(KataColumnLetters.CellText), elevation.Columns.Select(c => c.Row21));
        Assert.Equal(sheet.Row22.Select(KataColumnLetters.CellText), elevation.Columns.Select(c => c.Row22));
        Assert.Equal(sheet.Row23.Select(KataColumnLetters.CellText), elevation.Columns.Select(c => c.Row23));
        Assert.Equal(
            new[] { KataColumnKind.Support, KataColumnKind.Span, KataColumnKind.Support, KataColumnKind.Span, KataColumnKind.Support },
            elevation.Columns.Select(c => c.Kind));
    }

    [Fact]
    public void StationsStartAtTheLeftEndOfTheDrawing()
    {
        var elevation = Elevation(TwoSpansOnColumns());

        Assert.Equal(new Interval1D(0, 400), elevation.Columns[0].Extent);
        Assert.Equal(new Interval1D(400, 6000), elevation.Columns[1].Extent);
        Assert.Equal(new Interval1D(0, 12400), elevation.Bounds);
        Assert.Equal(new[] { 200.0, 6200.0, 12200.0 }, elevation.Grids.Select(g => g.X));
        Assert.Equal(new[] { "6000", "6000" }, elevation.GridDimensions.Select(d => d.Text));
    }

    [Fact]
    public void SpansAreNumberedFromTheLeftAndCarryTheSectionOfTheBeamUnderThem()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000, b: 220, h: 500, key: "B1"), Piece(6000, 12000, b: 250, h: 600, zOffset: -50, key: "B2") },
            new[] { Column(-200, 200), Column(5800, 6200), Column(11800, 12200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var elevation = Elevation(input);

        var spans = elevation.Columns.Where(c => c.Kind == KataColumnKind.Span).ToList();
        Assert.Equal(new int?[] { 1, 2 }, spans.Select(c => c.SpanNumber));
        Assert.Equal(new[] { "220x500", "250x600" }, spans.Select(c => c.SpanSection));
        Assert.Equal("-50", spans[1].Row19);
        Assert.Equal("-150", spans[1].Row21);
        Assert.Equal(-650.0, elevation.Beams[1].BottomMm);
        Assert.Equal(0.0, elevation.TopMm);
        Assert.Equal(-650.0, elevation.BottomMm);
        Assert.Equal(600.0, elevation.MaxBeamHeightMm);
    }

    [Fact]
    public void ReversedRunIsMirroredSoColumnCStaysOnTheLeft()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000) },
            new[] { Column(-200, 200, key: "C1"), Column(5700, 6300, upper: new Interval1D(5750, 6150), key: "C2") },
            new[] { Grid("1", 0), Grid("2", 6000) },
            Header);
        var sheet = KataRowBuilder.Build(input, new KataBuildOptions { Reverse = true });

        var elevation = Elevation(input, reverse: true);

        Assert.Equal(new Interval1D(0, 600), elevation.Columns[0].Extent);          // the 600 mm column, now first
        Assert.Equal("600", elevation.Columns[0].Row11);
        Assert.Equal(sheet.Row19.Select(KataColumnLetters.CellText), elevation.Columns.Select(c => c.Row19));
        Assert.Equal(new Interval1D(150, 550), elevation.Supports[0].Upper!.Extent);
        Assert.Equal("400;50", elevation.Supports[0].Upper!.Text);
        Assert.Equal(new[] { "2", "1" }, elevation.Grids.Select(g => g.Name));
    }

    [Fact]
    public void FreeEndsAreZeroWidthColumnsAtTheBeamEnds()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 7500) },
            new[] { Column(1300, 1700), Column(5800, 6200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var elevation = Elevation(input);

        Assert.Equal(KataColumnKind.FreeEnd, elevation.Columns[0].Kind);
        Assert.Equal(KataColumnKind.FreeEnd, elevation.Columns[^1].Kind);
        Assert.Equal(0.0, elevation.Columns[0].Extent.Length);
        Assert.Equal(7500.0, elevation.Columns[^1].Extent.Start);
        Assert.Equal(0, elevation.ColumnAt(30, zeroWidthReachMm: 50));
        Assert.Equal(1, elevation.ColumnAt(700, zeroWidthReachMm: 50));
    }

    [Fact]
    public void ReversedRunKeepsFreeEndsAndJointsInTheSheetOrder()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 3000, key: "B1"), Piece(3000, 7500, key: "B2") },
            new[] { Column(-200, 200), Column(5800, 6200) },
            Array.Empty<KataGridCrossing>(),
            Header);
        var options = new KataBuildOptions { Reverse = true, InsertJoints = true };
        var sheet = KataRowBuilder.Build(input, options);

        var elevation = Elevation(input, reverse: true, insertJoints: true);

        Assert.Equal(sheet.Row11.Select(KataColumnLetters.CellText), elevation.Columns.Select(c => c.Row11));
        Assert.Equal(
            new[] { KataColumnKind.FreeEnd, KataColumnKind.Span, KataColumnKind.Support, KataColumnKind.Span, KataColumnKind.Joint, KataColumnKind.Span, KataColumnKind.Support },
            elevation.Columns.Select(c => c.Kind));
        Assert.Equal(new Interval1D(0, 0), elevation.Columns[0].Extent);           // the free far end, now on the left
        Assert.Equal(4500.0, elevation.Columns[4].Extent.Start);                   // the joint at 3000, mirrored from 7500
        Assert.Null(elevation.ColumnAt(-500, zeroWidthReachMm: 50));
        Assert.Null(elevation.ColumnAt(9000, zeroWidthReachMm: 50));
    }

    [Fact]
    public void SheetBuiltForTheOtherDirectionIsRefused()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000) },
            new[] { Column(-200, 200), Column(5700, 6300) },
            Array.Empty<KataGridCrossing>(),
            Header);
        var reversedSheet = KataRowBuilder.Build(input, new KataBuildOptions { Reverse = true });

        Assert.Throws<InvalidOperationException>(() => KataElevationBuilder.Build(input, new KataBuildOptions(), reversedSheet));
    }

    [Fact]
    public void DeeperCrossingGirderAndColumnsAboveStayInsideTheDrawing()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000), Piece(6000, 12000) },
            new[] { Column(-200, 200), Girder(5850, 6150, "300x800"), Column(11800, 12200, upper: new Interval1D(11900, 12500)) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var elevation = Elevation(input);

        Assert.Equal(-800.0, elevation.BottomMm);
        Assert.Equal(new Interval1D(0, 12700), elevation.Bounds);
    }

    [Fact]
    public void JointBetweenTwoBeamsIsAZeroWidthColumnWhenJointsEnabled()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 3000, key: "B1"), Piece(3000, 6000, key: "B2") },
            new[] { Column(-200, 200), Column(5800, 6200) },
            Array.Empty<KataGridCrossing>(),
            Header);

        var elevation = Elevation(input, insertJoints: true);

        var joint = Assert.Single(elevation.Columns, c => c.Kind == KataColumnKind.Joint);
        Assert.Equal(3200.0, joint.Extent.Start);
        Assert.True(joint.IsZeroWidth);
        Assert.Equal(joint.Index, elevation.ColumnAt(3230, zeroWidthReachMm: 50));
    }

    [Fact]
    public void GirderSupportKeepsItsHeightAndCrossingBeamItsCentre()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000), Piece(6000, 12000) },
            new[]
            {
                Column(-200, 200),
                Girder(5850, 6150, "300x600"),
                Column(11800, 12200),
                Girder(11850, 12050, "200x400", "G2")                    // framing into the last column, centre 11950
            },
            new[] { Grid("1", 0), Grid("3", 12000) },
            Header);

        var elevation = Elevation(input);

        Assert.Equal(600.0, elevation.Supports[1].SectionHeightMm);
        Assert.Equal(KataSupportKind.Beam, elevation.Supports[1].Kind);
        Assert.Equal(12150.0, elevation.Supports[2].CrossingBeamX);
        Assert.Equal("-50", elevation.Columns[elevation.Supports[2].ColumnIndex].Row21);
    }

    [Fact]
    public void GridsOutsideEverySupportAreNotDrawn()
    {
        var input = new KataRunInput(
            new[] { Piece(0, 6000) },
            new[] { Column(-200, 200), Column(5800, 6200) },
            new[] { Grid("1", 50), Grid("1a", 3000), Grid("2", 6000) },
            Header);

        var elevation = Elevation(input);

        Assert.Equal(new[] { "50", "0" }, elevation.Grids.Select(g => g.OffsetText));
        Assert.Equal(new[] { "1", "2" }, elevation.Grids.Select(g => g.Name));
        Assert.Equal(new[] { "5950" }, elevation.GridDimensions.Select(d => d.Text));
    }

    [Fact]
    public void SpanStepWalksTheSpansAndStopsAtTheEnds()
    {
        var elevation = Elevation(TwoSpansOnColumns());

        Assert.Equal(1, elevation.SpanStep(null, +1));
        Assert.Equal(3, elevation.SpanStep(null, -1));
        Assert.Equal(3, elevation.SpanStep(1, +1));
        Assert.Equal(3, elevation.SpanStep(2, +1));                    // from a support to the next span
        Assert.Null(elevation.SpanStep(3, +1));
        Assert.Equal(1, elevation.SpanStep(3, -1));
        Assert.Equal(new Interval1D(0, 6400), elevation.FocusRange(1));
    }

    [Fact]
    public void LongRunKeepsOneDrawnColumnPerSheetColumn()
    {
        const int spans = 37;
        var pieces = Enumerable.Range(0, spans).Select(i => Piece(i * 5000, (i + 1) * 5000, key: $"B{i}")).ToArray();
        var columns = Enumerable.Range(0, spans + 1).Select(i => Column(i * 5000 - 150, i * 5000 + 150, key: $"C{i}")).ToArray();
        var input = new KataRunInput(pieces, columns, Array.Empty<KataGridCrossing>(), Header);

        var elevation = Elevation(input);

        Assert.Equal(2 * spans + 1, elevation.Columns.Count);
        Assert.Equal("BZ", KataColumnLetters.Letter(75));
        Assert.Equal("BY", elevation.Columns[^1].Letter);
        Assert.Empty(elevation.GridDimensions);
    }

    [Theory]
    [InlineData(0, "C")]
    [InlineData(23, "Z")]
    [InlineData(24, "AA")]
    [InlineData(49, "AZ")]
    [InlineData(50, "BA")]
    [InlineData(75, "BZ")]
    public void ColumnLettersFollowExcel(int index, string letter)
    {
        Assert.Equal(letter, KataColumnLetters.Letter(index));
    }

    [Fact]
    public void CellTextIsCultureInvariant()
    {
        Assert.Equal("12.5", KataColumnLetters.CellText(12.5));
        Assert.Equal("+3.300", KataColumnLetters.CellText(new KataText("+3.300")));
        Assert.Equal("", KataColumnLetters.CellText(null));
    }

    [Fact]
    public void FoundationSupportsWithUpperColumnsAreMappedToElevation()
    {
        // 2 footings (width 1700), span between them (clear span 2650), with upper column stubs of width 350
        var footing1 = new KataSupport(KataSupportKind.Foundation, new Interval1D(25370, 27070), "F1", Upper: new Interval1D(26045, 26395));
        var footing2 = new KataSupport(KataSupportKind.Foundation, new Interval1D(29720, 31420), "F2", Upper: new Interval1D(30395, 30745));
        var piece = Piece(25370, 31420, b: 300, h: 500, key: "B1");
        var input = new KataRunInput(
            new[] { piece },
            new[] { footing1, footing2 },
            new[] { Grid("14A", 26220), Grid("16A", 30520) },
            Header);

        var elevation = Elevation(input);

        Assert.Equal(3, elevation.Columns.Count);
        Assert.Equal(KataSupportKind.Foundation, elevation.Supports[0].Kind);
        Assert.Equal(KataSupportKind.Foundation, elevation.Supports[1].Kind);
        Assert.NotNull(elevation.Supports[0].Upper);
        Assert.NotNull(elevation.Supports[1].Upper);
        Assert.Equal(350.0, elevation.Supports[0].Upper!.Extent.Length);
        Assert.Equal(350.0, elevation.Supports[1].Upper!.Extent.Length);
        // Distance from footing start to column start: 26045 - 25370 = 675
        Assert.Equal(675.0, elevation.Supports[0].Upper!.Extent.Start - elevation.Supports[0].Extent.Start);
        // Clear distance between columns: 30395 - 26395 = 4000
        Assert.Equal(4000.0, elevation.Supports[1].Upper!.Extent.Start - elevation.Supports[0].Upper!.Extent.End);
    }

    [Fact]
    public void SecondaryGridsPassingThroughFoundationAreExcludedFromElevation()
    {
        // Footing 14A has primary grid 14A at 26220 (inside column [26045, 26395])
        // and secondary grid 15A at 26840 (inside footing [25370, 27070] but outside column).
        // Footing 16A has primary grid 16A at 30520 and secondary grid 17A at 31140.
        var footing1 = new KataSupport(KataSupportKind.Foundation, new Interval1D(25370, 27070), "F1", Upper: new Interval1D(26045, 26395));
        var footing2 = new KataSupport(KataSupportKind.Foundation, new Interval1D(29720, 31420), "F2", Upper: new Interval1D(30395, 30745));
        var piece = Piece(25370, 31420, b: 300, h: 500, key: "B1");
        var input = new KataRunInput(
            new[] { piece },
            new[] { footing1, footing2 },
            new[]
            {
                Grid("14A", 26220),
                Grid("15A", 26840),
                Grid("16A", 30520),
                Grid("17A", 31140)
            },
            Header);

        var sheet = KataRowBuilder.Build(input);
        var elevation = Elevation(input);

        // Sheet row 22 only picks primary grids 14A and 16A
        Assert.Equal(new[] { "14A", "", "16A" }, sheet.Row22.Select(v => v?.ToString() ?? ""));
        // Secondary grids 15A and 17A are silently skipped without "cross one support" warning
        Assert.DoesNotContain(sheet.Warnings, w => w.Contains("cross one support"));

        // Elevation only draws grids 14A and 16A
        Assert.Equal(new[] { "14A", "16A" }, elevation.Grids.Select(g => g.Name));
        // Grid dimension chain measures 4300 between 14A and 16A (30520 - 26220 = 4300), NOT 620
        Assert.Single(elevation.GridDimensions);
        Assert.Equal("4300", elevation.GridDimensions[0].Text);
    }
}
