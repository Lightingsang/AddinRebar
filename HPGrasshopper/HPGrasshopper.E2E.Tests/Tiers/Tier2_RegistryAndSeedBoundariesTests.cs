namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class Tier2_RegistryAndSeedBoundariesTests
{
    [Fact]
    public void TC_T2_REG_01_ScriptExceeding300LinesRejected()
    {
        var bridge = new GrasshopperE2EBridge();
        var sb = new StringBuilder();
        for (int i = 0; i < 305; i++) sb.AppendLine($"int num{i} = {i};");

        var quality = bridge.ValidateScript(sb.ToString());
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("300") || e.Contains("Q-B3"));
    }

    [Fact]
    public void TC_T2_REG_02_UnannotatedEmptyCatchBlockRejected()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "try { int a = 0; } catch (Exception) { }";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("empty catch", StringComparison.OrdinalIgnoreCase) || e.Contains("Q-B2"));
    }

    [Fact]
    public void TC_T2_REG_03_CommentedOutCodeStatementRejected()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "// string temp = \"hello\";\nreturn 1;";

        var quality = bridge.ValidateScript(code);
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("commented-out", StringComparison.OrdinalIgnoreCase) || e.Contains("Q-B1"));
    }

    [Fact]
    public void TC_T2_REG_04_PublishWithoutPriorTestingNegativeTest()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "gh_reg_" + Guid.NewGuid().ToString("N"));
        try
        {
            var (libPath, dbPath) = (Path.Combine(tempDir, "lib"), Path.Combine(tempDir, "registry.db"));
            var store = new ToolLibraryStore(libPath, NullLogger<ToolLibraryStore>.Instance);
            store.EnsureRoot();
            var db = new ToolRegistryDb(dbPath, NullLogger<ToolRegistryDb>.Instance);
            db.Initialize();

            var regOpts = Microsoft.Extensions.Options.Options.Create(new RegistryOptions { LibraryPath = libPath, DbPath = dbPath });
            var bridgeOpts = Microsoft.Extensions.Options.Options.Create(new BridgeOptions());
            var client = new RevitBridgeClient(bridgeOpts, NullLogger<RevitBridgeClient>.Instance, GrasshopperHostProfile.Instance);
            var manager = new ToolManager(regOpts, bridgeOpts, store, db, client, NullLogger<ToolManager>.Instance);
            var lifecycle = new ToolLifecycleService(manager, client, NullLogger<ToolLifecycleService>.Instance);

            var draft = new ToolRecord { Name = "draft_tool", Status = ToolStatus.Draft, Description = "Valid tool description.", Category = "Geometry" };
            manager.Save(draft, "draft", "test");
            var draftEx = Assert.Throws<ToolNotRunnableException>(() => lifecycle.Publish("draft_tool"));
            Assert.Contains("not been tested", draftEx.Message);

            var deprecated = new ToolRecord { Name = "deprecated_tool", Status = ToolStatus.Deprecated, Description = "Valid tool description.", Category = "Geometry" };
            manager.Save(deprecated, "deprecated", "test");
            var depEx = Assert.Throws<ToolNotRunnableException>(() => lifecycle.Publish("deprecated_tool"));
            Assert.Contains("deprecated", depEx.Message);

            var tested = new ToolRecord { Name = "tested_tool", Status = ToolStatus.Tested, Description = "Valid tool description.", Category = "Geometry" };
            manager.Save(tested, "tested", "test");
            var published = lifecycle.Publish("tested_tool");
            Assert.NotNull(published);
            Assert.Equal("pending_approval", published.Status);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void TC_T2_REG_05_ToolNameCollisionNegativeTest()
    {
        var existingRecord = new ToolRecord { Name = "create_levels_grids", Version = 1, Status = ToolStatus.Published };
        var candidate = new ToolRecord { Name = "create_levels_grids", Category = "Geometry", Description = "A duplicate tool record.", Code = "return 1;" };

        var report = ToolValidator.Validate(candidate, null, [existingRecord], newVersion: false, HostProfile.Grasshopper);
        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public void TC_T2_SED_01_Exact300LineUpperBoundVerification()
    {
        var bridge = new GrasshopperE2EBridge();
        var seeds = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly);

        foreach (var seed in seeds)
        {
            var quality = bridge.ValidateScript(seed.Code);
            Assert.True(quality.LineCount <= 300, $"Seed '{seed.Name}' has {quality.LineCount} lines, exceeding Rule C2 limit.");
        }
    }

    [Fact]
    public void TC_T2_SED_02_MissingRequiredSeedParameterNegativeTest()
    {
        using var doc = JsonDocument.Parse("{}");
        var args = new ScriptArgs(doc.RootElement);

        Assert.False(args.Has("elevations"));
        var ex = Assert.Throws<ArgumentException>(() => args.Require("elevations"));
        Assert.Contains("elevations", ex.Message);
    }

    [Fact]
    public async Task TC_T2_SED_03_ParameterFuzzingWithNegativeDimensions()
    {
        var seed = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly)
            .First(s => s.Name == "facade_panelization");
        Assert.Empty(ScriptGuard.Check(seed.Code, GuardProfile.Grasshopper));

        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };

        var negWidthArgs = JsonSerializer.Serialize(new { surfaceWidthMeters = -200.0, surfaceHeightMeters = 500.0, uDivisions = 4, vDivisions = 5 });
        var resWidth = await bridge.ExecuteAsync(new GrasshopperExecutionRequest(seed.Code, ArgsJson: negWidthArgs));
        Assert.True(resWidth.Success);
        Assert.NotNull(resWidth.OutputJson);

        using (var doc = JsonDocument.Parse(resWidth.OutputJson))
        {
            Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
            Assert.Contains("positive", doc.RootElement.GetProperty("message").GetString());
        }

        var negDivArgs = JsonSerializer.Serialize(new { surfaceWidthMeters = 100.0, surfaceHeightMeters = 50.0, uDivisions = -5, vDivisions = -5 });
        var resDiv = await bridge.ExecuteAsync(new GrasshopperExecutionRequest(seed.Code, ArgsJson: negDivArgs));
        Assert.True(resDiv.Success);
        Assert.NotNull(resDiv.OutputJson);

        using (var doc = JsonDocument.Parse(resDiv.OutputJson))
        {
            Assert.False(doc.RootElement.GetProperty("success").GetBoolean());
            Assert.Contains("greater than zero", doc.RootElement.GetProperty("message").GetString());
        }
    }

    [Fact]
    public void TC_T2_SED_04_NullGeometryListInputNegativeTest()
    {
        var json = "{\"geometries\": null}";
        using var doc = JsonDocument.Parse(json);
        var args = new ScriptArgs(doc.RootElement);

        Assert.False(args.Has("geometries"));
        Assert.False(args.IsArray);
    }

    [Fact]
    public async Task TC_T2_SED_05_SolarRadiationDateTimeRangeBoundary()
    {
        var seed = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly)
            .First(s => s.Name == "evaluate_solar_radiation");
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };

        var zeroSunArgs = JsonSerializer.Serialize(new
        {
            sunVector = new { x = 0.0, y = 0.0, z = 0.0 },
            targetSurfaceNormals = new[] { new { id = 1, normalX = 0.0, normalY = 0.0, normalZ = 1.0, area = 25.0 } },
            directRadiationWatts = 1000.0
        });
        var zeroRes = await bridge.ExecuteAsync(new GrasshopperExecutionRequest(seed.Code, ArgsJson: zeroSunArgs));
        Assert.True(zeroRes.Success);

        var opposingArgs = JsonSerializer.Serialize(new
        {
            sunVector = new { x = 0.0, y = 0.0, z = 1.0 },
            targetSurfaceNormals = new[] { new { id = 2, normalX = 0.0, normalY = 0.0, normalZ = -1.0, area = 25.0 } },
            directRadiationWatts = 1000.0
        });
        var oppRes = await bridge.ExecuteAsync(new GrasshopperExecutionRequest(seed.Code, ArgsJson: opposingArgs));
        Assert.True(oppRes.Success);
    }
}
