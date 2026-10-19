namespace HPGrasshopper.McpBridge.Tests.Coordinator;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.McpBridge.Coordinator;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

/// <summary>
///     Adversarial empirical stress suite for queue lifecycle, Raise failure handling,
///     rapid sequential operations, interleaved cancellations, and timeout recovery.
/// </summary>
public sealed class QueueLifecycleEmpiricalStressTests : IDisposable
{
    private readonly GrasshopperBridgeCoordinator _coordinator = GrasshopperBridgeCoordinator.Instance;
    private readonly FakeExternalEventRaiser _fakeRaiser = new();
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };

    public QueueLifecycleEmpiricalStressTests()
    {
        _coordinator.Initialize(_settings, _fakeRaiser);
        _coordinator.ExecutionHook = null;
    }

    public void Dispose()
    {
        _coordinator.ExecutionHook = null;
        _coordinator.Dispose();
    }

    [Fact]
    public async Task MultipleConsecutiveRaiseFailures_MustNeverExecuteOnSubsequentHostTicks()
    {
        _fakeRaiser.ReturnValue = false;
        var executedCount = 0;
        const int failureBatch = 20;

        for (var i = 0; i < failureBatch; i++)
        {
            var work = new McpBridgeWorkRequest($"Zombie_{i}", (_, _) =>
            {
                Interlocked.Increment(ref executedCount);
                return "executed";
            }, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _coordinator.RunOnRevitThreadAsync(work));
            Assert.Contains("ExternalEvent.Raise() was denied", ex.Message);
        }

        for (var tick = 0; tick < 5; tick++)
        {
            _coordinator.Execute(null!);
        }

        Assert.Equal(0, Volatile.Read(ref executedCount));

        // Recovery: Next valid request must execute normally
        _fakeRaiser.ReturnValue = true;
        var validWork = new McpBridgeWorkRequest("ValidWork", (_, _) =>
        {
            Interlocked.Increment(ref executedCount);
            return "recovered";
        }, CancellationToken.None);

        var task = _coordinator.RunOnRevitThreadAsync(validWork);
        _coordinator.Execute(null!);
        var result = await task;

        Assert.Equal("recovered", result);
        Assert.Equal(1, Volatile.Read(ref executedCount));
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task RapidAlternatingRaiseFailuresAndSuccesses_ExecutesOnlySuccessfulRaiseItems()
    {
        var executedSuccesses = new ConcurrentBag<int>();
        var executedFailures = new ConcurrentBag<int>();
        const int iterations = 30;

        for (var i = 0; i < iterations; i++)
        {
            var idx = i;
            _fakeRaiser.ReturnValue = false;
            var failedWork = new McpBridgeWorkRequest($"Fail_{idx}", (_, _) =>
            {
                executedFailures.Add(idx);
                return "fail";
            }, CancellationToken.None);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _coordinator.RunOnRevitThreadAsync(failedWork));
            _coordinator.Execute(null!);

            _fakeRaiser.ReturnValue = true;
            var successWork = new McpBridgeWorkRequest($"Success_{idx}", (_, _) =>
            {
                executedSuccesses.Add(idx);
                return $"ok_{idx}";
            }, CancellationToken.None);

            var successTask = _coordinator.RunOnRevitThreadAsync(successWork);
            _coordinator.Execute(null!);
            var res = await successTask;
            Assert.Equal($"ok_{idx}", res);
        }

        Assert.Empty(executedFailures);
        Assert.Equal(iterations, executedSuccesses.Count);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task InterleavedRapidCancellations_WhilePending_NeverExecuteOnUIThread()
    {
        _fakeRaiser.ReturnValue = true;
        _fakeRaiser.OnRaise = null; // Do not pump immediately
        const int totalItems = 50;
        var executed = new ConcurrentBag<int>();
        var tasks = new Task<object?>[totalItems];
        var ctsList = new CancellationTokenSource[totalItems];

        for (var i = 0; i < totalItems; i++)
        {
            var id = i;
            ctsList[i] = new CancellationTokenSource();
            var work = new McpBridgeWorkRequest($"Item_{id}", (_, _) =>
            {
                executed.Add(id);
                return $"result_{id}";
            }, ctsList[i].Token);

            tasks[i] = _coordinator.RunOnRevitThreadAsync(work);
            if (id % 2 == 0) ctsList[i].Cancel();
        }

        _coordinator.Execute(null!);

        for (var i = 0; i < totalItems; i++)
        {
            if (i % 2 == 0)
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tasks[i]);
            }
            else
            {
                var r = await tasks[i];
                Assert.Equal($"result_{i}", r);
            }
            ctsList[i].Dispose();
        }

        Assert.DoesNotContain(executed, x => x % 2 == 0);
        Assert.Equal(totalItems / 2, executed.Count);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task SequentialExecution_AfterTimeoutExpiration_RecoversAndExecutesNormally()
    {
        _fakeRaiser.ReturnValue = true;
        _fakeRaiser.OnRaise = null;

        var timeoutReq = new ExecuteRequest("1 + 1", "none") { TimeoutSeconds = 5 };
        var sw = Stopwatch.StartNew();
        var timeoutResult = await _coordinator.ExecuteAsync(101, timeoutReq, null, CancellationToken.None);
        sw.Stop();

        Assert.True(timeoutResult.IsError);
        Assert.True(timeoutResult.TimedOut);
        Assert.True(timeoutResult.RolledBack);
        Assert.False(_coordinator.IsBusy);

        _coordinator.Execute(null!);

        // Subsequent valid execution with active host pumping
        _fakeRaiser.OnRaise = () => _coordinator.Execute(null!);
        _coordinator.ExecutionHook = req => Task.FromResult(new ExecuteResult { Message = "recovered_success" });

        var validReq = new ExecuteRequest("2 + 2", "none") { TimeoutSeconds = 10 };
        var normalResult = await _coordinator.ExecuteAsync(102, validReq, null, CancellationToken.None);

        Assert.False(normalResult.IsError);
        Assert.Equal("recovered_success", normalResult.Message);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task AbortedTaskWithExceptionInWork_DoesNotBlockOrCorruptSubsequentValidTasks()
    {
        _fakeRaiser.ReturnValue = true;
        _fakeRaiser.OnRaise = null;

        var failingWork = new McpBridgeWorkRequest("FaultyTask", (_, _) =>
            throw new InvalidOperationException("Simulated geometry solver exception"), CancellationToken.None);

        var failTask = _coordinator.RunOnRevitThreadAsync(failingWork);
        _coordinator.Execute(null!);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => failTask);
        Assert.Contains("Simulated geometry solver exception", ex.Message);

        var normalWork = new McpBridgeWorkRequest("HealthyTask", (_, _) => "healthy", CancellationToken.None);
        var normalTask = _coordinator.RunOnRevitThreadAsync(normalWork);
        _coordinator.Execute(null!);

        Assert.Equal("healthy", await normalTask);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task ConcurrentHammerWithDynamicCancellation_ZeroZombiesExecuted()
    {
        _fakeRaiser.ReturnValue = true;
        const int workerCount = 30;
        var executedBag = new ConcurrentBag<int>();
        var cts = new CancellationTokenSource();
        var tasks = new Task[workerCount];

        for (var i = 0; i < workerCount; i++)
        {
            var id = i;
            tasks[i] = Task.Run(async () =>
            {
                using var itemCts = new CancellationTokenSource();
                if (id % 3 == 0) itemCts.Cancel();

                var work = new McpBridgeWorkRequest($"Stress_{id}", (_, _) =>
                {
                    executedBag.Add(id);
                    return id;
                }, itemCts.Token);

                try
                {
                    await _coordinator.RunOnRevitThreadAsync(work);
                }
                catch (OperationCanceledException) { /* expected for cancelled items */ }
            });
        }

        // Pump coordinator on main thread while workers enqueue
        for (var pump = 0; pump < 15; pump++)
        {
            _coordinator.Execute(null!);
            await Task.Delay(10);
        }

        await Task.WhenAll(tasks);
        _coordinator.Execute(null!);

        Assert.DoesNotContain(executedBag, id => id % 3 == 0);
        Assert.False(_coordinator.IsBusy);
    }
}
