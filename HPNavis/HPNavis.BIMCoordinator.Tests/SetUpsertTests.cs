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
    public void Registry_mistakes_fail_the_load_not_the_apply(string from, string to)
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
