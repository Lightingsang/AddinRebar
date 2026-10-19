namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Harness;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

public sealed class Tier5_CancellationStressTests
{
    [Fact]
    public async Task TC_T5_CNC_01_ImmediateCancellation_BeforeExecution()
    {
        var executor = new GrasshopperTestExecutor { IsExecutionEnabled = true };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = new ExecuteRequest("int x = 10; return x;", TransactionModes.Auto);
        var result = await executor.ExecuteAsync(request, null, cts.Token);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.False(executor.IsBusy);
    }

    [Fact]
    public async Task TC_T5_CNC_02_MidFlightCancellation_TransactionRollback()
    {
        var executor = new GrasshopperTestExecutor { IsExecutionEnabled = true };
        int initialCommitted = executor.CommittedElementCount;
        using var cts = new CancellationTokenSource();

        var request = new ExecuteRequest("return 42;", TransactionModes.Auto);
        var execTask = executor.ExecuteAsync(request, null, cts.Token);
        cts.Cancel();

        var result = await execTask;
        Assert.True(result.RolledBack);
        Assert.Equal(initialCommitted, executor.CommittedElementCount);
        Assert.False(executor.IsBusy);
    }

    [Fact]
    public async Task TC_T5_CNC_03_CorrelatedCancellation_SessionIsolation()
    {
        var executor = new GrasshopperTestExecutor { IsExecutionEnabled = true };
        using var ctsA = new CancellationTokenSource();
        var reqA = new ExecuteRequest("return 101;", TransactionModes.Auto, SessionId: "sess-A");
        var reqB = new ExecuteRequest("return 202;", TransactionModes.Auto, SessionId: "sess-B");

        var taskA = executor.ExecuteAsync(101L, reqA, null, ctsA.Token);
        var cancelResult = executor.Cancel(101L, "sess-A");
        var resA = await taskA;

        var taskB = executor.ExecuteAsync(102L, reqB, null, CancellationToken.None);
        var resB = await taskB;

        Assert.True(cancelResult.Cancelled);
        Assert.True(resA.RolledBack);
        Assert.False(resB.IsError);
        Assert.False(resB.RolledBack);
    }

    [Fact]
    public void TC_T5_CNC_04_CancelOfCancel_NoDeadlock()
    {
        var executor = new GrasshopperTestExecutor { IsExecutionEnabled = true };

        var firstCancel = executor.Cancel(999L, "unknown-session");
        var secondCancel = executor.Cancel(999L, "unknown-session");
        var globalCancel = executor.Cancel();

        Assert.False(firstCancel.Cancelled);
        Assert.False(secondCancel.Cancelled);
        Assert.False(globalCancel.WasRunning);
        Assert.False(executor.IsBusy);
    }

    [Fact]
    public async Task TC_T5_CNC_05_ThreadRelease_BusyFlagHonesty()
    {
        var executor = new GrasshopperTestExecutor { IsExecutionEnabled = true };
        int stateChangeCount = 0;
        executor.StateChanged += () => Interlocked.Increment(ref stateChangeCount);

        Assert.False(executor.IsBusy);
        var request = new ExecuteRequest("return 1;", TransactionModes.Auto);
        var result = await executor.ExecuteAsync(request, null, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.False(executor.IsBusy);
        Assert.True(stateChangeCount >= 2);

        var followUpResult = await executor.ExecuteAsync(request, null, CancellationToken.None);
        Assert.False(followUpResult.IsError);
    }

    [Fact]
    public async Task TC_T5_CNC_06_HighThroughputBurst_NoConnectionDrop()
    {
        await using var simulator = new GrasshopperPipeSimulator();
        simulator.Start();

        using var client = new NamedPipeClientStream(".", simulator.PipeName, PipeDirection.InOut);
        await client.ConnectAsync(3000);
        using var reader = new StreamReader(client, Encoding.UTF8);
        using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };

        for (int i = 1; i <= 20; i++)
        {
            await writer.WriteLineAsync($"{{\"jsonrpc\":\"2.0\",\"id\":{i},\"method\":\"grasshopper.ping\"}}");
            var line = await reader.ReadLineAsync();
            Assert.NotNull(line);
            Assert.Contains($"\"id\":{i}", line);
            Assert.Contains("\"pong\":true", line);
        }
    }
}
