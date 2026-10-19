namespace HPGrasshopper.McpBridge.Tests.Regression;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using Xunit;

public sealed class F8_F9_F11_SeedAndDataTreeRegressionTests
{
    private static string ResolveSeedDir(string category, string toolName)
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "HPGrasshopper.Mcp.Server", "Registry", "SeedLibrary", category, toolName));
        if (Directory.Exists(path)) return path;
        path = Path.GetFullPath(Path.Combine(baseDir, "HPGrasshopper.Mcp.Server", "Registry", "SeedLibrary", category, toolName));
        if (Directory.Exists(path)) return path;
        throw new DirectoryNotFoundException($"Seed directory not found for {category}/{toolName}");
    }

    [Theory]
    [InlineData("Geometry", "facade_panelization")]
    [InlineData("Geometry", "curtain_grid_layout")]
    [InlineData("Revit", "generate_stirrup_distribution")]
    [InlineData("Generic", "export_model_to_rhino")]
    public void F8_SeedContractHonesty_TransactionModeNone_AllFourSeeds(string category, string toolName)
    {
        var seedDir = ResolveSeedDir(category, toolName);
        var json = File.ReadAllText(Path.Combine(seedDir, "tool.json"));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("none", root.GetProperty("transaction").GetString());
        Assert.False(root.GetProperty("destructive").GetBoolean());
        var desc = root.GetProperty("description").GetString() ?? string.Empty;
        var title = root.GetProperty("title").GetString() ?? string.Empty;
        Assert.True(desc.Contains("Calculate", StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("Plan", StringComparison.OrdinalIgnoreCase) ||
                    title.Contains("Plan", StringComparison.OrdinalIgnoreCase));

        var code = File.ReadAllText(Path.Combine(seedDir, "code.cs"));
        Assert.DoesNotContain("new Transaction(", code);
        Assert.DoesNotContain("new TransactionGroup(", code);
    }

    [Fact]
    public void F8_SeedContractHonesty_CodeDoesNotContainFabricatedExportCounts()
    {
        var seedDir = ResolveSeedDir("Generic", "export_model_to_rhino");
        var code = File.ReadAllText(Path.Combine(seedDir, "code.cs"));

        Assert.DoesNotContain("5/10", code);
        Assert.Contains("GetElementCount()", code);
    }

    [Fact]
    public void F9_GenerateRebarCurves_ResolvesClosestBarDiameterAndNormal()
    {
        var seedDir = ResolveSeedDir("Revit", "generate_rebar_curves");
        var code = File.ReadAllText(Path.Combine(seedDir, "code.cs"));

        Assert.Contains("barDiameterMm", code);
        Assert.Contains("BarNominalDiameter", code);
        Assert.Contains("CrossProduct", code);
    }

    [Fact]
    public void F9_CreateFloorsSlabs_AppliesStructuralParameter()
    {
        var seedDir = ResolveSeedDir("Revit", "create_floors_slabs");
        var code = File.ReadAllText(Path.Combine(seedDir, "code.cs"));

        Assert.Contains("structural", code);
        Assert.Contains("FLOOR_PARAM_IS_STRUCTURAL", code);
    }

    [Fact]
    public void F9_DirectShapeFromGeometry_BranchesOnMeshAndSolid_RejectsUnsupported()
    {
        var seedDir = ResolveSeedDir("Geometry", "directshape_from_geometry");
        var code = File.ReadAllText(Path.Combine(seedDir, "code.cs"));

        Assert.Contains("isMesh", code);
        Assert.Contains("isSolid", code);
        Assert.Contains("Unsupported shapeType", code);
    }

    [Fact]
    public void F11_DataTree_MultiBranchStructure_PreservesPathsAndTypes()
    {
        var proxy = DispatchProxy.Create<IGH_Param, MultiBranchTestDouble>();
        var impl = (MultiBranchTestDouble)(object)proxy;

        var tree = new GH_Structure<IGH_Goo>();
        tree.Append(new GH_Number(42.5), new GH_Path(0, 0));
        tree.Append(new GH_String("BranchData"), new GH_Path(0, 1));
        tree.Append(new GH_Boolean(true), new GH_Path(1, 0));
        impl.VolatileData = tree;

        var extracted = PlayerDataTreeHelper.ExtractParamData(proxy);
        Assert.NotNull(extracted);
        var dict = Assert.IsType<Dictionary<string, object?>>(extracted);

        Assert.Equal(3, dict.Count);
        Assert.True(dict.ContainsKey("{0;0}"));
        Assert.True(dict.ContainsKey("{0;1}"));
        Assert.True(dict.ContainsKey("{1;0}"));

        var b00 = Assert.IsType<List<object?>>(dict["{0;0}"]);
        Assert.Equal(42.5, b00[0]);
        var b01 = Assert.IsType<List<object?>>(dict["{0;1}"]);
        Assert.Equal("BranchData", b01[0]);
        var b10 = Assert.IsType<List<object?>>(dict["{1;0}"]);
        Assert.Equal(true, b10[0]);
    }

    [Fact]
    public void F11_PlayerRunner_OutputDisambiguation_HandlesDuplicateNickNames()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var p1 = TestProxyFactory.CreateParam("RebarOut", instanceGuid: id1, volatileDataCount: 1);
        var p2 = TestProxyFactory.CreateParam("RebarOut", instanceGuid: id2, volatileDataCount: 1);
        var p3 = TestProxyFactory.CreateParam("RebarOut", instanceGuid: id3, volatileDataCount: 1);

        TestProxyFactory.GetProxy<TestParamProxy>(p1).VolatileDataAdded.Add(new GH_Number(10.0));
        TestProxyFactory.GetProxy<TestParamProxy>(p2).VolatileDataAdded.Add(new GH_Number(20.0));
        TestProxyFactory.GetProxy<TestParamProxy>(p3).VolatileDataAdded.Add(new GH_Number(30.0));

        var outputs = PlayerRunner.ExtractOutputs(new[] { p1, p2, p3 }, null);
        Assert.Equal(3, outputs.Count);
        Assert.True(outputs.ContainsKey("RebarOut"));
        Assert.True(outputs.ContainsKey($"RebarOut#{id2.ToString("D")[..8]}"));
        Assert.True(outputs.ContainsKey($"RebarOut#{id3.ToString("D")[..8]}"));
    }

    [Fact]
    public void F11_ExecutionEngine_MapsSolverErrorsToExecuteResult()
    {
        var errObj = new { success = false, error = "Solver divergence", rolledBack = true };
        var res = GrasshopperExecutionEngine.MapToExecuteResult(errObj);

        Assert.True(res.IsError);
        Assert.Equal("Solver divergence", res.Message);
        Assert.True(res.RolledBack);
    }

    public class MultiBranchTestDouble : DispatchProxy
    {
        public GH_Structure<IGH_Goo> VolatileData { get; set; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null) return null;
            if (targetMethod.Name == "get_VolatileData") return VolatileData;
            if (targetMethod.Name == "get_VolatileDataCount") return VolatileData.DataCount;
            if (targetMethod.Name == "get_NickName") return "MultiBranchParam";
            return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
        }
    }
}
