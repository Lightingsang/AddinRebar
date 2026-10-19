namespace HPGrasshopper.McpBridge.Tests.Execution;

using System;
using System.Text.Json;
using System.Threading;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

public sealed class RevitTransactionScopeTests
{
    private static readonly BridgeSettings DefaultSettings = new() { ExecutionEnabled = true };

    [Fact]
    public void Execute_WhenNoUiApp_ReturnsFailureImmediately()
    {
        var req = new ExecuteRequest("return 1;", "auto");
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.Execute(null, req, DefaultSettings, null, cts, (_, _, _) => null);

        Assert.True(result.IsError);
        Assert.Contains("No document is open in Revit", result.Message);
    }

    [Fact]
    public void Execute_ValidateState_ReadOnlyDoc_RejectsAutoAndManual()
    {
        var autoResult = RevitTransactionScope.ValidateState("auto", isReadOnly: true, isFamily: false, isModifiable: false, allowFamily: false);
        var manualResult = RevitTransactionScope.ValidateState("manual", isReadOnly: true, isFamily: false, isModifiable: false, allowFamily: false);

        Assert.NotNull(autoResult);
        Assert.True(autoResult.IsError);
        Assert.Contains("read-only", autoResult.Message);

        Assert.NotNull(manualResult);
        Assert.True(manualResult.IsError);
        Assert.Contains("read-only", manualResult.Message);
    }

    [Fact]
    public void Execute_ValidateState_ReadOnlyDoc_AllowsNone()
    {
        var result = RevitTransactionScope.ValidateState("none", isReadOnly: true, isFamily: false, isModifiable: false, allowFamily: false);
        Assert.Null(result);
    }

    [Fact]
    public void Execute_ValidateState_ModifiableDoc_Rejects()
    {
        var result = RevitTransactionScope.ValidateState("auto", isReadOnly: false, isFamily: false, isModifiable: true, allowFamily: false);

        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Contains("already has a transaction open", result.Message);
    }

    [Fact]
    public void Execute_ValidateState_FamilyDoc_Disallowed_Rejects()
    {
        var result = RevitTransactionScope.ValidateState("auto", isReadOnly: false, isFamily: true, isModifiable: false, allowFamily: false);

        Assert.NotNull(result);
        Assert.True(result.IsError);
        Assert.Contains("family", result.Message);
    }

    [Fact]
    public void Execute_ValidateState_FamilyDoc_Allowed_Passes()
    {
        var result = RevitTransactionScope.ValidateState("auto", isReadOnly: false, isFamily: true, isModifiable: false, allowFamily: true);
        Assert.Null(result);
    }

    [Fact]
    public void Execute_DryRunFalse_AssimilatesGroupAndCommitsAutoTx()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("return 100;", "auto", false) { Label = "PlaceColumn" };
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => 100);

        Assert.False(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Equal(1, ctx.GroupStartCount);
        Assert.Equal("MCP Grasshopper: PlaceColumn", ctx.LastGroupLabel);
        Assert.Equal(1, ctx.CommitAutoTxCount);
        Assert.Equal(1, ctx.AssimilateGroupCount);
        Assert.Equal(0, ctx.RollbackGroupCount);
        Assert.Equal(0, ctx.RollbackAutoTxCount);
    }

    [Fact]
    public void Execute_DryRunTrue_RollsBackGroupAndReturnsSuccess()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("return 42;", "auto", true) { Label = "PreviewBeam" };
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, log) =>
            {
                log("DryRun test step");
                return 42;
            });

        Assert.False(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Equal(1, ctx.GroupStartCount);
        Assert.Equal(1, ctx.CommitAutoTxCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Equal(0, ctx.AssimilateGroupCount);
        Assert.Contains(result.Logs, l => l.Contains("dryRun=true: All Revit document modifications rolled back cleanly."));
    }

    [Fact]
    public void Execute_ActionThrowsException_RollsBackGroupAndAutoTx()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("fail", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("Failed to construct Wall"));

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("Failed to construct Wall", result.Message);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Equal(0, ctx.AssimilateGroupCount);
    }

    [Fact]
    public void Execute_OnCancellation_CancelsAndRollsBack()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("cancel", "auto");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (ct, _) =>
            {
                ct.ThrowIfCancellationRequested();
                return null;
            });

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("cancelled", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void Execute_AutoTxCommitFailure_RollsBackAndReportsError()
    {
        using var ctx = new SimulatedTransactionContext { AutoTransactionCommitResult = false };
        var req = new ExecuteRequest("reject", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "done");

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("Revit rejected the change", result.Message);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void Execute_AutoTxRollbackThrows_StillRollsBackTransactionGroup()
    {
        using var ctx = new SimulatedTransactionContext { ThrowOnRollbackAutoTransaction = true };
        var req = new ExecuteRequest("fail", "auto");
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("First error"));

        Assert.True(result.IsError);
        Assert.Contains("First error", result.Message);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void Execute_TransactionModeNone_DoesNotStartGroupOrAutoTx()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("query", "none", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "queried",
            startAutoTransaction: false);

        Assert.False(result.IsError);
        Assert.Equal(0, ctx.GroupStartCount);
        Assert.Equal(0, ctx.AutoTxStartCount);
        Assert.Equal(0, ctx.CommitAutoTxCount);
        // Note: FinalizeTransactions calls AssimilateGroup on the context unconditionally; in RevitTransactionContext _group is null so it's a no-op
        Assert.Equal(1, ctx.AssimilateGroupCount);
    }

    [Fact]
    public void Execute_TransactionModeManual_StartsGroupWithoutAutoTx()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("manual", "manual", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "manual ok",
            startAutoTransaction: false);

        Assert.False(result.IsError);
        Assert.Equal(1, ctx.GroupStartCount);
        Assert.Equal(0, ctx.AutoTxStartCount);
        Assert.Equal(1, ctx.AssimilateGroupCount);
    }

    [Fact]
    public void Execute_ResultSerialization_SerializesComplexObjects()
    {
        var complexObject = new { Name = "Column", Height = 3500.0, Success = true };
        var json = RevitTransactionScope.SerializeValue(complexObject, out var valType);

        Assert.NotNull(json);
        Assert.NotNull(valType);
        Assert.Equal(JsonValueKind.Object, json.Value.ValueKind);
        Assert.Equal("Column", json.Value.GetProperty("Name").GetString());
        Assert.Equal(3500.0, json.Value.GetProperty("Height").GetDouble());
        Assert.True(json.Value.GetProperty("Success").GetBoolean());
    }
}
