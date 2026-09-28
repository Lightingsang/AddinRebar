using System.Collections.Generic;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public sealed class KataSheetGeometryCheckTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void A_symmetric_run_follows_the_direction_the_sheet_was_written_in(bool? prefer, bool reversed)
    {
        var spec = KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan());

        var result = KataSheetGeometryCheck.Compare(spec, KataRebarTestSheets.MeasuredSingleSpan(), prefer);

        Assert.Equal(reversed, result.Reversed);
    }

    [Fact]
    public void A_clearly_better_fit_wins_over_the_preferred_direction()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("C11", 500.0);
        table.Set("D11", 5900.0);
        var spec = KataDamSheetParser.Parse(table);

        var result = KataSheetGeometryCheck.Compare(spec, KataRebarTestSheets.MeasuredSingleSpan(500.0, 5900.0, 400.0), preferReversed: true);

        Assert.False(result.Reversed);
    }

    private static readonly KataBeamRebarSpec Sheet = KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan());

    [Fact]
    public void A_matching_run_passes_silently()
    {
        var result = KataSheetGeometryCheck.Compare(Sheet, KataRebarTestSheets.MeasuredSingleSpan(401.5, 5999.0, 400.0));

        Assert.Empty(result.Blocking);
        Assert.Empty(result.Warnings);
        Assert.False(result.Reversed);
    }

    [Fact]
    public void A_run_listed_from_the_other_end_is_read_backwards()
    {
        var spec = KataDamSheetParser.Parse(Set(KataRebarTestSheets.SingleSpan(), "E11", 500.0));

        var result = KataSheetGeometryCheck.Compare(spec, KataRebarTestSheets.MeasuredSingleSpan(500.0, 6000.0, 400.0));

        Assert.True(result.Reversed);
        Assert.Empty(result.Blocking);
        Assert.Equal(400.0, result.SheetOrder[0].LengthMm);
        Assert.Equal(500.0, result.SheetOrder[2].LengthMm);
    }

    [Theory]
    [InlineData(400.0003, 400.0003)]
    [InlineData(460.0, 400.0)]
    public void A_symmetric_run_is_not_reversed_by_noise_or_by_an_equally_bad_fit(double left, double right)
    {
        var sheet = KataDamSheetParser.Parse(Set(KataRebarTestSheets.SingleSpan(), "C11", left));

        var result = KataSheetGeometryCheck.Compare(sheet, KataRebarTestSheets.MeasuredSingleSpan(400.0, 6000.0, right));

        Assert.False(result.Reversed);
    }

    [Fact]
    public void A_difference_up_to_50_mm_warns_and_names_the_cell()
    {
        var result = KataSheetGeometryCheck.Compare(Sheet, KataRebarTestSheets.MeasuredSingleSpan(420.0, 5980.0, 400.0));

        Assert.Empty(result.Blocking);
        Assert.Contains(result.Warnings, w => w.StartsWith("C11"));
        Assert.Contains(result.Warnings, w => w.StartsWith("D11"));
    }

    [Fact]
    public void A_difference_over_50_mm_blocks()
    {
        var result = KataSheetGeometryCheck.Compare(Sheet, KataRebarTestSheets.MeasuredSingleSpan(460.0, 5940.0, 400.0));

        Assert.Contains(result.Blocking, b => b.StartsWith("C11"));
    }

    [Fact]
    public void A_different_number_of_columns_blocks()
    {
        var measured = new KataMeasuredBeam(300.0, 600.0, new List<KataMeasuredSegment>
        {
            new(KataMeasuredSupportKind.Column, 400.0),
            new(KataMeasuredSupportKind.None, 3000.0),
            new(KataMeasuredSupportKind.Column, 400.0),
            new(KataMeasuredSupportKind.None, 2600.0),
            new(KataMeasuredSupportKind.Column, 400.0)
        }, 1);

        var result = KataSheetGeometryCheck.Compare(Sheet, measured);

        Assert.Single(result.Blocking);
    }

    [Fact]
    public void A_section_different_from_Revit_is_reported_against_B5_and_B6()
    {
        var measured = KataRebarTestSheets.MeasuredSingleSpan() with { WidthMm = 320.0, HeightMm = 700.0 };

        var result = KataSheetGeometryCheck.Compare(Sheet, measured);

        Assert.Contains(result.Warnings, w => w.StartsWith("B6"));
        Assert.Contains(result.Blocking, b => b.StartsWith("B5"));
    }

    private static KataCellTable Set(KataCellTable table, string address, object value)
    {
        table.Set(address, value);
        return table;
    }
}
