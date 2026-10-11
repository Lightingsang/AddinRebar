using System.Text.Json;
using HPNavis.BIMCoordinator.Reports;
using Xunit;

namespace HPNavis.BIMCoordinator.Tests;

/// <summary>The clash export: which phase it reads, where its file may go, and the shape the report script reads.</summary>
public sealed class ClashSnapshotTests
{
    private static readonly ClashItemInfo Wall = new("Basic Wall", "Walls", "123456", "THCSLT-…-AA-0001_user.rvt", "TẦNG 2");
    private static readonly ClashItemInfo Pipe = new("Pipe Types", "Pipes", "654321", "THCSLT-…-EP-0001_user.rvt", "");

    [Theory]
    [InlineData(null, "350-400")]
    [InlineData("auto", "350-400")]
    [InlineData("350", "350-400")]
    [InlineData("200", "200")]
    public void The_phase_is_the_one_asked_for_or_the_only_one_in_the_model(string? requested, string expected)
    {
        var names = new[] { "HP|LOD350-400|P1|ARC-STR|A5 Doors-S2 Structural Walls", "HP|LOD350|P2|MEP-STR|M4-S3", "Test 1" };
        Assert.Equal(expected, ClashSnapshotRules.ResolvePhase(requested, names));
    }

    [Fact]
    public void Several_phases_or_none_need_the_caller_to_choose()
    {
        Assert.Throws<ArgumentException>(() => ClashSnapshotRules.ResolvePhase("auto", new[] { "HP|LOD200|P1|ARC-STR|A5-S2", "HP|LOD300|P1|ARC-STR|A5-S2" }));
        Assert.Throws<ArgumentException>(() => ClashSnapshotRules.ResolvePhase(null, new[] { "Test 1" }));
        Assert.Throws<ArgumentException>(() => ClashSnapshotRules.ResolvePhase("250", Array.Empty<string>()));
    }

    [Fact]
    public void The_report_goes_beside_the_model_and_nowhere_else()
    {
        Assert.Equal(@"Q:\My Drive\02_NAV\ClashReports", ClashSnapshotRules.FolderFor(@"Q:\My Drive\02_NAV\THCSLT.nwd"));
        Assert.Throws<ArgumentException>(() => ClashSnapshotRules.FolderFor(""));
        Assert.Throws<ArgumentException>(() => ClashSnapshotRules.FolderFor("THCSLT.nwd"));
        Assert.Throws<ArgumentException>(() => ClashSnapshotRules.FolderFor(@"\\server\share\THCSLT.nwd"));
    }

    [Fact]
    public void File_names_sort_by_time_and_name_the_phase()
    {
        var name = ClashSnapshotRules.FileNameFor("350-400", new DateTime(2026, 10, 11, 8, 5, 9, DateTimeKind.Utc));
        Assert.Matches(@"^clash-LOD350-400-20261011-\d{6}\.json$", name);
    }

    [Fact]
    public void The_level_falls_back_to_the_second_element_then_to_unknown()
    {
        Assert.Equal("TẦNG 2", ClashSnapshotRules.LevelOf(Pipe, Wall));
        Assert.Equal(ClashSnapshotRules.UnknownLevel, ClashSnapshotRules.LevelOf(Pipe, Pipe));
    }

    [Fact]
    public void The_snapshot_serialises_with_the_names_the_report_script_reads()
    {
        var row = new ClashSnapshotRow("HP|LOD350-400|P1|MEP-STR|M4 Pipes and Pipe Accessories-S3 Structural Columns", "HP_M4_S3", 1, "MEP-STR",
            "a29ca71a-ef4c-4db2-826a-e0aea2aa43f5", "Clash1", null, "New", -91.7, new[] { 1.0, 2.0, 3.0 }, "TẦNG 2", Pipe, Wall);
        var test = new ClashSnapshotTest(row.Test, "HP_M4_S3", 1, "MEP-STR", "M4", "S3", "Complete", "2026-10-11T02:00:00.0000000Z", 1,
            new Dictionary<string, int> { ["New"] = 1 });
        var snapshot = new ClashSnapshot(1, "2026-10-11T02:05:00.0000000Z", "THCSLT.nwd", "350-400", 10, new[] { test }, new[] { row }, false);

        using var json = JsonDocument.Parse(ClashSnapshotRules.Serialize(snapshot));
        var root = json.RootElement;
        Assert.Equal("350-400", root.GetProperty("phase").GetString());
        var first = root.GetProperty("rows")[0];
        Assert.Equal("a29ca71a-ef4c-4db2-826a-e0aea2aa43f5", first.GetProperty("guid").GetString());
        Assert.Equal("TẦNG 2", first.GetProperty("level").GetString()); // Vietnamese kept as text, not \u escapes
        Assert.Equal("Walls", first.GetProperty("item2").GetProperty("category").GetString());
        Assert.Equal(1, root.GetProperty("tests")[0].GetProperty("byStatus").GetProperty("New").GetInt32());
        Assert.Contains("TẦNG 2", ClashSnapshotRules.Serialize(snapshot));
        Assert.Equal(new Dictionary<string, int> { ["New"] = 1 }, ClashSnapshotRules.ByStatus(snapshot));
    }
}
