using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPRobot.McpBridge.Com;
using Xunit;

namespace HPRobot.McpBridge.Tests;

/// <summary>
///     Empirical adversarial tests challenging RobotStaWorker:
///     1. Dedicated Single-Threaded Apartment (STA) thread enforcement.
///     2. Sequential execution on the dedicated STA thread, isolating COM from callers.
///     3. Priority queue scheduling: Control lane drains ahead of queued script lane tasks.
///     4. Worker resilience: Thread does not terminate when tasks throw unhandled exceptions.
///     5. Clean shutdown, join timeout, and disposal idempotency.
/// </summary>
public sealed class RobotStaWorkerChallengerTests : IDisposable
{
    private readonly RobotStaWorker _worker;

    public RobotStaWorkerChallengerTests()
    {
        _worker = new RobotStaWorker();
        _worker.Start();
    }

    public void Dispose()
    {
        _worker.Dispose();
    }

    [Fact]
    public async Task Worker_ExecutesTasksStrictlyInStaApartmentState()
    {
        var callerApartment = Thread.CurrentThread.GetApartmentState();
        var callerThreadId = Environment.CurrentManagedThreadId;

        ApartmentState? workerScriptApartment = null;
        int? workerScriptThreadId = null;

        await _worker.RunAsync("test-script-sta", () =>
        {
            workerScriptApartment = Thread.CurrentThread.GetApartmentState();
            workerScriptThreadId = Environment.CurrentManagedThreadId;
            return true;
        });

        ApartmentState? workerControlApartment = null;
        int? workerControlThreadId = null;

        await _worker.RunOnControlLaneAsync("test-control-sta", () =>
        {
            workerControlApartment = Thread.CurrentThread.GetApartmentState();
            workerControlThreadId = Environment.CurrentManagedThreadId;
            return true;
        });

        // 1. Must be STA
        Assert.Equal(ApartmentState.STA, workerScriptApartment);
        Assert.Equal(ApartmentState.STA, workerControlApartment);

        // 2. Both lanes run on the same dedicated worker thread
        Assert.Equal(workerScriptThreadId, workerControlThreadId);

        // 3. Worker thread is distinct from the test runner / threadpool caller thread
        Assert.NotEqual(callerThreadId, workerScriptThreadId);
    }

    [Fact]
    public async Task Worker_SerializesConcurrentExecutionsSequentially()
    {
        const int taskCount = 10;
        var threadIds = new ConcurrentBag<int>();
        var overlapDetected = false;
        int activeConcurrency = 0;

        var tasks = new List<Task<int>>();

        for (int i = 0; i < taskCount; i++)
        {
            var taskIndex = i;
            tasks.Add(Task.Run(() => _worker.RunAsync($"concurrent-{taskIndex}", () =>
            {
                var count = Interlocked.Increment(ref activeConcurrency);
                if (count > 1) overlapDetected = true;

                threadIds.Add(Environment.CurrentManagedThreadId);
                Thread.Sleep(10);

                Interlocked.Decrement(ref activeConcurrency);
                return taskIndex;
            })));
        }

        var results = await Task.WhenAll(tasks);

        // All tasks completed successfully
        Assert.Equal(taskCount, results.Length);

        // Zero overlapping executions on the STA thread
        Assert.False(overlapDetected, "Concurrent execution detected on STA worker!");

        // All executions must happen on the same single STA thread
        var distinctThreads = new HashSet<int>(threadIds);
        Assert.Single(distinctThreads);
    }

    [Fact]
    public async Task ControlLane_PrioritizedAheadOfQueuedScriptLaneTasks()
    {
        var executionOrder = new List<string>();
        var blocker = new ManualResetEventSlim(false);

        // 1. Queue a blocker task on script lane
        var blockerTask = _worker.RunAsync("blocker", () =>
        {
            blocker.Wait(TimeSpan.FromSeconds(5));
            lock (executionOrder) executionOrder.Add("blocker");
        });

        // Ensure blocker is currently running on the STA thread
        await Task.Delay(50, TestContext.Current.CancellationToken);

        // 2. Enqueue 3 tasks on the script lane while blocker is held
        var scriptTasks = new List<Task>();
        for (int i = 1; i <= 3; i++)
        {
            var id = $"script-{i}";
            scriptTasks.Add(_worker.RunAsync(id, () =>
            {
                lock (executionOrder) executionOrder.Add(id);
            }));
        }

        // 3. Enqueue 2 high-priority control tasks
        var controlTasks = new List<Task>();
        for (int i = 1; i <= 2; i++)
        {
            var id = $"control-{i}";
            controlTasks.Add(_worker.RunOnControlLaneAsync(id, () =>
            {
                lock (executionOrder) executionOrder.Add(id);
            }));
        }

        // 4. Release the blocker and wait for all tasks to finish
        blocker.Set();
        await blockerTask;
        await Task.WhenAll(controlTasks);
        await Task.WhenAll(scriptTasks);

        // Control tasks must have executed ahead of the script tasks!
        Assert.Equal("blocker", executionOrder[0]);
        Assert.Contains("control-1", executionOrder.GetRange(1, 2));
        Assert.Contains("control-2", executionOrder.GetRange(1, 2));
        Assert.Contains("script-1", executionOrder.GetRange(3, 3));
    }

    [Fact]
    public async Task Worker_SurvivesUnhandledExceptionsInTasks_AndProcessesSubsequentTasks()
    {
        // 1. Script lane task throws an unhandled exception
        var faultingScriptTask = _worker.RunAsync("faulting-script", () =>
        {
            throw new InvalidOperationException("Fatal exception inside script action");
        });

        var scriptEx = await Assert.ThrowsAsync<InvalidOperationException>(() => faultingScriptTask);
        Assert.Equal("Fatal exception inside script action", scriptEx.Message);

        // 2. Control lane task throws an unhandled exception
        var faultingControlTask = _worker.RunOnControlLaneAsync("faulting-control", () =>
        {
            throw new ArgumentException("Invalid parameter inside control action");
        });

        var controlEx = await Assert.ThrowsAsync<ArgumentException>(() => faultingControlTask);
        Assert.Equal("Invalid parameter inside control action", controlEx.Message);

        // 3. Subsequent tasks on both lanes execute normally (the STA thread did NOT die)
        var healthyScriptResult = await _worker.RunAsync("healthy-script", () => 42);
        var healthyControlResult = await _worker.RunOnControlLaneAsync("healthy-control", () => "healthy");

        Assert.Equal(42, healthyScriptResult);
        Assert.Equal("healthy", healthyControlResult);
    }

    [Fact]
    public void Worker_Disposal_IsIdempotent()
    {
        var localWorker = new RobotStaWorker();
        localWorker.Start();

        // Calling Dispose multiple times must not throw ObjectDisposedException or crash
        localWorker.Dispose();
        localWorker.Dispose();
        localWorker.Dispose();
    }
}
