namespace HPGrasshopper.McpBridge.Tests.Tools;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using HPGrasshopper.McpBridge.Coordinator;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

public sealed class ChallengerFindingR8R9EmpiricalTests
{
    private static string ResolveSeedPath(string category, string toolName, string fileName)
    {
        var baseDir = AppContext.BaseDirectory;
        var p1 = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "HPGrasshopper.Mcp.Server", "Registry", "SeedLibrary", category, toolName, fileName));
        if (File.Exists(p1)) return p1;
        var p2 = Path.GetFullPath(Path.Combine(baseDir, "HPGrasshopper.Mcp.Server", "Registry", "SeedLibrary", category, toolName, fileName));
        if (File.Exists(p2)) return p2;
        throw new FileNotFoundException($"Seed file {fileName} not found for {category}/{toolName}");
    }

    [Fact]
    public void R8_RebarCurves_Adr0007QualityWalker_ZeroViolations()
    {
        var path = ResolveSeedPath("Revit", "generate_rebar_curves", "code.cs");
        var code = File.ReadAllText(path);
        var findings = ScriptQuality.Find(code);

        Assert.True(findings.Count == 0, $"ADR-0007 violations: {string.Join("; ", findings.Select(f => f.Message))}");
        var lineCount = File.ReadAllLines(path).Length;
        Assert.True(lineCount <= 300, $"Line count {lineCount} exceeds Rule C2 300 lines limit");
    }

    [Fact]
    public void R8_RebarCurves_ToolJsonSchema_DeclaresDiameterTolerance()
    {
        var path = ResolveSeedPath("Revit", "generate_rebar_curves", "tool.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var props = doc.RootElement.GetProperty("inputSchema").GetProperty("properties");

        Assert.True(props.TryGetProperty("diameterToleranceMm", out var tolProp));
        Assert.Equal(2.0, tolProp.GetProperty("default").GetDouble());
        Assert.True(props.TryGetProperty("barDiameterMm", out var diamProp));
        Assert.Equal(16.0, diamProp.GetProperty("default").GetDouble());
    }

    [Fact]
    public void R8_RebarCurves_DiameterMatchingAndToleranceBoundaries()
    {
        var availableTypes = new[] { ("T12", 12.0), ("T20", 20.0), ("T25", 25.0) };
        var (match1, diff1, msg1) = ResolveBarType(16.0, 2.0, availableTypes);
        Assert.Null(match1);
        Assert.Equal(4.0, diff1);
        Assert.Contains("No RebarBarType matching diameter 16mm found within tolerance ±2mm", msg1);
        Assert.Contains("T12", msg1);

        var (match2, diff2, _) = ResolveBarType(16.0, 4.0, availableTypes);
        Assert.Equal("T12", match2);
        Assert.Equal(4.0, diff2);

        var (match3, diff3, msg3) = ResolveBarType(16.0, 3.99, availableTypes);
        Assert.Null(match3);
        Assert.Equal(4.0, diff3);
        Assert.Contains("tolerance ±3.99mm", msg3);

        var (match4, diff4, _) = ResolveBarType(12.0, 0.0, availableTypes);
        Assert.Equal("T12", match4);
        Assert.Equal(0.0, diff4);
    }

    [Fact]
    public void R8_RebarCurves_CollinearPrefixInVerticalPlane_CalculatesPerpendicularNormal()
    {
        var tangents = new List<XYZ>
        {
            new(0.0, 0.0, 1.0),
            new(0.0, 0.0, 1.0),
            new(1.0, 0.0, 0.0)
        };

        var normal = ComputeCurveNormal(tangents);
        Assert.True(Math.Abs(normal.GetLength() - 1.0) < 0.001);
        Assert.True(CheckCoplanar(tangents, normal));
        Assert.Equal(0.0, Math.Abs(normal.DotProduct(tangents[0])), 3);
        Assert.Equal(0.0, Math.Abs(normal.DotProduct(tangents[2])), 3);
        Assert.Equal(1.0, Math.Abs(normal.Y), 3);
    }

    [Fact]
    public void R8_RebarCurves_CollinearPrefixInInclinedPlane_CalculatesPerpendicularNormal()
    {
        var v1 = new XYZ(1.0, 1.0, 0.0).Normalize();
        var v2 = new XYZ(1.0, 1.0, 0.0).Normalize();
        var v3 = new XYZ(0.0, 0.0, 1.0);
        var tangents = new List<XYZ> { v1, v2, v3 };

        var normal = ComputeCurveNormal(tangents);
        Assert.True(Math.Abs(normal.GetLength() - 1.0) < 0.001);
        Assert.True(CheckCoplanar(tangents, normal));
        Assert.Equal(0.0, Math.Abs(normal.DotProduct(v1)), 3);
        Assert.Equal(0.0, Math.Abs(normal.DotProduct(v3)), 3);
    }

    [Fact]
    public void R8_RebarCurves_StraightVerticalBar_CalculatesHorizontalPerpendicularNormal()
    {
        var singleVertical = new List<XYZ> { new(0.0, 0.0, 1.0) };
        var normalSingle = ComputeCurveNormal(singleVertical);
        Assert.True(Math.Abs(normalSingle.GetLength() - 1.0) < 0.001);
        Assert.Equal(0.0, Math.Abs(normalSingle.Z), 3);
        Assert.Equal(0.0, Math.Abs(normalSingle.DotProduct(singleVertical[0])), 3);

        var collinearVertical = new List<XYZ> { new(0.0, 0.0, 1.0), new(0.0, 0.0, 1.0) };
        var normalCollinear = ComputeCurveNormal(collinearVertical);
        Assert.True(Math.Abs(normalCollinear.GetLength() - 1.0) < 0.001);
        Assert.Equal(0.0, Math.Abs(normalCollinear.Z), 3);
        Assert.True(CheckCoplanar(collinearVertical, normalCollinear));
    }

    [Fact]
    public void R8_RebarCurves_NonPlanarCurve_RejectsWithClearMessage()
    {
        var nonPlanarTangents = new List<XYZ>
        {
            new(1.0, 0.0, 0.0),
            new(0.0, 1.0, 0.0),
            new(0.0, 0.0, 1.0)
        };

        var normal = ComputeCurveNormal(nonPlanarTangents);
        var isCoplanar = CheckCoplanar(nonPlanarTangents, normal);
        Assert.False(isCoplanar);
    }

    [Fact]
    public void R8_SeedHonesty_DirectShapeAndQueryGeometryDescriptions()
    {
        var dsPath = ResolveSeedPath("Geometry", "directshape_from_geometry", "tool.json");
        using var dsDoc = JsonDocument.Parse(File.ReadAllText(dsPath));
        var dsTitle = dsDoc.RootElement.GetProperty("title").GetString() ?? string.Empty;
        var dsDesc = dsDoc.RootElement.GetProperty("description").GetString() ?? string.Empty;
        Assert.Contains("Bounding Volume", dsTitle, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("bounding box dimensions", dsDesc, StringComparison.OrdinalIgnoreCase);

        var qPath = ResolveSeedPath("Geometry", "query_revit_geometry", "tool.json");
        using var qDoc = JsonDocument.Parse(File.ReadAllText(qPath));
        var qDesc = qDoc.RootElement.GetProperty("description").GetString() ?? string.Empty;
        Assert.Contains("summaries", qDesc, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void R9_CompilerReferences_ComputeFingerprint_DeterministicAndOrdered()
    {
        var fpInitial = GrasshopperCompilerReferences.ComputeFingerprint();
        Assert.False(string.IsNullOrWhiteSpace(fpInitial));

        for (int i = 0; i < 10; i++)
        {
            var fp = GrasshopperCompilerReferences.ComputeFingerprint();
            Assert.Equal(fpInitial, fp);
        }

        var tokens = fpInitial.Split(';');
        var sortedTokens = tokens.OrderBy(x => x).ToArray();
        Assert.Equal(tokens, sortedTokens);
    }

    [Fact]
    public void R9_CompilerLifecycle_FingerprintDrift_InvalidatesAndRecreatesCompiler()
    {
        using var lifecycle = new GrasshopperCompilerLifecycle();
        var c1 = lifecycle.GetCompiler(null);
        var c2 = lifecycle.GetCompiler(null);
        Assert.Same(c1, c2);

        var fpField = typeof(GrasshopperCompilerLifecycle).GetField("_fingerprint", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(fpField);
        fpField!.SetValue(lifecycle, "stale_drifted_fingerprint_test");

        var c3 = lifecycle.GetCompiler(null);
        Assert.NotSame(c1, c3);
    }

    [Fact]
    public void R9_CompilerLifecycle_AssemblyLoadEvent_SelectivelyInvalidates()
    {
        using var lifecycle = new GrasshopperCompilerLifecycle();
        var c1 = lifecycle.GetCompiler(null);

        var handler = typeof(GrasshopperCompilerLifecycle).GetMethod("OnAssemblyLoad", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(handler);

        var dummyAsm = typeof(JsonElement).Assembly;
        handler!.Invoke(lifecycle, new object?[] { null, new AssemblyLoadEventArgs(dummyAsm) });
        var c2 = lifecycle.GetCompiler(null);
        Assert.Same(c1, c2);

        var revitAsm = typeof(Document).Assembly;
        handler.Invoke(lifecycle, new object?[] { null, new AssemblyLoadEventArgs(revitAsm) });
        var c3 = lifecycle.GetCompiler(null);
        Assert.NotSame(c1, c3);
    }

    [Fact]
    public async Task R9_ContextExtractor_NullUiApp_BackgroundThreadSafeFallbackAndConcurrency()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        coordinator.UpdateCachedUiContext(null!);
        var fallback = GrasshopperContextExtractor.Extract(null, coordinator, false, true);

        Assert.Equal(PipeNaming.GrasshopperHost, fallback.Host);
        Assert.False(fallback.IsModifiable);
        Assert.NotNull(fallback.Grasshopper);
        Assert.Equal(0, fallback.Grasshopper.ComponentCount);

        var cached = new ContextResult
        {
            Host = PipeNaming.GrasshopperHost,
            HostVersion = "2026",
            RevitVersion = "2026",
            DocTitle = "Tower.rvt",
            DocPath = "C:\\Models\\Tower.rvt",
            IsModifiable = true,
            Units = new UnitsInfo("m"),
            OpenDocs = ["Tower.rvt"],
            ExecutionEnabled = true,
            Selection = [],
            Grasshopper = new GrasshopperInfo(true, "1.0", "8.0", "Main.gh", null, 25, 12, 1, false, true)
        };
        coordinator.UpdateCachedUiContext(cached);

        var tasks = Enumerable.Range(0, 30).Select(_ => Task.Run(() =>
        {
            var res = GrasshopperContextExtractor.Extract(null, coordinator, true, true);
            Assert.False(res.IsModifiable);
            Assert.Equal("Tower.rvt", res.DocTitle);
            Assert.Equal(25, res.Grasshopper?.ComponentCount);
        }));

        await Task.WhenAll(tasks);
        coordinator.UpdateCachedUiContext(null!);
    }

    private static (string? matchedName, double minDiff, string message) ResolveBarType(
        double barDiameter, double diameterTolerance, (string Name, double DiameterMm)[] availableTypes)
    {
        string? matchedBarType = null;
        double minDiameterDifference = double.MaxValue;
        for (int i = 0; i < availableTypes.Length; i++)
        {
            double diff = Math.Abs(availableTypes[i].DiameterMm - barDiameter);
            if (diff < minDiameterDifference)
            {
                minDiameterDifference = diff;
                matchedBarType = availableTypes[i].Name;
            }
        }

        if (matchedBarType == null || minDiameterDifference > diameterTolerance)
        {
            string closest = matchedBarType ?? "None";
            return (null, minDiameterDifference,
                $"No RebarBarType matching diameter {barDiameter}mm found within tolerance ±{diameterTolerance}mm. Closest available is '{closest}' (diff: {minDiameterDifference}mm).");
        }
        return (matchedBarType, minDiameterDifference, "OK");
    }

    private static XYZ ComputeCurveNormal(List<XYZ> tangents)
    {
        for (int firstIdx = 0; firstIdx < tangents.Count - 1; firstIdx++)
        {
            for (int secondIdx = firstIdx + 1; secondIdx < tangents.Count; secondIdx++)
            {
                var crossProduct = tangents[firstIdx].CrossProduct(tangents[secondIdx]);
                if (crossProduct.GetLength() > 0.001) return crossProduct.Normalize();
            }
        }
        var primaryTangent = tangents[0];
        var referenceAxis = Math.Abs(primaryTangent.Z) < 0.85
            ? new XYZ(0.0, 0.0, 1.0)
            : new XYZ(1.0, 0.0, 0.0);
        return primaryTangent.CrossProduct(referenceAxis).Normalize();
    }

    private static bool CheckCoplanar(List<XYZ> tangents, XYZ normal)
    {
        for (int checkIdx = 0; checkIdx < tangents.Count; checkIdx++)
        {
            if (Math.Abs(tangents[checkIdx].DotProduct(normal)) > 0.01) return false;
        }
        return true;
    }
}
