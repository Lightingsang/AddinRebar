using HPNavis.BIMCoordinator.Rules;
using HPNavis.BIMCoordinator.SearchSets;
using Xunit;

namespace HPNavis.BIMCoordinator.Tests;

/// <summary>The write rule behind every apply: never overwrite without approval, approval set by set, nothing deleted.</summary>
public sealed class SetUpsertTests
{
    [Theory]
    [InlineData(false, false, false, UpsertAction.Create)]
    [InlineData(false, false, true, UpsertAction.Create)]
    [InlineData(true, true, false, UpsertAction.Unchanged)]
    [InlineData(true, true, true, UpsertAction.Unchanged)]
    [InlineData(true, false, false, UpsertAction.Conflict)]
    [InlineData(true, false, true, UpsertAction.Update)]
    public void Decide_never_overwrites_a_differing_set_without_approval(bool exists, bool same, bool allowUpdate, UpsertAction expected)
    {
        Assert.Equal(expected, SetUpsert.Decide(exists, same, allowUpdate));
    }

    [Fact]
    public void Approval_names_the_sets_it_covers()
    {
        Assert.Throws<ArgumentException>(() => SetUpsert.RequireCodesForUpdate(true, null));
        Assert.Throws<ArgumentException>(() => SetUpsert.RequireCodesForUpdate(true, Array.Empty<string>()));
        SetUpsert.RequireCodesForUpdate(true, new[] { "A2" });
        SetUpsert.RequireCodesForUpdate(false, null);
    }

    [Fact]
    public void Sets_no_entry_owns_are_reported()
    {
        var registry = SearchSetPlan.All(BaseSetCatalog.Default, ClashMatrix.Default, includeExtras: true).Select(p => $"{p.Folder}/{p.DisplayName}").ToList();
        var saved = registry.Concat(new[] { "HP BIMCoordinator/ARC/A2 Ceilings", "HP BIMCoordinator/MEP/my own set" }).ToList();
        Assert.Equal(new[] { "HP BIMCoordinator/ARC/A2 Ceilings", "HP BIMCoordinator/MEP/my own set" }, SetUpsert.Orphans(saved, registry));
    }

    [Theory]
    [InlineData("\"value\": \"true\"", "\"value\": \"yes\"")]
    [InlineData("\"value\": \"32\"", "\"value\": \"-1\"")]
    [InlineData("\"parent\": \"M4\", \"folder\": \"MEP/Details/M4 Pipes by system\", \"name\": \"Fire Protection Wet\"", "\"folder\": \"MEP/Details/M4 Pipes by system\", \"name\": \"Fire Protection Wet\"")]
    [InlineData("\"parent\": \"M2\", \"folder\": \"MEP/Details/M2 Cable trays by model\", \"name\": \"Cable trays - EC model\"", "\"parent\": \"M4\", \"folder\": \"MEP/Details/M2 Cable trays by model\", \"name\": \"Cable trays - EC model\"")]
    [InlineData("{ \"code\": \"A2\", \"name\": \"Ceilings\",", "{ \"code\": \"A2\", \"roles\": [ \"AA\" ], \"name\": \"Ceilings\",")]
    [InlineData("\"clashSelection\": \"M4.9\"", "\"clashSelection\": \"M2.1\"")]
    [InlineData("\"clashSelection\": \"M4.9\"", "\"clashSelection\": \"M4.99\"")]
    public void Registry_mistakes_fail_the_load_not_the_apply(string from, string to)
    {
        var json = ClashMatrix.ReadResource(BaseSetCatalog.ResourceName);
        Assert.Contains(from, json);
        Assert.Throws<InvalidOperationException>(() => BaseSetCatalog.Parse(json.Replace(from, to), ClashMatrix.Default));
    }

    [Theory]
    [InlineData(false, false, Relocation.None)]
    [InlineData(true, false, Relocation.None)]
    [InlineData(false, true, Relocation.Move)]
    [InlineData(true, true, Relocation.KeepOld)]
    public void A_set_at_an_earlier_path_moves_only_into_a_free_place(bool atCurrent, bool atPrevious, Relocation expected)
    {
        Assert.Equal(expected, SetUpsert.Relocate(atCurrent, atPrevious));
    }

    [Theory]
    [InlineData(null, "created", "created", false)]
    [InlineData("moved", "unchanged", "moved", false)]
    [InlineData("moved", "updated", "moved+updated", false)]
    [InlineData("move", "conflict", "move+conflict", true)]
    [InlineData(null, "conflict", "conflict", true)]
    public void A_move_joins_the_upsert_and_still_counts_as_a_conflict(string? move, string upsert, string expected, bool conflict)
    {
        var action = SetUpsert.Describe(move, upsert);
        Assert.Equal(expected, action);
        Assert.Equal(conflict, SetUpsert.IsConflict(action));
    }

    [Fact]
    public void Plumbing_fixtures_sets_moved_under_mep_remember_their_old_place()
    {
        var plans = SearchSetPlan.All(BaseSetCatalog.Default, ClashMatrix.Default, includeExtras: true).ToDictionary(p => p.Code);
        Assert.Equal("HP BIMCoordinator/MEP/Plumbing fixtures", plans["X.1"].Folder);
        Assert.Equal("HP BIMCoordinator/MEP/Plumbing fixtures", plans["X.2"].Folder);
        Assert.Equal("X.2 ARC plumbing fixtures", plans["X.2"].DisplayName);
        Assert.Equal(new[] { "Plumbing Fixtures" }, plans["X.2"].Categories);
        Assert.Equal(new[] { "HP BIMCoordinator", "Not in matrix", "X.2 ARC plumbing fixtures and sprinklers" }, plans["X.2"].Previous.Single());
        Assert.Equal(new[] { "Plumbing Equipment", "Sprinklers" }, plans["X.6"].Categories);
        Assert.Equal("HP BIMCoordinator/Not in matrix", plans["X.6"].Folder);
        Assert.Empty(plans["X.3"].Previous);
    }

    [Theory]
    [InlineData("\"previously\": [ \"Not in matrix/X.1 MEP plumbing fixtures and equipment\" ]", "\"previously\": [ \"X.1 MEP plumbing fixtures and equipment\" ]")]
    [InlineData("\"previously\": [ \"Not in matrix/X.1 MEP plumbing fixtures and equipment\" ]", "\"previously\": [ \"Not in matrix/X.3 ARC structural elements\" ]")]
    [InlineData("\"previously\": [ \"Not in matrix/X.1 MEP plumbing fixtures and equipment\" ]", "\"previously\": [ \"Not in matrix/X.2 ARC plumbing fixtures and sprinklers\" ]")]
    public void An_earlier_path_must_name_a_folder_and_never_a_live_or_claimed_set(string from, string to)
    {
        var json = ClashMatrix.ReadResource(BaseSetCatalog.ResourceName);
        Assert.Contains(from, json);
        Assert.Throws<InvalidOperationException>(() => BaseSetCatalog.Parse(json.Replace(from, to), ClashMatrix.Default));
    }

    [Fact]
    public void Role_codes_are_read_case_insensitively_like_the_search()
    {
        Assert.Equal("AA", IsoFileName.Role("thcslt-hpc-tt-zz-m3-aa-0001.nwc"));
    }
}
