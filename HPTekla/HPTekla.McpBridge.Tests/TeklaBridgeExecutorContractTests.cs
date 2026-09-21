using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Host;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Tekla.Structures.Model;
using Xunit;
using SysTask = System.Threading.Tasks.Task;

namespace HPTekla.McpBridge.Tests;

public sealed class TeklaBridgeExecutorContractTests
{
    [Fact]
    public async SysTask GetContextAsync_WhenDispatcherTimesOut_ThrowsBridgeRequestExceptionBusy()
    {
        // Set short grace so dispatcher expires quickly
        using var dispatcher = new TeklaThreadDispatcher(TimeSpan.FromMilliseconds(200));
        var model = new Model();
        var settings = new BridgeSettings { ExecutionEnabled = true };
        using var executor = new TeklaBridgeExecutor(model, dispatcher, settings);

        // Call GetContextAsync without pumping any ticks
        var ex = await Assert.ThrowsAsync<BridgeRequestException>(() =>
            executor.GetContextAsync(false, CancellationToken.None));

        Assert.Equal(BridgeErrorCode.Busy, ex.Code);
        Assert.Contains("Tekla Structures", ex.Message);
    }

    [Fact]
    public async SysTask ExecuteAsync_WhenDispatcherTimesOut_RethrowsBridgeRequestExceptionBusy()
    {
        // Verified contract:
        // ExecuteAsync rethrows BridgeRequestException so RequestDispatcher formats it as JSON-RPC error code -32002 (Busy).
        using var dispatcher = new TeklaThreadDispatcher(TimeSpan.FromMilliseconds(200));
        var model = new Model();
        var settings = new BridgeSettings { ExecutionEnabled = true };
        using var executor = new TeklaBridgeExecutor(model, dispatcher, settings);

        var request = new ExecuteRequest(
            Code: "return 123;",
            Label: "timeout-test");

        var ex = await Assert.ThrowsAsync<BridgeRequestException>(() =>
            executor.ExecuteAsync(request, null, CancellationToken.None));

        Assert.Equal(BridgeErrorCode.Busy, ex.Code);
        Assert.Contains("Tekla Structures", ex.Message);
        Assert.Contains("dialog", ex.Message);
    }

    [Fact]
    public async SysTask ExecuteAsync_WhenCancelled_ReturnsFailureWithTimedOutTrue()
    {
        using var dispatcher = new TeklaThreadDispatcher(TimeSpan.FromSeconds(5));
        var model = new Model();
        var settings = new BridgeSettings { ExecutionEnabled = true };
        using var executor = new TeklaBridgeExecutor(model, dispatcher, settings);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = new ExecuteRequest(
            Code: "return 123;",
            Label: "cancel-test");

        // Access queue to pump tick so the cancellation is processed immediately
        var queueField = typeof(TeklaThreadDispatcher).GetField("_queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var queue = (MainThreadQueue)queueField.GetValue(dispatcher);

        var task = executor.ExecuteAsync(request, null, cts.Token);
        queue.OnTick();

        var result = await task;

        Assert.True(result.IsError);
        Assert.True(result.TimedOut);
        Assert.True(result.RolledBack);
        Assert.Contains("cancelled", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async SysTask ExecuteAsync_WhenCancelCalledOnExecutor_CancelsAndReturnsTimedOutTrue()
    {
        using var dispatcher = new TeklaThreadDispatcher(TimeSpan.FromSeconds(5));
        var model = new Model();
        var settings = new BridgeSettings { ExecutionEnabled = true };
        using var executor = new TeklaBridgeExecutor(model, dispatcher, settings);

        var request = new ExecuteRequest(
            Code: "return 123;",
            Label: "cancel-method-test");

        var task = executor.ExecuteAsync(request, null, CancellationToken.None);

        // Cancel via executor.Cancel() while queued
        var cancelResult = executor.Cancel();
        Assert.True(cancelResult.Cancelled);

        // Pump queue tick so the cancellation is processed immediately
        var queueField = typeof(TeklaThreadDispatcher).GetField("_queue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var queue = (MainThreadQueue)queueField.GetValue(dispatcher);
        queue.OnTick();

        var result = await task;
        Assert.True(result.IsError);
        Assert.True(result.TimedOut);
        Assert.True(result.RolledBack);
    }
}
