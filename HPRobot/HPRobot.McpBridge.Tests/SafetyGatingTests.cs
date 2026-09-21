using System;
using System.Threading.Tasks;
using HPRobot.McpBridge.Safety;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

namespace HPRobot.McpBridge.Tests;

public sealed class SafetyGatingTests
{
    [Fact]
    public void InitialState_BothGatesAreDisabled()
    {
        var guard = new RobotSafetyGuard();
        var (exec, heavy) = guard.GetState();

        Assert.False(guard.IsExecutionEnabled);
        Assert.False(guard.IsHeavyOperationsEnabled);
        Assert.False(exec);
        Assert.False(heavy);
    }

    [Fact]
    public void EnsureExecutionAllowed_WhenExecutionDisabled_ThrowsCode32001()
    {
        var guard = new RobotSafetyGuard { IsExecutionEnabled = false };

        var ex = Assert.Throws<BridgeRequestException>(() => guard.EnsureExecutionAllowed());
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, ex.Code);
        Assert.Equal(-32001, ex.Code);
        Assert.Contains("Code execution is disabled", ex.Message);
        Assert.Equal(RobotSafetyGuard.ExecutionDisabledMessage, ex.Message);
    }

    [Theory]
    [InlineData(RobotTier.Read)]
    [InlineData(RobotTier.Write)]
    [InlineData(RobotTier.DeleteHeavy)]
    public void EnsureTierAllowed_WhenExecutionDisabled_RefusesAllTiersWithCode32001(RobotTier tier)
    {
        var guard = new RobotSafetyGuard { IsExecutionEnabled = false };

        var ex = Assert.Throws<BridgeRequestException>(() => guard.EnsureTierAllowed(tier));
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, ex.Code);
        Assert.Equal(-32001, ex.Code);
        Assert.Contains("Code execution is disabled", ex.Message);
    }

    [Fact]
    public void EnsureHeavyOperationsAllowed_WhenExecutionDisabled_ThrowsExecutionDisabledMessage()
    {
        var guard = new RobotSafetyGuard { IsExecutionEnabled = false };

        var ex = Assert.Throws<BridgeRequestException>(() => guard.EnsureHeavyOperationsAllowed());
        Assert.Equal(-32001, ex.Code);
        Assert.Equal(RobotSafetyGuard.ExecutionDisabledMessage, ex.Message);
    }

    [Fact]
    public void WhenExecutionEnabled_ReadAndWriteAllowed_TierDRefusedWithCode32001()
    {
        var guard = new RobotSafetyGuard
        {
            IsExecutionEnabled = true,
            IsHeavyOperationsEnabled = false
        };

        // Allowed without exceptions
        guard.EnsureExecutionAllowed();
        guard.EnsureTierAllowed(RobotTier.Read);
        guard.EnsureTierAllowed(RobotTier.Write);

        // Tier D / Heavy refused with specific message and code -32001
        var exTier = Assert.Throws<BridgeRequestException>(() => guard.EnsureTierAllowed(RobotTier.DeleteHeavy));
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, exTier.Code);
        Assert.Equal(-32001, exTier.Code);
        Assert.Equal(RobotSafetyGuard.HeavyOperationsDisabledMessage, exTier.Message);
        Assert.Contains("Heavy operations", exTier.Message);

        var exHeavy = Assert.Throws<BridgeRequestException>(() => guard.EnsureHeavyOperationsAllowed());
        Assert.Equal(-32001, exHeavy.Code);
        Assert.Equal(RobotSafetyGuard.HeavyOperationsDisabledMessage, exHeavy.Message);
    }

    [Fact]
    public void WhenBothGatesEnabled_AllTiersAllowedWithoutException()
    {
        var guard = new RobotSafetyGuard
        {
            IsExecutionEnabled = true,
            IsHeavyOperationsEnabled = true
        };

        guard.EnsureExecutionAllowed();
        guard.EnsureHeavyOperationsAllowed();
        guard.EnsureTierAllowed(RobotTier.Read);
        guard.EnsureTierAllowed(RobotTier.Write);
        guard.EnsureTierAllowed(RobotTier.DeleteHeavy);
    }

    [Fact]
    public void ToggleCoupling_HeavyCannotBeEnabled_WithoutExecution()
    {
        var guard = new RobotSafetyGuard
        {
            IsExecutionEnabled = false,
            IsHeavyOperationsEnabled = true // Attempt to enable heavy operations
        };

        Assert.False(guard.IsExecutionEnabled);
        Assert.False(guard.IsHeavyOperationsEnabled, "Heavy operations must not be enabled when execution is disabled.");
    }

    [Fact]
    public void ToggleCoupling_DisablingExecution_AutomaticallyDisablesHeavy()
    {
        var guard = new RobotSafetyGuard
        {
            IsExecutionEnabled = true,
            IsHeavyOperationsEnabled = true
        };
        Assert.True(guard.IsExecutionEnabled);
        Assert.True(guard.IsHeavyOperationsEnabled);

        // Disable master execution gate
        guard.IsExecutionEnabled = false;

        Assert.False(guard.IsExecutionEnabled);
        Assert.False(guard.IsHeavyOperationsEnabled, "Disabling execution must reset heavy operations to false.");
    }

    [Fact]
    public void StateChangedEvent_FiresOnTransitions()
    {
        var guard = new RobotSafetyGuard();
        int eventCount = 0;
        guard.StateChanged += () => eventCount++;

        guard.IsExecutionEnabled = true;
        Assert.Equal(1, eventCount);

        // Redundant set should not fire
        guard.IsExecutionEnabled = true;
        Assert.Equal(1, eventCount);

        guard.IsHeavyOperationsEnabled = true;
        Assert.Equal(2, eventCount);

        guard.IsHeavyOperationsEnabled = false;
        Assert.Equal(3, eventCount);

        guard.IsExecutionEnabled = false;
        Assert.Equal(4, eventCount);
    }

    [Fact]
    public void ThreadSafety_ConcurrentAccessStress()
    {
        var guard = new RobotSafetyGuard();

        Parallel.For(0, 1000, i =>
        {
            if (i % 2 == 0)
            {
                guard.IsExecutionEnabled = true;
                guard.IsHeavyOperationsEnabled = (i % 4 == 0);
            }
            else
            {
                guard.IsExecutionEnabled = false;
            }

            var (exec, heavy) = guard.GetState();
            if (!exec)
            {
                Assert.False(heavy);
            }
        });
    }
}
