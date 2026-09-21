using System;
using System.Threading.Tasks;
using HPExcel.McpBridge.Safety;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

namespace HPExcel.McpBridge.Tests;

public class ExcelSafetyGuardTests
{
    [Fact]
    public void InitialState_AllDisabled()
    {
        var guard = new ExcelSafetyGuard();
        Assert.False(guard.IsExecutionEnabled);
        Assert.False(guard.IsWriteEnabled);
        Assert.False(guard.IsDestructiveEnabled);
    }

    [Fact]
    public void Cascading_DisableExecution_DisablesAll()
    {
        var guard = new ExcelSafetyGuard();
        guard.IsExecutionEnabled = true;
        guard.IsWriteEnabled = true;
        guard.IsDestructiveEnabled = true;

        Assert.True(guard.IsExecutionEnabled);
        Assert.True(guard.IsWriteEnabled);
        Assert.True(guard.IsDestructiveEnabled);

        guard.IsExecutionEnabled = false;

        Assert.False(guard.IsExecutionEnabled);
        Assert.False(guard.IsWriteEnabled);
        Assert.False(guard.IsDestructiveEnabled);
    }

    [Fact]
    public void Cascading_DisableWrite_DisablesDestructive()
    {
        var guard = new ExcelSafetyGuard();
        guard.IsExecutionEnabled = true;
        guard.IsWriteEnabled = true;
        guard.IsDestructiveEnabled = true;

        guard.IsWriteEnabled = false;

        Assert.True(guard.IsExecutionEnabled);
        Assert.False(guard.IsWriteEnabled);
        Assert.False(guard.IsDestructiveEnabled);
    }

    [Fact]
    public void Rejection_CannotEnableWrite_WhenExecutionDisabled()
    {
        var guard = new ExcelSafetyGuard();
        Assert.False(guard.IsExecutionEnabled);

        guard.IsWriteEnabled = true;
        Assert.False(guard.IsWriteEnabled);
    }

    [Fact]
    public void Rejection_CannotEnableDestructive_WhenWriteDisabled()
    {
        var guard = new ExcelSafetyGuard();
        guard.IsExecutionEnabled = true;
        Assert.False(guard.IsWriteEnabled);

        guard.IsDestructiveEnabled = true;
        Assert.False(guard.IsDestructiveEnabled);
    }

    [Fact]
    public void EnsureTierAllowed_ThrowsWhenDisabled_WithCorrectErrorCodesAndMessages()
    {
        var guard = new ExcelSafetyGuard();

        // 1. All disabled -> Tier R throws -32001
        var exR = Assert.Throws<BridgeRequestException>(() => guard.EnsureTierAllowed(ExcelTier.ReadOnly));
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, exR.Code);
        Assert.Equal(-32001, exR.Code);
        Assert.Contains("Allow AI execution", exR.Message);

        // 2. Execution enabled -> Tier R allowed, Tier W throws -32001 with write message
        guard.IsExecutionEnabled = true;
        guard.EnsureTierAllowed(ExcelTier.ReadOnly); // Allowed

        var exW = Assert.Throws<BridgeRequestException>(() => guard.EnsureTierAllowed(ExcelTier.Write));
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, exW.Code);
        Assert.Equal(-32001, exW.Code);
        Assert.Contains("Allow write operations", exW.Message);

        // 3. Write enabled -> Tier W allowed, Tier D throws -32001 with destructive message
        guard.IsWriteEnabled = true;
        guard.EnsureTierAllowed(ExcelTier.Write); // Allowed

        var exD = Assert.Throws<BridgeRequestException>(() => guard.EnsureTierAllowed(ExcelTier.Destructive));
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, exD.Code);
        Assert.Equal(-32001, exD.Code);
        Assert.Contains("Allow destructive operations", exD.Message);

        // 4. Destructive enabled -> Tier D allowed
        guard.IsDestructiveEnabled = true;
        guard.EnsureTierAllowed(ExcelTier.Destructive); // Allowed
    }

    [Fact]
    public void StateChanged_FiresOnEveryMeaningfulToggle()
    {
        var guard = new ExcelSafetyGuard();
        int stateChangedCount = 0;
        guard.StateChanged += () => stateChangedCount++;

        guard.IsExecutionEnabled = true;
        Assert.Equal(1, stateChangedCount);

        guard.IsWriteEnabled = true;
        Assert.Equal(2, stateChangedCount);

        guard.IsDestructiveEnabled = true;
        Assert.Equal(3, stateChangedCount);

        // Setting to same value does not re-fire
        guard.IsDestructiveEnabled = true;
        Assert.Equal(3, stateChangedCount);

        // Disabling execution fires and resets all
        guard.IsExecutionEnabled = false;
        Assert.Equal(4, stateChangedCount);
        Assert.False(guard.IsWriteEnabled);
        Assert.False(guard.IsDestructiveEnabled);
    }

    [Fact]
    public async Task ConcurrentToggling_NeverViolatesInvariants()
    {
        var guard = new ExcelSafetyGuard();
        var violations = 0;
        var running = true;

        var task1 = Task.Run(() =>
        {
            var rnd = new Random(1);
            while (running)
            {
                guard.IsExecutionEnabled = rnd.Next(2) == 0;
            }
        });

        var task2 = Task.Run(() =>
        {
            var rnd = new Random(2);
            while (running)
            {
                guard.IsWriteEnabled = rnd.Next(2) == 0;
            }
        });

        var task3 = Task.Run(() =>
        {
            var rnd = new Random(3);
            while (running)
            {
                guard.IsDestructiveEnabled = rnd.Next(2) == 0;
            }
        });

        var checker = Task.Run(() =>
        {
            for (int i = 0; i < 100_000; i++)
            {
                var (exec, write, dest) = guard.GetState();

                // Invariants:
                // write requires exec
                // dest requires write and exec
                if (!exec && write) violations++;
                if (!exec && dest) violations++;
                if (!write && dest) violations++;
            }
        });

        await checker;
        running = false;
        await Task.WhenAll(task1, task2, task3);

        Assert.Equal(0, violations);
    }
}
