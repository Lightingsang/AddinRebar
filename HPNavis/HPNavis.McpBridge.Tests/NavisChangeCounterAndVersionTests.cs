using HPNavis.McpBridge.Model;
using HPNavis.McpBridge.Service;
using Xunit;
using Fingerprint = HPNavis.McpBridge.Service.NavisChangeCounter.Fingerprint;

namespace HPNavis.McpBridge.Tests;

/// <summary>Fingerprint arithmetic that backs `Changed` and the `none` check, plus the runtime→year table.</summary>
public sealed class NavisChangeCounterAndVersionTests
{
    private static Fingerprint Base() => new(SelectionSets: 1, SavedViewpoints: 6, Models: 1, SelectedItems: 0, SelectionHash: 17, ClashTests: 1, NextUndo: "user edit", IsModified: false);

    [Fact]
    public void Added_and_deleted_come_from_the_collection_counts()
    {
        var before = Base();
        var after = before with { SelectionSets = 2, SavedViewpoints = 7, ClashTests = 0, NextUndo = "MCP: x", IsModified = true };

        var delta = NavisChangeCounter.Delta(before, after);

        Assert.Equal(2, delta.Added);
        Assert.Equal(1, delta.Deleted);
        Assert.Equal(0, delta.Modified);
        Assert.False(before.SameAs(after));
    }

    [Fact]
    public void A_move_without_a_count_change_reports_one_modification()
    {
        var before = Base();
        var selectionChanged = before with { SelectedItems = 3, SelectionHash = 99 };
        var undoOnly = before with { NextUndo = "MCP: select" };

        Assert.Equal(new[] { 0, 1, 0 }, new[] { NavisChangeCounter.Delta(before, selectionChanged).Added, NavisChangeCounter.Delta(before, selectionChanged).Modified, NavisChangeCounter.Delta(before, selectionChanged).Deleted });
        Assert.Equal(1, NavisChangeCounter.Delta(before, undoOnly).Modified);
    }

    [Fact]
    public void Identical_snapshots_are_the_same_and_change_nothing()
    {
        var before = Base();
        var after = before with { };

        Assert.True(before.SameAs(after));
        var delta = NavisChangeCounter.Delta(before, after);
        Assert.Equal((0, 0, 0), (delta.Added, delta.Modified, delta.Deleted));
    }

    [Theory]
    [InlineData(21, 2024, true)]
    [InlineData(22, 2025, true)]
    [InlineData(23, 2026, true)]
    [InlineData(24, 2027, true)]
    [InlineData(99, NavisVersion.BuiltFor, false)]
    [InlineData(null, NavisVersion.BuiltFor, false)]
    public void Runtime_major_maps_to_the_product_year_or_falls_back_to_the_build_target(int? runtimeMajor, int expectedYear, bool expectedKnown)
    {
        var year = NavisVersion.YearFor(runtimeMajor, out var known);

        Assert.Equal(expectedYear, year);
        Assert.Equal(expectedKnown, known);
    }
}
