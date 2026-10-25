using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using HPRebar.Core.KataExport.Calculators;
using HPRebar.Core.KataExport.Models;
using HPRebar.Core.KataExport.Web;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;
using HPRebar.Core.Tests.KataRebar;
using Xunit;
using static HPRebar.Core.Tests.KataExport.TestKataData;

namespace HPRebar.Core.Tests.KataExport;

/// <summary>
/// The records the Kata Export page draws: the same geometry as the drawing builders, in the JSON shape the page
/// reads, and the fixture the page's tests draw (B03, as "Dam kata test.rvt" models it) kept in step with the mapper.
/// Run with HPREBAR_UPDATE_KATA_FIXTURES=1 to rewrite the fixtures.
/// </summary>
public sealed class KataWebSceneMapperTests
{
    private const string UpdateVariable = "HPREBAR_UPDATE_KATA_FIXTURES";

    /// <summary>The page's wire format: HPWebHost's BridgeJson options (web defaults, camelCase enums).</summary>
    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
    };

    [Fact]
    public void Without_a_plan_the_scene_has_the_elevation_only()
    {
        var elevation = Elevation(TwoSpansOnColumns());

        var scene = KataWebSceneMapper.Map(elevation, null, KataStationMap.Identity);

        Assert.Null(scene.Rebar);
        Assert.Equal(new[] { "C", "D", "E", "F", "G" }, scene.Elevation.Columns.Select(c => c.Letter));
        Assert.Equal(new KataWebRange(400, 6000), scene.Elevation.Columns[1].Extent);
        Assert.Equal(elevation.TopMm, scene.Elevation.TopMm);
        Assert.Equal(elevation.BottomMm, scene.Elevation.BottomMm);
        Assert.Equal(new[] { 200.0, 6200.0, 12200.0 }, scene.Elevation.Grids.Select(g => g.X));
    }

    [Fact]
    public void The_rebar_layer_is_what_the_drawing_builders_draw()
    {
        var plan = B03Plan();
        var tags = KataBarTagBuilder.Build(plan.Spec, plan.Layout, plan.Rules.StirrupDiameter);
        var cuts = KataSectionCuts.Build(plan.Spec, plan.Layout);
        var drawing = KataElevationDrawingBuilder.Build(plan.Spec, plan.Layout, cuts, plan.Rules.StirrupDiameter, KataBarTagBuilder.StirrupRowOf(tags));

        var rebar = KataWebSceneMapper.Map(B03Elevation(), plan, KataStationMap.Identity).Rebar!;

        Assert.Equal(drawing.Lines.Count, rebar.Lines.Count);
        Assert.Equal(drawing.Lines.Select(l => l.Pen), rebar.Lines.Select(l => l.Pen));
        Assert.Equal(drawing.Lines.Select(l => l.Points.Count), rebar.Lines.Select(l => l.Points.Count));
        Assert.Equal(drawing.Lines.Select(l => l.Keys ?? Array.Empty<string>()), rebar.Lines.Select(l => l.Keys));
        Assert.Equal(drawing.Dims.Select(d => d.Text), rebar.Dims.Select(d => d.Text));
        Assert.Equal(tags.Select(t => t.InsertX).Select(x => Math.Round(x, 1)), rebar.Tags.Select(t => t.InsertX));
        Assert.Equal(cuts.Select(c => c.Number), rebar.Cuts.Select(c => c.Number));
        Assert.Equal(plan.Layout.TotalBarCount, rebar.BarCount);
        Assert.Contains(rebar.Lines, l => l.Pen == KataDrawingPen.Bar && l.Keys.Count > 0);
    }

    [Fact]
    public void Lengths_are_rounded_to_a_tenth_of_a_millimetre()
    {
        var rebar = KataWebSceneMapper.Map(B03Elevation(), B03Plan(), KataStationMap.Identity).Rebar!;

        Assert.All(rebar.Lines.SelectMany(l => l.Points), p =>
        {
            Assert.Equal(Math.Round(p.X, 1), p.X);
            Assert.Equal(Math.Round(p.Z, 1), p.Z);
        });
    }

    [Fact]
    public void A_section_is_built_on_request_and_an_unknown_number_has_none()
    {
        var plan = B03Plan();
        var cut = KataSectionCuts.Build(plan.Spec, plan.Layout)[0];
        var expected = KataSectionDrawingBuilder.Build(plan.Spec, plan.Layout, plan.Rules, cut, mirror: true);

        var section = KataWebSceneMapper.MapSection(plan, cut.Number, mirror: true)!;

        Assert.Equal(cut.Number, section.Number);
        Assert.True(section.Mirror);
        Assert.Equal(expected.Bars.Count, section.Bars.Count);
        Assert.Equal(expected.Tags.Select(t => t.BlockState), section.Tags.Select(t => t.BlockState));
        Assert.Equal(expected.Lines.SelectMany(l => l.Vertices).Select(v => v.Bulge), section.Lines.SelectMany(l => l.Vertices).Select(v => v.Bulge));
        Assert.Null(KataWebSceneMapper.MapSection(plan, 999, mirror: false));
    }

    [Fact]
    public void The_wire_shape_has_points_as_objects_and_enums_as_words()
    {
        var json = JsonSerializer.Serialize(KataWebSceneMapper.Map(B03Elevation(), B03Plan(), KataStationMap.Identity), Wire);

        Assert.Contains("\"pen\": \"bar\"", json);
        Assert.Contains("\"kind\": \"support\"", json);
        Assert.Matches("\"points\": \\[\\s*\\{\\s*\"x\": ", json);
        Assert.DoesNotContain("item1", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void The_scene_stays_small()
    {
        var json = JsonSerializer.Serialize(KataWebSceneMapper.Map(B03Elevation(), B03Plan(), KataStationMap.Identity), Wire);

        // Four spans, 33.6 m: the page should never need more than a few hundred kilobytes for one run.
        Assert.InRange(Encoding.UTF8.GetByteCount(json), 1, 400_000);
    }

    [Fact]
    public void The_page_fixtures_match_the_mapper()
    {
        var plan = B03Plan();
        var cut = KataSectionCuts.Build(plan.Spec, plan.Layout)[0];
        var fixtures = new Dictionary<string, object>
        {
            ["kata-scene-b03.json"] = KataWebSceneMapper.Map(B03Elevation(), plan, B03StationMap()),
            [$"kata-section-b03-{cut.Number}.json"] = KataWebSceneMapper.MapSection(plan, cut.Number, mirror: false)!,
        };

        bool update = Environment.GetEnvironmentVariable(UpdateVariable) == "1";
        var stale = new List<string>();
        foreach (var (name, value) in fixtures)
        {
            var path = Path.Combine(FixtureFolder(), name);
            var expected = JsonSerializer.Serialize(value, Wire).ReplaceLineEndings("\n") + "\n";
            var actual = File.Exists(path) ? File.ReadAllText(path).ReplaceLineEndings("\n") : null;
            if (actual == expected) continue;

            stale.Add(name);
            if (!update) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, expected, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        // A rewrite fails once too, so a run that left the variable set never hides drift.
        Assert.True(stale.Count == 0, update
            ? $"Rewritten from the mapper, review and commit: {string.Join(", ", stale)}."
            : $"Missing or out of date with KataWebSceneMapper: {string.Join(", ", stale)}. Run `dotnet test` with {UpdateVariable}=1 and commit them.");
    }

    private static KataElevation Elevation(KataRunInput input)
    {
        var options = new KataBuildOptions();
        return KataElevationBuilder.Build(input, options, KataRowBuilder.Build(input, options));
    }

    // B03 as "Dam kata test.rvt" models it (read 2026-10-06): 400 columns, spans 10400 | 13400 | 6200, console 2000,
    // span 1 1200 deep, the rest 900; span 3 and the console 300 wide, the console's top 200 down.
    private static readonly double[] B03Lengths = { 400, 10400, 400, 13400, 400, 6200, 400, 2000 };

    private static KataRebarPlan B03Plan()
    {
        var segments = B03Lengths.Select((l, i) => new KataMeasuredSegment(
            i % 2 == 0 ? KataMeasuredSupportKind.Column : KataMeasuredSupportKind.None, l, i == 1 ? 1200.0 : i % 2 == 1 ? 900.0 : 0.0)).ToList();
        return KataRebarPlanner.Plan(KataDamSheetParser.Parse(KataDwgBeam.B03.Sheet()), new KataMeasuredBeam(500.0, 1200.0, segments, 4));
    }

    private static KataElevation B03Elevation() => Elevation(new KataRunInput(
        new[]
        {
            Piece(200, 11000, b: 500, h: 1200, key: "B03-1"),
            Piece(11000, 24800, b: 500, h: 900, key: "B03-2"),
            Piece(24800, 31400, b: 300, h: 900, key: "B03-3"),
            Piece(31400, 33600, b: 300, h: 900, zOffset: -200, key: "B03-4"),
        },
        new[] { Column(0, 400), Column(10800, 11200), Column(24600, 25000), Column(31200, 31600) },
        new[] { Grid("1", 200), Grid("2", 11000), Grid("3", 24800), Grid("4", 31400) },
        new KataHeader("B03", 1, 1200.0, 500.0, 3600.0)));

    /// <summary>As the window computes it: the plan's local X from the first support's outer face.</summary>
    private static KataStationMap B03StationMap()
    {
        var supports = B03Elevation().Columns.Where(c => c.Kind == KataColumnKind.Support).ToList();
        return KataStationMap.For(supports[0].Extent.Start, supports[^1].Extent.End, sameOrder: true);
    }

    /// <summary>The page's fixtures, beside this test project under HPRebar/.</summary>
    private static string FixtureFolder()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var frontend = Path.Combine(directory.FullName, "HPBimToolkit.Frontend");
            if (Directory.Exists(frontend)) return Path.Combine(frontend, "src", "kata", "__fixtures__");
        }

        throw new DirectoryNotFoundException($"HPBimToolkit.Frontend not found above {AppContext.BaseDirectory}.");
    }
}
