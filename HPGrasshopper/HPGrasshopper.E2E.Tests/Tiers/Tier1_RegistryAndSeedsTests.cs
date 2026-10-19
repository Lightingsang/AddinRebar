namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

public sealed class Tier1_RegistryAndSeedsTests
{
    [Fact]
    public void TC_T1_REG_01_SearchToolsDiscovery()
    {
        var profile = HostProfile.Grasshopper;
        Assert.NotNull(profile);
        Assert.Equal("grasshopper", profile.HostId);
        Assert.Equal("execute_grasshopper_code", profile.ExecuteToolName);
        Assert.Equal("get_grasshopper_context", profile.ContextToolName);
    }

    [Fact]
    public void TC_T1_REG_02_ProposeToolDraftRegistration()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "int a = 1; int b = 2; return a + b;";

        var quality = bridge.ValidateScript(code);
        Assert.True(quality.IsValid);
        Assert.True(quality.LineCount <= 300);
    }

    [Fact]
    public async Task TC_T1_REG_03_TestToolSandboxExecution()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var req = new GrasshopperExecutionRequest("return 42;", DryRun: true);
        var result = await bridge.ExecuteAsync(req);

        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(0, bridge.GetCommittedElementCount());
    }

    [Fact]
    public void TC_T1_REG_04_PublishToolPromotion()
    {
        var bridge = new GrasshopperE2EBridge();
        string validCode = "return doc.Name;";
        var quality = bridge.ValidateScript(validCode);
        Assert.True(quality.IsValid);
        Assert.Empty(quality.Errors);

        string invalidCode = "// var x = 1;\nreturn 42;";
        var invalidQuality = bridge.ValidateScript(invalidCode);
        Assert.False(invalidQuality.IsValid);
        Assert.Contains(invalidQuality.Errors, e => e.Contains("commented-out", StringComparison.OrdinalIgnoreCase) || e.Contains("Q-B1"));
    }

    [Fact]
    public void TC_T1_REG_05_ManageToolRetirementLifecycle()
    {
        var profile = HostProfile.Grasshopper;
        Assert.Contains("execute_grasshopper_code", profile.CoreToolNames);
        Assert.Contains("read_grasshopper_canvas", profile.CoreToolNames);

        var candidateToRetire = "custom_helper_tool";
        bool isCore = profile.CoreToolNames.Contains(candidateToRetire);
        Assert.False(isCore);
    }

    [Fact]
    public void TC_T1_SED_01_SeedCatalogCompleteness()
    {
        var seeds = GrasshopperE2EContracts.EmbeddedSeeds;
        Assert.Equal(12, seeds.Count);

        var snakeCase = new Regex("^[a-z0-9_]+$");
        foreach (var seed in seeds)
        {
            Assert.Matches(snakeCase, seed);
        }

        Assert.Contains("create_levels_grids", seeds);
        Assert.Contains("generate_columns_beams", seeds);
        Assert.Contains("generate_rebar_curves", seeds);
        Assert.Contains("facade_panelization", seeds);
        Assert.Contains("evaluate_solar_radiation", seeds);
    }

    [Fact]
    public void TC_T1_SED_02_SeedToolSchemaCompleteness()
    {
        var seeds = GrasshopperE2EContracts.EmbeddedSeeds;
        foreach (var seed in seeds)
        {
            Assert.False(string.IsNullOrWhiteSpace(seed));
            Assert.True(seed.Length >= 5);
        }
    }

    [Fact]
    public void TC_T1_SED_03_SeedExamplesManifestValidation()
    {
        var seeds = GrasshopperE2EContracts.EmbeddedSeeds;
        Assert.Contains("create_levels_grids", seeds);
        Assert.Contains("generate_columns_beams", seeds);
        Assert.Contains("facade_panelization", seeds);
    }

    [Fact]
    public void TC_T1_SED_04_SeedCodeAstQualityWalkerPass()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly);
        Assert.Equal(12, seeds.Count);

        foreach (var seed in seeds)
        {
            var findings = ScriptQuality.Find(seed.Code);
            var errors = findings.Where(f => f.Severity == ScriptQuality.Error).ToList();
            Assert.True(errors.Count == 0, $"Seed '{seed.Name}' failed quality checks: {string.Join(", ", errors.Select(e => e.Message))}");
        }
    }

    [Fact]
    public void TC_T1_SED_05_StructuralRebarSeedsParameterCoverage()
    {
        var argsJson = "{\"barDiameter\": 18.0, \"stirrupSpacing\": 150.0, \"cover\": 30.0}";
        using var doc = JsonDocument.Parse(argsJson);
        var args = new ScriptArgs(doc.RootElement);

        Assert.Equal(18.0, args.Double("barDiameter"));
        Assert.Equal(150.0, args.Double("stirrupSpacing"));
        Assert.Equal(30.0, args.Double("cover"));
    }
}
