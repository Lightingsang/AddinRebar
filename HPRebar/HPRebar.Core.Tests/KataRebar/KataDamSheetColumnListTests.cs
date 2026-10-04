using System.Linq;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>Where the list of supports and spans of row 11 ends.</summary>
public sealed class KataDamSheetColumnListTests
{
    [Fact]
    public void Template_captions_of_row_10_beyond_the_data_do_not_add_spans()
    {
        var table = KataRebarTestSheets.SingleSpan();
        string[] captions = { "Nhịp", "Cột ", "Nhịp", "Cột ", "Nhịp", "Cột ", "Nhịp", "Cột " };
        for (int i = 0; i < captions.Length; i++)
            table.Set(10, 6 + i, captions[i]); // F10..M10, as in the Kata template

        var spec = KataDamSheetParser.Parse(table);

        Assert.Equal(2, spec.Supports.Count);
        Assert.Single(spec.Spans);
    }

    [Fact]
    public void A_zero_width_interior_support_merges_its_spans_and_a_zero_width_end_stays_a_console()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("E11", 0.0);
        table.Set("F10", "Nhịp");
        table.Set("F11", 1500.0);
        table.Set("G10", "Cột ");
        table.Set("G11", 0.0);

        var spec = KataDamSheetParser.Parse(table);

        Assert.Equal(new[] { 400.0, 0.0 }, spec.Supports.Select(s => s.ColumnWidth));
        Assert.Equal(7500.0, Assert.Single(spec.Spans).Length);
        Assert.Equal(new[] { 0, 1 }, spec.Supports.Select(s => s.SupportIndex));
    }
}
