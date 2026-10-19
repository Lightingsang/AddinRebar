namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Types;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class FindingN7EmpiricalChallengerTests
{
    [Fact]
    public void N7_DuplicateNicknameR_RequestedOutputsR_ValueANotOverwritten_BothPreserved()
    {
        var g1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var g2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var paramA = TestProxyFactory.CreateParam("R", "ResultA", instanceGuid: g1, volatileDataCount: 1);
        var paramB = TestProxyFactory.CreateParam("R", "ResultB", instanceGuid: g2, volatileDataCount: 1);

        TestProxyFactory.GetProxy<TestParamProxy>(paramA).VolatileDataAdded.Add(new GH_String("Value_A"));
        TestProxyFactory.GetProxy<TestParamProxy>(paramB).VolatileDataAdded.Add(new GH_String("Value_B"));

        var objects = new[] { paramA, paramB };
        var requested = new[] { "R" };

        var outputs = PlayerOutputExtractor.ExtractOutputs(objects, requested);

        Assert.True(outputs.ContainsKey("R"));
        var aggregatedList = Assert.IsAssignableFrom<List<object?>>(outputs["R"]);
        Assert.Equal(2, aggregatedList.Count);
        Assert.Equal("Value_A", aggregatedList[0]);
        Assert.Equal("Value_B", aggregatedList[1]);

        Assert.True(outputs.ContainsKey("R#aaaaaaaa"));
        Assert.Equal("Value_A", outputs["R#aaaaaaaa"]);
        Assert.True(outputs.ContainsKey("R#bbbbbbbb"));
        Assert.Equal("Value_B", outputs["R#bbbbbbbb"]);
    }

    [Fact]
    public void N7_TriplicateNicknameR_RequestedOutputsR_AllThreePreserved()
    {
        var g1 = Guid.Parse("11111111-0000-0000-0000-000000000000");
        var g2 = Guid.Parse("22222222-0000-0000-0000-000000000000");
        var g3 = Guid.Parse("33333333-0000-0000-0000-000000000000");

        var p1 = TestProxyFactory.CreateParam("R", instanceGuid: g1, volatileDataCount: 1);
        var p2 = TestProxyFactory.CreateParam("R", instanceGuid: g2, volatileDataCount: 1);
        var p3 = TestProxyFactory.CreateParam("R", instanceGuid: g3, volatileDataCount: 1);

        TestProxyFactory.GetProxy<TestParamProxy>(p1).VolatileDataAdded.Add(new GH_Number(100.0));
        TestProxyFactory.GetProxy<TestParamProxy>(p2).VolatileDataAdded.Add(new GH_Number(200.0));
        TestProxyFactory.GetProxy<TestParamProxy>(p3).VolatileDataAdded.Add(new GH_Number(300.0));

        var objects = new[] { p1, p2, p3 };
        var outputs = PlayerOutputExtractor.ExtractOutputs(objects, new[] { "R" });

        var aggregated = Assert.IsAssignableFrom<List<object?>>(outputs["R"]);
        Assert.Equal(3, aggregated.Count);
        Assert.Equal(100.0, aggregated[0]);
        Assert.Equal(200.0, aggregated[1]);
        Assert.Equal(300.0, aggregated[2]);
        Assert.Equal(100.0, outputs["R#11111111"]);
        Assert.Equal(200.0, outputs["R#22222222"]);
        Assert.Equal(300.0, outputs["R#33333333"]);
    }

    [Fact]
    public void N7_UnfilteredQuery_NullRequestedOutputs_ExtractsAllAndPasses()
    {
        var pA = TestProxyFactory.CreateParam("Width", volatileDataCount: 1);
        var pB = TestProxyFactory.CreateParam("Height", volatileDataCount: 1);
        TestProxyFactory.GetProxy<TestParamProxy>(pA).VolatileDataAdded.Add(new GH_Number(15.0));
        TestProxyFactory.GetProxy<TestParamProxy>(pB).VolatileDataAdded.Add(new GH_Number(25.0));

        var objects = new[] { pA, pB };
        var outputs = PlayerOutputExtractor.ExtractOutputs(objects, requestedOutputs: null);

        Assert.Equal(2, outputs.Count);
        Assert.Equal(15.0, outputs["Width"]);
        Assert.Equal(25.0, outputs["Height"]);
    }

    [Fact]
    public void N7_UnfilteredQuery_DuplicateNicknames_DisambiguatesWithSuffixAndPasses()
    {
        var g1 = Guid.Parse("44444444-0000-0000-0000-000000000000");
        var g2 = Guid.Parse("55555555-0000-0000-0000-000000000000");

        var p1 = TestProxyFactory.CreateParam("Area", instanceGuid: g1, volatileDataCount: 1);
        var p2 = TestProxyFactory.CreateParam("Area", instanceGuid: g2, volatileDataCount: 1);
        TestProxyFactory.GetProxy<TestParamProxy>(p1).VolatileDataAdded.Add(new GH_Number(50.0));
        TestProxyFactory.GetProxy<TestParamProxy>(p2).VolatileDataAdded.Add(new GH_Number(75.0));

        var objects = new[] { p1, p2 };
        var outputs = PlayerOutputExtractor.ExtractOutputs(objects, requestedOutputs: null);

        Assert.Equal(2, outputs.Count);
        Assert.Equal(50.0, outputs["Area"]);
        Assert.Equal(75.0, outputs["Area#55555555"]);
    }
}
