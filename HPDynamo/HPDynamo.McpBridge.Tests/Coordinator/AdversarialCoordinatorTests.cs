namespace HPDynamo.McpBridge.Tests.Coordinator;

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.McpBridge.Coordinator;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

public sealed class AdversarialCoordinatorTests
{
    [Fact]
    public async Task ConcurrencyRace_50ParallelExecutions_OnlyOneRunsRestRejected()
    {
        var coordinator = DynamoBridgeCoordinator.Instance;
        coordinator.Initialize(new BridgeSettings { ExecutionEnabled = true }, null);
        coordinator.UnregisterLiveCanvas();

        var busyField = typeof(DynamoBridgeCoordinator).GetField("_busy", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(busyField);

        // Ensure initially not busy
        busyField.SetValue(coordinator, 0);

        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var results = new ConcurrentBag<ExecuteResult>();

        // Set busy to simulate an ongoing long run
        busyField.SetValue(coordinator, 1);

        var tasks = new Task[50];
        for (int i = 0; i < 50; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                await startGate.Task;
                var res = await coordinator.ExecuteAsync(new ExecuteRequest("return 1;", "none"), null, CancellationToken.None);
                results.Add(res);
            });
        }

        // Release all 50 threads simultaneously
        startGate.SetResult();
        await Task.WhenAll(tasks);

        // Reset busy
        busyField.SetValue(coordinator, 0);

        Assert.Equal(50, results.Count);
        foreach (var res in results)
        {
            Assert.True(res.IsError);
            Assert.Contains("Another Dynamo script or graph is still running", res.Message);
        }
    }

    [Fact]
    public void CanvasFlapping_MultipleRegisterAndUnregisterCycles_MaintainsConsistentState()
    {
        var coordinator = DynamoBridgeCoordinator.Instance;
        coordinator.Initialize(new BridgeSettings { ExecutionEnabled = true }, null);

        for (int i = 0; i < 20; i++)
        {
            coordinator.RegisterLiveCanvas(null!, null, null);
            Assert.False(coordinator.IsLiveCanvasAttached);

            coordinator.UnregisterLiveCanvas();
            Assert.False(coordinator.IsLiveCanvasAttached);
            Assert.Null(coordinator.ActiveViewModel);
            Assert.Null(coordinator.ActiveWorkspace);
        }
    }

    [Fact]
    public void SetActiveDocumentTitle_NullAndEmpty_DoesNotThrow()
    {
        var coordinator = DynamoBridgeCoordinator.Instance;
        coordinator.SetActiveDocumentTitle(null);
        Assert.Null(coordinator.ActiveDocumentTitle);

        coordinator.SetActiveDocumentTitle("");
        Assert.Equal("", coordinator.ActiveDocumentTitle);

        coordinator.SetActiveDocumentTitle("Project.rvt");
        Assert.Equal("Project.rvt", coordinator.ActiveDocumentTitle);
    }
}
