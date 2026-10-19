namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Harness;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

public sealed class Tier4_RealWorldWorkloadTests
{
    [Fact]
    public async Task TC_T4_RW_01_ParametricTowerStructuralGridAndFraming()
    {
        var executor = new GrasshopperTestExecutor { IsExecutionEnabled = true };
        var script = GrasshopperWorkloadScripts.TowerGridScript;

        var guardViolations = ScriptGuard.Check(script, GuardProfile.Grasshopper);
        Assert.Empty(guardViolations);

        var analysis = ScriptAnalyzer.Analyze(script, AnalyzerProfile.Grasshopper);
        Assert.True(analysis.HasLoops);
        Assert.Contains(analysis.ArgKeys, a => a.Key == "elevations");
        Assert.Contains(analysis.ArgKeys, a => a.Key == "gridSpacings");
        Assert.Contains(analysis.ArgKeys, a => a.Key == "gridLength");
        Assert.True(analysis.LineCount <= 300);

        var quality = ScriptQuality.Find(script);
        Assert.DoesNotContain(quality, q => q.Severity == ScriptQuality.Error);

        var args = JsonSerializer.SerializeToElement(new
        {
            elevations = new[] { 0.0, 3.6, 7.2, 10.8, 14.4 },
            gridSpacings = new[] { 6.0, 6.0, 6.0, 6.0 },
            gridLength = 30.0
        });

        var request = new ExecuteRequest(script, TransactionModes.Auto, DryRun: false, Args: args, SessionId: "rw-01");
        var result = await executor.ExecuteAsync(request, null, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.False(result.RolledBack);
        Assert.True(result.DurationMs >= 0);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public async Task TC_T4_RW_02_FacadeAdaptivePanelizationAndCladding()
    {
        var executor = new GrasshopperTestExecutor { IsExecutionEnabled = true };
        var script = GrasshopperWorkloadScripts.FacadePanelizationScript;

        var guardViolations = ScriptGuard.Check(script, GuardProfile.Grasshopper);
        Assert.Empty(guardViolations);

        var analysis = ScriptAnalyzer.Analyze(script, AnalyzerProfile.Grasshopper);
        Assert.True(analysis.HasLoops);
        Assert.Contains(analysis.ArgKeys, a => a.Key == "uDivisions");
        Assert.Contains(analysis.ArgKeys, a => a.Key == "vDivisions");
        Assert.Contains(analysis.ArgKeys, a => a.Key == "surfaceWidthMeters");
        Assert.True(analysis.LineCount <= 300);

        var quality = ScriptQuality.Find(script);
        Assert.DoesNotContain(quality, q => q.Severity == ScriptQuality.Error);

        var args = JsonSerializer.SerializeToElement(new
        {
            surfaceWidthMeters = 36.0,
            surfaceHeightMeters = 18.0,
            uDivisions = 12,
            vDivisions = 6,
            panelFamilyName = "Adaptive-Quad-Glass"
        });

        var request = new ExecuteRequest(script, TransactionModes.Auto, DryRun: false, Args: args, SessionId: "rw-02");
        var result = await executor.ExecuteAsync(request, null, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.False(result.RolledBack);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task TC_T4_RW_03_AutomatedBeamRebarDetailingAndStirrups()
    {
        var executor = new GrasshopperTestExecutor { IsExecutionEnabled = true };
        var script = GrasshopperWorkloadScripts.BeamRebarStirrupScript;

        var guardViolations = ScriptGuard.Check(script, GuardProfile.Grasshopper);
        Assert.Empty(guardViolations);

        var analysis = ScriptAnalyzer.Analyze(script, AnalyzerProfile.Grasshopper);
        Assert.Contains(analysis.ArgKeys, a => a.Key == "beamElementId");
        Assert.Contains(analysis.ArgKeys, a => a.Key == "denseSpacingMm");
        Assert.Contains(analysis.ArgKeys, a => a.Key == "sparseSpacingMm");
        Assert.Contains(analysis.ArgKeys, a => a.Key == "supportZoneRatio");

        var quality = ScriptQuality.Find(script);
        Assert.DoesNotContain(quality, q => q.Severity == ScriptQuality.Error);

        var args = JsonSerializer.SerializeToElement(new
        {
            beamElementId = 20501L,
            denseSpacingMm = 100.0,
            sparseSpacingMm = 200.0,
            supportZoneRatio = 0.25,
            spanMeters = 6.0
        });

        var request = new ExecuteRequest(script, TransactionModes.Auto, DryRun: false, Args: args, SessionId: "rw-03");
        var result = await executor.ExecuteAsync(request, null, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task TC_T4_RW_04_DryRunDualSafetyAndCleanRollbackGuarantee()
    {
        var executor = new GrasshopperTestExecutor { IsExecutionEnabled = true };
        int initialCommitted = executor.CommittedElementCount;
        var script = GrasshopperWorkloadScripts.DryRunCurveScript;

        var guardViolations = ScriptGuard.Check(script, GuardProfile.Grasshopper);
        Assert.Empty(guardViolations);

        var dryReq = new ExecuteRequest(script, TransactionModes.Auto, DryRun: true, SessionId: "rw-04-dry");
        var dryResult = await executor.ExecuteAsync(dryReq, null, CancellationToken.None);

        Assert.False(dryResult.IsError);
        Assert.True(dryResult.RolledBack);
        Assert.Equal(initialCommitted, executor.CommittedElementCount);

        var liveReq = new ExecuteRequest(script, TransactionModes.Auto, DryRun: false, SessionId: "rw-04-live");
        var liveResult = await executor.ExecuteAsync(liveReq, null, CancellationToken.None);

        Assert.False(liveResult.IsError);
        Assert.False(liveResult.RolledBack);
        Assert.True(executor.CommittedElementCount > initialCommitted);
    }

    [Fact]
    public async Task TC_T4_RW_05_AutonomousAiAgentToolLifecycle()
    {
        var executor = new GrasshopperTestExecutor { IsExecutionEnabled = true };
        var draftCode = GrasshopperWorkloadScripts.BeamDeflectionCandidateScript;

        var quality = ScriptQuality.Find(draftCode);
        Assert.DoesNotContain(quality, q => q.Severity == ScriptQuality.Error);

        var guard = ScriptGuard.Check(draftCode, GuardProfile.Grasshopper);
        Assert.Empty(guard);

        var analysis = executor.Analyze(new AnalyzeRequest(draftCode));
        var schemaJson = """{"type":"object","properties":{"spanMeters":{"type":"number"},"depthMeters":{"type":"number"}}}""";
        var candidate = new ToolRecord
        {
            Name = "check_beam_deflection",
            Category = "Geometry",
            Description = "Evaluates structural beam deflection limits according to building code.",
            Transaction = "auto",
            TimeoutSeconds = 30,
            Code = draftCode,
            InputSchema = JsonSerializer.Deserialize<JsonElement>(schemaJson),
            Examples = [new ToolExample { Title = "Span check", Args = JsonSerializer.Deserialize<JsonElement>("""{"spanMeters":6.0,"depthMeters":0.4}""") }]
        };

        var report = ToolValidator.Validate(candidate, analysis, [], false, HostProfile.Grasshopper);
        Assert.True(report.IsValid);

        var testRes = await executor.ExecuteAsync(new ExecuteRequest(draftCode, TransactionModes.Auto, DryRun: true), null, CancellationToken.None);
        Assert.True(testRes.RolledBack);

        var prodRes = await executor.ExecuteAsync(new ExecuteRequest(draftCode, TransactionModes.Auto, DryRun: false), null, CancellationToken.None);
        Assert.False(prodRes.RolledBack);
    }

    [Fact]
    public async Task TC_T4_RW_06_SolarRadiationAnalysisAndDirectShapeHeatmap()
    {
        var executor = new GrasshopperTestExecutor { IsExecutionEnabled = true };
        var script = GrasshopperWorkloadScripts.SolarRadiationScript;

        var guardViolations = ScriptGuard.Check(script, GuardProfile.Grasshopper);
        Assert.Empty(guardViolations);

        var analysis = ScriptAnalyzer.Analyze(script, AnalyzerProfile.Grasshopper);
        Assert.True(analysis.HasLoops);
        Assert.Contains(analysis.ArgKeys, a => a.Key == "sunVector");
        Assert.Contains(analysis.ArgKeys, a => a.Key == "targetSurfaceNormals");
        Assert.Contains(analysis.ArgKeys, a => a.Key == "directRadiationWatts");

        var quality = ScriptQuality.Find(script);
        Assert.DoesNotContain(quality, q => q.Severity == ScriptQuality.Error);

        var args = JsonSerializer.SerializeToElement(new
        {
            sunVector = new { x = 0.5, y = 0.5, z = 0.7071 },
            targetSurfaceNormals = new[]
            {
                new { id = 1, normalX = 0.0, normalY = 1.0, normalZ = 0.0, area = 15.0 },
                new { id = 2, normalX = 0.0, normalY = 0.0, normalZ = 1.0, area = 25.0 }
            },
            directRadiationWatts = 950.0
        });

        var request = new ExecuteRequest(script, TransactionModes.Auto, DryRun: false, Args: args, SessionId: "rw-06");
        var result = await executor.ExecuteAsync(request, null, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.NotNull(result.Value);
    }
}
