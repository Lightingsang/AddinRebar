using HPAutoCad.Aec.Classification;
using HPAutoCad.Aec.Geometry;
using HPAutoCad.Aec.Model;
using Xunit;

namespace HPAutoCad.Aec.Tests;

public sealed class ClassificationTests
{
    private static readonly GeometryTolerance Tol = GeometryTolerance.Default;
    private static readonly ClassificationRuleSet Rules = ClassificationRuleSet.LoadEmbedded();

    private static Pt P(double x, double y) => new(x, y);

    private static AecEntityRecord Rect(string handle, string layer, double x, double y, double w, double h, string type = "LWPOLYLINE") =>
        new() { Handle = handle, Type = type, Layer = layer, Shape = new PlanShape([P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h)], true), BoundsMm = Box.Of(P(x, y), P(x + w, y + h)), AreaMm2 = w * h };

    private static AecEntityRecord Run(string handle, string layer, Pt a, Pt b, string type = "LINE") =>
        new() { Handle = handle, Type = type, Layer = layer, Shape = PlanShape.Segment(a, b), BoundsMm = Box.Of(a, b), LengthMm = a.DistanceXY(b) };

    private static AecEntityRecord Block(string handle, string layer, string name, Pt at, IReadOnlyDictionary<string, string>? attributes = null) =>
        new() { Handle = handle, Type = "INSERT", Layer = layer, BlockName = name, PositionMm = at, Attributes = attributes, Shape = PlanShape.Rectangle(Box.Of(at, at + P(900, 100))), BoundsMm = Box.Of(at, at + P(900, 100)) };

    private static AecEntityRecord Text(string handle, string text, Pt at) =>
        new() { Handle = handle, Type = "TEXT", Layer = "A-TEXT", Text = text, Shape = PlanShape.Rectangle(Box.Of(at, at + P(300, 100))), BoundsMm = Box.Of(at, at + P(300, 100)) };

    private static IReadOnlyList<AecObject> Classify(params AecEntityRecord[] records) =>
        AecClassifier.Classify(records, Rules, AecClassifier.TextIndex.Empty, Tol, CancellationToken.None);

    [Fact]
    public void Embedded_rule_set_loads_and_validates()
    {
        Assert.Equal("default", Rules.Name);
        Assert.True(Rules.Rules.Count >= 20);
        Assert.All(Rules.Rules, r => Assert.True(AecType.IsKnown(r.AecType)));
        Assert.True(Rules.UsesNearbyText);
        Assert.Equal(Rules.Rules.Count, Rules.Rules.Select(r => r.Id).Distinct().Count());
    }

    [Fact]
    public void Column_outline_beam_run_wall_and_pipe_are_classified_with_evidence_and_dimensions()
    {
        var objects = Classify(
            Rect("C1", "S-COL", 0, 0, 400, 600),
            Run("B1", "S-BEAM", P(200, 300), P(5800, 300)),
            Run("W1", "A-WALL", P(0, 3000), P(4000, 3000), "LWPOLYLINE"),
            Run("P1", "M-PIPE-CHW", P(0, 5000), P(9000, 5000), "LWPOLYLINE"));

        var byHandle = objects.ToDictionary(o => o.Handle);
        Assert.Equal(AecType.StructuralColumn, byHandle["C1"].AecType);
        Assert.Equal(Discipline.Structural, byHandle["C1"].Discipline);
        Assert.True(byHandle["C1"].Confidence >= 0.9);
        Assert.Equal(400.0, byHandle["C1"].Properties["widthMm"]);
        Assert.Equal(600.0, byHandle["C1"].Properties["depthMm"]);
        Assert.Contains(byHandle["C1"].Evidence, e => e.Contains("S-COL"));
        Assert.Contains(byHandle["C1"].Evidence, e => e.Contains("400×600"));

        Assert.Equal(AecType.StructuralBeam, byHandle["B1"].AecType);
        Assert.Equal(5600.0, byHandle["B1"].Properties["lengthMm"]);
        Assert.Equal(0.0, byHandle["B1"].Properties["orientationDeg"]);
        Assert.Equal(AecType.ArchitecturalWall, byHandle["W1"].AecType);
        Assert.Equal(AecType.Pipe, byHandle["P1"].AecType);
        Assert.Equal(Discipline.Mep, byHandle["P1"].Discipline);
    }

    [Fact]
    public void Size_rules_reject_a_slab_sized_outline_on_a_column_layer_and_report_it_unknown_only_on_request()
    {
        var slab = Rect("X", "S-COL", 0, 0, 8000, 6000);

        Assert.Empty(Classify(slab));
        var unknown = AecClassifier.Classify([slab], Rules, AecClassifier.TextIndex.Empty, Tol, CancellationToken.None, includeUnknown: true);
        Assert.Equal(AecType.Unknown, Assert.Single(unknown).AecType);
        Assert.Equal(0, unknown[0].Confidence);
    }

    [Fact]
    public void Nearby_text_raises_confidence_and_appears_as_evidence()
    {
        var column = Rect("C1", "S-COL", 0, 0, 400, 400);
        var texts = new AecClassifier.TextIndex([Text("T1", "C12", P(450, 100)), Text("T2", "FAR AWAY", P(9000, 9000))]);

        var plain = AecClassifier.Classify([column], Rules, AecClassifier.TextIndex.Empty, Tol, CancellationToken.None).Single();
        var marked = AecClassifier.Classify([column], Rules, texts, Tol, CancellationToken.None).Single();

        Assert.True(marked.Confidence > plain.Confidence);
        Assert.Contains(marked.Evidence, e => e.Contains("C12"));
    }

    [Fact]
    public void Blocks_classify_by_name_and_layer_with_alternatives_when_two_rules_match()
    {
        var door = Block("D1", "A-DOOR", "DOOR-0900", P(0, 0), new Dictionary<string, string> { ["MARK"] = "D01" });
        var furnitureDoor = Block("D2", "A-FURN", "DOOR-0900", P(0, 0));

        var objects = Classify(door, furnitureDoor).ToDictionary(o => o.Handle);

        Assert.Equal(AecType.Door, objects["D1"].AecType);
        Assert.Equal(0.9, objects["D1"].Confidence);
        Assert.Equal("D01", ((IReadOnlyDictionary<string, string>)objects["D1"].Properties["attributes"])["MARK"]);
        // block name says door (0.9), layer says furniture (0.85): door wins, furniture is the alternative
        Assert.Equal(AecType.Door, objects["D2"].AecType);
        Assert.Contains(objects["D2"].Alternatives, a => a.AecType == AecType.Furniture);
    }

    [Fact]
    public void Discipline_filter_and_min_confidence_narrow_the_answer()
    {
        var records = new[] { Rect("C1", "S-COL", 0, 0, 400, 400), Run("W1", "A-WALL", P(0, 0), P(1000, 0)) };

        var structural = AecClassifier.Classify(records, Rules, AecClassifier.TextIndex.Empty, Tol, CancellationToken.None, new HashSet<string> { Discipline.Structural });
        Assert.Equal(["C1"], structural.Select(o => o.Handle));

        var strict = AecClassifier.Classify(records, Rules, AecClassifier.TextIndex.Empty, Tol, CancellationToken.None, minConfidence: 0.95);
        Assert.Empty(strict);
    }

    [Fact]
    public void Rotated_rectangle_measures_its_own_sides_not_the_bounding_box()
    {
        // 400 × 800 rotated 30°
        var c = Math.Cos(Math.PI / 6);
        var s = Math.Sin(Math.PI / 6);
        Pt R(double x, double y) => new(x * c - y * s, x * s + y * c);
        var record = new AecEntityRecord { Handle = "R", Type = "LWPOLYLINE", Layer = "S-COL", Shape = new PlanShape([R(0, 0), R(800, 0), R(800, 400), R(0, 400)], true) };

        var metrics = ShapeMetrics.Of(record);

        Assert.Equal(400, metrics.WidthMm!.Value, 6);
        Assert.Equal(800, metrics.DepthMm!.Value, 6);
        Assert.Equal(30, metrics.OrientationDeg!.Value, 6);
        Assert.Equal(2, metrics.AspectRatio!.Value, 6);
    }

    [Fact]
    public void Aec_objects_serialise_with_the_bridge_options_and_without_the_record()
    {
        // The bridge serialises with camelCase + IgnoreCycles; a required-but-ignored member would throw InvalidOperationException here.
        var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase, ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
        var result = new AnalysisResult<AecObject> { Items = Classify(Rect("C1", "S-COL", 0, 0, 400, 600), Block("D1", "A-DOOR", "DOOR-0900", P(0, 0), new Dictionary<string, string> { ["MARK"] = "D01" })), Count = 2 };

        var json = System.Text.Json.JsonSerializer.Serialize(result, options);

        Assert.Contains("\"aecType\":\"StructuralColumn\"", json);
        Assert.Contains("\"widthMm\":400", json);
        Assert.Contains("\"attributes\":{\"MARK\":\"D01\"}", json);
        Assert.DoesNotContain("\"record\"", json);
        Assert.DoesNotContain("\"shape\"", json);
    }

    [Fact]
    public void Rule_files_are_validated_on_load()
    {
        Assert.Throws<ArgumentException>(() => ClassificationRuleSet.Parse("""{"rules":[{"id":"x","aecType":"Spaceship"}]}""", "t", "test"));
        Assert.Throws<ArgumentException>(() => ClassificationRuleSet.Parse("""{"rules":[{"id":"x","aecType":"Door","confidence":2}]}""", "t", "test"));
        Assert.Throws<ArgumentException>(() => ClassificationRuleSet.Parse("""{"rules":[{"id":"x","aecType":"Door","textPattern":"("}]}""", "t", "test"));
        Assert.Throws<ArgumentException>(() => ClassificationRuleSet.Parse("""{"rules":[{"id":"x","aecType":"Door"},{"id":"X","aecType":"Window"}]}""", "t", "test"));
        Assert.Throws<ArgumentException>(() => ClassificationRuleSet.Parse("not json", "t", "test"));
        Assert.Throws<ArgumentException>(() => ClassificationRuleSet.Load(@"..\evil"));

        var custom = ClassificationRuleSet.Parse("""{"version":2,"rules":[{"id":"my-col","aecType":"StructuralColumn","confidence":0.7,"layers":["COT*"],"closed":true}]}""", "mine", "test");
        Assert.Equal(2, custom.Version);
        Assert.Equal(AecType.StructuralColumn, AecClassifier.Classify([Rect("C", "COT-BT", 0, 0, 300, 300)], custom, AecClassifier.TextIndex.Empty, Tol, CancellationToken.None).Single().AecType);
    }

    [Theory]
    [InlineData("STRUCT", "LINE", AecType.Unknown)]
    [InlineData("S-STRUCTURE", "LINE", AecType.Unknown)]
    [InlineData("A-CLNG-GRID", "LINE", AecType.Unknown)]
    [InlineData("S-MONG", "LWPOLYLINE", AecType.Unknown)]
    [InlineData("M-DAMPER", "LINE", AecType.Unknown)]
    [InlineData("COTAS", "LINE", AecType.Unknown)]
    [InlineData("COLD-WATER-TAP", "LINE", AecType.Unknown)]
    [InlineData("W-CLOSET", "LINE", AecType.Unknown)]
    [InlineData("STEEL-BRACE", "LINE", AecType.Unknown)]
    [InlineData("CUA-SO", "LINE", AecType.Window)]
    [InlineData("CUASO-1200", "LINE", AecType.Window)]
    [InlineData("ONGGIO", "LINE", AecType.Duct)]
    [InlineData("M-DUCT-SUPPLY", "LINE", AecType.Duct)]
    [InlineData("DAM", "LINE", AecType.StructuralBeam)]
    [InlineData("KC-DAM-T1", "LINE", AecType.StructuralBeam)]
    [InlineData("S-GRID", "LINE", AecType.StructuralGrid)]
    [InlineData("TRUC", "LINE", AecType.StructuralGrid)]
    [InlineData("A-WALL-INT", "LINE", AecType.ArchitecturalWall)]
    [InlineData("TUONG-BAO", "LINE", AecType.ArchitecturalWall)]
    [InlineData("P-SANR-PIPE", "LINE", AecType.Pipe)]
    [InlineData("ONG-CAP", "LINE", AecType.Pipe)]
    public void Layer_stems_are_anchored_to_name_boundaries(string layer, string type, string expected)
    {
        var record = Run("X", layer, P(0, 0), P(4000, 0), type);

        var objects = AecClassifier.Classify([record], Rules, AecClassifier.TextIndex.Empty, Tol, CancellationToken.None, includeUnknown: true);

        Assert.Equal(expected, Assert.Single(objects).AecType);
    }

    [Fact]
    public void Fixture_and_slab_layers_do_not_bleed_into_each_other()
    {
        var fixture = Block("F", "P-SANR-FIXT", "WC-1", P(0, 0));
        var slab = Rect("S", "SAN-T1", 0, 0, 6000, 4000);

        var objects = Classify(fixture, slab).ToDictionary(o => o.Handle);

        Assert.Equal(AecType.Fixture, objects["F"].AecType);
        Assert.Equal(AecType.StructuralSlab, objects["S"].AecType);
    }

    [Fact]
    public void Block_and_text_stand_in_rectangles_are_sized_but_never_closed_footprints()
    {
        var block = Block("B", "S-COL", "COL-400", P(0, 0));
        var closedOnly = ClassificationRuleSet.Parse("""{"rules":[{"id":"ring","aecType":"StructuralColumn","confidence":0.8,"types":["INSERT"],"closed":true}]}""", "t", "test");
        var sized = ClassificationRuleSet.Parse("""{"rules":[{"id":"big","aecType":"Equipment","confidence":0.8,"types":["INSERT"],"minSizeMm":500}]}""", "t", "test");

        var metrics = ShapeMetrics.Of(block);

        Assert.False(metrics.Closed);
        Assert.Equal(100, metrics.WidthMm);
        Assert.Equal(900, metrics.DepthMm);
        Assert.Equal(900, metrics.SizeMm);
        Assert.Empty(AecClassifier.Classify([block], closedOnly, AecClassifier.TextIndex.Empty, Tol, CancellationToken.None));
        Assert.Equal(AecType.Equipment, AecClassifier.Classify([block], sized, AecClassifier.TextIndex.Empty, Tol, CancellationToken.None).Single().AecType);
        Assert.Equal(AecType.StructuralColumn, Classify(block).Single().AecType); // the block-name rule still applies
    }

    [Fact]
    public void Equal_confidence_keeps_rule_file_order()
    {
        var rules = ClassificationRuleSet.Parse(
            """{"rules":[{"id":"first","aecType":"Pipe","confidence":0.8,"layers":["X*"]},{"id":"second","aecType":"Duct","confidence":0.8,"layers":["X*"]},{"id":"third","aecType":"CableTray","confidence":0.9,"layers":["X*"]}]}""",
            "t", "test");

        var o = AecClassifier.Classify([Run("R", "X-1", P(0, 0), P(100, 0))], rules, AecClassifier.TextIndex.Empty, Tol, CancellationToken.None).Single();

        Assert.Equal("third", o.RuleId);
        Assert.Equal(["first", "second"], o.Alternatives.Select(a => a.RuleId));
    }

    [Fact]
    public void Rule_files_refuse_null_criteria_silently_widening_a_rule_and_misspelt_keys()
    {
        var nulls = ClassificationRuleSet.Parse("""{"rules":[{"id":"x","aecType":"Door","layers":null,"types":null,"blockNames":null}]}""", "t", "test");
        Assert.Empty(nulls.Rules[0].Layers);
        Assert.Equal(AecType.Door, AecClassifier.Classify([Run("R", "ANY", P(0, 0), P(100, 0))], nulls, AecClassifier.TextIndex.Empty, Tol, CancellationToken.None).Single().AecType);

        var typo = Assert.Throws<ArgumentException>(() => ClassificationRuleSet.Parse("""{"rules":[{"id":"x","aecType":"Door","layer":["A-DOOR*"]}]}""", "t", "test"));
        Assert.Contains("not valid JSON", typo.Message);
        Assert.Throws<ArgumentException>(() => ClassificationRuleSet.Parse("""{"rules":[{"id":"x","aecType":"Door","minSizeMm":-1}]}""", "t", "test"));
        Assert.Throws<ArgumentException>(() => ClassificationRuleSet.Parse("""{"rules":[{"id":"x","aecType":"Door","nearbyTextBoost":0.9}]}""", "t", "test"));
        Assert.Throws<ArgumentException>(() => ClassificationRuleSet.Parse("""{"rules":[{"id":"x","aecType":"Door","layers":["A-DOOR", ""]}]}""", "t", "test"));
        Assert.Throws<ArgumentException>(() => ClassificationRuleSet.Parse("""{"rules":null}""", "t", "test"));
    }

    [Fact]
    public void Verbose_text_and_attributes_are_cut_so_one_block_cannot_fill_a_page()
    {
        var attributes = Enumerable.Range(0, 12).ToDictionary(i => $"TAG{i}", i => new string('x', 500));
        var block = Block("B", "A-DOOR", "DOOR-1", P(0, 0), attributes);

        var properties = Classify(block).Single().Properties;

        var carried = (IReadOnlyDictionary<string, string>)properties["attributes"];
        Assert.Equal(AecClassifier.MaxAttributes, carried.Count);
        Assert.All(carried.Values, v => Assert.Equal(AecClassifier.MaxTextChars + 1, v.Length));
        Assert.Equal(12 - AecClassifier.MaxAttributes, properties["attributesTruncated"]);
    }

    [Fact]
    public void Rule_set_source_never_names_the_profile_folder()
    {
        Assert.Equal("embedded", Rules.Source);
        Assert.DoesNotContain("AppData", Rules.Source, StringComparison.OrdinalIgnoreCase);
    }
}
