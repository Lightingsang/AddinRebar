using System;
using System.Collections.Generic;
using HPRobot.McpBridge.Units;
using Xunit;

namespace HPRobot.McpBridge.Tests;

public sealed class RobotUnitsPolicyTests
{
    [Fact]
    public void Units_HasStandardMetricMetadata()
    {
        var units = RobotUnitsPolicy.Units;

        Assert.Equal("Metric", units.Label);
        Assert.Equal(1.0, units.MmPerUnit);
        Assert.NotNull(units.Note);
        Assert.Contains("Metric: length m, force kN, moment kN·m, stress MPa", units.Note);
    }

    [Fact]
    public void Run_WithNullUnitMngr_ExecutesBodyDirectly()
    {
        var logs = new List<string>();
        bool bodyExecuted = false;

        var result = RobotUnitsPolicy.Run(null, logs, () =>
        {
            bodyExecuted = true;
        });

        Assert.True(bodyExecuted);
        Assert.Null(result);
        Assert.Empty(logs);
    }

    [Fact]
    public void Run_WithNullUnitMngr_PropagatesBodyException()
    {
        var logs = new List<string>();

        Assert.Throws<InvalidOperationException>(() =>
            RobotUnitsPolicy.Run(null, logs, () => throw new InvalidOperationException("Test Failure")));
    }
}
