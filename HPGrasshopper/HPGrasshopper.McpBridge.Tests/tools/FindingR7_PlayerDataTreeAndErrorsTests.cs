namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Rhino.Geometry;
using Xunit;

public sealed class FindingR7_PlayerDataTreeAndErrorsTests
{
    [Fact]
    public void SetParamValue_NumericArray3Elements_OnNumberParam_AddsThreeScalars()
    {
        var param = TestProxyFactory.CreateParam("Num", "NumberParam");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.TypeName = "Number";

        using var jsonDoc = JsonDocument.Parse("[10.5, 20.5, 30.5]");
        var success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement);

        Assert.True(success);
        Assert.Equal(3, proxy.VolatileDataAdded.Count);
        Assert.All(proxy.VolatileDataAdded, item => Assert.IsType<GH_Number>(item));
    }

    [Fact]
    public void SetParamValue_NumericArray3Elements_OnPointParam_AddsPoint3d()
    {
        var param = TestProxyFactory.CreateParam("Pt", "PointParam");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.TypeName = "Point";

        using var jsonDoc = JsonDocument.Parse("[1.0, 2.0, 3.0]");
        var success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement);

        Assert.True(success);
        Assert.Single(proxy.VolatileDataAdded);
        var pt = Assert.IsType<GH_Point>(proxy.VolatileDataAdded[0]);
        Assert.Equal(1.0, pt.Value.X);
        Assert.Equal(2.0, pt.Value.Y);
        Assert.Equal(3.0, pt.Value.Z);
    }

    [Fact]
    public void SetParamValue_PointObject_AddsPoint3d()
    {
        var param = TestProxyFactory.CreateParam("Pt", "PointParam");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);

        using var jsonDoc = JsonDocument.Parse("""{ "x": 10.0, "y": 20.0, "z": 30.0 }""");
        var success = PlayerDataTreeHelper.SetParamValue(param, jsonDoc.RootElement);

        Assert.True(success);
        Assert.Single(proxy.VolatileDataAdded);
        var pt = Assert.IsType<GH_Point>(proxy.VolatileDataAdded[0]);
        Assert.Equal(10.0, pt.Value.X);
        Assert.Equal(20.0, pt.Value.Y);
        Assert.Equal(30.0, pt.Value.Z);
    }

    [Fact]
    public void TryParsePoint_ObjectWithoutZ_DefaultsZToZero()
    {
        using var jsonDoc = JsonDocument.Parse("""{ "x": 5.5, "y": 6.5 }""");
        var parsed = PlayerDataTreeHelper.TryParsePoint(jsonDoc.RootElement, out var pt);

        Assert.True(parsed);
        Assert.Equal(5.5, pt.X);
        Assert.Equal(6.5, pt.Y);
        Assert.Equal(0.0, pt.Z);
    }

    [Fact]
    public void TryParsePath_StrictValidation_RejectsMalformedPaths()
    {
        Assert.False(PlayerDataTreeHelper.TryParsePath("{0;invalid;2}", out _));
        Assert.False(PlayerDataTreeHelper.TryParsePath("{0;-1}", out _));
        Assert.False(PlayerDataTreeHelper.TryParsePath("0;1", out _)); // missing braces
        Assert.True(PlayerDataTreeHelper.TryParsePath("{0;1;2}", out var validPath));
        Assert.Equal(3, validPath.Length);
    }

    [Fact]
    public void PlayerOutputExtractor_MatchesOutputByGuidAndName()
    {
        var paramGuid = Guid.NewGuid();
        var param = TestProxyFactory.CreateParam("Result", "TotalResult", volatileDataCount: 1, instanceGuid: paramGuid);
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.VolatileDataAdded.Add(new GH_Number(42.0));

        var objects = new List<IGH_DocumentObject> { param };

        // Test matching by Guid string
        var outputsByGuid = PlayerOutputExtractor.ExtractOutputs(objects, new[] { paramGuid.ToString("D") });
        Assert.True(outputsByGuid.ContainsKey(paramGuid.ToString("D")));

        // Test matching by Name
        var outputsByName = PlayerOutputExtractor.ExtractOutputs(objects, new[] { "TotalResult" });
        Assert.True(outputsByName.ContainsKey("TotalResult") || outputsByName.ContainsKey("Result"));
    }
}
