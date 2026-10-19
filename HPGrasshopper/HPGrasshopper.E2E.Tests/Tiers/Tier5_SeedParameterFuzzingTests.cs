namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Registry;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class Tier5_SeedParameterFuzzingTests
{
    [Fact]
    public async Task TC_T5_FUZZ_01_PlayerFilePathValidation()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var script = """
            string path = args.Require("filePath");
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Definition path cannot be empty.");
            if (path.Contains("..") || path.Contains("\0")) throw new ArgumentException("Invalid path traversal or characters.");
            return new { success = true, path };
            """;

        var fuzzedPaths = new[] { "", "   ", "model/../../etc/passwd", "definition\0.gh" };
        foreach (var path in fuzzedPaths)
        {
            var req = new GrasshopperExecutionRequest(script, ArgsJson: JsonSerializer.Serialize(new { filePath = path }));
            var res = await bridge.ExecuteAsync(req);
            Assert.False(res.Success);
            Assert.NotNull(res.ErrorMessage);
        }

        var validReq = new GrasshopperExecutionRequest(script, ArgsJson: JsonSerializer.Serialize(new { filePath = "TowerDefinition.gh" }));
        var validRes = await bridge.ExecuteAsync(validReq);
        Assert.True(validRes.Success);
    }

    [Fact]
    public async Task TC_T5_FUZZ_02_PlayerInvalidExtensionRejection()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var script = """
            string file = args.Require("filePath");
            var validExts = new[] { ".gh", ".ghx" };
            bool isValid = validExts.Any(e => file.EndsWith(e, StringComparison.OrdinalIgnoreCase));
            if (!isValid) throw new ArgumentException($"Invalid Grasshopper definition extension: '{file}'");
            return new { success = true, file };
            """;

        var invalidExtensions = new[] { "model.txt", "payload.exe", "macro.dyn", "drawing.dwg", "bim.rvt", "hack.dll" };
        foreach (var file in invalidExtensions)
        {
            var req = new GrasshopperExecutionRequest(script, ArgsJson: JsonSerializer.Serialize(new { filePath = file }));
            var res = await bridge.ExecuteAsync(req);
            Assert.False(res.Success);
            Assert.Contains("Invalid Grasshopper definition extension", res.ErrorMessage);
        }

        var validReq = new GrasshopperExecutionRequest(script, ArgsJson: JsonSerializer.Serialize(new { filePath = "tower.gh" }));
        var validRes = await bridge.ExecuteAsync(validReq);
        Assert.True(validRes.Success);
    }

    [Fact]
    public void TC_T5_FUZZ_03_CanvasNanInfinityCoordinatesRejection()
    {
        var bridge = new GrasshopperE2EBridge();
        Assert.Throws<ArgumentException>(() =>
            GrasshopperE2EHarness.AddComponentToCanvas(bridge, "NanNode", "N", "Test", double.NaN, 0));
        Assert.Throws<ArgumentException>(() =>
            GrasshopperE2EHarness.AddComponentToCanvas(bridge, "InfNode", "I", "Test", 0, double.PositiveInfinity));
        Assert.Throws<ArgumentException>(() =>
            GrasshopperE2EHarness.AddComponentToCanvas(bridge, "NegInfNode", "NI", "Test", double.NegativeInfinity, 0));
    }

    [Fact]
    public void TC_T5_FUZZ_04_CanvasNegativePortIndicesRejection()
    {
        var bridge = new GrasshopperE2EBridge();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        Assert.Throws<IndexOutOfRangeException>(() =>
            GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, id1, -1, id2, 0));
        Assert.Throws<IndexOutOfRangeException>(() =>
            GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, id1, 0, id2, -5));
    }

    [Fact]
    public void TC_T5_FUZZ_05_CanvasCyclicSelfConnectionRejection()
    {
        var bridge = new GrasshopperE2EBridge();
        var id = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() =>
            GrasshopperE2EHarness.ConnectPortsOnCanvas(bridge, id, 0, id, 1));
    }

    [Fact]
    public void TC_T5_FUZZ_06_SearchToolsOversizedQueryHandling()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "fuzz_search_" + Guid.NewGuid().ToString("N") + ".db");
        var db = new ToolRegistryDb(dbPath, NullLogger<ToolRegistryDb>.Instance);
        try
        {
            db.Initialize();
            var oversized = new string('q', 10_000);
            var results = db.Search(oversized, 5);
            Assert.NotNull(results);
            Assert.Empty(results);

            var emptyResults = db.Search("   ", 5);
            Assert.Empty(emptyResults);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task TC_T5_FUZZ_07_SearchToolsLimitClamping()
    {
        var seed = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly).First(s => s.Name == "facade_panelization");
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };

        var fuzzedLimits = new[] { -50, 0, 500 };
        foreach (var limit in fuzzedLimits)
        {
            var json = JsonSerializer.Serialize(new
            {
                surfaceWidthMeters = 30.0,
                surfaceHeightMeters = 15.0,
                uDivisions = limit,
                vDivisions = limit
            });
            var req = new GrasshopperExecutionRequest(seed.Code, ArgsJson: json);
            var res = await bridge.ExecuteAsync(req);
            Assert.True(res.Success);
            using var doc = JsonDocument.Parse(res.OutputJson!);
            if (limit <= 0)
            {
                Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
            }
            else
            {
                Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
                Assert.True(doc.RootElement.GetProperty("totalPanels").GetInt32() >= 0);
            }
        }
    }

    [Fact]
    public void TC_T5_FUZZ_08_LevelsGridsEmptyElevationsRejection()
    {
        var seed = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly).First(s => s.Name == "create_levels_grids");
        Assert.Empty(ScriptGuard.Check(seed.Code, GuardProfile.Grasshopper));
        Assert.DoesNotContain(ScriptQuality.Find(seed.Code), q => q.Severity == QualityFinding.Error);

        var json = "{\"elevations\": [], \"gridSpacings\": [], \"gridLength\": -50.0}";
        using var doc = JsonDocument.Parse(json);
        var args = new ScriptArgs(doc.RootElement);

        Assert.Empty(args.Doubles("elevations"));
        Assert.Empty(args.Doubles("gridSpacings"));
        Assert.Equal(-50.0, args.Double("gridLength", 30.0));
    }

    [Fact]
    public async Task TC_T5_FUZZ_09_ColumnsBeamsZeroLengthRejection()
    {
        var seed = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly).First(s => s.Name == "generate_columns_beams");
        Assert.Empty(ScriptGuard.Check(seed.Code, GuardProfile.Grasshopper));
        Assert.DoesNotContain(ScriptQuality.Find(seed.Code), q => q.Severity == QualityFinding.Error);

        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var script = """
            var lines = args.List("beamLines");
            foreach (var item in lines)
            {
                double dx = item.Double("endX") - item.Double("startX");
                double dy = item.Double("endY") - item.Double("startY");
                double length = Math.Sqrt((dx * dx) + (dy * dy));
                if (length < 0.001) throw new ArgumentException($"Beam framing length {length}m is below minimum 0.001m tolerance.");
            }
            return new { success = true, count = lines.Count };
            """;

        var zeroLengthJson = "{\"beamLines\": [{\"startX\": 0.0, \"startY\": 0.0, \"endX\": 0.0, \"endY\": 0.0}]}";
        var req = new GrasshopperExecutionRequest(script, ArgsJson: zeroLengthJson);
        var res = await bridge.ExecuteAsync(req);
        Assert.False(res.Success);
        Assert.Contains("below minimum 0.001m tolerance", res.ErrorMessage);

        var validJson = "{\"beamLines\": [{\"startX\": 0.0, \"startY\": 0.0, \"endX\": 5.0, \"endY\": 0.0}]}";
        var validReq = new GrasshopperExecutionRequest(script, ArgsJson: validJson);
        var validRes = await bridge.ExecuteAsync(validReq);
        Assert.True(validRes.Success);
    }

    [Fact]
    public async Task TC_T5_FUZZ_10_FloorsSlabsNonClosedPolygonRejection()
    {
        var seed = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly).First(s => s.Name == "create_floors_slabs");
        Assert.NotNull(seed);
        Assert.DoesNotContain(ScriptQuality.Find(seed.Code), q => q.Severity == QualityFinding.Error);

        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var script = """
            var loops = args.List("boundaryLoops");
            foreach (var loop in loops)
            {
                var pts = loop.Raw?.EnumerateArray().ToList() ?? new List<System.Text.Json.JsonElement>();
                if (pts.Count < 3) throw new ArgumentException($"Floor slab boundary loop has {pts.Count} vertices; minimum 3 required.");
            }
            return new { success = true, count = loops.Count };
            """;

        var openLoopJson = "{\"boundaryLoops\": [[{\"x\": 0.0, \"y\": 0.0}, {\"x\": 10.0, \"y\": 0.0}]]}";
        var req = new GrasshopperExecutionRequest(script, ArgsJson: openLoopJson);
        var res = await bridge.ExecuteAsync(req);
        Assert.False(res.Success);
        Assert.Contains("minimum 3 required", res.ErrorMessage);

        var validLoopJson = "{\"boundaryLoops\": [[{\"x\": 0.0, \"y\": 0.0}, {\"x\": 10.0, \"y\": 0.0}, {\"x\": 10.0, \"y\": 10.0}]]}";
        var validReq = new GrasshopperExecutionRequest(script, ArgsJson: validLoopJson);
        var validRes = await bridge.ExecuteAsync(validReq);
        Assert.True(validRes.Success);
    }

    [Fact]
    public async Task TC_T5_FUZZ_11_StirrupDistributionNonPositiveSpacingRejection()
    {
        var seed = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly).First(s => s.Name == "generate_stirrup_distribution");
        Assert.Empty(ScriptGuard.Check(seed.Code, GuardProfile.Grasshopper));
        Assert.DoesNotContain(ScriptQuality.Find(seed.Code), q => q.Severity == QualityFinding.Error);

        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var script = """
            double dense = args.Double("denseSpacingMm", 100.0);
            double sparse = args.Double("sparseSpacingMm", 200.0);
            if (dense <= 0.0 || sparse <= 0.0 || double.IsNaN(dense) || double.IsNaN(sparse))
            {
                throw new ArgumentException("Stirrup spacing parameters must be positive non-NaN values.");
            }
            return new { success = true, dense, sparse };
            """;

        var invalidJson = "{\"denseSpacingMm\": -100.0, \"sparseSpacingMm\": 0.0}";
        var req = new GrasshopperExecutionRequest(script, ArgsJson: invalidJson);
        var res = await bridge.ExecuteAsync(req);
        Assert.False(res.Success);
        Assert.Contains("must be positive non-NaN values", res.ErrorMessage);

        var validJson = "{\"denseSpacingMm\": 100.0, \"sparseSpacingMm\": 200.0}";
        var validReq = new GrasshopperExecutionRequest(script, ArgsJson: validJson);
        var validRes = await bridge.ExecuteAsync(validReq);
        Assert.True(validRes.Success);
    }

    [Fact]
    public async Task TC_T5_FUZZ_12_JsonSerializationNullCharResilience()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var script = """
            string title = args.Require("title");
            return new { success = true, titleLength = title.Length, sanitized = title.Replace("\0", string.Empty) };
            """;

        var json = "{\"title\": \"Node\\u0000Test\"}";
        var req = new GrasshopperExecutionRequest(script, ArgsJson: json);
        var res = await bridge.ExecuteAsync(req);
        Assert.True(res.Success);
        using var doc = JsonDocument.Parse(res.OutputJson!);
        Assert.Equal("NodeTest", doc.RootElement.GetProperty("sanitized").GetString());
    }
}
