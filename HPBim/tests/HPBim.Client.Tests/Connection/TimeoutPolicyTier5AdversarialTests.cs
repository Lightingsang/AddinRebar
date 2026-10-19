namespace HPBim.Client.Tests.Connection;

using System;
using HPBim.Client.Connection;
using Xunit;

public sealed class TimeoutPolicyTier5AdversarialTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_NonPositiveTransportTimeout_ThrowsArgumentOutOfRangeException(int seconds)
    {
        var policy = new TimeoutPolicy { TransportTimeout = TimeSpan.FromSeconds(seconds) };
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_NonPositiveQueueDeadline_ThrowsArgumentOutOfRangeException(int seconds)
    {
        var policy = new TimeoutPolicy { QueueDeadline = TimeSpan.FromSeconds(seconds) };
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_NonPositiveBaseExecutionBudget_ThrowsArgumentOutOfRangeException(int seconds)
    {
        var policy = new TimeoutPolicy { BaseExecutionBudget = TimeSpan.FromSeconds(seconds) };
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.Validate());
    }

    [Fact]
    public void Validate_MaxLessThanBase_ThrowsArgumentOutOfRangeException()
    {
        var policy = new TimeoutPolicy
        {
            BaseExecutionBudget = TimeSpan.FromSeconds(60),
            MaxExecutionBudget = TimeSpan.FromSeconds(59)
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.Validate());
    }

    [Fact]
    public void Validate_ExactEquality_BaseAndMaxBudget_ValidatesSuccessfully()
    {
        var policy = new TimeoutPolicy
        {
            BaseExecutionBudget = TimeSpan.FromSeconds(45),
            MaxExecutionBudget = TimeSpan.FromSeconds(45)
        };
        var ex = Record.Exception(() => policy.Validate());
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void ComputeExecutionBudget_ZeroOrNegativeItems_ReturnsDefaultExecutionBudget(int count)
    {
        var policy = new TimeoutPolicy();
        var budget = policy.ComputeExecutionBudget(count);
        Assert.Equal(policy.DefaultExecutionBudget, budget);
    }

    [Fact]
    public void ComputeExecutionBudget_OneItem_ReturnsBasePlusOneIncrement()
    {
        var policy = new TimeoutPolicy();
        var budget = policy.ComputeExecutionBudget(1);
        var expected = policy.BaseExecutionBudget + policy.PerItemBudget;
        Assert.Equal(expected, budget);
    }

    [Theory]
    [InlineData(5699, false)]
    [InlineData(5700, true)]
    [InlineData(5701, true)]
    [InlineData(100000, true)]
    public void ComputeExecutionBudget_ClampsStrictlyToMaxExecutionBudget(int count, bool isAtMax)
    {
        var policy = new TimeoutPolicy();
        var budget = policy.ComputeExecutionBudget(count);

        if (isAtMax)
        {
            Assert.Equal(policy.MaxExecutionBudget, budget);
        }
        else
        {
            Assert.True(budget < policy.MaxExecutionBudget);
        }
    }

    [Fact]
    public void ComputeTotalTimeout_SumOfQueueDeadlineAndExecutionBudget()
    {
        var policy = new TimeoutPolicy();
        int[] testCounts = { 0, 1, 50, 6000 };

        foreach (int count in testCounts)
        {
            var total = policy.ComputeTotalTimeout(count);
            var expected = policy.QueueDeadline + policy.ComputeExecutionBudget(count);
            Assert.Equal(expected, total);
        }
    }

    [Fact]
    public void ComputeExecutionBudget_HugeCountIntMax_CalculatesSafelyWithoutOverflow()
    {
        var policy = new TimeoutPolicy();
        var budget = policy.ComputeExecutionBudget(int.MaxValue);
        Assert.Equal(policy.MaxExecutionBudget, budget);
    }

    [Fact]
    public void CustomPerItemBudget_ZeroPerItem_YieldsConstantBaseBudget()
    {
        var policy = new TimeoutPolicy { PerItemBudget = TimeSpan.Zero };
        var budget = policy.ComputeExecutionBudget(500);
        Assert.Equal(policy.BaseExecutionBudget, budget);
    }
}
