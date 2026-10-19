namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class PlayerDataTreeChallengerTests
{
    [Fact]
    public void ParsePath_DeeplyNestedAndEdgeCases_ParsesCorrectly()
    {
        var deepPath = PlayerDataTreeHelper.ParsePath("{0;1;2;3;4;5;6;7;8;9}");
        Assert.Equal(10, deepPath.Length);
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(i, deepPath.Indices[i]);
        }

        var emptyBraces = PlayerDataTreeHelper.ParsePath("{}");
        Assert.Equal(new GH_Path(0), emptyBraces);

        var emptyWhitespace = PlayerDataTreeHelper.ParsePath("   ");
        Assert.Equal(new GH_Path(0), emptyWhitespace);

        var extraSemicolons = PlayerDataTreeHelper.ParsePath("{0;;;3}");
        Assert.Equal(2, extraSemicolons.Length);
        Assert.Equal(0, extraSemicolons.Indices[0]);
        Assert.Equal(3, extraSemicolons.Indices[1]);

        var invalidSegment = PlayerDataTreeHelper.ParsePath("{0;invalid;5}");
        Assert.Equal(3, invalidSegment.Length);
        Assert.Equal(0, invalidSegment.Indices[1]);
        Assert.Equal(5, invalidSegment.Indices[2]);
    }

    [Fact]
    public void DataTree_EmptyAndNonTreeObjects_ReturnsFalse()
    {
        var param = TestProxyFactory.CreateParam("TreeInput");

        using var emptyObj = JsonDocument.Parse("{}");
        bool emptySuccess = PlayerDataTreeHelper.SetParamValue(param, emptyObj.RootElement);
        Assert.False(emptySuccess);

        using var nonTreeObj = JsonDocument.Parse("""{"key": "value", "count": 10}""");
        bool nonTreeSuccess = PlayerDataTreeHelper.SetParamValue(param, nonTreeObj.RootElement);
        Assert.False(nonTreeSuccess);
    }

    [Fact]
    public void DataTree_SingleItemTreeScalarAndArray_AddsVolatileData()
    {
        var paramScalar = TestProxyFactory.CreateParam("ScalarBranch");
        using var jsonScalar = JsonDocument.Parse("""{ "{0}": 99.5 }""");
        bool scalarSuccess = PlayerDataTreeHelper.SetParamValue(paramScalar, jsonScalar.RootElement);
        Assert.True(scalarSuccess);
        var proxyScalar = TestProxyFactory.GetProxy<TestParamProxy>(paramScalar);
        Assert.Single(proxyScalar.VolatileDataAdded);
        Assert.IsType<GH_Number>(proxyScalar.VolatileDataAdded[0]);

        var paramArray = TestProxyFactory.CreateParam("ArrayBranch");
        using var jsonArray = JsonDocument.Parse("""{ "{0;1}": ["hello"] }""");
        bool arraySuccess = PlayerDataTreeHelper.SetParamValue(paramArray, jsonArray.RootElement);
        Assert.True(arraySuccess);
        var proxyArray = TestProxyFactory.GetProxy<TestParamProxy>(paramArray);
        Assert.Single(proxyArray.VolatileDataAdded);
        Assert.IsType<GH_String>(proxyArray.VolatileDataAdded[0]);
    }

    [Fact]
    public void DataTree_HighBranchingPaths_PopulatesAllBranches()
    {
        var param = TestProxyFactory.CreateParam("WideTree");
        var dict = new Dictionary<string, object>();
        for (int i = 0; i < 50; i++)
        {
            dict[$"{{{i}}}"] = new[] { i * 1.5 };
        }
        var jsonText = JsonSerializer.Serialize(dict);
        using var jsonDoc = JsonDocument.Parse(jsonText);

        bool success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement);
        Assert.True(success);
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        Assert.Equal(50, proxy.VolatileDataAdded.Count);
    }

    [Fact]
    public void DataTree_DeeplyNestedBranches_PopulatesData()
    {
        var param = TestProxyFactory.CreateParam("DeepTree");
        using var jsonDoc = JsonDocument.Parse("""
        {
            "{0;1;2;3;4;5}": [10.0, 20.0],
            "{0;1;2;3;4;6}": [30.0]
        }
        """);

        bool success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement);
        Assert.True(success);
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        Assert.Equal(3, proxy.VolatileDataAdded.Count);
    }

    [Fact]
    public void ExtractParamData_HighBranchingStructure_PreservesAllBranches()
    {
        var proxyInstance = DispatchProxy.Create<IGH_Param, ChallengerMultiBranchDouble>();
        var impl = (ChallengerMultiBranchDouble)(object)proxyInstance;

        var structure = new GH_Structure<IGH_Goo>();
        for (int i = 0; i < 25; i++)
        {
            structure.Append(new GH_Integer(i * 10), new GH_Path(i));
        }
        impl.VolatileData = structure;

        var extracted = PlayerDataTreeHelper.ExtractParamData(proxyInstance);
        Assert.NotNull(extracted);
        var dict = Assert.IsType<Dictionary<string, object?>>(extracted);
        Assert.Equal(25, dict.Count);

        for (int i = 0; i < 25; i++)
        {
            var key = $"{{{i}}}";
            Assert.True(dict.ContainsKey(key));
            var items = Assert.IsType<List<object?>>(dict[key]);
            Assert.Single(items);
            Assert.Equal(i * 10, items[0]);
        }
    }

    [Fact]
    public void ExtractParamData_DeeplyNestedSinglePath_ExtractsAsDictionary()
    {
        var proxyInstance = DispatchProxy.Create<IGH_Param, ChallengerMultiBranchDouble>();
        var impl = (ChallengerMultiBranchDouble)(object)proxyInstance;

        var structure = new GH_Structure<IGH_Goo>();
        var deepPath = new GH_Path(0, 1, 2, 3);
        structure.Append(new GH_String("DeepLeaf"), deepPath);
        impl.VolatileData = structure;

        var extracted = PlayerDataTreeHelper.ExtractParamData(proxyInstance);
        Assert.NotNull(extracted);
        var dict = Assert.IsType<Dictionary<string, object?>>(extracted);
        Assert.Single(dict);
        Assert.True(dict.ContainsKey("{0;1;2;3}"));
        var items = Assert.IsType<List<object?>>(dict["{0;1;2;3}"]);
        Assert.Single(items);
        Assert.Equal("DeepLeaf", items[0]);
    }

    public class ChallengerMultiBranchDouble : DispatchProxy
    {
        public GH_Structure<IGH_Goo> VolatileData { get; set; } = new();
        public Guid InstanceGuid { get; set; } = Guid.NewGuid();
        public string NickName { get; set; } = "ChallengerTreeParam";

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null) return null;
            var name = targetMethod.Name;
            if (name == "get_VolatileData") return VolatileData;
            if (name == "get_VolatileDataCount") return VolatileData.DataCount;
            if (name == "get_InstanceGuid") return InstanceGuid;
            if (name == "get_NickName") return NickName;
            return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
        }
    }
}
