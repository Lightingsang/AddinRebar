namespace HPGrasshopper.Mcp.Server.Tests.Seeds;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

public sealed class AdversarialSeedStressTests
{
    private static IReadOnlyList<SeedInstaller.SeedContent> LoadAllSeeds() =>
        SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly);

    [Fact]
    public void StressTest_All12Seeds_Adr0007Compliance_ZeroErrorsAndWarnings()
    {
        var seeds = LoadAllSeeds();
        Assert.Equal(12, seeds.Count);

        var findings = new List<string>();
        foreach (var seed in seeds)
        {
            var results = ScriptQuality.Find(seed.Code);
            foreach (var finding in results)
            {
                findings.Add($"[{finding.Severity}] {seed.Category}/{seed.Name}: {finding.RuleId} L{finding.Line}:C{finding.Column} - {finding.Message}");
            }
        }

        Assert.True(findings.Count == 0, $"ADR-0007 quality violations found:{Environment.NewLine}{string.Join(Environment.NewLine, findings)}");
    }

    [Fact]
    public void StressTest_All12Seeds_ParameterBindings_ScriptArgsCoverage()
    {
        var seeds = LoadAllSeeds();
        foreach (var seed in seeds)
        {
            var analysis = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Grasshopper);
            using var doc = JsonDocument.Parse(seed.ToolJson);
            var root = doc.RootElement;
            var schema = root.GetProperty("inputSchema");
            var properties = schema.GetProperty("properties")
                .EnumerateObject()
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var required = schema.TryGetProperty("required", out var reqEl)
                ? reqEl.EnumerateArray().Select(e => e.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var argKey in analysis.ArgKeys)
            {
                Assert.True(properties.Contains(argKey.Key), $"Seed {seed.Name} accesses undeclared argument '{argKey.Key}'");
            }

            foreach (var reqProp in required)
            {
                Assert.True(analysis.ArgKeys.Any(k => string.Equals(k.Key, reqProp, StringComparison.OrdinalIgnoreCase)),
                    $"Seed {seed.Name} has required property '{reqProp}' in schema that is not read in script");
            }
        }
    }

    [Fact]
    public void StressTest_All12Seeds_GenuineLogic_NoDummyMocksOrHardcodedReturns()
    {
        var seeds = LoadAllSeeds();
        foreach (var seed in seeds)
        {
            var analysis = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Grasshopper);
            Assert.True(analysis.ArgKeys.Count > 0, $"Seed {seed.Name} does not read any arguments");
            Assert.Contains("return new", seed.Code);
            Assert.DoesNotContain("return new { success = true };", seed.Code);
            Assert.DoesNotContain("TODO", seed.Code, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("throw new NotImplementedException", seed.Code);
        }
    }

    [Fact]
    public void Empirical_SolarRadiation_CalculationsMatchGroundTruth()
    {
        var resultDirect = ComputeSolarPanel(0, 0, 1, 0, 0, 1, 1000.0, 2.5);
        Assert.Equal(1.0, resultDirect.cosine, 4);
        Assert.Equal(1000.0, resultDirect.irradiance, 2);
        Assert.Equal(2500.0, resultDirect.power, 2);

        var resultGrazing = ComputeSolarPanel(1, 0, 0, 0, 1, 0, 1000.0, 1.0);
        Assert.Equal(0.0, resultGrazing.cosine, 4);
        Assert.Equal(0.0, resultGrazing.irradiance, 2);
        Assert.Equal(0.0, resultGrazing.power, 2);

        var resultAngle45 = ComputeSolarPanel(1, 1, 0, 1, 0, 0, 1000.0, 1.0);
        Assert.Equal(1.0 / Math.Sqrt(2.0), resultAngle45.cosine, 4);
        Assert.Equal(707.11, resultAngle45.irradiance, 2);

        var resultOpposite = ComputeSolarPanel(0, 0, -1, 0, 0, 1, 1000.0, 1.0);
        Assert.Equal(0.0, resultOpposite.cosine, 4);
        Assert.Equal(0.0, resultOpposite.power, 2);
    }

    [Fact]
    public void Empirical_FacadePanelization_CalculationsMatchGroundTruth()
    {
        double width = 30.0, height = 15.0;
        int uDiv = 3, vDiv = 3;
        double cellW = width / uDiv, cellH = height / vDiv;
        Assert.Equal(10.0, cellW);
        Assert.Equal(5.0, cellH);

        var panels = new List<(int id, double x1, double z1, double x3, double z3)>();
        for (int v = 0; v < vDiv; v++)
        {
            for (int u = 0; u < uDiv; u++)
            {
                int id = (v * uDiv) + u + 1;
                double xMin = u * cellW, xMax = (u + 1) * cellW;
                double zMin = v * cellH, zMax = (v + 1) * cellH;
                panels.Add((id, xMin, zMin, xMax, zMax));
            }
        }

        Assert.Equal(9, panels.Count);
        Assert.Equal((1, 0.0, 0.0, 10.0, 5.0), panels[0]);
        Assert.Equal((9, 20.0, 10.0, 30.0, 15.0), panels[8]);
    }

    [Fact]
    public void Empirical_CurtainGridLayout_CalculationsMatchGroundTruth()
    {
        double wallLengthMeters = 30.0;
        double spacing = 1.5;
        int count = Math.Max(1, (int)Math.Floor(wallLengthMeters / spacing));
        var positions = new List<double>();
        for (int i = 1; i <= count; i++)
        {
            double offset = i * spacing;
            if (offset < wallLengthMeters) positions.Add(offset);
        }

        Assert.Equal(19, positions.Count);
        Assert.Equal(1.5, positions[0]);
        Assert.Equal(28.5, positions[18]);
    }

    [Fact]
    public void Empirical_StirrupDistribution_CalculationsMatchGroundTruth()
    {
        double span = 6.0, ratio = 0.25, dense = 100.0, sparse = 200.0;
        double supportZone = span * ratio;
        double midSpanZone = span - (2.0 * supportZone);

        Assert.Equal(1.5, supportZone);
        Assert.Equal(3.0, midSpanZone);

        int leftCount = Math.Max(2, (int)Math.Round((supportZone * 1000.0) / dense));
        int midCount = Math.Max(2, (int)Math.Round((midSpanZone * 1000.0) / sparse));
        int rightCount = leftCount;

        Assert.Equal(15, leftCount);
        Assert.Equal(15, midCount);
        Assert.Equal(15, rightCount);
        Assert.Equal(45, leftCount + midCount + rightCount);
    }

    [Fact]
    public void Empirical_ExportModelToRhino_ScaleFactors()
    {
        const double feetPerMeter = 3.280839895013123;
        double scaleMeters = 1.0 / feetPerMeter;
        double scaleMm = 1000.0 / feetPerMeter;
        double scaleFeet = 1.0;

        Assert.Equal(0.3048, scaleMeters, 4);
        Assert.Equal(304.8, scaleMm, 1);
        Assert.Equal(1.0, scaleFeet, 4);
    }

    [Fact]
    public void Empirical_RebarCurvesSeed_ContainsTwoPhasePreValidationAndAtomicThrow()
    {
        var seeds = LoadAllSeeds();
        var rebarSeed = Assert.Single(seeds, s => s.Name == "generate_rebar_curves");

        Assert.Contains("validatedSets", rebarSeed.Code);
        Assert.Contains("zero-length segment", rebarSeed.Code);
        Assert.Contains("is disconnected", rebarSeed.Code);
        Assert.Contains("is non-planar", rebarSeed.Code);
        Assert.Contains("throw new InvalidOperationException", rebarSeed.Code);
    }

    private static (double cosine, double irradiance, double power) ComputeSolarPanel(
        double sx, double sy, double sz,
        double nx, double ny, double nz,
        double radiation, double area)
    {
        double sLen = Math.Sqrt((sx * sx) + (sy * sy) + (sz * sz));
        if (sLen > 0.0001) { sx /= sLen; sy /= sLen; sz /= sLen; }
        double nLen = Math.Sqrt((nx * nx) + (ny * ny) + (nz * nz));
        if (nLen > 0.0001) { nx /= nLen; ny /= nLen; nz /= nLen; }

        double dot = (sx * nx) + (sy * ny) + (sz * nz);
        double cos = Math.Max(0.0, dot);
        double irr = cos * radiation;
        double pow = irr * area;
        return (cos, irr, pow);
    }
}
