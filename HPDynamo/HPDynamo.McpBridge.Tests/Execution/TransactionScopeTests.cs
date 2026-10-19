namespace HPDynamo.McpBridge.Tests.Execution;

using System;
using System.Text.Json;
using System.Threading;
using HPDynamo.McpBridge.Execution;
using HPDynamo.McpBridge.Tests.Fakes;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

public sealed class TransactionScopeTests
{
    private static readonly BridgeSettings DefaultSettings = new() { ExecutionEnabled = true };

    [Fact]
    public void Execute_WhenNoDocumentOpen_ReturnsFailureImmediately()
    {
        var req = new ExecuteRequest("return 1;", "auto");
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.Execute(null!, req, DefaultSettings, null, cts, (_, _, _) => null);

        Assert.True(result.IsError);
        Assert.Contains("No document is open in Revit", result.Message);
    }

    [Fact]
    public void Execute_WhenDocumentReadOnlyAndTransactionAuto_ReturnsFailure()
    {
        var failure = RevitTransactionScope.ValidateState(
            mode: "auto",
            isReadOnly: true,
            isFamily: false,
            isModifiable: false,
            allowFamily: false);

        Assert.NotNull(failure);
        Assert.True(failure.IsError);
        Assert.Contains("document is read-only", failure.Message);
    }

    [Fact]
    public void Execute_WhenDocumentModifiable_ReturnsOpenTransactionError()
    {
        var failure = RevitTransactionScope.ValidateState(
            mode: "auto",
            isReadOnly: false,
            isFamily: false,
            isModifiable: true,
            allowFamily: false);

        Assert.NotNull(failure);
        Assert.True(failure.IsError);
        Assert.Contains("already has a transaction open", failure.Message);
    }

    [Fact]
    public void Execute_DryRunTrue_RollsBackTransactionGroup()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("return 42;", "auto", true);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, log) =>
            {
                log("Processing element");
                return 42;
            });

        Assert.False(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Equal(1, ctx.GroupStartCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Equal(0, ctx.AssimilateGroupCount);
        Assert.Contains(result.Logs, l => l.Contains("dryRun=true"));
    }

    [Fact]
    public void Execute_DryRunFalse_AssimilatesTransactionGroup()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("return 100;", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => 100);

        Assert.False(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Equal(1, ctx.CommitAutoTxCount);
        Assert.Equal(1, ctx.AssimilateGroupCount);
        Assert.Equal(0, ctx.RollbackGroupCount);
    }

    [Fact]
    public void Execute_OnActionException_RollsBackCleanlyAndSurfacesError()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("throw new InvalidOperationException();", "auto");
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("Geometry creation failed due to curve intersection."));

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("Geometry creation failed", result.Message);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
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
    }

    [Fact]
    public void Execute_ResultSerialization_SerializesComplexReturnValueToJsonElement()
    {
        var complexObject = new { Count = 5, Status = "Ok", Area = 125.5 };
        var json = RevitTransactionScope.SerializeValue(complexObject, out var valType);

        Assert.NotNull(json);
        Assert.NotNull(valType);
        Assert.Equal(JsonValueKind.Object, json.Value.ValueKind);
        Assert.Equal(5, json.Value.GetProperty("Count").GetInt32());
        Assert.Equal("Ok", json.Value.GetProperty("Status").GetString());
        Assert.Equal(125.5, json.Value.GetProperty("Area").GetDouble());
    }

    [Fact]
    public void Execute_OnPreCancelled_WhenActionDoesNotThrow_StillCancelsAndRollsBack()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("pre-cancelled", "auto");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "fast result that ignores cancellation token");

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("cancelled", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, ctx.CommitAutoTxCount);
        Assert.Equal(0, ctx.AssimilateGroupCount);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void Execute_WhenAutoTransactionRollbackThrows_StillRollsBackTransactionGroup()
    {
        using var ctx = new SimulatedTransactionContext();
        ctx.ThrowOnRollbackAutoTransaction = true;
        var req = new ExecuteRequest("fail-rollback", "auto");
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("Initial failure trigger"));

        Assert.True(result.IsError);
        Assert.Contains("Initial failure trigger", result.Message);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.False(ctx.IsGroupStarted);
    }
}
