namespace HPGrasshopper.McpBridge.Tests.Regression;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

/// <summary>
///     Empirical verification tests for Milestone M1 Findings (N1–N5).
/// </summary>
public sealed class Milestone1RemediationEmpiricalTests
{
    private static readonly BridgeSettings DefaultSettings = new() { ExecutionEnabled = true };

    private sealed class FailingCanvasScope : ICanvasUndoScope
    {
        public bool IsCommitted { get; private set; }
        public bool ReturnCleanOnRollback { get; set; }

        public void Commit() => IsCommitted = true;

        public CanvasRollbackResult Rollback()
        {
            if (ReturnCleanOnRollback) return CanvasRollbackResult.Clean();
            return CanvasRollbackResult.Failed(new[] { "Canvas wire cleanup failed" });
        }

        public void Dispose()
        {
            if (!IsCommitted) Rollback();
        }
    }

    [Fact]
    public void N1_GeometryValidationMath_RejectsInvalidCurves()
    {
        // 1. Zero/short length
        Assert.False(ValidateLineSegment(0.0, 0.0, 0.0, 0.0, 0.0, 0.0));
        Assert.True(ValidateLineSegment(0.0, 0.0, 0.0, 1.0, 0.0, 0.0));

        // 2. Continuity gap
        Assert.False(CheckContinuity(new (double x, double y)[] { (0, 0), (1, 0) }, new (double x, double y)[] { (1.5, 0), (2, 0) }));
        Assert.True(CheckContinuity(new (double x, double y)[] { (0, 0), (1, 0) }, new (double x, double y)[] { (1, 0), (2, 0) }));

        // 3. Coplanarity
        var planar = new (double x, double y, double z)[] { (0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 0) };
        var nonPlanar = new (double x, double y, double z)[] { (0, 0, 0), (1, 0, 0), (1, 1, 0), (0, 1, 1) };
        Assert.True(CheckCoplanar(planar));
        Assert.False(CheckCoplanar(nonPlanar));
    }

    [Fact]
    public void N2_ExecutionFailure_CanvasRollbackFails_ReturnsRolledBackFalseAndSurfacesError()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvasScope = new FailingCanvasScope { ReturnCleanOnRollback = false };
        var req = new ExecuteRequest("fault", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("Revit failure"),
            canvasScope: canvasScope);

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Contains("Revit failure", result.Message);
        Assert.Contains("Additionally, canvas rollback failed: Canvas wire cleanup failed", result.Message);
    }

    [Fact]
    public void N2_DryRun_CanvasRollbackFails_ReturnsRolledBackFalseAndSurfacesError()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvasScope = new FailingCanvasScope { ReturnCleanOnRollback = false };
        var req = new ExecuteRequest("dry-run", "auto", true);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "preview-result",
            canvasScope: canvasScope);

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Contains("Canvas rollback failed", result.Message);
        Assert.Contains("Canvas wire cleanup failed", result.Message);
    }

    [Fact]
    public void N3_ExecuteBatch_PreCancelledToken_ThrowsOperationCanceledException()
    {
        var editor = new CanvasEditor();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        using var doc = JsonDocument.Parse("{\"operations\": []}");

        Assert.Throws<OperationCanceledException>(() =>
            editor.ExecuteBatch(null, doc.RootElement, false, cts.Token));
    }

    [Fact]
    public void N3_ExecuteBatch_ExternalScope_DefersCommitToCaller()
    {
        var editor = new CanvasEditor();
        using var scope = TestScopeFactory.CreateHeadlessScope();
        using var cts = new CancellationTokenSource();
        using var doc = JsonDocument.Parse("{\"operations\": []}");

        editor.ExecuteBatch(null, doc.RootElement, false, cts.Token, scope);

        Assert.False(scope.IsCommitted);
        scope.Commit();
        Assert.True(scope.IsCommitted);
    }

    [Fact]
    public void N4_BaselineObjects_ExcludedFromDeletionDuringRollback()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var baselineGuid = Guid.NewGuid();

        var baselineField = typeof(CanvasUndoScope).GetField("_baselineGuids", BindingFlags.NonPublic | BindingFlags.Instance);
        var baselineSet = (HashSet<Guid>)baselineField!.GetValue(scope)!;
        baselineSet.Add(baselineGuid);

        scope.TrackAddedObject(baselineGuid);

        var rollbackResult = scope.Rollback();
        Assert.True(rollbackResult.Success);

        var addedField = typeof(CanvasUndoScope).GetField("_addedGuids", BindingFlags.NonPublic | BindingFlags.Instance);
        var addedList = (List<Guid>)addedField!.GetValue(scope)!;
        Assert.Contains(baselineGuid, addedList);
    }

    [Fact]
    public void N5_TargetDocPath_WhitespaceNormalizedToNull()
    {
        var work = new McpBridgeWorkRequest(
            "NormalizePathTest",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: "Model.rvt",
            TargetDocPath: "   \t\n   ",
            TargetDefId: null);

        Assert.Null(work.TargetDocPath);
    }

    [Fact]
    public void N5_Coordinator_UpdateActiveSnapshot_WhitespaceNormalizedToNull()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        try
        {
            coordinator.UpdateActiveSnapshot("   ", "   ", null, "   ", "   ");
            Assert.Null(coordinator.ActiveDocumentTitle);
            Assert.Null(coordinator.ActiveDocumentPath);
            Assert.Null(coordinator.ActiveDefinitionName);
            Assert.Null(coordinator.ActiveDefinitionPath);
        }
        finally
        {
            coordinator.UpdateActiveSnapshot(null, null, null);
        }
    }

    [Fact]
    public void N5_RequireTargetIdentityTrue_WhenNoTargetSpecified_Throws()
    {
        var work = new McpBridgeWorkRequest(
            "MutatingOp",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: null,
            TargetDocPath: null,
            TargetDefId: null,
            RequireTargetIdentity: true);

        var ex = Assert.Throws<InvalidOperationException>(() => work.ValidateActiveDocument(null));
        Assert.Contains("Write operation 'MutatingOp' rejected", ex.Message);
        Assert.Contains("Target document or definition identity must be specified", ex.Message);
    }

    private static bool ValidateLineSegment(double x1, double y1, double z1, double x2, double y2, double z2)
    {
        double dx = x2 - x1, dy = y2 - y1, dz = z2 - z1;
        return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz)) >= 1e-4;
    }

    private static bool CheckContinuity((double x, double y)[] c1, (double x, double y)[] c2)
    {
        double dx = c2[0].x - c1[1].x, dy = c2[0].y - c1[1].y;
        return Math.Sqrt((dx * dx) + (dy * dy)) <= 1e-4;
    }

    private static bool CheckCoplanar((double x, double y, double z)[] pts)
    {
        if (pts.Length < 4) return true;
        double v1x = pts[1].x - pts[0].x, v1y = pts[1].y - pts[0].y, v1z = pts[1].z - pts[0].z;
        double v2x = pts[2].x - pts[0].x, v2y = pts[2].y - pts[0].y, v2z = pts[2].z - pts[0].z;
        double nx = (v1y * v2z) - (v1z * v2y), ny = (v1z * v2x) - (v1x * v2z), nz = (v1x * v2y) - (v1y * v2x);
        double len = Math.Sqrt((nx * nx) + (ny * ny) + (nz * nz));
        if (len < 1e-6) return true;
        nx /= len; ny /= len; nz /= len;
        for (int i = 3; i < pts.Length; i++)
        {
            double dist = Math.Abs(((pts[i].x - pts[0].x) * nx) + ((pts[i].y - pts[0].y) * ny) + ((pts[i].z - pts[0].z) * nz));
            if (dist > 1e-4) return false;
        }
        return true;
    }
}
