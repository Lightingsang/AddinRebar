namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Types;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class FindingN7DuplicateOutputNicknamesTests
{
    [Fact]
    public void ExtractOutputs_DuplicateNicknameWithRequestedOutputs_PreservesAllValuesWithoutOverwrite()
    {
        var guid1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var guid2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var param1 = TestProxyFactory.CreateParam("R", "Param1", instanceGuid: guid1, volatileDataCount: 1);
        var proxy1 = TestProxyFactory.GetProxy<TestParamProxy>(param1);
        proxy1.VolatileDataAdded.Add(new GH_String("OutputA"));

        var param2 = TestProxyFactory.CreateParam("R", "Param2", instanceGuid: guid2, volatileDataCount: 1);
        var proxy2 = TestProxyFactory.GetProxy<TestParamProxy>(param2);
        proxy2.VolatileDataAdded.Add(new GH_String("OutputB"));

        var objects = new[] { param1, param2 };
        var requestedOutputs = new[] { "R" };

        var result = PlayerOutputExtractor.ExtractOutputs(objects, requestedOutputs);

        Assert.NotEmpty(result);
        var allValues = new List<string>();
        foreach (var val in result.Values)
        {
            if (val is string s) allValues.Add(s);
            else if (val is IEnumerable<object?> list)
            {
                foreach (var item in list)
                    if (item != null) allValues.Add(item.ToString()!);
            }
        }

        Assert.Contains("OutputA", allValues);
        Assert.Contains("OutputB", allValues);
        Assert.True(result.ContainsKey("R#11111111"));
        Assert.True(result.ContainsKey("R#22222222"));
    }

    [Fact]
    public void ExtractOutputs_SingleNicknameWithRequestedOutputs_ReturnsDirectAliasWithoutPrefix()
    {
        var param = TestProxyFactory.CreateParam("Result", volatileDataCount: 1);
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.VolatileDataAdded.Add(new GH_Number(42.0));

        var objects = new[] { param };
        var requestedOutputs = new[] { "Result" };

        var result = PlayerOutputExtractor.ExtractOutputs(objects, requestedOutputs);

        Assert.Single(result);
        Assert.True(result.ContainsKey("Result"));
        Assert.Equal(42.0, result["Result"]);
    }
}
