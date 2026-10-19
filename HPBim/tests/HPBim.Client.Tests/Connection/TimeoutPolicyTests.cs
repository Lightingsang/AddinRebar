namespace HPBim.Client.Tests.Connection;

using System;
using HPBim.Client.Connection;
using Xunit;

public sealed class TimeoutPolicyTests
{
    [Fact]
    public void Defaults_MatchSpecification()
    {
        var policy = new TimeoutPolicy();
        Assert.Equal(TimeSpan.FromSeconds(5), policy.TransportTimeout);
        Assert.Equal(TimeSpan.FromSeconds(30), policy.QueueDeadline);
        Assert.Equal(TimeSpan.FromSeconds(15), policy.BaseExecutionBudget);
        Assert.Equal(TimeSpan.FromMilliseconds(50), policy.PerItemBudget);
        Assert.Equal(TimeSpan.FromSeconds(60), policy.DefaultExecutionBudget);
        Assert.Equal(TimeSpan.FromMinutes(5), policy.MaxExecutionBudget);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void ComputeExecutionBudget_NonPositiveCount_ReturnsDefaultBudget(int count)
    {
        var policy = new TimeoutPolicy();
        Assert.Equal(TimeSpan.FromSeconds(60), policy.ComputeExecutionBudget(count));
    }

    [Fact]
    public void ComputeExecutionBudget_ScalesLinearlyWithItemCount()
    {
        var policy = new TimeoutPolicy();

        // 100 items: 15s + (100 * 50ms) = 15s + 5s = 20s
        Assert.Equal(TimeSpan.FromSeconds(20), policy.ComputeExecutionBudget(100));

        // 1000 items: 15s + (1000 * 50ms) = 15s + 50s = 65s
        Assert.Equal(TimeSpan.FromSeconds(65), policy.ComputeExecutionBudget(1000));
    }

    [Fact]
    public void ComputeExecutionBudget_ClampsToMaxBudget()
    {
        var policy = new TimeoutPolicy();

        // 10,000 items: 15s + 500s = 515s > 300s -> clamped to 300s
        Assert.Equal(TimeSpan.FromMinutes(5), policy.ComputeExecutionBudget(10000));
    }

    [Fact]
    public void ComputeTotalTimeout_SumsQueueDeadlineToExecutionBudget()
    {
        var policy = new TimeoutPolicy();

        // 100 items: 30s queue + 20s exec = 50s total
        Assert.Equal(TimeSpan.FromSeconds(50), policy.ComputeTotalTimeout(100));

        // Alias check
        Assert.Equal(policy.ComputeTotalTimeout(100), policy.CalculateTotalTimeout(100));
        Assert.Equal(policy.ComputeExecutionBudget(100), policy.CalculateExecutionBudget(100));
    }

    [Fact]
    public void Validate_SucceedsOnDefaultConfiguration()
    {
        var policy = new TimeoutPolicy();
        policy.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_ThrowsOnZeroOrNegativeTimeouts()
    {
        var badTransport = new TimeoutPolicy { TransportTimeout = TimeSpan.Zero };
        Assert.Throws<ArgumentOutOfRangeException>(() => badTransport.Validate());

        var badQueue = new TimeoutPolicy { QueueDeadline = TimeSpan.FromSeconds(-1) };
        Assert.Throws<ArgumentOutOfRangeException>(() => badQueue.Validate());

        var badBudget = new TimeoutPolicy { BaseExecutionBudget = TimeSpan.Zero };
        Assert.Throws<ArgumentOutOfRangeException>(() => badBudget.Validate());

        var badMax = new TimeoutPolicy { MaxExecutionBudget = TimeSpan.FromSeconds(10), BaseExecutionBudget = TimeSpan.FromSeconds(20) };
        Assert.Throws<ArgumentOutOfRangeException>(() => badMax.Validate());
    }
}
