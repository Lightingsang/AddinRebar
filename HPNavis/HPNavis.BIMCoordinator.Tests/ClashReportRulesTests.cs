using HPNavis.BIMCoordinator.Reports;
using Xunit;

namespace HPNavis.BIMCoordinator.Tests;

/// <summary>The clash report with pictures: which tests a phase run covers, which results it lists, where its HTML goes.</summary>
public sealed class ClashReportRulesTests
{
    private static readonly PhaseTestState[] Model =
    {
        new("HP|LOD350-400|P2|ARC-STR|A5 Doors-S2 Structural Walls", HasRun: true),
        new("HP|LOD350-400|P1|MEP-STR|M4 Pipes-S3 Structural Columns", HasRun: false),
        new("HP|LOD350-400|P1|ARC-STR|A5 Doors-S3 Structural Columns", HasRun: true),
        new("HP|LOD200|P1|ARC-STR|A5 Doors-S2 Structural Walls", HasRun: false),
        new("Test 1", HasRun: false),
    };

    [Fact]
    public void A_phase_run_takes_its_HP_tests_in_name_order_and_nothing_else()
    {
        var list = ClashReportRules.PhaseTests(Model, "350-400", onlyNotRun: false);

        Assert.Equal("350-400", list.Phase);
        Assert.Equal(new[]
        {
            "HP|LOD350-400|P1|ARC-STR|A5 Doors-S3 Structural Columns",
            "HP|LOD350-400|P1|MEP-STR|M4 Pipes-S3 Structural Columns",
            "HP|LOD350-400|P2|ARC-STR|A5 Doors-S2 Structural Walls",
        }, list.Names);
        Assert.Equal(0, list.SkippedAlreadyRun);
    }

    [Fact]
    public void Resuming_keeps_only_the_tests_that_never_ran()
    {
        var list = ClashReportRules.PhaseTests(Model, "350", onlyNotRun: true);

        Assert.Equal(new[] { "HP|LOD350-400|P1|MEP-STR|M4 Pipes-S3 Structural Columns" }, list.Names);
        Assert.Equal(2, list.SkippedAlreadyRun);
    }

    [Fact]
    public void A_batch_resumes_after_the_last_name_it_attempted_whatever_changed_in_between()
    {
        var names = ClashReportRules.PhaseTests(Model, "350-400", onlyNotRun: false).Names;

        Assert.Equal(names, ClashReportRules.RemainingAfter(names, ""));
        Assert.Equal(new[] { names[2] }, ClashReportRules.RemainingAfter(names, names[1]));
        Assert.Empty(ClashReportRules.RemainingAfter(names, names[2]));
        // the cursor test itself was deleted meanwhile: the next name still follows it
        Assert.Equal(new[] { names[1], names[2] }, ClashReportRules.RemainingAfter(names.Skip(1).ToList(), names[0]));
    }

    [Fact]
    public void A_test_name_found_twice_is_run_once_and_counts_as_run_only_when_every_copy_ran()
    {
        var twice = Model.Concat(new[] { new PhaseTestState("HP|LOD350-400|P1|ARC-STR|A5 Doors-S3 Structural Columns", HasRun: false) }).ToList();

        var all = ClashReportRules.PhaseTests(twice, "350-400", onlyNotRun: false);
        var notRun = ClashReportRules.PhaseTests(twice, "350-400", onlyNotRun: true);

        Assert.Equal(3, all.Names.Count);
        Assert.Contains("HP|LOD350-400|P1|ARC-STR|A5 Doors-S3 Structural Columns", notRun.Names);
    }

    [Theory]
    [InlineData(100, 30, 300, true)]
    [InlineData(280, 30, 300, false)]
    [InlineData(0, 0, 30, true)]
    public void A_new_test_starts_only_while_the_slowest_one_still_fits(double elapsed, double slowest, int budget, bool starts)
    {
        Assert.Equal(starts, ClashReportRules.MayStartAnother(elapsed, slowest, budget));
    }

    [Fact]
    public void Open_clashes_are_the_ones_pictured_first()
    {
        Assert.True(ClashReportRules.IsOpen("Reviewed"));
        Assert.False(ClashReportRules.IsOpen("Resolved"));
        Assert.True(ClashReportRules.IsReported("Resolved"));
    }

    [Fact]
    public void Several_phases_need_the_caller_to_name_one_and_an_absent_phase_is_refused()
    {
        Assert.Throws<ArgumentException>(() => ClashReportRules.PhaseTests(Model, "auto", onlyNotRun: false));
        Assert.Throws<ArgumentException>(() => ClashReportRules.PhaseTests(Model, "300", onlyNotRun: false));
    }

    [Theory]
    [InlineData("New", true)]
    [InlineData("Active", true)]
    [InlineData("reviewed", true)]
    [InlineData("Resolved", true)]
    [InlineData("Approved", false)]
    public void The_report_lists_open_and_resolved_clashes_but_not_approved_ones(string status, bool reported)
    {
        Assert.Equal(reported, ClashReportRules.IsReported(status));
    }

    [Fact]
    public void The_html_of_an_export_sits_in_a_folder_named_after_its_snapshot()
    {
        Assert.Equal(@"Q:\02_NAV\ClashReports\clash-LOD350-400-20261011-090000",
            ClashReportRules.HtmlFolderFor(@"Q:\02_NAV\ClashReports\clash-LOD350-400-20261011-090000.json"));
    }

    [Theory]
    [InlineData("Item Properties", true)]
    [InlineData("Properties", true)]
    [InlineData("Image", false)]
    [InlineData("Grid Location", false)]
    [InlineData("Item ID", false)]
    public void Only_the_full_property_lists_stay_out_of_the_report(string field, bool left)
    {
        Assert.Equal(left, ClashReportRules.IsPropertyListField(field));
    }

    [Fact]
    public void A_missing_report_format_names_what_navisworks_offers()
    {
        var names = new[] { "Text", "XML", "HTML", "HTML (Tabular)" };

        Assert.Equal(3, ClashReportRules.IndexOf(names, "html (tabular)", "format"));
        var error = Assert.Throws<InvalidOperationException>(() => ClashReportRules.IndexOf(names, "PDF", "format"));
        Assert.Contains("HTML (Tabular)", error.Message);
    }
}
