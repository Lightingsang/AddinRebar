using System.Linq;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public sealed class KataStirrupSectionParserTests
{
    [Fact]
    public void The_legend_in_column_A_is_not_a_stirrup()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("A25", "Đai □");
        table.Set("A26", "Đai U");
        table.Set("A27", "Đai C");

        var spec = KataDamSheetParser.Parse(table);

        Assert.Equal(new[] { KataStirrupBranchSpec.Outer }, spec.GlobalStirrup.Branches.ToArray());
    }

    [Fact]
    public void A_type_without_bars_is_a_template_leftover()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("C25", "Đai □");
        table.Set("C26", "Đai U");
        table.Set("E25", "Đai C");

        var spec = KataDamSheetParser.Parse(table);

        Assert.Single(spec.GlobalStirrup.Branches);
        Assert.Empty(spec.DetailingNotes);
    }

    [Fact]
    public void Each_column_pair_lists_inner_stirrups_by_type_and_wrapped_bars()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("C25", "Đai U");
        table.Set("D25", "3-4");
        table.Set("C26", "Đai C");
        table.Set("D26", "2");
        table.Set("M25", "Đai □");
        table.Set("N25", "2-5");

        var branches = KataDamSheetParser.Parse(table).GlobalStirrup.Branches;

        Assert.Equal(4, branches.Count);
        Assert.True(branches[0].IsOuterHoop);
        Assert.Equal(new KataStirrupBranchSpec(KataStirrupShapeType.CapStirrup, "3-4", "C25"), branches[1]);
        Assert.Equal(new KataStirrupBranchSpec(KataStirrupShapeType.CrossTie, "2", "C26"), branches[2]);
        Assert.Equal(new KataStirrupBranchSpec(KataStirrupShapeType.ClosedHoop, "2-5", "M25"), branches[3]);
    }

    [Fact]
    public void Row_24_markers_and_span_reinforcement_stirrups_become_notes()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("C24", "30");
        table.Set("D23", "a100/200");

        var notes = KataDamSheetParser.Parse(table).DetailingNotes;

        Assert.Contains(notes, n => n.Address == "C24" && n.Text == "30");
        Assert.Contains(notes, n => n.Address == "D23" && n.Text == "a100/200");
    }

    [Fact]
    public void Every_bar_group_of_B11_is_kept_in_order()
    {
        var table = KataRebarTestSheets.SingleSpan();
        table.Set("B11", "2f20;2f16");

        var spec = KataDamSheetParser.Parse(table);

        Assert.Equal(new[] { "2f20", "2f16" }, spec.TopMainItems.Select(i => i.ToString()).ToArray());
        Assert.Equal(20.0, spec.TopContinuous.Diameter);
    }

    [Fact]
    public void Sheet_columns_are_recorded()
    {
        var spec = KataDamSheetParser.Parse(KataRebarTestSheets.SingleSpan());

        Assert.Equal(3, spec.Supports[0].SheetColumn);
        Assert.Equal(4, spec.Spans[0].SheetColumn);
        Assert.Equal(5, spec.Supports[1].SheetColumn);
    }
}
