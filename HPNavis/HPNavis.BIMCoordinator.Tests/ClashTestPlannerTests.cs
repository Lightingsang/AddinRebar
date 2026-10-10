using HPNavis.BIMCoordinator.ClashTests;
using HPNavis.BIMCoordinator.Rules;
using HPNavis.BIMCoordinator.SearchSets;
using Xunit;

namespace HPNavis.BIMCoordinator.Tests;

public sealed class ClashTestPlannerTests
{
    private static readonly ClashMatrix Matrix = ClashMatrix.Default;
    private static string SetName(string code) => NavisSetPath(code);

    // same identity the compiler uses: folder path + display name
    private static string NavisSetPath(string code)
    {
        var plan = SearchSetPlan.For(BaseSetCatalog.Default, Matrix, code);
        return $"{plan.Folder}/{plan.DisplayName}";
    }

    private static Dictionary<string, SetState> AllSets() =>
        Matrix.Groups.ToDictionary(g => g.Code, _ => new SetState(true, true));

    private static ExistingTest Stored(ClashRule rule, string lod, double toleranceMm) =>
        new(ClashTestNaming.NameFor(rule, lod), "Hard", toleranceMm, SetName(rule.Left), SetName(rule.Right), rule.Priority);

    [Fact]
    public void Empty_document_creates_one_test_per_rule()
    {
        var plan = ClashTestPlanner.Plan(Matrix, "350", SetName, AllSets(), Array.Empty<ExistingTest>());
        Assert.Equal(184, plan.Count(PlanAction.Create));
        Assert.Equal(10, plan.ToleranceMm);
        Assert.Equal(184, plan.Tests.Select(t => t.Name).Distinct().Count());
    }

    [Fact]
    public void Applying_twice_changes_nothing()
    {
        var existing = Matrix.RulesFor("350").Select(r => Stored(r, "350", 10.0000001)).ToList();
        var plan = ClashTestPlanner.Plan(Matrix, "350", SetName, AllSets(), existing);
        Assert.Equal(184, plan.Count(PlanAction.Unchanged));
        Assert.Empty(plan.ToWrite);
    }

    [Fact]
    public void A_drifted_tolerance_or_selection_is_an_update()
    {
        var rules = Matrix.RulesFor("350");
        var existing = new[]
        {
            Stored(rules[0], "350", 30),
            Stored(rules[1], "350", 10) with { SetB = "something else" },
        };
        var plan = ClashTestPlanner.Plan(Matrix, "350", SetName, AllSets(), existing, ruleIds: new[] { rules[0].Id, rules[1].Id });
        Assert.All(plan.Tests, t => Assert.Equal(PlanAction.Update, t.Action));
        Assert.Contains("tolerance 30 → 10 mm", plan.Tests[0].Reason);
        Assert.Contains("selections", plan.Tests[1].Reason);
    }

    [Fact]
    public void Missing_or_empty_sets_skip_the_test_and_never_widen_it()
    {
        var sets = AllSets();
        sets["M3"] = new SetState(false, null);
        sets["M6"] = new SetState(true, false);
        var plan = ClashTestPlanner.Plan(Matrix, "350", SetName, sets, Array.Empty<ExistingTest>());
        Assert.All(plan.Tests.Where(t => t.RuleId.Contains("M3")), t => Assert.Equal(PlanAction.SkipMissingSet, t.Action));
        Assert.All(plan.Tests.Where(t => t.RuleId.Contains("M6") && !t.RuleId.Contains("M3")), t => Assert.Equal(PlanAction.SkipEmptySet, t.Action));
        Assert.DoesNotContain(plan.ToWrite, t => t.SetA.StartsWith("M3") || t.SetB.StartsWith("M3"));
    }

    [Fact]
    public void Our_tests_the_matrix_no_longer_lists_are_reported_not_removed()
    {
        var foreign = new ExistingTest("ARC vs STR: Walls", "Hard", 10, null, null, 0);
        var stale = new ExistingTest("HP|P1|LOD350|ARC-MEP|A1-M2", "Hard", 10, null, null, 1); // A1-M2 is not a matrix pair
        var plan = ClashTestPlanner.Plan(Matrix, "350", SetName, AllSets(), new[] { foreign, stale });
        Assert.Equal(new[] { stale.Name }, plan.Orphans);
    }

    [Fact]
    public void Names_round_trip_to_rule_and_lod()
    {
        foreach (var rule in Matrix.Rules)
        {
            var parsed = ClashTestNaming.Parse(ClashTestNaming.NameFor(rule, "300"));
            Assert.Equal((rule.Id, "300"), parsed);
        }

        Assert.Equal("HP|P2|LOD350|ARC-ARC|A1-A1", ClashTestNaming.NameFor(Matrix.Rules.Single(r => r.Id == "HP_A1_A1"), "350"));
        Assert.Null(ClashTestNaming.Parse("ARC vs ARC: Furnitures-ARC"));
    }

    [Fact]
    public void Canaries_cover_different_discipline_pairs_first()
    {
        var existing = Matrix.RulesFor("350").Select(r => Stored(r, "350", 10)).ToList();
        var plan = ClashTestPlanner.Plan(Matrix, "350", SetName, AllSets(), existing);
        var canaries = CanarySelector.Select(Matrix, plan, 3);
        Assert.Equal(3, canaries.Count);
        Assert.All(canaries, c => Assert.Equal(1, c.Priority));
        var pairs = canaries.Select(c => Matrix.Rules.Single(r => r.Id == c.RuleId).DisciplinePair).ToList();
        Assert.Equal(3, pairs.Distinct().Count());
    }

    [Fact]
    public void Explicit_canary_must_be_up_to_date()
    {
        var plan = ClashTestPlanner.Plan(Matrix, "350", SetName, AllSets(), Array.Empty<ExistingTest>());
        Assert.Throws<ArgumentException>(() => CanarySelector.Select(Matrix, plan, 3, new[] { "HP_A1_A8" }));
    }

    [Fact]
    public void A_priority_change_renames_the_stored_test_instead_of_orphaning_it()
    {
        var rule = Matrix.Rules.Single(r => r.Id == "HP_A1_A8");
        var stored = Stored(rule, "350", 10) with { Name = "HP|P3|LOD350|ARC-ARC|A1-A8", Priority = 3 };
        var plan = ClashTestPlanner.Plan(Matrix, "350", SetName, AllSets(), new[] { stored }, ruleIds: new[] { rule.Id });
        var test = Assert.Single(plan.Tests);
        Assert.Equal(PlanAction.Update, test.Action);
        Assert.Equal(stored.Name, test.CurrentName);
        Assert.Equal("HP|P1|LOD350|ARC-ARC|A1-A8", test.Name);
        Assert.Contains("name", test.Reason);
        Assert.Empty(plan.Orphans);
    }

    [Fact]
    public void A_second_test_for_the_same_rule_is_reported_as_duplicate()
    {
        var rule = Matrix.RulesFor("350")[0];
        var first = Stored(rule, "350", 10);
        var plan = ClashTestPlanner.Plan(Matrix, "350", SetName, AllSets(), new[] { first, first }, ruleIds: new[] { rule.Id });
        Assert.Equal(PlanAction.Unchanged, Assert.Single(plan.Tests).Action);
        Assert.Equal(new[] { first.Name }, plan.Duplicates);
    }

    [Fact]
    public void Tests_of_another_lod_are_not_touched()
    {
        var rule = Matrix.RulesFor("300")[0];
        var plan = ClashTestPlanner.Plan(Matrix, "350", SetName, AllSets(), new[] { Stored(rule, "300", 30) }, ruleIds: new[] { rule.Id });
        Assert.Equal(PlanAction.Create, Assert.Single(plan.Tests).Action);
        Assert.Empty(plan.Orphans);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void An_unknown_priority_is_the_callers_error(int priority)
    {
        Assert.Throws<ArgumentException>(() => Matrix.RulesFor("350", priorities: new[] { priority }));
    }

    [Fact]
    public void Canary_rule_ids_are_capped_like_the_count()
    {
        var plan = ClashTestPlanner.Plan(Matrix, "350", SetName, AllSets(), Matrix.RulesFor("350").Select(r => Stored(r, "350", 10)).ToList());
        var many = Matrix.RulesFor("350").Take(CoordinatorTools.MaxCanaries + 1).Select(r => r.Id).ToList();
        Assert.Throws<ArgumentException>(() => CanarySelector.Select(Matrix, plan, 3, many));
        Assert.Throws<ArgumentException>(() => CanarySelector.Select(Matrix, plan, CoordinatorTools.MaxCanaries + 1));
    }

    [Fact]
    public void A_full_plan_listing_stays_well_under_the_64_KB_output_cap()
    {
        var plan = ClashTestPlanner.Plan(Matrix, "350", SetName, AllSets(), Array.Empty<ExistingTest>());
        var listing = plan.Tests.Select(t => new { t.RuleId, t.Name, t.Priority, action = t.Action.ToString(), reason = "search set M3, M6 not in the document — apply the search sets first" });
        var bytes = System.Text.Encoding.UTF8.GetByteCount(System.Text.Json.JsonSerializer.Serialize(listing));
        Assert.True(bytes < 48 * 1024, $"{bytes} bytes");
    }

    [Fact]
    public void A_test_bound_to_a_same_named_set_in_an_old_folder_is_re_pointed()
    {
        var rule = Matrix.Rules.Single(r => r.Id == "HP_A2_S5");
        var stale = Stored(rule, "350", 10) with { SetA = "HP BIMCoordinator/ARC/A2 Ceilings", SetB = "HP BIMCoordinator/STR/S5 Structural Framing" };
        var test = Assert.Single(ClashTestPlanner.Plan(Matrix, "350", SetName, AllSets(), new[] { stale }, ruleIds: new[] { rule.Id }).Tests);
        Assert.Equal(PlanAction.Update, test.Action);
        Assert.Contains("HP BIMCoordinator/Architecture/A2 Ceilings", test.Reason);
    }
}