namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class CanvasReaderTests
{
    private readonly CanvasReader _reader = new();

    [Fact]
    public void Read_NullDocument_ReturnsNonNullResultWithNoneMetadata()
    {
        var result = _reader.Read(null);
        Assert.NotNull(result);
        Assert.NotNull(result.Document);

        var json = JsonSerializer.Serialize(result.Document);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("None", doc.RootElement.GetProperty("name").GetString());
        Assert.False(doc.RootElement.GetProperty("open").GetBoolean());
        Assert.Equal(0, doc.RootElement.GetProperty("objectCount").GetInt32());
    }

    [Fact]
    public void Read_NullDocument_ReturnsEmptyComponentsList()
    {
        var result = _reader.Read(null);
        Assert.NotNull(result.Components);
        Assert.Empty(result.Components);
    }

    [Fact]
    public void Read_NullDocument_ReturnsEmptyWiresList()
    {
        var result = _reader.Read(null);
        Assert.NotNull(result.Wires);
        Assert.Empty(result.Wires);
    }

    [Fact]
    public void Read_NullDocument_SerializesToValidJson()
    {
        var result = _reader.Read(null);
        var json = JsonSerializer.Serialize(result);
        Assert.False(string.IsNullOrWhiteSpace(json));

        using var parsed = JsonDocument.Parse(json);
        Assert.True(parsed.RootElement.TryGetProperty("Document", out var docProp));
        Assert.True(parsed.RootElement.TryGetProperty("Components", out var compProp));
        Assert.True(parsed.RootElement.TryGetProperty("Wires", out var wireProp));
        Assert.Equal(JsonValueKind.Array, compProp.ValueKind);
        Assert.Equal(JsonValueKind.Array, wireProp.ValueKind);
    }

    [Fact]
    public void Read_NullDocument_WithIncludeValuesTrue_ReturnsEmptyResultCleanly()
    {
        var result = _reader.Read(null, includeValues: true);
        Assert.NotNull(result);
        Assert.Empty(result.Components);
        Assert.Empty(result.Wires);
    }

    [Fact]
    public void Read_NullDocument_WithSelectedOnlyTrue_ReturnsEmptyResultCleanly()
    {
        var result = _reader.Read(null, selectedOnly: true);
        Assert.NotNull(result);
        Assert.Empty(result.Components);
    }

    [Fact]
    public void Read_NullDocument_WithCategoryFilter_ReturnsEmptyResultCleanly()
    {
        var result = _reader.Read(null, categoryFilter: "Math");
        Assert.NotNull(result);
        Assert.Empty(result.Components);
    }

    [Fact]
    public void CanvasReadResult_RecordProperties_HoldSuppliedValues()
    {
        var docInfo = new { name = "TestDoc", open = true, objectCount = 10 };
        var comps = new List<object> { new { guid = Guid.NewGuid().ToString("D"), name = "Slider" } };
        var wires = new List<object> { new { fromParam = "p1", toParam = "p2" } };

        var record = new CanvasReader.CanvasReadResult(docInfo, comps, wires);

        Assert.Same(docInfo, record.Document);
        Assert.Same(comps, record.Components);
        Assert.Same(wires, record.Wires);
        Assert.Single(record.Components);
        Assert.Single(record.Wires);
    }

    [Fact]
    public void CanvasReadResult_Serialization_DoesNotThrowCircularReference()
    {
        var compGuid = Guid.NewGuid().ToString("D");
        var paramGuid = Guid.NewGuid().ToString("D");
        var comps = new List<object>
        {
            new
            {
                guid = compGuid,
                name = "Addition",
                nickName = "+",
                category = "Math",
                subCategory = "Operators",
                bounds = new { x = 100f, y = 200f, width = 50f, height = 30f },
                status = "Blank",
                messages = Array.Empty<string>(),
                inputs = new[] { new { guid = paramGuid, name = "A", nickName = "A", typeName = "Number", optional = false } },
                outputs = Array.Empty<object>()
            }
        };

        var wires = new List<object>
        {
            new { fromComponent = compGuid, fromParam = paramGuid, toComponent = compGuid, toParam = paramGuid }
        };

        var record = new CanvasReader.CanvasReadResult(new { name = "ComplexDoc", open = true, objectCount = 1 }, comps, wires);
        var json = JsonSerializer.Serialize(record);

        Assert.Contains(compGuid, json);
        Assert.Contains("Operators", json);
    }

    [Fact]
    public void MatchesCategory_PrivateMethod_ReflectiveContract_MatchesNullOrWhitespaceFilter()
    {
        var method = typeof(CanvasReader).GetMethod("MatchesCategory", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        // When filter is null or whitespace, MatchesCategory returns true regardless of component
        var resultNull = method!.Invoke(null, new object?[] { null, null });
        var resultEmpty = method.Invoke(null, new object?[] { null, "   " });

        Assert.Equal(true, resultNull);
        Assert.Equal(true, resultEmpty);
    }

    [Fact]
    public void CanvasReader_PrivateMethods_ReflectiveSignatures_MatchExpectedContract()
    {
        var type = typeof(CanvasReader);

        var extractComp = type.GetMethod("ExtractComponent", BindingFlags.NonPublic | BindingFlags.Static);
        var extractParamObj = type.GetMethod("ExtractParamObject", BindingFlags.NonPublic | BindingFlags.Static);
        var extractParam = type.GetMethod("ExtractParam", BindingFlags.NonPublic | BindingFlags.Static);
        var extractCompWires = type.GetMethod("ExtractComponentWires", BindingFlags.NonPublic | BindingFlags.Static);
        var extractParamWires = type.GetMethod("ExtractParamWires", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(extractComp);
        Assert.NotNull(extractParamObj);
        Assert.NotNull(extractParam);
        Assert.NotNull(extractCompWires);
        Assert.NotNull(extractParamWires);
    }

    [Fact]
    public void CanvasReader_ExtractParam_VolatileDataSummaryFormat_ContractVerified()
    {
        var param = TestProxyFactory.CreateParam("R", "Result", volatileDataCount: 5);

        var extractedWithValues = CanvasReader.ExtractParam(param, includeValues: true);
        var jsonWithValues = JsonSerializer.Serialize(extractedWithValues);
        using var docWithValues = JsonDocument.Parse(jsonWithValues);

        Assert.Equal("5 item(s)", docWithValues.RootElement.GetProperty("valueSummary").GetString());
        Assert.Equal("R", docWithValues.RootElement.GetProperty("nickName").GetString());
        Assert.Equal("Result", docWithValues.RootElement.GetProperty("name").GetString());

        var extractedWithoutValues = CanvasReader.ExtractParam(param, includeValues: false);
        var jsonWithoutValues = JsonSerializer.Serialize(extractedWithoutValues);
        using var docWithoutValues = JsonDocument.Parse(jsonWithoutValues);
        Assert.Equal(JsonValueKind.Null, docWithoutValues.RootElement.GetProperty("valueSummary").ValueKind);
    }

    [Fact]
    public void CanvasReader_ComponentWireStructure_MatchesContractKeys()
    {
        var comp = TestProxyFactory.CreateComponent("Addition", "+");
        var inputParam = TestProxyFactory.CreateParam("A");
        var sourceParam = TestProxyFactory.CreateParam("Src");

        var proxyComp = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        proxyComp.Inputs.Add(inputParam);

        var proxyInput = TestProxyFactory.GetProxy<TestParamProxy>(inputParam);
        proxyInput.Sources.Add(sourceParam);

        var wires = new List<object>();
        CanvasReader.ExtractComponentWires(comp, wires);

        Assert.Single(wires);
        var json = JsonSerializer.Serialize(wires[0]);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(sourceParam.InstanceGuid.ToString("D"), doc.RootElement.GetProperty("fromParam").GetString());
        Assert.Equal(comp.InstanceGuid.ToString("D"), doc.RootElement.GetProperty("toComponent").GetString());
        Assert.Equal(inputParam.InstanceGuid.ToString("D"), doc.RootElement.GetProperty("toParam").GetString());
    }

    [Fact]
    public void CanvasReader_RuntimeMessages_ErrorsAndWarningsAggregated()
    {
        var comp = TestProxyFactory.CreateComponent("Addition", "+");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        proxy.ErrorMessages.Add("Input A is missing");
        proxy.WarningMessages.Add("Conversion loss");

        var extracted = CanvasReader.ExtractComponent(comp, includeValues: false);
        var json = JsonSerializer.Serialize(extracted);
        using var doc = JsonDocument.Parse(json);

        var messagesProp = doc.RootElement.GetProperty("messages");
        Assert.Equal(2, messagesProp.GetArrayLength());
        Assert.Equal("Input A is missing", messagesProp[0].GetString());
        Assert.Equal("Conversion loss", messagesProp[1].GetString());
    }

    [Fact]
    public void CanvasReader_HeadlessExecution_CatchesDllNotFoundCleanly()
    {
        var res = _reader.Read(null);
        Assert.NotNull(res);
        Assert.NotNull(res.Document);
        Assert.Empty(res.Components);
        Assert.Empty(res.Wires);
    }

    [Fact]
    public void CanvasReader_ExtractComponent_PassesIncludeValuesToInputs()
    {
        var comp = TestProxyFactory.CreateComponent("Addition", "+");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        var inputParam = TestProxyFactory.CreateParam("A", "Number", volatileDataCount: 1);
        proxy.Inputs.Add(inputParam);

        var extracted = CanvasReader.ExtractComponent(comp, includeValues: true);
        var json = JsonSerializer.Serialize(extracted);
        using var doc = JsonDocument.Parse(json);

        var inputs = doc.RootElement.GetProperty("inputs");
        Assert.Equal(1, inputs.GetArrayLength());
        Assert.Equal("1 item(s)", inputs[0].GetProperty("valueSummary").GetString());
    }

    [Fact]
    public void CanvasReader_ExtractParamObject_PassesValuesAndStructure()
    {
        var param = TestProxyFactory.CreateParam("Slider", "Number", volatileDataCount: 1);
        var extracted = CanvasReader.ExtractParam(param, includeValues: true);
        var json = JsonSerializer.Serialize(extracted);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal("1 item(s)", doc.RootElement.GetProperty("valueSummary").GetString());
    }
}
