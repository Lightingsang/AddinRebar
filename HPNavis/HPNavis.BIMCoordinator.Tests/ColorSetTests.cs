using System.Security.Cryptography;
using HPNavis.BIMCoordinator.Colors;
using HPNavis.BIMCoordinator.Rules;
using HPNavis.BIMCoordinator.SearchSets;
using Xunit;

namespace HPNavis.BIMCoordinator.Tests;

/// <summary>The colour registry is the company sheet ColorSearchSet(DSC) plus the agreed mapping, and painting is one colour per element.</summary>
public sealed class ColorSetTests
{
    private static readonly ColorSetCatalog Colors = ColorSetCatalog.Default;
    private static readonly BaseSetCatalog Catalog = BaseSetCatalog.Default;

    [Fact]
    public void Registry_holds_the_44_sheet_sets_in_sheet_order()
    {
        Assert.Equal(44, Colors.Sets.Count);
        Assert.Equal(Enumerable.Range(1, 44), Colors.Sets.Select(s => s.Order));
        Assert.Equal("HP_A_All", Colors.Sets[0].DisplayName);
        Assert.Equal("HP_M_Pipe_Liquid", Colors.Sets[43].DisplayName);
        Assert.Equal(5, Colors.Sets.Count(s => s.Rgb is null));
    }

    [Theory]
    [InlineData("HP_E_CableTray(ELV)", 65, 100, 210)]
    [InlineData("HP_E_CableTray(LV)", 255, 128, 64)]
    [InlineData("HP_F_FireSprinkler", 255, 0, 0)]
    [InlineData("HP_P_Drainage_SoilPipe", 128, 128, 64)]
    [InlineData("HP_P_Supply_ColdWater", 101, 182, 101)]
    [InlineData("HP_M_Equipment", 128, 128, 255)]
    public void Rgb_comes_from_the_sheet(string name, int r, int g, int b)
    {
        Assert.Equal(new[] { r, g, b }, Colors.Set(name).Rgb);
    }

    [Theory]
    [InlineData("HP_A_All")]
    [InlineData("HP_S_All")]
    [InlineData("HP_E_CommunicationDevices")]
    [InlineData("HP_E_Conduit")]
    [InlineData("HP_E_DataDevices")]
    public void Default_sets_never_paint(string name)
    {
        Assert.False(Colors.Set(name).Paints);
    }

    [Fact]
    public void Registry_was_generated_from_the_committed_workbook()
    {
        var workbook = Path.Combine(ClashMatrixTests.HPNavisRoot(), "tools", "bim-coordinator", Colors.Source.Workbook);
        using var sha = SHA256.Create();
        var hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(workbook))).Replace("-", "").ToLowerInvariant();
        Assert.Equal(hash, Colors.Source.Sha256);
    }

    [Fact]
    public void User_decisions_are_in_the_mapping()
    {
        Assert.Equal(new[] { "EC", "EF" }, Colors.Set("HP_E_CableTray(ELV)").Roles);
        Assert.Equal(new[] { "EE" }, Colors.Set("HP_E_CableTray(LV)").Roles);
        Assert.Equal(new[] { "TP", "TNT" }, Colors.Set("HP_P_Drainage_WastePipe").Conditions.Single().Alternatives);
        Assert.Equal("SH", Colors.Set("HP_P_Drainage_SoilPipe").Conditions.Single().Value);
        Assert.Equal("TN(BM)", Colors.Set("HP_P_Drainage_KitchenPipe").Conditions.Single().Value);
        Assert.Equal("Fire Protection Wet", Colors.Set("HP_F_FireSprinkler").Conditions.Single().Value);
    }

    [Fact]
    public void Sets_without_a_reliable_rule_are_pending_and_never_built()
    {
        var pending = Colors.Sets.Where(s => s.Pending is not null).Select(s => s.DisplayName).ToList();
        Assert.Contains("HP_P_Supply_RawWater", pending);
        Assert.Contains("HP_M_Pipe_Gas", pending);
        Assert.DoesNotContain(Colors.Plans(Catalog), p => pending.Contains(p.DisplayName));
        Assert.Throws<ArgumentException>(() => Colors.Plans(Catalog, new[] { "HP_M_Pipe_Gas" }));
    }

    [Fact]
    public void Colour_sets_live_in_their_own_folder_under_their_sheet_names()
    {
        var plan = Colors.Plans(Catalog, new[] { "C17" }).Single();
        Assert.Equal(new[] { "HP BIMCoordinator", "Color" }, plan.FolderPath);
        Assert.Equal("HP_P_Drainage_RainWater", plan.DisplayName);
        Assert.Equal(SetKind.Color, plan.Kind);
        Assert.All(plan.Groups, g => Assert.Equal("TNM", g[2].Value));
        Assert.All(plan.Groups, g => Assert.True(g[2].Property.ByDisplayName));
    }

    [Fact]
    public void Value_alternatives_become_one_group_each()
    {
        var plan = Colors.Plans(Catalog, new[] { "HP_P_Drainage_WastePipe" }).Single();
        Assert.Equal(Catalog.RolesOf("MEP").Count * 2, plan.Groups.Count);
        Assert.Equal(new[] { "TNT", "TP" }, plan.Groups.Select(g => g[2].Value).Distinct().OrderBy(v => v, StringComparer.Ordinal));
    }

    [Fact]
    public void A_discipline_wide_set_matches_any_category_of_its_files()
    {
        var plan = Colors.Plans(Catalog, new[] { "HP_A_All" }).Single();
        var group = Assert.Single(plan.Groups);
        Assert.Equal(ConditionKind.Wildcard, group[0].Kind);
        Assert.Equal("*", group[0].Value);
        Assert.Equal("*-AA-????*", group[1].Value);
    }

    [Theory]
    [InlineData("\"rgb\": [\n        65,", "\"rgb\": [\n        265,")]
    [InlineData("\"discipline\": \"ARC\"", "\"discipline\": \"XYZ\"")]
    [InlineData("\"roles\": [\n        \"EE\"\n      ]", "\"roles\": [\n        \"AA\"\n      ]")]
    public void A_broken_colour_registry_fails_the_load(string from, string to)
    {
        var json = ClashMatrix.ReadResource(ColorSetCatalog.ResourceName).Replace("\r\n", "\n");
        Assert.Contains(from, json);
        Assert.Throws<InvalidOperationException>(() => ColorSetCatalog.Parse(json.Replace(from, to), Catalog));
    }

    [Fact]
    public void Paint_order_is_sheet_order_without_default_or_pending_sets()
    {
        var order = ColorPaintPlan.PaintOrder(Colors.Sets.AsEnumerable().Reverse());
        Assert.Equal(order.Select(e => e.Order).OrderBy(o => o), order.Select(e => e.Order));
        Assert.All(order, e => Assert.True(e.Rgb is not null && e.Pending is null));
        Assert.Equal(Colors.Sets.Count(s => s.Paints), order.Count);
        Assert.True(order.ToList().FindIndex(e => e.DisplayName == "HP_P_PipeFitting+Accessory") > order.ToList().FindIndex(e => e.DisplayName == "HP_P_Supply_ColdWater"));
    }

    [Fact]
    public void The_last_painting_set_holding_an_element_decides_its_colour()
    {
        var red = new[] { 255, 0, 0 };
        var grey = new[] { 192, 192, 192 };
        var order = new List<(IReadOnlyList<int>, Func<string, bool>)>
        {
            (red, e => e is "pipe" or "fitting"),
            (grey, e => e == "fitting"),
        };
        Assert.Same(red, ColorPaintPlan.ExpectedRgb(new[] { "pipe" }, order));
        Assert.Same(grey, ColorPaintPlan.ExpectedRgb(new[] { "fitting" }, order));
        Assert.Null(ColorPaintPlan.ExpectedRgb(new[] { "wall" }, order));
    }

    [Fact]
    public void A_nested_element_takes_the_colour_of_the_last_set_reaching_it_through_any_ancestor()
    {
        var blue = new[] { 0, 0, 255 };
        var orange = new[] { 255, 128, 0 };
        var equipmentFirst = new List<(IReadOnlyList<int>, Func<string, bool>)> { (blue, e => e == "ahu"), (orange, e => e == "valve") };
        var valveFirst = new List<(IReadOnlyList<int>, Func<string, bool>)> { (orange, e => e == "valve"), (blue, e => e == "ahu") };
        string[] valveGeometry = { "valve-solid", "valve", "ahu", "level" };
        string[] ahuGeometry = { "ahu-casing", "ahu", "level" };

        Assert.Same(orange, ColorPaintPlan.ExpectedRgb(valveGeometry, equipmentFirst));
        Assert.Same(blue, ColorPaintPlan.ExpectedRgb(valveGeometry, valveFirst));
        Assert.Same(blue, ColorPaintPlan.ExpectedRgb(ahuGeometry, equipmentFirst));
    }

    [Fact]
    public void A_paint_run_that_found_no_painting_set_or_missed_one_is_not_usable()
    {
        var painting = new ColorSetPaint("C03", "HP_E_CableTray(ELV)", new[] { 65, 100, 210 }, 42);
        var defaultSet = new ColorSetPaint("C01", "HP_A_All", null, 1000);
        var none = Array.Empty<ColorCheck>();
        var pending = new ColorSetSkip("C24", "HP_P_Supply_RawWater", "pending: no rule", false);
        var missing = new ColorSetSkip("C17", "HP_P_Drainage_RainWater", "not in the document", true);

        Assert.True(new ColorPaintOutcome(new[] { painting }, new[] { pending }, none, 42, 0).Usable);
        Assert.False(new ColorPaintOutcome(new[] { defaultSet }, Array.Empty<ColorSetSkip>(), none, 0, 0).Usable);
        Assert.False(new ColorPaintOutcome(Array.Empty<ColorSetPaint>(), new[] { missing }, none, 0, 0).Usable);
        Assert.False(new ColorPaintOutcome(new[] { painting }, new[] { missing }, none, 42, 0).Usable);
    }

    public static IEnumerable<object[]> TautologicalOrRedundantDefinitions()
    {
        ConditionDefinition SystemType(string op, string type, params string[] values) => new() { Property = "systemType", Op = op, Type = type, Values = values.ToList() };
        SetDefinition Pipes(params ConditionDefinition[] conditions) => new() { Code = "X1", Name = "probe", Categories = new() { "Pipes" }, Conditions = conditions.ToList() };

        yield return new object[] { Pipes(SystemType("notEquals", "string", "TNM", "SH")) };
        yield return new object[] { Pipes(SystemType("equals", "bool", "true", "false")) };
        yield return new object[] { Pipes(new ConditionDefinition { Property = "systemType", Value = "TNM", Values = new() { "SH" } }) };
        yield return new object[] { Pipes(SystemType("equals", "string", "TNM", "TNM")) };
        yield return new object[] { new SetDefinition { Code = "X2", Name = "probe", Categories = new() { "*", "Pipes" } } };
        yield return new object[] { new SetDefinition { Code = "X3", Name = "probe", Categories = new() { "Pipes", "Pipes" } } };
        yield return new object[]
        {
            new SetDefinition
            {
                Code = "X4", Name = "probe", Categories = new() { "Pipes", "Ducts", "Conduits" },
                Conditions = new() { SystemType("equals", "string", Enumerable.Range(1, 17).Select(i => $"S{i}").ToArray()) },
            },
        };
    }

    [Theory]
    [MemberData(nameof(TautologicalOrRedundantDefinitions))]
    public void A_definition_that_would_match_everything_or_explode_fails_validation(SetDefinition definition)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Catalog.ValidateDefinition(definition, "colour registry"));
        Assert.StartsWith("colour registry: X", error.Message);
    }

    [Theory]
    [InlineData(65 / 255.0, 100 / 255.0, 210 / 255.0, true)]
    [InlineData(66 / 255.0, 100 / 255.0, 210 / 255.0, false)]
    [InlineData(65.4 / 255.0, 100 / 255.0, 210 / 255.0, true)]
    public void Read_back_tolerates_half_a_colour_step(double r, double g, double b, bool expected)
    {
        Assert.Equal(expected, ColorPaintPlan.Matches(new[] { 65, 100, 210 }, r, g, b));
    }
}
