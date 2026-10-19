namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Rhino.Geometry;
using Xunit;

public sealed class ChallengerEmpiricalM3Tests
{
    [Fact]
    public void F10_MixedValidAndInvalidBatch_PreflightFailsAndAtomicityPreserved()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "validNode", "x": 100.0, "y": 100.0 },
                { "type": "Subtraction", "x": "NaN", "y": 200.0 }
            ],
            "wiresToConnect": [
                { "source": "validNode", "sourceOutput": -1, "target": "validNode", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement);
        Assert.False(preflight.IsValid);
        Assert.True(preflight.Errors.Count >= 2);
        Assert.Contains(preflight.Errors, e => e.Operation == "componentsToAdd" && e.Reason.Contains("finite number"));
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains("non-negative"));

        var editor = new CanvasEditor();
        var rawResult = editor.ExecuteBatch(null, jsonDoc.RootElement, false);
        var json = JsonSerializer.Serialize(rawResult);
        using var parsed = JsonDocument.Parse(json);

        Assert.False(parsed.RootElement.GetProperty("success").GetBoolean());
        Assert.Equal("No active Grasshopper document is open.", parsed.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public void F10_Preflight_WiresToUnknownComponent_FailsValidation()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "nodeA" }
            ],
            "wiresToConnect": [
                { "source": "nodeA", "target": "ghostComponentTarget" }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => null);
        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains("ghostComponentTarget"));
    }

    [Fact]
    public void F10_CreatedComponentInfo_PreservesTempIdAndGeneratesValidGuid()
    {
        var tempId = "calc_step_42";
        var expectedGuid = Guid.NewGuid();
        var info = new CreatedComponentInfo(
            3,
            tempId,
            expectedGuid.ToString("D"),
            "Addition",
            "+",
            "Addition");

        Assert.Equal(3, info.Index);
        Assert.Equal("calc_step_42", info.TempId);
        Assert.True(Guid.TryParse(info.InstanceGuid, out var parsedGuid));
        Assert.Equal(expectedGuid, parsedGuid);
        Assert.Equal("Addition", info.Name);
        Assert.Equal("+", info.NickName);
    }

    [Fact]
    public void F10_DisconnectWires_MultiPortComponent_DisconnectsExactPort()
    {
        var compSrc = TestProxyFactory.CreateComponent("SourceComp", "S");
        var compTgt = TestProxyFactory.CreateComponent("TargetComp", "T");

        var srcOut0 = TestProxyFactory.CreateParam("Out0");
        var srcOut1 = TestProxyFactory.CreateParam("Out1");
        TestProxyFactory.GetProxy<TestComponentProxy>(compSrc).Outputs.AddRange(new[] { srcOut0, srcOut1 });

        var tgtIn0 = TestProxyFactory.CreateParam("In0");
        var tgtIn1 = TestProxyFactory.CreateParam("In1");
        TestProxyFactory.GetProxy<TestComponentProxy>(compTgt).Inputs.AddRange(new[] { tgtIn0, tgtIn1 });

        tgtIn0.AddSource(srcOut0);
        tgtIn1.AddSource(srcOut1);

        var tempMap = new Dictionary<string, IGH_DocumentObject>(StringComparer.OrdinalIgnoreCase)
        {
            ["Src"] = compSrc,
            ["Tgt"] = compTgt
        };

        using var jsonDoc = JsonDocument.Parse("""
        {
            "wiresToDisconnect": [
                { "source": "Src", "sourceOutput": 1, "target": "Tgt", "targetInput": 1 }
            ]
        }
        """);

        var disconnectMethod = typeof(CanvasEditor).GetMethod("DisconnectWires", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(disconnectMethod);

        var scope = TestScopeFactory.CreateHeadlessScope();
        var disconnected = (int)disconnectMethod!.Invoke(null, new object?[] { null, jsonDoc.RootElement, tempMap, scope })!;

        Assert.Equal(1, disconnected);
        Assert.False(tgtIn1.Sources.Contains(srcOut1));
        Assert.True(tgtIn0.Sources.Contains(srcOut0));
    }

    [Fact]
    public void F11_ExtractParamData_MultiBranchDataTree_PreservesAllBranchesAndPaths()
    {
        var multiBranchProxy = DispatchProxy.Create<IGH_Param, MultiBranchParamDouble>();
        var doubleImpl = (MultiBranchParamDouble)(object)multiBranchProxy;

        var structure = new GH_Structure<IGH_Goo>();
        structure.Append(new GH_Number(10.5), new GH_Path(0, 0));
        structure.Append(new GH_Number(20.5), new GH_Path(0, 0));
        structure.Append(new GH_String("BranchB"), new GH_Path(0, 1));
        structure.Append(new GH_Point(new Point3d(1.0, 2.0, 3.0)), new GH_Path(1, 0));

        doubleImpl.VolatileData = structure;

        var extracted = PlayerDataTreeHelper.ExtractParamData(multiBranchProxy);
        Assert.NotNull(extracted);
        var dict = Assert.IsType<Dictionary<string, object?>>(extracted);

        Assert.Equal(3, dict.Count);
        Assert.True(dict.ContainsKey("{0;0}"));
        Assert.True(dict.ContainsKey("{0;1}"));
        Assert.True(dict.ContainsKey("{1;0}"));

        var branch00 = Assert.IsType<List<object?>>(dict["{0;0}"]);
        Assert.Equal(2, branch00.Count);
        Assert.Equal(10.5, branch00[0]);
        Assert.Equal(20.5, branch00[1]);

        var branch01 = Assert.IsType<List<object?>>(dict["{0;1}"]);
        Assert.Single(branch01);
        Assert.Equal("BranchB", branch01[0]);
    }

    [Fact]
    public void F11_ExtractOutputs_TriplicateNickNames_DisambiguatesAllOutputs()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var guid3 = Guid.NewGuid();

        var p1 = TestProxyFactory.CreateParam("Area", instanceGuid: guid1, volatileDataCount: 1);
        var p2 = TestProxyFactory.CreateParam("Area", instanceGuid: guid2, volatileDataCount: 1);
        var p3 = TestProxyFactory.CreateParam("Area", instanceGuid: guid3, volatileDataCount: 1);

        TestProxyFactory.GetProxy<TestParamProxy>(p1).VolatileDataAdded.Add(new GH_Number(100.0));
        TestProxyFactory.GetProxy<TestParamProxy>(p2).VolatileDataAdded.Add(new GH_Number(200.0));
        TestProxyFactory.GetProxy<TestParamProxy>(p3).VolatileDataAdded.Add(new GH_Number(300.0));

        var objects = new IGH_DocumentObject[] { p1, p2, p3 };
        var outputs = PlayerRunner.ExtractOutputs(objects, null);

        Assert.Equal(3, outputs.Count);
        Assert.True(outputs.ContainsKey("Area"));
        Assert.True(outputs.ContainsKey($"Area#{guid2.ToString("D")[..8]}"));
        Assert.True(outputs.ContainsKey($"Area#{guid3.ToString("D")[..8]}"));
    }

    [Fact]
    public void F11_ComponentRuntimeErrors_ProduceErrorMessages()
    {
        var comp = TestProxyFactory.CreateComponent("FailingSolverComp", "Fail");
        var compProxy = TestProxyFactory.GetProxy<TestComponentProxy>(comp);
        compProxy.ErrorMessages.Add("Computation diverged at iteration 10");
        compProxy.WarningMessages.Add("Input unit assumed meters");

        var errors = comp.RuntimeMessages(GH_RuntimeMessageLevel.Error);
        var warnings = comp.RuntimeMessages(GH_RuntimeMessageLevel.Warning);

        Assert.Single(errors);
        Assert.Equal("Computation diverged at iteration 10", errors[0]);
        Assert.Single(warnings);
        Assert.Equal("Input unit assumed meters", warnings[0]);
    }

    [Fact]
    public void F13_CalculateDimensions_ClampsZoomToStrictBounds()
    {
        var (wMinNeg, hMinNeg) = ImageExporter.CalculateDimensions(1000, 800, -5.0);
        Assert.Equal(200, wMinNeg);
        Assert.Equal(200, hMinNeg);

        var (wMinBoundary, hMinBoundary) = ImageExporter.CalculateDimensions(1000, 800, 0.1);
        Assert.Equal(200, wMinBoundary);
        Assert.Equal(200, hMinBoundary);

        var (wSmall, hSmall) = ImageExporter.CalculateDimensions(50, 50, 1.0);
        Assert.Equal(200, wSmall);
        Assert.Equal(200, hSmall);

        var (wMaxBoundary, hMaxBoundary) = ImageExporter.CalculateDimensions(1000, 800, 5.0);
        Assert.Equal(5000, wMaxBoundary);
        Assert.Equal(4000, hMaxBoundary);

        var (wMaxOverflow, hMaxOverflow) = ImageExporter.CalculateDimensions(1000, 800, 99.0);
        Assert.Equal(5000, wMaxOverflow);
        Assert.Equal(4000, hMaxOverflow);
    }

    [Fact]
    public void F13_Export_NullCanvas_CleanFailure()
    {
        var exporter = new ImageExporter();
        var rawResult = exporter.Export(null, "FullCanvas", 1.0);
        var json = JsonSerializer.Serialize(rawResult);
        using var parsed = JsonDocument.Parse(json);

        Assert.False(parsed.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("not open or accessible", parsed.RootElement.GetProperty("error").GetString());
    }

    public class MultiBranchParamDouble : DispatchProxy
    {
        public GH_Structure<IGH_Goo> VolatileData { get; set; } = new();
        public Guid InstanceGuid { get; set; } = Guid.NewGuid();
        public string NickName { get; set; } = "DataTreeParam";

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
