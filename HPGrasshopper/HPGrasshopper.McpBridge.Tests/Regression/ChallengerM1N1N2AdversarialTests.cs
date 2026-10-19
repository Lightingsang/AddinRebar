namespace HPGrasshopper.McpBridge.Tests.Regression;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using Autodesk.Revit.DB;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

/// <summary>
///     Adversarial empirical challenge tests for Findings N1 and N2.
/// </summary>
public sealed class ChallengerM1N1N2AdversarialTests
{
    private static readonly BridgeSettings DefaultSettings = new() { ExecutionEnabled = true };

    private sealed class TrackingCanvasScope : ICanvasUndoScope
    {
        public bool IsCommitted { get; private set; }
        public bool RollbackCalled { get; private set; }
        public bool ShouldThrowOnRollback { get; set; }
        public bool ReturnCleanOnRollback { get; set; } = true;
        public List<string> RollbackErrors { get; set; } = new();

        public void Commit() => IsCommitted = true;

        public CanvasRollbackResult Rollback()
        {
            RollbackCalled = true;
            if (ShouldThrowOnRollback)
                throw new InvalidOperationException("Simulated unexpected exception in CanvasRollback.");
            return ReturnCleanOnRollback
                ? CanvasRollbackResult.Clean()
                : CanvasRollbackResult.Failed(RollbackErrors.Count > 0 ? RollbackErrors : new[] { "Canvas wire cleanup failed" });
        }

        public void Dispose() { if (!IsCommitted && !RollbackCalled) Rollback(); }
    }

    [Fact]
    public void N1_MixedValidAndNonPlanarSets_RejectsBeforeCreation_ZeroRebarsCreated()
    {
        var validPlanarSet = "[{\"startX\":0,\"startY\":0,\"startZ\":0,\"endX\":1,\"endY\":0,\"endZ\":0},{\"startX\":1,\"startY\":0,\"startZ\":0,\"endX\":1,\"endY\":1,\"endZ\":0},{\"startX\":1,\"startY\":1,\"startZ\":0,\"endX\":0,\"endY\":1,\"endZ\":0},{\"startX\":0,\"startY\":1,\"startZ\":0,\"endX\":0,\"endY\":0,\"endZ\":0}]";
        var nonPlanarSet = "[{\"startX\":0,\"startY\":0,\"startZ\":0,\"endX\":1,\"endY\":0,\"endZ\":0},{\"startX\":1,\"startY\":0,\"startZ\":0,\"endX\":1,\"endY\":1,\"endZ\":0},{\"startX\":1,\"startY\":1,\"startZ\":0,\"endX\":0,\"endY\":1,\"endZ\":1},{\"startX\":0,\"startY\":1,\"startZ\":1,\"endX\":0,\"endY\":0,\"endZ\":0}]";
        using var jsonDoc = JsonDocument.Parse($"[{validPlanarSet},{nonPlanarSet}]");

        var (success, error, created) = SimulateRebarExecution(jsonDoc.RootElement);

        Assert.False(success);
        Assert.Equal(0, created);
        Assert.Contains("Rebar curve set 1 is non-planar", error);
    }

    [Fact]
    public void N1_MixedValidAndDisconnectedSets_RejectsBeforeCreation_ZeroRebarsCreated()
    {
        var validPlanarSet = "[{\"startX\":0,\"startY\":0,\"startZ\":0,\"endX\":1,\"endY\":0,\"endZ\":0},{\"startX\":1,\"startY\":0,\"startZ\":0,\"endX\":2,\"endY\":0,\"endZ\":0}]";
        var disconnectedSet = "[{\"startX\":0,\"startY\":0,\"startZ\":0,\"endX\":1,\"endY\":0,\"endZ\":0},{\"startX\":1.5,\"startY\":0,\"startZ\":0,\"endX\":2.5,\"endY\":0,\"endZ\":0}]";
        using var jsonDoc = JsonDocument.Parse($"[{validPlanarSet},{disconnectedSet}]");

        var (success, error, created) = SimulateRebarExecution(jsonDoc.RootElement);

        Assert.False(success);
        Assert.Equal(0, created);
        Assert.Contains("disconnected between segment 0 and 1", error);
    }

    [Fact]
    public void N1_MixedValidAndZeroLengthSets_RejectsBeforeCreation_ZeroRebarsCreated()
    {
        var validPlanarSet = "[{\"startX\":0,\"startY\":0,\"startZ\":0,\"endX\":1,\"endY\":0,\"endZ\":0}]";
        var zeroLengthSet = "[{\"startX\":1,\"startY\":0,\"startZ\":0,\"endX\":1,\"endY\":0,\"endZ\":0}]";
        using var jsonDoc = JsonDocument.Parse($"[{validPlanarSet},{zeroLengthSet}]");

        var (success, error, created) = SimulateRebarExecution(jsonDoc.RootElement);

        Assert.False(success);
        Assert.Equal(0, created);
        Assert.Contains("Rebar curve set 1 contains a zero-length segment at index 0.", error);
    }

    [Fact]
    public void N1_CreationPhaseFailure_TriggersRevitTransactionRollback_ZeroRebarsCommitted()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("create_rebar", "auto", false);
        using var cts = new CancellationTokenSource();
        var createdIds = new List<long>();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) =>
            {
                createdIds.Add(1001);
                throw new InvalidOperationException("Failed creating rebar for curve set 1: Host bounds exceeded.");
            });

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Equal(0, ctx.AssimilateGroupCount);
        Assert.Contains("Failed creating rebar for curve set 1", result.Message);
    }

    [Fact]
    public void N2_DoubleFault_ActionThrows_CanvasRollbackFails_ReturnsRolledBackFalse()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvas = new TrackingCanvasScope
        {
            ReturnCleanOnRollback = false,
            RollbackErrors = new List<string> { "Port unbind error", "Stale wire lock" }
        };
        var req = new ExecuteRequest("fail_action", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("Database constraint violation"),
            canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.True(canvas.RollbackCalled);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Contains("Database constraint violation", result.Message);
        Assert.Contains("Additionally, canvas rollback failed: Port unbind error; Stale wire lock", result.Message);
    }

    [Fact]
    public void N2_DoubleFault_ActionThrows_CanvasRollbackThrows_TrappedSafelyWithRolledBackFalse()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvas = new TrackingCanvasScope { ShouldThrowOnRollback = true };
        var req = new ExecuteRequest("fail_action", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("Geometry kernel fault"),
            canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.True(canvas.RollbackCalled);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Contains("Geometry kernel fault", result.Message);
        Assert.Contains("Canvas rollback threw InvalidOperationException: Simulated unexpected exception", result.Message);
    }

    [Fact]
    public void N2_DoubleFault_DryRun_CanvasRollbackFails_ReturnsRolledBackFalse()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvas = new TrackingCanvasScope
        {
            ReturnCleanOnRollback = false,
            RollbackErrors = new List<string> { "Corrupted undo stack" }
        };
        var req = new ExecuteRequest("dry_run_fault", "auto", true);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "dry-run-value",
            canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Contains("Canvas rollback failed with 1 error(s): Corrupted undo stack", result.Message);
    }

    [Fact]
    public void N2_DoubleFault_DryRun_CanvasRollbackThrows_ReturnsRolledBackFalse()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvas = new TrackingCanvasScope { ShouldThrowOnRollback = true };
        var req = new ExecuteRequest("dry_run_fault_throw", "auto", true);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "preview",
            canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Contains("Canvas rollback threw InvalidOperationException", result.Message);
    }

    [Fact]
    public void N2_DoubleFault_Cancellation_CanvasRollbackFails_ReturnsRolledBackFalse()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvas = new TrackingCanvasScope
        {
            ReturnCleanOnRollback = false,
            RollbackErrors = new List<string> { "Canvas memory leak" }
        };
        var req = new ExecuteRequest("cancel_fault", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (ct, _) =>
            {
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
                return null;
            },
            canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.True(canvas.RollbackCalled);
        Assert.Contains("Execution was cancelled", result.Message);
        Assert.Contains("Additionally, canvas rollback failed: Canvas memory leak", result.Message);
    }

    private static (bool Success, string Error, int CreatedCount) SimulateRebarExecution(JsonElement root)
    {
        const double scale = 3.280839895013123;
        var validatedSets = new List<(XYZ Normal, List<Curve> Curves)>();

        for (int setIndex = 0; setIndex < root.GetArrayLength(); setIndex++)
        {
            var rawItem = root[setIndex];
            var segments = new List<Curve>();
            int segIdx = 0;
            foreach (var seg in rawItem.EnumerateArray())
            {
                double sx = seg.GetProperty("startX").GetDouble() * scale, sy = seg.GetProperty("startY").GetDouble() * scale, sz = seg.GetProperty("startZ").GetDouble() * scale;
                double ex = seg.GetProperty("endX").GetDouble() * scale, ey = seg.GetProperty("endY").GetDouble() * scale, ez = seg.GetProperty("endZ").GetDouble() * scale;
                var p1 = new XYZ(sx, sy, sz);
                var p2 = new XYZ(ex, ey, ez);
                if (p1.DistanceTo(p2) < 0.001)
                    return (false, $"Rebar curve set {setIndex} contains a zero-length segment at index {segIdx}.", 0);
                segments.Add(Line.CreateBound(p1, p2));
                segIdx++;
            }
            for (int i = 0; i < segments.Count - 1; i++)
            {
                if (segments[i].GetEndPoint(1).DistanceTo(segments[i + 1].GetEndPoint(0)) > 0.01)
                    return (false, $"Rebar curve set {setIndex} is disconnected between segment {i} and {i + 1}.", 0);
            }
            var tangents = new List<XYZ>();
            foreach (var s in segments)
            {
                var dir = s.GetEndPoint(1) - s.GetEndPoint(0);
                if (dir.GetLength() > 0.0001) tangents.Add(dir.Normalize());
            }
            var normal = ComputeCurveNormal(tangents);
            if (!CheckCoplanar(segments, tangents, normal))
                return (false, $"Rebar curve set {setIndex} is non-planar; all segments must lie in a single plane.", 0);
            validatedSets.Add((normal, segments));
        }
        return (true, string.Empty, validatedSets.Count);
    }

    private static XYZ ComputeCurveNormal(List<XYZ> tangents)
    {
        for (int i = 0; i < tangents.Count - 1; i++)
        {
            for (int j = i + 1; j < tangents.Count; j++)
            {
                var cross = tangents[i].CrossProduct(tangents[j]);
                if (cross.GetLength() > 0.001) return cross.Normalize();
            }
        }
        var p = tangents[0];
        var refAxis = Math.Abs(p.Z) < 0.85 ? new XYZ(0, 0, 1) : new XYZ(1, 0, 0);
        return p.CrossProduct(refAxis).Normalize();
    }

    private static bool CheckCoplanar(List<Curve> segments, List<XYZ> tangents, XYZ normal)
    {
        foreach (var t in tangents)
        {
            if (Math.Abs(t.DotProduct(normal)) > 0.01) return false;
        }
        if (segments.Count > 0)
        {
            var orig = segments[0].GetEndPoint(0);
            foreach (var s in segments)
            {
                if (Math.Abs((s.GetEndPoint(0) - orig).DotProduct(normal)) > 0.01 ||
                    Math.Abs((s.GetEndPoint(1) - orig).DotProduct(normal)) > 0.01) return false;
            }
        }
        return true;
    }
}
