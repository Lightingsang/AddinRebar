namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Text.RegularExpressions;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Hosts;
using Xunit;

public sealed class Tier1_BuildAndIntegrityTests
{
    [Fact]
    public void TC_T1_BLD_01_TotalToolSurfaceInvariant()
    {
        var surface = GrasshopperE2EHarness.BuildToolSurfaceList();
        Assert.True(surface.Count >= GrasshopperE2EContracts.ExpectedTotalToolsCount);
    }

    [Fact]
    public void TC_T1_BLD_02_InspectTypeToolSuppressionInvariant()
    {
        var surface = GrasshopperE2EHarness.BuildToolSurfaceList();
        Assert.DoesNotContain(GrasshopperHostProfile.SuppressedEngineToolName, surface);
    }

    [Fact]
    public void TC_T1_BLD_03_ToolNamingSnakeCaseUniformity()
    {
        var surface = GrasshopperE2EHarness.BuildToolSurfaceList();
        var snakeCaseRegex = new Regex("^[a-z0-9_]+$");

        foreach (var tool in surface)
        {
            Assert.Matches(snakeCaseRegex, tool);
        }
    }

    [Fact]
    public void TC_T1_BLD_04_McpSharedSingleDependencyDirection()
    {
        Assert.Equal("grasshopper", PipeNaming.GrasshopperHost);
        Assert.Equal(2026, HostProfile.Grasshopper.DefaultVersion);
        var pipeName = PipeNaming.For(HostProfile.Grasshopper.HostId, HostProfile.Grasshopper.DefaultVersion);
        Assert.Equal("hpgrasshopper-mcp-2026", pipeName);
    }

    [Fact]
    public void TC_T1_INT_01_CoreToolsCountInvariant()
    {
        var profile = HostProfile.Grasshopper;
        Assert.NotNull(profile);
        Assert.Equal("grasshopper", profile.HostId);
        Assert.Equal("execute_grasshopper_code", profile.ExecuteToolName);
        Assert.Equal("get_grasshopper_context", profile.ContextToolName);
        Assert.True(profile.CoreToolNames.Count >= 6);
        Assert.Contains("read_grasshopper_canvas", profile.CoreToolNames);
        Assert.Contains("run_grasshopper_player", profile.CoreToolNames);
        Assert.Contains("batch_edit_grasshopper_canvas", profile.CoreToolNames);
        Assert.Contains("export_grasshopper_canvas_image", profile.CoreToolNames);
    }
}
