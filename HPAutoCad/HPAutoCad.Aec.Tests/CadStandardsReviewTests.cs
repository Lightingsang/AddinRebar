using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Issues;
using HPAutoCad.Aec.Model;
using HPAutoCad.Aec.Standards;
using Xunit;

namespace HPAutoCad.Aec.Tests;

/// <summary>The phase-D review's findings, pinned: empty exemption lists exempt nothing, envelopes stay under the cap at the page maximum,
/// every space is checked on its own, default names cover real conventions, a catastrophic pattern is the caller's error.</summary>
public sealed class CadStandardsReviewTests
{
    private static readonly JsonSerializerOptions Bridge = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, ReferenceHandler = ReferenceHandler.IgnoreCycles, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    private static readonly HashSet<string> AllChecks = new(StandardsIssueType.All, StringComparer.OrdinalIgnoreCase);

    private static AecEntityRecord Entity(string handle, string type, string layer, string color = "ByLayer", string space = "Model", string? style = null) =>
        new() { Handle = handle, Type = type, Layer = layer, Color = color, Linetype = "ByLayer", Lineweight = "ByLayer", Space = space, Style = style, BoundsMm = Box.Of(new Pt(0, 0), new Pt(100, 100)) };

    private static LayerRecord Layer(string name, bool dependent = false, bool hidden = false) => new(name, false, false, false, "7", "Continuous", "ByLineWeightDefault", dependent, hidden);

    private static IReadOnlyList<AuditIssue> Check(string rulesJson, IEnumerable<AecEntityRecord> records, DrawingTables? tables = null, bool whole = true) =>
        CadStandardsChecker.Check(records.ToArray(), tables ?? DrawingTables.Empty, CadStandardsRuleSet.Parse(rulesJson, "t", "test"), AllChecks, whole, CancellationToken.None);

    [Fact]
    public void A_rule_without_exemption_lists_still_checks_everything()
    {
        var tables = new DrawingTables { Layers = [Layer("walls_old"), Layer("S-EMPTY")], Blocks = [new("door 900", false, false, false)] };
        var records = new[] { Entity("L0", "LINE", "0"), Entity("L1", "LINE", "S-COL", color: "1") };

        var naming = Check("""{"layerNaming":{"pattern":"^[A-Z]{1,2}-.*$"}}""", records, tables);
        var zero = Check("""{"layerZero":{}}""", records);
        var overrides = Check("""{"overrides":{"color":true}}""", records);
        var blocks = Check("""{"blockNaming":{"pattern":"^[A-Z0-9-]+$"}}""", records, tables);
        var unused = Check("""{"unusedLayers":{}}""", records, tables);

        Assert.Single(naming, i => i.Type == StandardsIssueType.LayerNaming && i.Layer == "walls_old");
        Assert.Single(zero, i => i.Type == StandardsIssueType.LayerZero && i.Handles[0] == "L0");
        Assert.Single(overrides, i => i.Type == StandardsIssueType.ColorOverride && i.Handles[0] == "L1");
        Assert.Single(blocks, i => i.Type == StandardsIssueType.BlockNaming);
        Assert.Equal(["S-EMPTY", "walls_old"], unused.Where(i => i.Type == StandardsIssueType.UnusedLayer).Select(i => i.Layer).OrderBy(x => x));
        Assert.False(CadStandardsChecker.Exempts([], "anything"));
        Assert.True(CadStandardsChecker.Exempts(["HP-MCP-*"], "HP-MCP-ISSUES"));
    }

    [Fact]
    public void Layer_zero_findings_are_not_doubled_by_the_entity_layer_rule()
    {
        var issues = Check("""{"layerZero":{},"entityLayer":[{"types":["TEXT"],"layers":["*TEXT*"]}]}""", [Entity("T0", "TEXT", "0")]);

        Assert.Single(issues);
        Assert.Equal(StandardsIssueType.LayerZero, issues[0].Type);
    }

    [Fact]
    public void Dependent_and_hidden_symbols_and_block_content_layers_are_never_reported()
    {
        var tables = new DrawingTables
        {
            Layers = [Layer("SITE|walls_old", dependent: true), Layer("*ADSK_CONSTRAINTS", hidden: true), Layer("A-ANNO-TTLB"), Layer("S-COL")],
            Blocks = [new("SITE|door 900", false, false, false, IsDependent: true)],
            LayersUsedInBlocks = ["A-ANNO-TTLB"],
        };

        var issues = Check("""{"layerNaming":{"pattern":"^[A-Z]{1,2}-[A-Z0-9-]+$"},"blockNaming":{"pattern":"^[A-Z0-9-]+$"},"unusedLayers":{}}""", [Entity("C1", "LWPOLYLINE", "S-COL")], tables);

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("S-COLUMNS", true)]
    [InlineData("S-COL-CONCRETE", true)]
    [InlineData("KT-KICHTHUOC", true)]
    [InlineData("A-ANNO-TEXT-0.25", true)]
    [InlineData("M-HVAC-DUCT-SUPL-EXST-L03", true)]
    [InlineData("walls_old", false)]
    [InlineData("Layer1", false)]
    public void Default_layer_naming_accepts_real_ncs_and_vietnamese_names(string name, bool ok)
    {
        var issues = CadStandardsChecker.Check([], new DrawingTables { Layers = [Layer(name)] }, CadStandardsRuleSet.LoadEmbedded(), new HashSet<string> { StandardsIssueType.LayerNaming }, true, CancellationToken.None);
        Assert.Equal(ok, issues.Count == 0);
    }

    [Theory]
    [InlineData("DIMENSION", "KT-TUONG", true)]       // a KT- discipline prefix is not a dimension layer
    [InlineData("DIMENSION", "KT-KICHTHUOC", false)]
    [InlineData("ARC_DIMENSION", "S-COL", true)]       // every dimension flavour is covered
    [InlineData("TEXT", "A-CHUNG", true)]              // *CHU* used to pass this
    [InlineData("TEXT", "S-COL-T", true)]              // *-T used to pass this
    [InlineData("TEXT", "A-ANNO-TEXT", false)]
    [InlineData("TEXT", "GHICHU-1", false)]
    public void Default_entity_layer_stems_are_anchored(string type, string layer, bool reported)
    {
        var issues = CadStandardsChecker.Check([Entity("X", type, layer)], DrawingTables.Empty, CadStandardsRuleSet.LoadEmbedded(), new HashSet<string> { StandardsIssueType.EntityLayer }, false, CancellationToken.None);
        Assert.Equal(reported, issues.Count == 1);
    }

    [Fact]
    public void A_catastrophic_pattern_is_the_rule_files_fault_not_an_engine_failure()
    {
        var rules = CadStandardsRuleSet.Parse("""{"layerNaming":{"pattern":"^(a+)+$"}}""", "t", "test");
        var tables = new DrawingTables { Layers = [Layer(new string('a', 40) + "!")] };

        var error = Assert.Throws<ArgumentException>(() => CadStandardsChecker.Check([], tables, rules, AllChecks, true, CancellationToken.None));

        Assert.Contains("layerNaming/blockNaming.pattern", error.Message);
        Assert.Throws<ArgumentException>(() => CadStandardsRuleSet.Parse("""{"textHeights":{"allowedMm":[2.5],"space":"layouts"}}""", "t", "test"));
    }

    [Fact]
    public void Geometry_issues_never_cross_spaces()
    {
        AecEntityRecord Line(string handle, string space) => new() { Handle = handle, Type = "LINE", Layer = "A-ANNO-TTLB", Space = space, Shape = PlanShape.Segment(new Pt(0, 0), new Pt(841, 0)), BoundsMm = Box.Of(new Pt(0, 0), new Pt(841, 0)) };
        var types = new HashSet<string>(GeometryIssueType.All);

        var twoLayouts = GeometryIssueDetector.Detect([Line("B1", "Layout1"), Line("B2", "Layout2")], types, GeometryTolerance.Default, CancellationToken.None);
        var sameLayout = GeometryIssueDetector.Detect([Line("B1", "Layout1"), Line("B2", "Layout1")], types, GeometryTolerance.Default, CancellationToken.None);

        Assert.Empty(twoLayouts);
        Assert.Single(sameLayout, i => i.Type == GeometryIssueType.Duplicate);
        Assert.Equal("GEO-0001", sameLayout[0].IssueId);
    }

    [Fact]
    public void A_full_page_of_the_longest_issues_stays_under_the_result_cap()
    {
        // The worst case the review measured: entity_layer with a long layer name, two handles, the action text, plus a rich summary.
        var items = Enumerable.Range(0, AecTools.MaxIssueLimit).Select(i => new AuditIssue($"STD-{i + 1:0000}", AuditCategory.Standards, StandardsIssueType.EntityLayer, IssueSeverity.Warning, [$"{0x2A00 + i:X}", $"{0x3A00 + i:X}"], new Pt(123456.7, 98765.4), 1234.56,
            $"MTEXT {0x2A00 + i:X} on 'A-ANNO-TTLB-TEXT-NOTES-SUPPLEMENTARY': text belongs on an annotation layer.", "Move it to a layer rule text-on-annotation-layer allows (update_entities_batch set.layer).", "A-ANNO-TTLB-TEXT-NOTES-SUPPLEMENTARY", "text-on-annotation-layer")).ToArray();
        var result = new AnalysisResult<AuditIssue> { Items = items, Count = 5000, Truncated = true, Summary = new
        {
            examined = 100000, wholeDrawing = true, issues = 5000, bySeverity = new { critical = 10, warning = 4000, info = 990 },
            byType = StandardsIssueType.All.ToDictionary(t => t, _ => 500), checkedTypes = StandardsIssueType.All,
            allowedTextStyles = new[] { "Standard", "HP-NOTES", "HP-TITLE", "ROMANS", "SIMPLEX", "ISOCP" }, allowedTextHeightsMm = new[] { 1.8, 2.5, 3.5, 5.0, 7.0, 10.0 }, allowedDimStyles = new[] { "ISO-25", "HP-DIM" },
            entityLayerRules = CadStandardsRuleSet.LoadEmbedded().EntityLayer.Select(r => new { r.Id, r.Types, r.Layers }).ToArray(),
            ruleSet = new { name = "user", source = "rules\\cad-standards.json", version = 1 },
        } };
        result.Warnings.Add("limit is capped at 100 per page; page with offset.");

        var bytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(result, Bridge));

        Assert.True(bytes < 60_000, $"{bytes} bytes");
    }
}
