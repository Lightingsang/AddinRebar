namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class PlayerDataTreeTests
{
    [Fact]
    public void SetParamValue_SingleNumber_AddsVolatileDataNumber()
    {
        var param = TestProxyFactory.CreateParam("InputA");
        using var jsonDoc = JsonDocument.Parse("42.5");
        bool success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement);

        Assert.True(success);
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        Assert.Single(proxy.VolatileDataAdded);
        Assert.IsType<GH_Number>(proxy.VolatileDataAdded[0]);
    }

    [Fact]
    public void SetParamValue_Boolean_AddsVolatileDataBoolean()
    {
        var param = TestProxyFactory.CreateParam("Toggle");
        using var jsonDoc = JsonDocument.Parse("true");
        bool success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement);

        Assert.True(success);
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        Assert.Single(proxy.VolatileDataAdded);
        Assert.IsType<GH_Boolean>(proxy.VolatileDataAdded[0]);
    }

    [Fact]
    public void SetParamValue_Point3dArray_AddsVolatileDataPoint()
    {
        var param = TestProxyFactory.CreateParam("Pt");
        using var jsonDoc = JsonDocument.Parse("[10.0, 20.0, 30.0]");
        bool success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement);

        Assert.True(success);
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        Assert.Single(proxy.VolatileDataAdded);
        Assert.IsType<GH_Point>(proxy.VolatileDataAdded[0]);
    }

    [Fact]
    public void SetParamValue_DataTree_ParsesPathsAndAddsBranches()
    {
        var param = TestProxyFactory.CreateParam("TreeInput");
        using var jsonDoc = JsonDocument.Parse("""
        {
            "{0}": [1.0, 2.0],
            "{0;1}": [3.0]
        }
        """);

        bool success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement);
        Assert.True(success);
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        Assert.Equal(3, proxy.VolatileDataAdded.Count);
    }

    [Fact]
    public void ParsePath_MultipleIndices_ReturnsCorrectGHPath()
    {
        var path = PlayerDataTreeHelper.ParsePath("{0;1;2}");
        Assert.Equal(3, path.Length);
        Assert.Equal(0, path.Indices[0]);
        Assert.Equal(1, path.Indices[1]);
        Assert.Equal(2, path.Indices[2]);
    }

    [Fact]
    public void FindParam_ComponentInputByGuid_ResolvesInputParameter()
    {
        var comp = TestProxyFactory.CreateComponent("Multiply", "*");
        var inputGuid = Guid.NewGuid();
        var inputParam = TestProxyFactory.CreateParam("A", "Number", instanceGuid: inputGuid);
        TestProxyFactory.GetProxy<TestComponentProxy>(comp).Inputs.Add(inputParam);

        var objects = new IGH_DocumentObject[] { comp };
        var found = PlayerDataTreeHelper.FindParam(objects, inputGuid.ToString("D"));

        Assert.NotNull(found);
        Assert.Equal(inputGuid, found.InstanceGuid);
    }

    [Fact]
    public void ExtractOutputs_DuplicateNickNames_DisambiguatesWithoutOverwrite()
    {
        var guidA = Guid.NewGuid();
        var guidB = Guid.NewGuid();
        var paramA = TestProxyFactory.CreateParam("Result", instanceGuid: guidA, volatileDataCount: 1);
        var paramB = TestProxyFactory.CreateParam("Result", instanceGuid: guidB, volatileDataCount: 1);

        TestProxyFactory.GetProxy<TestParamProxy>(paramA).VolatileDataAdded.Add(new GH_Number(100.0));
        TestProxyFactory.GetProxy<TestParamProxy>(paramB).VolatileDataAdded.Add(new GH_Number(200.0));

        var objects = new IGH_DocumentObject[] { paramA, paramB };
        var outputs = PlayerRunner.ExtractOutputs(objects, null);

        Assert.Equal(2, outputs.Count);
        Assert.True(outputs.ContainsKey("Result"));
        Assert.True(outputs.ContainsKey($"Result#{guidB.ToString("D")[..8]}"));
    }

    [Fact]
    public void ConvertGooToTypedValue_ReturnsNativeTypes()
    {
        var numGoo = new GH_Number(3.14);
        var intGoo = new GH_Integer(42);
        var boolGoo = new GH_Boolean(true);
        var strGoo = new GH_String("Grasshopper");

        Assert.Equal(3.14, PlayerDataTreeHelper.ConvertGooToTypedValue(numGoo));
        Assert.Equal(42, PlayerDataTreeHelper.ConvertGooToTypedValue(intGoo));
        Assert.Equal(true, PlayerDataTreeHelper.ConvertGooToTypedValue(boolGoo));
        Assert.Equal("Grasshopper", PlayerDataTreeHelper.ConvertGooToTypedValue(strGoo));
    }

    [Fact]
    public void ExtractParamData_VolatileDataCountZero_ReturnsNull()
    {
        var param = TestProxyFactory.CreateParam("EmptyParam", volatileDataCount: 0);
        var extracted = PlayerDataTreeHelper.ExtractParamData(param);
        Assert.Null(extracted);
    }
}
