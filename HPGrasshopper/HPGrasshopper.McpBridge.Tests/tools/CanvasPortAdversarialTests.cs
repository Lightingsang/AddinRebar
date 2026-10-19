namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class CanvasPortAdversarialTests
{
    private readonly CanvasEditor _editor = new();
    private static readonly MethodInfo ExtractPortIndexMethod =
        typeof(CanvasEditor).GetMethod("ExtractPortIndex", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Theory]
    [InlineData("\"foo\"", "sourceOutput port index must be a valid integer")]
    [InlineData("\"0\"", "sourceOutput port index must be a valid integer")]
    [InlineData("\"\"", "sourceOutput port index must be a valid integer")]
    [InlineData("null", "sourceOutput port index must be a valid integer")]
    [InlineData("1.5", "sourceOutput port index must be a valid integer")]
    [InlineData("-0.01", "sourceOutput port index must be a valid integer")]
    [InlineData("-1", "sourceOutput port index must be non-negative")]
    [InlineData("-99999", "sourceOutput port index must be non-negative")]
    [InlineData("9999999999999999999", "sourceOutput port index must be a valid integer")]
    [InlineData("-9999999999999999999", "sourceOutput port index must be a valid integer")]
    [InlineData("true", "sourceOutput port index must be a valid integer")]
    [InlineData("false", "sourceOutput port index must be a valid integer")]
    [InlineData("[0, 1]", "sourceOutput port index must be a valid integer")]
    [InlineData("{\"p\": 0}", "sourceOutput port index must be a valid integer")]
    public void Preflight_WiresToConnect_AdversarialSourceOutput_ReturnsCleanErrors(string portJson, string expectedReason)
    {
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "nodeA" },
                { "type": "NumberSlider", "tempId": "nodeB" }
            ],
            "wiresToConnect": [
                { "source": "nodeA", "sourceOutput": {{portJson}}, "target": "nodeB", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => null);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains(expectedReason));
    }

    [Theory]
    [InlineData("\"bar\"", "targetInput port index must be a valid integer")]
    [InlineData("\"1\"", "targetInput port index must be a valid integer")]
    [InlineData("null", "targetInput port index must be a valid integer")]
    [InlineData("3.1415", "targetInput port index must be a valid integer")]
    [InlineData("-5", "targetInput port index must be non-negative")]
    [InlineData("9999999999999999999", "targetInput port index must be a valid integer")]
    [InlineData("true", "targetInput port index must be a valid integer")]
    [InlineData("[0]", "targetInput port index must be a valid integer")]
    [InlineData("{\"nested\": 1}", "targetInput port index must be a valid integer")]
    public void Preflight_WiresToConnect_AdversarialTargetInput_ReturnsCleanErrors(string portJson, string expectedReason)
    {
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "nodeA" },
                { "type": "NumberSlider", "tempId": "nodeB" }
            ],
            "wiresToConnect": [
                { "source": "nodeA", "sourceOutput": 0, "target": "nodeB", "targetInput": {{portJson}} }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => null);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains(expectedReason));
    }

    [Theory]
    [InlineData("sourceOutput", "\"str\"", "sourceOutput port index must be a valid integer")]
    [InlineData("sourceOutput", "null", "sourceOutput port index must be a valid integer")]
    [InlineData("sourceOutput", "2.5", "sourceOutput port index must be a valid integer")]
    [InlineData("sourceOutput", "-1", "sourceOutput port index must be non-negative")]
    [InlineData("targetInput", "\"str\"", "targetInput port index must be a valid integer")]
    [InlineData("targetInput", "null", "targetInput port index must be a valid integer")]
    [InlineData("targetInput", "2.5", "targetInput port index must be a valid integer")]
    [InlineData("targetInput", "-10", "targetInput port index must be non-negative")]
    public void Preflight_WiresToDisconnect_AdversarialPortTypes_ReturnsCleanErrors(string propName, string portJson, string expectedReason)
    {
        var dummyGuidA = Guid.NewGuid();
        var dummyGuidB = Guid.NewGuid();
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "wiresToDisconnect": [
                { "source": "{{dummyGuidA}}", "target": "{{dummyGuidB}}", "{{propName}}": {{portJson}} }
            ]
        }
        """);

        var dummyComp = TestProxyFactory.CreateComponent("Dummy", "D");
        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: g => (g == dummyGuidA || g == dummyGuidB) ? dummyComp : null);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToDisconnect" && e.Reason.Contains(expectedReason));
    }

    [Fact]
    public void Preflight_CombinedAdversarialBatch_CollectsAllErrorsWithoutThrowing()
    {
        var dummyGuidA = Guid.NewGuid();
        var dummyGuidB = Guid.NewGuid();
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "nodeA" },
                { "type": "NumberSlider", "tempId": "nodeB" }
            ],
            "wiresToConnect": [
                { "source": "nodeA", "sourceOutput": "invalid_str", "target": "nodeB", "targetInput": null },
                { "source": "nodeA", "sourceOutput": -5, "target": "nodeB", "targetInput": 1.75 }
            ],
            "wiresToDisconnect": [
                { "source": "{{dummyGuidA}}", "target": "{{dummyGuidB}}", "sourceOutput": true, "targetInput": -99 }
            ]
        }
        """);

        var dummyComp = TestProxyFactory.CreateComponent("Dummy", "D");
        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: g => (g == dummyGuidA || g == dummyGuidB) ? dummyComp : null);
        Assert.False(preflight.IsValid);
        Assert.Equal(6, preflight.Errors.Count);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Index == 0 && e.Reason.Contains("sourceOutput"));
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Index == 0 && e.Reason.Contains("targetInput"));
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Index == 1 && e.Reason.Contains("non-negative"));
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Index == 1 && e.Reason.Contains("valid integer"));
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToDisconnect" && e.Index == 0 && e.Reason.Contains("sourceOutput"));
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToDisconnect" && e.Index == 0 && e.Reason.Contains("targetInput"));
    }

    [Fact]
    public void Preflight_ValidPortIndices_BothConnectAndDisconnect_Succeeds()
    {
        var dummyGuidA = Guid.NewGuid();
        var dummyGuidB = Guid.NewGuid();
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "nodeA" },
                { "type": "NumberSlider", "tempId": "nodeB" }
            ],
            "wiresToConnect": [
                { "source": "nodeA", "sourceOutput": 0, "target": "nodeB", "targetInput": 0 },
                { "source": "nodeA", "target": "nodeB" },
                { "source": "nodeA", "sourceOutput": 2147483647, "target": "nodeB", "targetInput": 1 }
            ],
            "wiresToDisconnect": [
                { "source": "{{dummyGuidA}}", "target": "{{dummyGuidB}}", "sourceOutput": 0, "targetInput": 0 },
                { "source": "{{dummyGuidA}}", "target": "{{dummyGuidB}}" }
            ]
        }
        """);

        var dummyComp = TestProxyFactory.CreateComponent("Dummy", "D");
        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement,
            findObject: g => (g == dummyGuidA || g == dummyGuidB) ? dummyComp : null,
            canCreateObject: _ => true);
        Assert.True(preflight.IsValid);
        Assert.Empty(preflight.Errors);
    }

    [Theory]
    [InlineData("{\"port\": \"str\"}", "port", 0)]
    [InlineData("{\"port\": null}", "port", 0)]
    [InlineData("{\"port\": 1.5}", "port", 0)]
    [InlineData("{\"port\": true}", "port", 0)]
    [InlineData("{\"port\": [1, 2]}", "port", 0)]
    [InlineData("{\"port\": {\"a\": 1}}", "port", 0)]
    [InlineData("{\"port\": 9999999999999999999}", "port", 0)]
    [InlineData("{}", "port", 0)]
    [InlineData("{\"port\": 42}", "port", 42)]
    public void CanvasEditor_ExtractPortIndex_AdversarialPayloads_FallsBackToZeroSafely(string json, string prop, int expected)
    {
        using var doc = JsonDocument.Parse(json);
        var actual = (int)ExtractPortIndexMethod.Invoke(null, new object[] { doc.RootElement, prop })!;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExecuteBatch_AdversarialPortPayload_PreflightFailsAndAtomicityPreserved()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "nodeA", "x": 100.0, "y": 100.0 }
            ],
            "wiresToConnect": [
                { "source": "nodeA", "sourceOutput": "corrupted_port", "target": "nodeA", "targetInput": null }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => null);
        Assert.False(preflight.IsValid);

        var preflightFailureMethod = typeof(CanvasEditor).GetMethod("CreatePreflightFailure", BindingFlags.NonPublic | BindingFlags.Static)!;
        var rawResult = preflightFailureMethod.Invoke(null, new object[] { preflight })!;
        var json = JsonSerializer.Serialize(rawResult);
        using var parsed = JsonDocument.Parse(json);

        Assert.False(parsed.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("Preflight validation failed", parsed.RootElement.GetProperty("error").GetString());
        Assert.False(parsed.RootElement.GetProperty("rolledBack").GetBoolean());
        Assert.Equal(0, parsed.RootElement.GetProperty("added").GetInt32());
        Assert.Equal(0, parsed.RootElement.GetProperty("connected").GetInt32());
        Assert.True(parsed.RootElement.GetProperty("errors").GetArrayLength() >= 2);

        var nullDocResult = _editor.ExecuteBatch(null, jsonDoc.RootElement, dryRun: false);
        var nullDocJson = JsonSerializer.Serialize(nullDocResult);
        using var nullDocParsed = JsonDocument.Parse(nullDocJson);
        Assert.False(nullDocParsed.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("No active Grasshopper document is open.", nullDocParsed.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public void ConnectWires_And_DisconnectWires_ValidPorts_FunctionCorrectly()
    {
        var compSrc = TestProxyFactory.CreateComponent("SrcComp", "S");
        var compTgt = TestProxyFactory.CreateComponent("TgtComp", "T");

        var srcOut = TestProxyFactory.CreateParam("Out0");
        var tgtIn = TestProxyFactory.CreateParam("In0");
        TestProxyFactory.GetProxy<TestComponentProxy>(compSrc).Outputs.Add(srcOut);
        TestProxyFactory.GetProxy<TestComponentProxy>(compTgt).Inputs.Add(tgtIn);

        var tempMap = new Dictionary<string, IGH_DocumentObject>(StringComparer.OrdinalIgnoreCase)
        {
            ["Src"] = compSrc,
            ["Tgt"] = compTgt
        };

        using var connectDoc = JsonDocument.Parse("""
        {
            "wiresToConnect": [
                { "source": "Src", "sourceOutput": 0, "target": "Tgt", "targetInput": 0 }
            ]
        }
        """);

        var connectMethod = typeof(CanvasEditor).GetMethod("ConnectWires", BindingFlags.NonPublic | BindingFlags.Static)!;
        var disconnectMethod = typeof(CanvasEditor).GetMethod("DisconnectWires", BindingFlags.NonPublic | BindingFlags.Static)!;
        var scope = TestScopeFactory.CreateHeadlessScope();

        var connected = (int)connectMethod.Invoke(null, new object?[] { null, connectDoc.RootElement, tempMap, scope })!;
        Assert.Equal(1, connected);
        Assert.True(tgtIn.Sources.Contains(srcOut));

        using var disconnectDoc = JsonDocument.Parse("""
        {
            "wiresToDisconnect": [
                { "source": "Src", "sourceOutput": 0, "target": "Tgt", "targetInput": 0 }
            ]
        }
        """);

        var disconnected = (int)disconnectMethod.Invoke(null, new object?[] { null, disconnectDoc.RootElement, tempMap, scope })!;
        Assert.Equal(1, disconnected);
        Assert.False(tgtIn.Sources.Contains(srcOut));
    }
}
