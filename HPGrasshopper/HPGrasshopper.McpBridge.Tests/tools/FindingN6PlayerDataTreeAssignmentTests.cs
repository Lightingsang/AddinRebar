namespace HPGrasshopper.McpBridge.Tests.Tools;

using System.Text.Json;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class FindingN6PlayerDataTreeAssignmentTests
{
    [Fact]
    public void SetParamValue_WhenAddVolatileDataReturnsFalse_ReturnsFalseAndRejects()
    {
        var param = TestProxyFactory.CreateParam("TargetNumber");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.AddVolatileDataReturnValue = false;

        using var jsonDoc = JsonDocument.Parse("123.45");

        var success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement, out var errorMessage);

        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("refused", errorMessage, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetParamValue_DataTree_MalformedBranchPath_ReturnsFalseAndReportsError()
    {
        var param = TestProxyFactory.CreateParam("TreeParam");
        using var jsonDoc = JsonDocument.Parse("""
        {
            "{0}": [10],
            "{a}": [20]
        }
        """);

        var success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement, out var errorMessage);

        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("{a}", errorMessage);
    }

    [Fact]
    public void SetParamValue_ArrayWithNullItem_ReturnsFalseAndRejectsWithoutIndexShift()
    {
        var param = TestProxyFactory.CreateParam("NumberList");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);

        using var jsonDoc = JsonDocument.Parse("[10, null, 30]");

        var success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement, out var errorMessage);

        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("null", errorMessage, System.StringComparison.OrdinalIgnoreCase);
        Assert.Empty(proxy.VolatileDataAdded);
    }

    [Fact]
    public void TryStageParamValue_ValidScalarAndPoints_Succeeds()
    {
        var param = TestProxyFactory.CreateParam("PtParam");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.TypeName = "Point";

        using var jsonDoc = JsonDocument.Parse("""{"x": 1.0, "y": 2.0, "z": 3.0}""");

        var success = PlayerDataTreeHelper.TryStageParamValue(param, jsonDoc.RootElement, out var staged, out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(staged);
        Assert.Single(staged);
    }
}
