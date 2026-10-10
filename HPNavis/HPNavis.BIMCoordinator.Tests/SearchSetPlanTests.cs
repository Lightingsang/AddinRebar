using HPNavis.BIMCoordinator.Rules;
using HPNavis.BIMCoordinator.SearchSets;
using Xunit;

namespace HPNavis.BIMCoordinator.Tests;

public sealed class SearchSetPlanTests
{
    private static readonly BaseSetCatalog Catalog = BaseSetCatalog.Default;
    private static readonly ClashMatrix Matrix = ClashMatrix.Default;

    [Fact]
    public void Every_matrix_group_has_a_base_set()
    {
        Assert.Equal(Matrix.Groups.Select(g => g.Code).OrderBy(c => c), Catalog.Sets.Select(s => s.Code).OrderBy(c => c));
    }

    [Theory]
    [InlineData("AA", "ARC")]
    [InlineData("ES", "STR")]
    [InlineData("EC", "MEP")]
    [InlineData("EE", "MEP")]
    [InlineData("EF", "MEP")]
    [InlineData("EP", "MEP")]
    [InlineData("ZZ", null)]
    public void Role_codes_of_the_project_files_map_to_one_discipline(string role, string? discipline)
    {
        Assert.Equal(discipline, Catalog.DisciplineOfRole(role));
    }

    [Fact]
    public void Architectural_and_structural_walls_share_the_category_but_not_the_files()
    {
        var a8 = SearchSetPlan.For(Catalog, Matrix, "A8");
        var s2 = SearchSetPlan.For(Catalog, Matrix, "S2");
        Assert.Equal(new[] { "HP BIMCoordinator", "Architecture" }, a8.FolderPath);
        Assert.Equal(new[] { "HP BIMCoordinator", "Structure" }, s2.FolderPath);
        Assert.All(a8.Groups.Concat(s2.Groups), g => Assert.Equal("Walls", g[0].Value));
        Assert.Empty(a8.Groups.Select(g => g[1].Value).Intersect(s2.Groups.Select(g => g[1].Value)));
    }

    [Fact]
    public void A_plan_is_roles_times_categories_and_groups()
    {
        var plan = SearchSetPlan.For(Catalog, Matrix, "M3");
        Assert.Equal(Catalog.RolesOf("MEP").Count * Catalog.Set("M3").Categories.Count, plan.Groups.Count);
        Assert.All(plan.Groups, g =>
        {
            Assert.Equal(2, g.Count);
            Assert.Equal(ConditionKind.Equals, g[0].Kind);
            Assert.Equal(ConditionKind.Wildcard, g[1].Kind);
        });
        Assert.Equal(SetKind.Base, plan.Kind);
        Assert.Equal("M3 Ducts and Duct Accessories", plan.DisplayName);
    }

    [Fact]
    public void Architectural_floors_exclude_floors_flagged_structural_but_keep_unflagged_ones()
    {
        var plan = SearchSetPlan.For(Catalog, Matrix, "A6");
        Assert.All(plan.Groups, g =>
        {
            var structural = Assert.Single(g, c => c.Property.Property == "lcldrevit_parameter_-1001954");
            Assert.Equal(ConditionKind.NotEquals, structural.Kind);
            Assert.Equal("bool", structural.ValueType);
            Assert.Equal("true", structural.Value);
        });
    }

    [Fact]
    public void Mechanical_equipment_is_hvac_and_lighting_devices_are_electrical()
    {
        Assert.Equal(new[] { "Mechanical Equipment" }, Catalog.Set("M6").Categories);
        Assert.DoesNotContain("Mechanical Equipment", Catalog.Set("M5").Categories);
        Assert.Contains("Lighting Devices", Catalog.Set("M5").Categories);
        Assert.Equal(new[] { "Lighting Fixtures" }, Catalog.Set("M7").Categories);
    }

    [Fact]
    public void No_category_belongs_to_two_base_sets_of_one_discipline()
    {
        foreach (var discipline in Matrix.Groups.GroupBy(g => g.Discipline))
        {
            var categories = discipline.SelectMany(g => Catalog.Set(g.Code).Categories).ToList();
            Assert.Equal(categories.Count, categories.Distinct().Count());
        }
    }

    [Fact]
    public void Flex_ducts_and_pipes_are_not_checked()
    {
        Assert.DoesNotContain(Catalog.AllDefinitions.SelectMany(s => s.AllCategories), c => c.StartsWith("Flex", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_set_says_where_its_conditions_come_from()
    {
        Assert.All(Catalog.AllDefinitions, d => Assert.False(string.IsNullOrWhiteSpace(d.Evidence), d.Code));
    }

    [Fact]
    public void Clash_scope_filters_pipes_by_diameter_and_keeps_fittings_whole()
    {
        var plan = SearchSetPlan.For(Catalog, Matrix, "M4.9");
        Assert.Equal(SetKind.Detail, plan.Kind);
        Assert.Equal("M4", plan.Parent);
        var pipes = plan.Groups.Where(g => g[0].Value == "Pipes").ToList();
        Assert.All(pipes, g => Assert.Contains(g, c => c.Kind == ConditionKind.AtLeast && c.Value == "32"));
        Assert.All(plan.Groups.Where(g => g[0].Value != "Pipes"), g => Assert.Equal(2, g.Count));
        Assert.Equal(new[] { "HP BIMCoordinator", "MEP", "Details", "M4 Clash scope" }, plan.FolderPath);
    }

    [Fact]
    public void A_detail_narrowed_to_one_model_uses_only_that_role()
    {
        var plan = SearchSetPlan.For(Catalog, Matrix, "M2.1");
        Assert.Equal(new[] { "EC" }, plan.Roles);
        Assert.All(plan.Groups, g => Assert.Equal("*-EC-????*", g[1].Value));
    }

    [Fact]
    public void Base_scope_is_the_21_groups_and_extras_are_opt_in()
    {
        Assert.Equal(21, SearchSetPlan.All(Catalog, Matrix).Count);
        Assert.Equal(21 + Catalog.Details.Count + Catalog.Auxiliary.Count, SearchSetPlan.All(Catalog, Matrix, includeExtras: true).Count);
    }
    [Theory]
    [InlineData("THCSLT-HPC-TT-ZZ-M3-EP-0001.nwc", "EP")]
    [InlineData("THCSLT-HPC-LH_HB_HC-ZZ-M3-AA-0001.nwc", "AA")]
    [InlineData(@"Q:\x\02_STR\THBB2-HPC-NTT_CHR-ZZ-NC-ES-0001.nwc", "ES")]
    [InlineData("THCSLT-HPC-ZZ-ZZ-CM-ZZ-0001.nwd", "ZZ")]
    [InlineData("THCSLT-HPC-TT-ZZ-M3-AA-0001_sangtq6ANLE.rvt", "AA")]   // Revit local copy reported by Item > Source File
    [InlineData("gatehouse_pub.nwd", null)]
    [InlineData("", null)]
    public void Iso_role_is_read_before_the_number(string name, string? role)
    {
        Assert.Equal(role, IsoFileName.Role(name));
    }

    [Fact]
    public void Role_wildcard_needs_the_number_after_the_role()
    {
        Assert.Equal("*-ES-????*", SearchSetPlan.RoleWildcard("ES"));
    }

    [Fact]
    public void A_catalog_without_a_group_fails_the_load()
    {
        var json = ClashMatrix.ReadResource(BaseSetCatalog.ResourceName).Replace("\"code\": \"M7\"", "\"code\": \"M8\"");
        Assert.Throws<InvalidOperationException>(() => BaseSetCatalog.Parse(json, Matrix));
    }

    [Theory]
    [InlineData("\"property\": \"structural\"", "\"property\": \"nope\"")]
    [InlineData("\"op\": \"notEquals\"", "\"op\": \"lessThan\"")]
    [InlineData("\"parent\": \"M2\"", "\"parent\": \"Q9\"")]
    public void A_broken_registry_fails_the_load(string from, string to)
    {
        var json = ClashMatrix.ReadResource(BaseSetCatalog.ResourceName);
        Assert.Contains(from, json);
        Assert.Throws<InvalidOperationException>(() => BaseSetCatalog.Parse(json.Replace(from, to), Matrix));
    }
}