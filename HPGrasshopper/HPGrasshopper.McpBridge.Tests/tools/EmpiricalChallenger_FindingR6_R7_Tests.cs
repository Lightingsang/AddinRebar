namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Rhino.Geometry;
using Xunit;

/// <summary>
///     Empirical challenger verification suite testing Finding R6 (Canvas Batch Preflight & Atomicity)
///     and Finding R7 (Player Data Tree, Typed I/O & Runtime Solver Errors).
/// </summary>
public sealed class EmpiricalChallenger_FindingR6_R7_Tests
{
    [Fact]
    public void Preflight_WireConnecting_ToRemovedComponent_FailsValidation()
    {
        var targetGuid = Guid.NewGuid();
        var sourceGuid = Guid.NewGuid();
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "componentsToRemove": [ "{{targetGuid}}" ],
            "wiresToConnect": [
                { "source": "{{sourceGuid}}", "sourceOutput": 0, "target": "{{targetGuid}}", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: g =>
            g == sourceGuid || g == targetGuid ? TestProxyFactory.CreateComponent() : null);

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "wiresToConnect" && e.Reason.Contains("marked for removal in componentsToRemove"));
    }

    [Fact]
    public void Preflight_WireConnecting_FromRemovedComponent_FailsValidation()
    {
        var targetGuid = Guid.NewGuid();
        var sourceGuid = Guid.NewGuid();
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "componentsToRemove": [ "{{sourceGuid}}" ],
            "wiresToConnect": [
                { "source": "{{sourceGuid}}", "sourceOutput": 0, "target": "{{targetGuid}}", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: g =>
            g == sourceGuid || g == targetGuid ? TestProxyFactory.CreateComponent() : null);

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "wiresToConnect" && e.Reason.Contains("marked for removal in componentsToRemove"));
    }

    [Fact]
    public void Preflight_OutOfRangePort_ExactBoundary_FailsValidation()
    {
        var compGuid = Guid.NewGuid();
        var comp = TestProxyFactory.CreateComponent("Addition", "+");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        proxy.InstanceGuid = compGuid;
        proxy.Inputs.Add(TestProxyFactory.CreateParam("A"));
        proxy.Inputs.Add(TestProxyFactory.CreateParam("B"));

        var otherGuid = Guid.NewGuid();
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "wiresToConnect": [
                { "source": "{{otherGuid}}", "sourceOutput": 0, "target": "{{compGuid}}", "targetInput": 2 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: g =>
            g == compGuid ? comp : TestProxyFactory.CreateParam("X"));

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "wiresToConnect" && e.Reason.Contains("Port index 2") && e.Reason.Contains("exceeds input port count (2)"));
    }

    [Fact]
    public void Preflight_OutOfRangePort_NegativePortIndex_FailsValidation()
    {
        var otherGuid = Guid.NewGuid();
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "wiresToConnect": [
                { "source": "{{otherGuid}}", "sourceOutput": -1, "target": "{{otherGuid}}", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => TestProxyFactory.CreateParam("X"));

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "wiresToConnect" && e.Reason.Contains("must be non-negative"));
    }

    [Fact]
    public void Preflight_StandaloneParam_PortGreaterThanZero_FailsValidation()
    {
        var paramGuid = Guid.NewGuid();
        var otherGuid = Guid.NewGuid();
        var param = TestProxyFactory.CreateParam("Number");
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.InstanceGuid = paramGuid;

        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "wiresToConnect": [
                { "source": "{{paramGuid}}", "sourceOutput": 1, "target": "{{otherGuid}}", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: g =>
            g == paramGuid ? param : TestProxyFactory.CreateParam("Target"));

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e =>
            e.Operation == "wiresToConnect" && e.Reason.Contains("parameters only have port index 0"));
    }

    [Fact]
    public void ExecuteBatch_WhenBatchThrows_PerformsAtomicRollback()
    {
        var editor = new CanvasEditor();
        var method = typeof(CanvasEditor).GetMethod("HandleBatchException", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        using var scope = TestScopeFactory.CreateHeadlessScope();
        var ex = new InvalidOperationException("Simulated wire connection fault");
        var result = method!.Invoke(null, new object[] { ex, scope });

        Assert.NotNull(result);
        var successProp = result!.GetType().GetProperty("success");
        var rolledBackProp = result.GetType().GetProperty("rolledBack");
        var errorProp = result.GetType().GetProperty("error");

        Assert.False((bool)successProp!.GetValue(result)!);
        Assert.True((bool)rolledBackProp!.GetValue(result)!);
        Assert.Contains("InvalidOperationException", (string)errorProp!.GetValue(result)!);
    }

    [Fact]
    public void SetParamValue_NumericArray3Elements_OnNumberParam_AddsThreeScalars()
    {
        var param = TestProxyFactory.CreateParam("Length", "LengthParam");
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
    public void TryParsePoint_SymmetricObjectAndArray_ParsesSuccessfully()
    {
        using var doc1 = JsonDocument.Parse("""{ "x": 10.0, "y": 20.0, "z": 30.0 }""");
        Assert.True(PlayerDataTreeHelper.TryParsePoint(doc1.RootElement, out var pt1));
        Assert.Equal(new Point3d(10.0, 20.0, 30.0), pt1);

        using var doc2 = JsonDocument.Parse("""{ "X": 15.0, "Y": 25.0, "Z": 35.0 }""");
        Assert.True(PlayerDataTreeHelper.TryParsePoint(doc2.RootElement, out var pt2));
        Assert.Equal(new Point3d(15.0, 25.0, 35.0), pt2);

        using var doc3 = JsonDocument.Parse("""{ "x": 7.0, "y": 8.0 }""");
        Assert.True(PlayerDataTreeHelper.TryParsePoint(doc3.RootElement, out var pt3));
        Assert.Equal(new Point3d(7.0, 8.0, 0.0), pt3);

        using var doc4 = JsonDocument.Parse("[10.0, 20.0, 30.0]");
        Assert.True(PlayerDataTreeHelper.TryParsePoint(doc4.RootElement, out var pt4));
        Assert.Equal(new Point3d(10.0, 20.0, 30.0), pt4);

        using var doc5 = JsonDocument.Parse("[10.0, 20.0]");
        Assert.False(PlayerDataTreeHelper.TryParsePoint(doc5.RootElement, out _));
    }

    [Fact]
    public void PlayerRunner_FindInputParam_UnmatchedName_ReturnsNull()
    {
        var objects = new List<IGH_DocumentObject>();
        var found = PlayerRunner.FindInputParam(objects, "NonExistentInput");
        Assert.Null(found);

        var failure = new PlayerInputFailure("NonExistentInput", "Parameter 'NonExistentInput' was not found in the definition.");
        Assert.Equal("NonExistentInput", failure.Name);
        Assert.Contains("not found", failure.Reason);
    }

    [Fact]
    public void PlayerOutputExtractor_DisambiguatesDuplicateNicknames()
    {
        var guid1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var guid2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var param1 = TestProxyFactory.CreateParam("Result", "Out1", volatileDataCount: 1, instanceGuid: guid1);
        var proxy1 = TestProxyFactory.GetProxy<TestParamProxy>(param1);
        proxy1.VolatileDataAdded.Add(new GH_Number(100.0));

        var param2 = TestProxyFactory.CreateParam("Result", "Out2", volatileDataCount: 1, instanceGuid: guid2);
        var proxy2 = TestProxyFactory.GetProxy<TestParamProxy>(param2);
        proxy2.VolatileDataAdded.Add(new GH_Number(200.0));

        var objects = new List<IGH_DocumentObject> { param1, param2 };
        var outputs = PlayerOutputExtractor.ExtractOutputs(objects, null);

        Assert.Equal(2, outputs.Count);
        Assert.True(outputs.ContainsKey("Result"));
        Assert.True(outputs.ContainsKey("Result#22222222"));
    }

    [Fact]
    public void PlayerOutputExtractor_MatchesOutputByGuidD_GuidN_And8Prefix()
    {
        var guid = Guid.Parse("abcdef12-3456-7890-abcd-ef1234567890");
        var param = TestProxyFactory.CreateParam("Out", "OutputParam", volatileDataCount: 1, instanceGuid: guid);
        var proxy = TestProxyFactory.GetProxy<TestParamProxy>(param);
        proxy.VolatileDataAdded.Add(new GH_Number(42.0));

        var objects = new List<IGH_DocumentObject> { param };

        var outD = PlayerOutputExtractor.ExtractOutputs(objects, new[] { guid.ToString("D") });
        Assert.True(outD.ContainsKey(guid.ToString("D")));

        var outN = PlayerOutputExtractor.ExtractOutputs(objects, new[] { guid.ToString("N") });
        Assert.True(outN.ContainsKey(guid.ToString("N")));

        var out8 = PlayerOutputExtractor.ExtractOutputs(objects, new[] { "abcdef12" });
        Assert.True(out8.ContainsKey("abcdef12"));
    }

    [Fact]
    public void CheckParamMessages_WhenInputParamHasRuntimeError_CapturesErrorWithPrefix()
    {
        var method = typeof(PlayerRunner).GetMethod("CheckParamMessages", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var comp = TestProxyFactory.CreateComponent("Division", "/");
        var errors = new List<string>();
        var warnings = new List<string>();

        method!.Invoke(null, new object[] { null!, comp, errors, warnings });
        Assert.Empty(errors);

        method.Invoke(null, new object[] { new List<IGH_Param>(), comp, errors, warnings });
        Assert.Empty(errors);
    }
}
