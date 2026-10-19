namespace HPGrasshopper.McpBridge.Tests.Coordinator;

using System;
using HPGrasshopper.McpBridge.Coordinator;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class FindingN10ContextStaleAndIsolationTests : IDisposable
{
    public FindingN10ContextStaleAndIsolationTests()
    {
        ResetCoordinator();
    }

    public void Dispose()
    {
        ResetCoordinator();
    }

    private static void ResetCoordinator()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        coordinator.UpdateActiveSnapshot(null, null, null, null, null);
        coordinator.UpdateCachedUiContext(null);
        coordinator.MarkContextFresh();
    }

    [Fact]
    public void ExtractNullUiApp_WhenDocumentSwitched_DoesNotMixUnitsOrSelectionOrDefinition()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var docAContext = new ContextResult
        {
            Host = PipeNaming.GrasshopperHost,
            DocTitle = "ProjectA.rvt",
            DocPath = "C:\\ProjectA.rvt",
            Units = new UnitsInfo("feet"),
            Selection = new[] { new ElementInfo(101, "Walls", "Wall-A") },
            Grasshopper = new GrasshopperInfo(true, "1.0", "8.0", "DefA.gh", "C:\\DefA.gh", 42, 10, 0, false, true)
        };
        coordinator.UpdateCachedUiContext(docAContext);

        coordinator.UpdateActiveSnapshot("ProjectB.rvt", "C:\\ProjectB.rvt", Guid.NewGuid(), "DefB.gh", "C:\\DefB.gh");

        var result = GrasshopperContextExtractor.Extract(null, coordinator, false, true);

        Assert.Equal("ProjectB.rvt", result.DocTitle);
        Assert.NotEqual("feet", result.Units.Length);
        Assert.Equal("unavailable", result.Units.Length);
        Assert.Empty(result.Selection);
        Assert.NotEqual("DefA.gh", result.Grasshopper?.ActiveDefinitionName);
        Assert.True(coordinator.IsContextUnavailable || coordinator.IsContextStale);
    }

    [Fact]
    public void ExtractNullUiApp_WhenSameDocument_PreservesTimestampAndMarksStale()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var initialContext = new ContextResult
        {
            Host = PipeNaming.GrasshopperHost,
            DocTitle = "ProjectSame.rvt",
            DocPath = "C:\\ProjectSame.rvt",
            Units = new UnitsInfo("mm"),
            Grasshopper = new GrasshopperInfo(true, "1.0", "8.0", "Same.gh", "C:\\Same.gh", 5, 2, 0, false, true)
        };
        coordinator.UpdateCachedUiContext(initialContext);
        coordinator.SetActiveDocumentTitle("ProjectSame.rvt");

        var result = GrasshopperContextExtractor.Extract(null, coordinator, false, true);

        Assert.Equal("ProjectSame.rvt", result.DocTitle);
        Assert.False(result.IsModifiable);
        Assert.True(coordinator.IsContextStale);
        Assert.NotNull(coordinator.LastSnapshotTimestamp);
    }

    [Fact]
    public void ExtractNullUiApp_WhenUncached_DoesNotAssumeMetersOrFakeCounts()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        coordinator.UpdateCachedUiContext(null);
        coordinator.SetActiveDocumentTitle("NewModel.rvt");

        var result = GrasshopperContextExtractor.Extract(null, coordinator, false, true);

        Assert.Equal("unavailable", result.Units.Length);
        Assert.False(result.Grasshopper?.IsEditorLoaded ?? false);
        Assert.True(coordinator.IsContextUnavailable);
    }
}
