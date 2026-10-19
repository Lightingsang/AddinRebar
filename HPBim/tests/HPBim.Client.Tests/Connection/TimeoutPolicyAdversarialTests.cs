namespace HPBim.Client.Tests.Connection;

using System;
using HPBim.Client.Connection;
using Xunit;

public sealed class AdversarialTimeoutPolicyTests
{
    [Theory]
    [InlineData(0, 60, 90)]
    [InlineData(-1, 60, 90)]
    [InlineData(-1000, 60, 90)]
    [InlineData(int.MinValue, 60, 90)]
    public void CalculateBudgets_NonPositiveItems_ReturnsDefaultBudget(
        int itemCount,
        int expectedExecSec,
        int expectedTotalSec)
    {
        var policy = new TimeoutPolicy();
        var execBudget = policy.CalculateExecutionBudget(itemCount);
        var totalTimeout = policy.CalculateTotalTimeout(itemCount);

        Assert.Equal(TimeSpan.FromSeconds(expectedExecSec), execBudget);
        Assert.Equal(TimeSpan.FromSeconds(expectedTotalSec), totalTimeout);
    }

    [Fact]
    public void CalculateBudgets_OneItem_CalculatesBasePlusOneUnit()
    {
        var policy = new TimeoutPolicy();
        // 1 item: 15s + (1 * 50ms) = 15.05s
        var execBudget = policy.CalculateExecutionBudget(1);
        var totalTimeout = policy.CalculateTotalTimeout(1);

        Assert.Equal(TimeSpan.FromMilliseconds(15050), execBudget);
        Assert.Equal(TimeSpan.FromMilliseconds(45050), totalTimeout);
    }

    [Theory]
    [InlineData(5699, 299950, false)] // Just under 300s limit
    [InlineData(5700, 300000, true)]  // Exact 300s boundary (15s + 5700 * 50ms = 300s)
    [InlineData(5701, 300000, true)]  // Just over 300s -> clamped
    [InlineData(10000, 300000, true)] // 10,000 items -> clamped
    [InlineData(100000, 300000, true)]// 100,000 items -> clamped
    public void CalculateBudgets_BoundaryAndClamping_ClampsToMaxExecutionBudget(
        int itemCount,
        long expectedExecMs,
        bool isClamped)
    {
        var policy = new TimeoutPolicy();
        var execBudget = policy.CalculateExecutionBudget(itemCount);

        Assert.Equal(TimeSpan.FromMilliseconds(expectedExecMs), execBudget);
        if (isClamped)
        {
            Assert.Equal(policy.MaxExecutionBudget, execBudget);
        }
    }

    [Fact]
    public void CalculateBudgets_HugeItemCount_DoesNotOverflowLongOrCrash()
    {
        var policy = new TimeoutPolicy();
        var execBudget = policy.CalculateExecutionBudget(int.MaxValue);

        Assert.Equal(policy.MaxExecutionBudget, execBudget);
        Assert.Equal(policy.QueueDeadline + policy.MaxExecutionBudget, policy.CalculateTotalTimeout(int.MaxValue));
    }

    [Fact]
    public void CustomOverrides_TakePrecedenceOverDefaultConstants()
    {
        var customPolicy = new TimeoutPolicy
        {
            TransportTimeout = TimeSpan.FromSeconds(2),
            QueueDeadline = TimeSpan.FromSeconds(45),
            BaseExecutionBudget = TimeSpan.FromSeconds(10),
            PerItemBudget = TimeSpan.FromMilliseconds(100),
            DefaultExecutionBudget = TimeSpan.FromSeconds(120),
            MaxExecutionBudget = TimeSpan.FromMinutes(10)
        };

        // 0 items -> custom DefaultExecutionBudget (120s)
        Assert.Equal(TimeSpan.FromSeconds(120), customPolicy.CalculateExecutionBudget(0));
        Assert.Equal(TimeSpan.FromSeconds(165), customPolicy.CalculateTotalTimeout(0));

        // 50 items: 10s + (50 * 100ms) = 15s
        Assert.Equal(TimeSpan.FromSeconds(15), customPolicy.CalculateExecutionBudget(50));
        Assert.Equal(TimeSpan.FromSeconds(60), customPolicy.CalculateTotalTimeout(50));

        // 10,000 items: 10s + 1000s = 1010s > 600s -> clamped to 600s
        Assert.Equal(TimeSpan.FromMinutes(10), customPolicy.CalculateExecutionBudget(10000));
        Assert.Equal(TimeSpan.FromSeconds(645), customPolicy.CalculateTotalTimeout(10000));
    }

    [Fact]
    public void Validate_BoundaryAndInvalidSettings_ThrowsExpectedExceptions()
    {
        var validCustom = new TimeoutPolicy
        {
            BaseExecutionBudget = TimeSpan.FromSeconds(30),
            MaxExecutionBudget = TimeSpan.FromSeconds(30) // equal is allowed
        };
        validCustom.Validate();

        var invalidMax = new TimeoutPolicy
        {
            BaseExecutionBudget = TimeSpan.FromSeconds(30),
            MaxExecutionBudget = TimeSpan.FromSeconds(29)
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => invalidMax.Validate());

        var zeroTransport = new TimeoutPolicy { TransportTimeout = TimeSpan.Zero };
        Assert.Throws<ArgumentOutOfRangeException>(() => zeroTransport.Validate());
    }

    [Fact]
    public void CustomOverrides_TighterMaxExecutionBudget_ExposesDefaultBudgetInversion()
    {
        // When a user sets a tighter MaxExecutionBudget (e.g. 30s) while leaving DefaultExecutionBudget at default (60s):
        var policy = new TimeoutPolicy
        {
            MaxExecutionBudget = TimeSpan.FromSeconds(30),
            BaseExecutionBudget = TimeSpan.FromSeconds(10)
        };

        // For 100 items: 10s + 100*50ms = 15s < 30s -> 15s
        Assert.Equal(TimeSpan.FromSeconds(15), policy.CalculateExecutionBudget(100));

        // For 10,000 items: clamped to MaxExecutionBudget (30s)
        Assert.Equal(TimeSpan.FromSeconds(30), policy.CalculateExecutionBudget(10000));

        // For 0 items: returns DefaultExecutionBudget (60s), which exceeds MaxExecutionBudget (30s)!
        var budgetZeroItems = policy.CalculateExecutionBudget(0);
        Assert.Equal(TimeSpan.FromSeconds(60), budgetZeroItems);
        Assert.True(budgetZeroItems > policy.MaxExecutionBudget);
    }
}
