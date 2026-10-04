using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using Xunit;
using static HPRebar.Core.Tests.KataExport.TestKataData;

namespace HPRebar.Core.Tests.KataExport;

/// <summary>
/// Row 20 of sheet "Dam": the beam crossing the run at a support ("b x h", or "b" when as deep as B5), and a span's
/// own width in front of the side bars the user typed.
/// </summary>
public sealed class KataRow20Tests
{
    /// <summary>Two spans on three columns; a crossing beam of <paramref name="first"/> at the first column,
    /// one of <paramref name="middle"/> at the middle one; the second span <paramref name="secondWidth"/> wide.</summary>
    private static KataRunInput Run(string? first, string? middle, double secondWidth = 220.0)
    {
        var supports = new System.Collections.Generic.List<KataSupport> { Column(-200, 200), Column(5800, 6200), Column(11800, 12200) };
        if (first is not null) supports.Add(Girder(-125, 125, first, "G0"));
        if (middle is not null) supports.Add(Girder(5900, 6100, middle, "G1"));
        return new KataRunInput(
            new[] { Piece(0, 6000), Piece(6000, 12000, b: secondWidth) },
            supports,
            new[] { Grid("1", 0), Grid("2", 6000), Grid("3", 12000) },
            Header);
    }

    [Fact]
    public void A_crossing_beam_shallower_than_the_beam_is_written_b_x_h_and_one_as_deep_as_B5_by_its_width_alone()
    {
        var sheet = KataRowBuilder.Build(Run("250x400", "200x500"));

        Assert.Equal("250x400", sheet.Row20[0]);
        Assert.Equal(200.0, sheet.Row20[2]);
        Assert.Null(sheet.Row20[4]);
    }

    [Fact]
    public void A_support_where_Revit_shows_no_crossing_beam_keeps_what_the_user_typed()
    {
        var sheet = KataRowBuilder.Build(Run(null, null));

        Assert.All(new[] { 0, 2, 4 }, i => Assert.Null(sheet.Row20[i]));
    }

    [Fact]
    public void Spans_as_wide_as_B6_say_nothing_and_once_one_span_differs_every_span_gives_its_width()
    {
        var plain = KataRowBuilder.Build(Run(null, null));
        var changed = KataRowBuilder.Build(Run(null, null, secondWidth: 150.0));

        Assert.Equal(new KataSpanWidthCell(null), plain.Row20[1]);
        Assert.Equal(new KataSpanWidthCell(220.0), changed.Row20[1]);
        Assert.Equal(new KataSpanWidthCell(150.0), changed.Row20[3]);
    }

    [Fact]
    public void Row_20_follows_the_run_when_it_is_written_from_its_far_end()
    {
        var sheet = KataRowBuilder.Build(Run("250x400", null, secondWidth: 150.0), new KataBuildOptions { Reverse = true });

        Assert.Equal("250x400", sheet.Row20[4]);
        Assert.Equal(new KataSpanWidthCell(150.0), sheet.Row20[1]);
    }

    [Fact]
    public void The_preview_shows_row_20()
    {
        var input = Run("250x400", null, secondWidth: 150.0);
        var sheet = KataRowBuilder.Build(input);

        var elevation = KataElevationBuilder.Build(input, new KataBuildOptions(), sheet);

        Assert.Equal("250x400", elevation.Columns[0].Row20);
        Assert.Equal("150", elevation.Columns[3].Row20);
        Assert.Equal("", elevation.Columns[2].Row20);
    }

    [Theory]
    [InlineData("2f12", 300.0, "300;2f12")]
    [InlineData("300;2f12", null, "2f12")]
    [InlineData("250;0f12", 300.0, "300;0f12")]
    [InlineData("0f12", null, "0f12")]
    [InlineData(null, null, "")]
    [InlineData("0", 300.0, "300;0")]
    [InlineData("0", null, "0")]
    [InlineData("300;0", null, "0")]
    [InlineData("250,2f12", 300.0, "300;2f12")]
    public void A_span_cell_keeps_the_side_bars_the_user_typed_and_takes_the_width_from_Revit(string? existing, double? width, string expected)
    {
        Assert.Equal(expected, KataRow20.ComposeSpanCell(existing, width));
    }

    [Fact]
    public void A_span_cell_with_only_a_width_is_written_as_a_number()
    {
        Assert.Equal(300.0, KataRow20.ComposeSpanCell("250", 300.0));
    }
}
