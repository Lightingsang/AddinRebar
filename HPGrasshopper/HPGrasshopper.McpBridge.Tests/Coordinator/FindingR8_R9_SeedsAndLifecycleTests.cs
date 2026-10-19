namespace HPGrasshopper.McpBridge.Tests.Coordinator;

using System;
using HPGrasshopper.McpBridge.Coordinator;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class FindingR8_R9_SeedsAndLifecycleTests
{
    [Fact]
    public void ComputeFingerprint_ProducesNonEmptyDeterministicString()
    {
        var fp1 = GrasshopperCompilerReferences.ComputeFingerprint();
        var fp2 = GrasshopperCompilerReferences.ComputeFingerprint();

        Assert.False(string.IsNullOrWhiteSpace(fp1));
        Assert.Equal(fp1, fp2);
    }

    [Fact]
    public void CompilerLifecycle_InvalidateCompiler_ResetsCachedCompiler()
    {
        using var lifecycle = new GrasshopperCompilerLifecycle();
        var compiler1 = lifecycle.GetCompiler(null);
        Assert.NotNull(compiler1);

        lifecycle.InvalidateCompiler();
        var compiler2 = lifecycle.GetCompiler(null);
        Assert.NotNull(compiler2);
    }

    [Fact]
    public void ContextExtractor_NullUiApp_WithoutCache_ReturnsSafeFallbackWithoutThrowing()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        coordinator.UpdateCachedUiContext(null!);
        var result = GrasshopperContextExtractor.Extract(null, coordinator, false, true);

        Assert.NotNull(result);
        Assert.Equal(PipeNaming.GrasshopperHost, result.Host);
        Assert.False(result.IsModifiable);
        Assert.NotNull(result.Grasshopper);
        Assert.Equal(0, result.Grasshopper.ComponentCount);
    }

    [Fact]
    public void ContextExtractor_NullUiApp_WithCachedContext_ReturnsClonedCacheWithModifiableFalse()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var fakeContext = new ContextResult
        {
            Host = PipeNaming.GrasshopperHost,
            HostVersion = "2026",
            RevitVersion = "2026",
            DocTitle = "Project1.rvt",
            DocPath = "C:\\Projects\\Project1.rvt",
            IsModifiable = true,
            Units = new UnitsInfo("m"),
            OpenDocs = ["Project1.rvt"],
            ExecutionEnabled = true,
            Selection = [],
            Grasshopper = new GrasshopperInfo(true, "1.0", "8.0", "Main.gh", null, 10, 5, 0, false, true)
        };

        coordinator.UpdateCachedUiContext(fakeContext);

        var result = GrasshopperContextExtractor.Extract(null, coordinator, false, true);

        Assert.NotNull(result);
        Assert.Equal("Project1.rvt", result.DocTitle);
        Assert.False(result.IsModifiable); // Background thread context must have IsModifiable = false
        Assert.NotNull(result.Grasshopper);
        Assert.Equal(10, result.Grasshopper.ComponentCount);

        coordinator.UpdateCachedUiContext(null!);
    }
}
