namespace HPGrasshopper.McpBridge.Tests.Regression;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Undo;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class F4_F10_F13_CanvasToolRegressionTests
{
    [Fact]
    public void F4_BatchAtomicity_SolveException_TriggersUndoScopeRollback()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var method = typeof(CanvasEditor).GetMethod("HandleBatchException", BindingFlags.NonPublic | BindingFlags.Static)!;
        var res = method.Invoke(null, new object[] { new InvalidOperationException("Solve failure"), scope })!;
        var json = JsonSerializer.Serialize(res);
        using var parsed = JsonDocument.Parse(json);

        Assert.False(parsed.RootElement.GetProperty("success").GetBoolean());
        Assert.True(parsed.RootElement.GetProperty("rolledBack").GetBoolean());
        Assert.Contains("Solve failure", parsed.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public void F4_NativeUndoRecord_PopulatesUndoActionsForOperations()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var p1 = TestProxyFactory.CreateParam("P1");
        var p2 = TestProxyFactory.CreateParam("P2");
        scope.TrackConnectedWire(p1, p2);
        scope.TrackDisconnectedWire(p1, p2);

        var type = typeof(CanvasUndoScope);
        var recordField = type.GetField("_undoRecord", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var record = (GH_UndoRecord)recordField.GetValue(scope)!;
        Assert.NotNull(record);
        Assert.True(record.ActionCount >= 2);
        scope.Commit();
    }

    [Fact]
    public void F10_BatchPreflight_CollectsDetailedErrorsPerOperation()
    {
        using var doc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "InvalidType_XYZ", "tempId": "nodeA" }
            ],
            "wiresToConnect": [
                { "source": "nodeA", "sourceOutput": -1, "target": "nodeB", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(doc.RootElement, findObject: _ => null, canCreateObject: _ => false);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "componentsToAdd" && e.Index == 0);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains("non-negative"));
    }

    [Fact]
    public void F10_BatchPreflight_UnknownComponentTarget_FailsValidation()
    {
        var ghostGuid = Guid.NewGuid();
        using var doc = JsonDocument.Parse($$"""
        {
            "wiresToConnect": [
                { "source": "{{ghostGuid}}", "sourceOutput": 0, "target": "{{ghostGuid}}", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(doc.RootElement, findObject: _ => null);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains("not found"));
    }

    [Fact]
    public void F10_CreatedComponentInfo_MapsTempIdToInstanceGuid()
    {
        var guid = Guid.NewGuid();
        var info = new CreatedComponentInfo(0, "temp_1", guid.ToString("D"), "Addition", "+", "Component_Add");

        Assert.Equal(0, info.Index);
        Assert.Equal("temp_1", info.TempId);
        Assert.Equal(guid.ToString("D"), info.InstanceGuid);
        Assert.Equal("Addition", info.Name);
        Assert.Equal("+", info.NickName);
        Assert.Equal("Component_Add", info.Type);
    }

    [Fact]
    public void F10_DisconnectWires_MultiPortComponent_DisconnectsExactPortPair()
    {
        var compSrc = TestProxyFactory.CreateComponent("SrcComp", "S");
        var compTgt = TestProxyFactory.CreateComponent("TgtComp", "T");

        var srcOut0 = TestProxyFactory.CreateParam("Out0");
        var srcOut1 = TestProxyFactory.CreateParam("Out1");
        var tgtIn0 = TestProxyFactory.CreateParam("In0");
        var tgtIn1 = TestProxyFactory.CreateParam("In1");

        TestProxyFactory.GetProxy<TestComponentProxy>(compSrc).Outputs.Add(srcOut0);
        TestProxyFactory.GetProxy<TestComponentProxy>(compSrc).Outputs.Add(srcOut1);
        TestProxyFactory.GetProxy<TestComponentProxy>(compTgt).Inputs.Add(tgtIn0);
        TestProxyFactory.GetProxy<TestComponentProxy>(compTgt).Inputs.Add(tgtIn1);

        tgtIn0.AddSource(srcOut0);
        tgtIn1.AddSource(srcOut1);

        var tempMap = new Dictionary<string, IGH_DocumentObject>(StringComparer.OrdinalIgnoreCase)
        {
            ["Src"] = compSrc,
            ["Tgt"] = compTgt
        };

        using var disconnectDoc = JsonDocument.Parse("""
        {
            "wiresToDisconnect": [
                { "source": "Src", "sourceOutput": 1, "target": "Tgt", "targetInput": 1 }
            ]
        }
        """);

        var disconnectMethod = typeof(CanvasEditor).GetMethod("DisconnectWires", BindingFlags.NonPublic | BindingFlags.Static)!;
        using var scope = TestScopeFactory.CreateHeadlessScope();

        var disconnected = (int)disconnectMethod.Invoke(null, new object?[] { null, disconnectDoc.RootElement, tempMap, scope })!;
        Assert.Equal(1, disconnected);
        Assert.True(tgtIn0.Sources.Contains(srcOut0), "Port 0->0 must remain intact");
        Assert.False(tgtIn1.Sources.Contains(srcOut1), "Port 1->1 must be disconnected");
    }

    [Fact]
    public void F13_ImageExporter_CalculateDimensions_ClampsZoomToStrictBounds()
    {
        var (wLow, hLow) = ImageExporter.CalculateDimensions(1000, 1000, 0.01);
        Assert.Equal(200, wLow);
        Assert.Equal(200, hLow);

        var (wHigh, hHigh) = ImageExporter.CalculateDimensions(1000, 1000, 10.0);
        Assert.Equal(5000, wHigh);
        Assert.Equal(5000, hHigh);
    }

    [Fact]
    public void F13_ImageExporter_Export_NullCanvas_ReturnsCleanErrorWithoutThrowing()
    {
        var exporter = new ImageExporter();
        var res = exporter.Export(null);
        var json = JsonSerializer.Serialize(res);
        using var parsed = JsonDocument.Parse(json);

        Assert.False(parsed.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("canvas is not open", parsed.RootElement.GetProperty("error").GetString());
    }
}
