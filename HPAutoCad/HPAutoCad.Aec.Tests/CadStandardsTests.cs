using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Standards;
using Xunit;

namespace HPAutoCad.Aec.Tests;

public sealed class CadStandardsTests
{
    private static readonly CadStandardsRuleSet Rules = CadStandardsRuleSet.LoadEmbedded();
    private static readonly HashSet<string> AllChecks = new(StandardsIssueType.All, StringComparer.OrdinalIgnoreCase);

    private static AecEntityRecord Entity(string handle, string type, string layer, string color = "ByLayer", string linetype = "ByLayer", string lineweight = "ByLayer", string? style = null, double? heightMm = null, string space = "Model") =>
        new() { Handle = handle, Type = type, Layer = layer, Color = color, Linetype = linetype, Lineweight = lineweight, Style = style, TextHeightMm = heightMm, Space = space, BoundsMm = Box.Of(new Pt(0, 0), new Pt(100, 100)) };

    private static LayerRecord Layer(string name, bool xref = false) => new(name, false, false, false, "7", "Continuous", "ByLineWeightDefault", xref);

    private static IReadOnlyList<AuditIssue> Check(IEnumerable<AecEntityRecord> records, DrawingTables? tables = null, CadStandardsRuleSet? rules = null, IReadOnlySet<string>? checks = null, bool whole = true) =>
        CadStandardsChecker.Check(records.ToArray(), tables ?? DrawingTables.Empty, rules ?? Rules, checks ?? AllChecks, whole, CancellationToken.None);

    [Fact]
    public void Embedded_rule_set_loads_and_enables_the_documented_checks()
    {
        Assert.Equal("default", Rules.Name);
        Assert.Equal("embedded", Rules.Source);
        var enabled = Rules.EnabledChecks();
        Assert.Contains(StandardsIssueType.LayerNaming, enabled);
        Assert.Contains(StandardsIssueType.EntityLayer, enabled);
        Assert.Contains(StandardsIssueType.LayerZero, enabled);
        Assert.Contains(StandardsIssueType.ColorOverride, enabled);
        Assert.Contains(StandardsIssueType.BlockNaming, enabled);
        Assert.Contains(StandardsIssueType.UnusedLayer, enabled);
        Assert.DoesNotContain(StandardsIssueType.TextStyle, enabled); // empty allowed list = not checked
        Assert.DoesNotContain(StandardsIssueType.TextHeight, enabled);
    }

    [Theory]
    [InlineData("S-COL", true)]
    [InlineData("A-WALL-INTR", true)]
    [InlineData("M-HVAC-DUCT-SUPL", true)]
    [InlineData("KC-DAM", true)]
    [InlineData("0", true)]
    [InlineData("Defpoints", true)]
    [InlineData("HP-MCP-ISSUES", true)]
    [InlineData("walls_old", false)]
    [InlineData("Layer1", false)]
    [InlineData("s-col", false)]
    [InlineData("S_COL", false)]
    public void Layer_naming_follows_the_discipline_major_pattern_with_exemptions(string name, bool ok)
    {
        var issues = Check([], new DrawingTables { Layers = [Layer(name)] }, checks: new HashSet<string> { StandardsIssueType.LayerNaming });

        Assert.Equal(ok, issues.Count == 0);
        if (!ok) Assert.Equal(name, issues[0].Layer);
    }

    [Fact]
    public void Xref_dependent_layers_are_never_reported()
    {
        var issues = Check([], new DrawingTables { Layers = [Layer("BG|walls_old", xref: true)] });
        Assert.Empty(issues);
    }

    [Fact]
    public void Entity_layer_layer_zero_and_override_checks_report_per_entity_with_the_rule_named()
    {
        var issues = Check([
            Entity("T1", "TEXT", "S-COL"),                                  // text off an annotation layer
            Entity("T2", "TEXT", "A-TEXT"),                                 // fine
            Entity("L1", "LINE", "0"),                                      // layer 0
            Entity("B1", "INSERT", "0"),                                    // blocks may sit on 0
            Entity("L2", "LINE", "S-BEAM", color: "1"),                     // colour override
            Entity("L3", "LINE", "S-BEAM", linetype: "HIDDEN"),             // linetype override
            Entity("L4", "LINE", "S-BEAM", lineweight: "LineWeight050"),   // lineweight override
            Entity("H1", "HATCH", "S-COL", color: "8"),                     // hatch: colour exempt, but not on a hatch layer
            Entity("M1", "LINE", "HP-MCP-ISSUES", color: "1"),              // markup layer exempt from overrides
        ]);

        var byType = issues.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.Select(i => i.Handles[0]).ToArray());
        Assert.Equal(["H1", "T1"], byType[StandardsIssueType.EntityLayer].OrderBy(h => h));
        Assert.Equal(["L1"], byType[StandardsIssueType.LayerZero]);
        Assert.Equal(["L2"], byType[StandardsIssueType.ColorOverride]);
        Assert.Equal(["L3"], byType[StandardsIssueType.LinetypeOverride]);
        Assert.Equal(["L4"], byType[StandardsIssueType.LineweightOverride]);
        Assert.Contains("0.5 mm", issues.Single(i => i.Handles[0] == "L4").Description);
        Assert.Equal("text-on-annotation-layer", issues.Single(i => i.Handles[0] == "T1").Rule);
        Assert.Equal("HP-MCP-ISSUES", issues.SingleOrDefault(i => i.Handles.Contains("M1"))?.Layer ?? "HP-MCP-ISSUES"); // no issue on M1
        Assert.DoesNotContain(issues, i => i.Handles.Contains("M1") || i.Handles.Contains("B1") || i.Handles.Contains("T2"));
        Assert.All(issues, i => Assert.NotNull(i.LocationMm));
    }

    [Fact]
    public void Style_and_height_rules_apply_only_when_the_set_lists_allowed_values_and_the_space_matches()
    {
        var rules = CadStandardsRuleSet.Parse("""{"textStyles":{"allowed":["Standard","HP-*"]},"textHeights":{"allowedMm":[2.5,3.5],"space":"paper"},"dimStyles":{"allowed":["ISO-25"]}}""", "t", "test");
        var issues = Check([
            Entity("T1", "TEXT", "A-TEXT", style: "Arial", heightMm: 3.5, space: "Layout1"),   // style wrong, height fine
            Entity("T2", "MTEXT", "A-TEXT", style: "HP-NOTE", heightMm: 4, space: "Layout1"),  // height wrong
            Entity("T3", "TEXT", "A-TEXT", style: "Standard", heightMm: 250, space: "Model"),  // model space: height not checked
            Entity("D1", "DIMENSION", "A-DIMS", style: "Standard"),                            // dim style wrong
        ], rules: rules);

        Assert.Equal(["T1"], issues.Where(i => i.Type == StandardsIssueType.TextStyle).Select(i => i.Handles[0]));
        Assert.Equal(["T2"], issues.Where(i => i.Type == StandardsIssueType.TextHeight).Select(i => i.Handles[0]));
        Assert.Equal(4, issues.Single(i => i.Type == StandardsIssueType.TextHeight).ValueMm);
        Assert.Equal(["D1"], issues.Where(i => i.Type == StandardsIssueType.DimStyle).Select(i => i.Handles[0]));
    }

    [Fact]
    public void Unused_layers_and_block_names_are_table_checks_and_unused_needs_the_whole_drawing()
    {
        var tables = new DrawingTables
        {
            Layers = [Layer("S-COL"), Layer("S-UNUSED"), Layer("0"), Layer("Defpoints")],
            Blocks = [new("DOOR-0900", false, false, false), new("door 900", false, false, false), new("*U12", true, false, false), new("*Model_Space", false, true, false), new("A$C1234", false, false, false)],
        };
        var records = new[] { Entity("C1", "LWPOLYLINE", "S-COL") };

        var whole = Check(records, tables);
        var subset = Check(records, tables, whole: false);

        Assert.Equal(["S-UNUSED"], whole.Where(i => i.Type == StandardsIssueType.UnusedLayer).Select(i => i.Layer));
        Assert.DoesNotContain(subset, i => i.Type == StandardsIssueType.UnusedLayer);
        Assert.Single(whole.Where(i => i.Type == StandardsIssueType.BlockNaming), i => i.Description.Contains("'door 900'"));
    }

    [Fact]
    public void Ids_follow_the_stable_severity_order_on_every_run()
    {
        var records = new[] { Entity("L2", "LINE", "S-BEAM", color: "1"), Entity("T1", "TEXT", "S-COL"), Entity("L1", "LINE", "0") };
        var tables = new DrawingTables { Layers = [Layer("walls_old")] };

        var first = Check(records, tables);
        var second = Check(records.Reverse(), tables);

        Assert.Equal(first.Select(i => (i.IssueId, i.Type, i.Handles.FirstOrDefault(), i.Layer)), second.Select(i => (i.IssueId, i.Type, i.Handles.FirstOrDefault(), i.Layer)));
        Assert.Equal("STD-0001", first[0].IssueId);
        Assert.Equal(IssueSeverity.Warning, first[0].Severity);            // warnings before the info-level colour override
        Assert.Equal(IssueSeverity.Info, first[^1].Severity);
        Assert.Equal([StandardsIssueType.ColorOverride, StandardsIssueType.UnusedLayer], first.Where(i => i.Severity == IssueSeverity.Info).Select(i => i.Type)); // info issues sorted by type
    }

    [Fact]
    public void Rule_files_are_validated_and_misspelt_keys_refused()
    {
        Assert.Throws<ArgumentException>(() => CadStandardsRuleSet.Parse("""{"layerNaming":{"pattern":"("}}""", "t", "test"));
        Assert.Throws<ArgumentException>(() => CadStandardsRuleSet.Parse("""{"layerZero":{"severity":"fatal"}}""", "t", "test"));
        Assert.Throws<ArgumentException>(() => CadStandardsRuleSet.Parse("""{"entityLayer":[{"types":["TEXT"]}]}""", "t", "test"));
        Assert.Throws<ArgumentException>(() => CadStandardsRuleSet.Parse("""{"layerNamng":{"pattern":"x"}}""", "t", "test"));
        Assert.Throws<ArgumentException>(() => CadStandardsRuleSet.Parse("""{"textHeights":{"allowedMm":[0]}}""", "t", "test"));
        Assert.Throws<ArgumentException>(() => CadStandardsRuleSet.Load(@"..\evil"));

        var minimal = CadStandardsRuleSet.Parse("""{"layerNaming":{"pattern":"^X-","exempt":null}}""", "mine", "test");
        Assert.Equal([StandardsIssueType.LayerNaming], minimal.EnabledChecks());
        Assert.Empty(minimal.LayerNaming!.Exempt);
    }

    [Fact]
    public void Audit_issue_ordering_and_severity_floor()
    {
        AuditIssue Make(string id, string category, string severity, string type, string handle) => new(id, category, type, severity, [handle], null, null, "d");
        var ordered = AuditIssue.Ordered([
            Make("STD-0001", AuditCategory.Standards, IssueSeverity.Info, "unused_layer", "B"),
            Make("GEO-0002", AuditCategory.Geometry, IssueSeverity.Critical, "self_intersection", "Z"),
            Make("GEO-0001", AuditCategory.Geometry, IssueSeverity.Warning, "endpoint_gap", "A"),
            Make("STD-0002", AuditCategory.Standards, IssueSeverity.Warning, "layer_zero", "A"),
        ]);

        Assert.Equal(["GEO-0002", "GEO-0001", "STD-0002", "STD-0001"], ordered.Select(i => i.IssueId));
        Assert.True(ordered[0].AtLeast(IssueSeverity.Warning));
        Assert.False(ordered[^1].AtLeast(IssueSeverity.Warning));
        Assert.Equal(AuditCategory.Geometry, AuditIssue.From(new GeometryIssue("GEO-0009", "duplicate", IssueSeverity.Warning, ["A", "B"], null, 0, 1, "dup")).Category);
    }
}
