using System;
using System.IO;
using HPPowerBi.McpBridge;
using Xunit;

namespace HPPowerBi.McpBridge.Tests;

public sealed class Milestone2BridgeEntryTests : IDisposable
{
    public Milestone2BridgeEntryTests()
    {
        BridgeEntry.Dispose();
    }

    [Fact]
    public void Constants_MatchArchitectureContract()
    {
        Assert.Equal("HPPowerBi", BridgeEntry.VendorFolder);
        Assert.Equal("McpBridge", BridgeEntry.ProductFolder);
        Assert.Equal("Power BI", BridgeEntry.HostName);
        Assert.Equal("2026", BridgeEntry.HostVersion);
        Assert.Equal(2026, BridgeEntry.HostVersionNumber);
        Assert.Equal("hppowerbi-mcp-2026", BridgeEntry.PipeName);

        Assert.NotNull(BridgeEntry.LogDirectory);
        Assert.NotNull(BridgeEntry.SnapshotDirectory);
        Assert.Contains("HPPowerBi", BridgeEntry.LogDirectory);
        Assert.Contains("Snapshots", BridgeEntry.SnapshotDirectory);
    }

    [Fact]
    public void StartAndDispose_ManagesContainerLifecycle()
    {
        var container = BridgeEntry.Start();

        Assert.NotNull(container);
        Assert.Same(container, BridgeEntry.Current);

        Assert.NotNull(container.Host);
        Assert.Equal("hppowerbi-mcp-2026", container.Host.PipeName);
        Assert.Equal("Power BI", container.Host.HostName);

        Assert.NotNull(container.Executor);
        Assert.NotNull(container.ConnectionManager);
        Assert.NotNull(container.Guard);
        Assert.NotNull(container.SnapshotManager);
        Assert.NotNull(container.CloudClient);
        Assert.NotNull(container.Dispatcher);

        BridgeEntry.Dispose();
        Assert.Null(BridgeEntry.Current);
    }

    public void Dispose()
    {
        BridgeEntry.Dispose();
    }
}
