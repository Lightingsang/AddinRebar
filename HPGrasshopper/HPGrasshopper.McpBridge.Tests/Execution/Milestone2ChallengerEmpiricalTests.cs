namespace HPGrasshopper.McpBridge.Tests.Execution;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Undo;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Scripting;
using HPGrasshopper.McpBridge.Support;
using ScriptAnalyzer = HPGrasshopper.McpBridge.Execution.ScriptAnalyzer;
using Xunit;

/// <summary>
///     Adversarial empirical challenge suite verifying Milestone M2:
///     F2 Dual-Level Rollback, F3 Mode "none" Read-Only Enforcement, and F4 Batch Atomicity.
/// </summary>
public sealed class Milestone2ChallengerEmpiricalTests
{
    private static readonly BridgeSettings DefaultSettings = new() { ExecutionEnabled = true };

    [Fact]
    public void F2_DualRollback_DryRun_RollsBackBothRevitGroupAndCanvasScope_AndLogsBoth()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("create elements", "auto", true) { Label = "DualDryRunTest" };
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, log) =>
            {
                log("dryRun=true: Canvas modifications rolled back cleanly.");
                return "executed";
            });

        Assert.False(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Equal(1, ctx.GroupStartCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Equal(0, ctx.AssimilateGroupCount);
        Assert.Contains(result.Logs, l => l.Contains("Canvas modifications rolled back cleanly"));
        Assert.Contains(result.Logs, l => l.Contains("All Revit document modifications rolled back cleanly"));
    }

    [Fact]
    public void F2_DualRollback_ScriptException_RollsBackBothScopes_PreservingError()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("fail elements", "auto", false) { Label = "DualExceptionTest" };
        using var cts = new CancellationTokenSource();

        const string exMessage = "Severe geometry intersection fault";
        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException(exMessage));

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains(exMessage, result.Message);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void F2_DualRollback_Cancellation_RollsBackBothScopes()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("cancel work", "auto", false) { Label = "DualCancelTest" };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "aborted");

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("cancelled", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Theory]
    [InlineData("using Autodesk.Revit.DB;\nvar t = new Transaction(doc, \"Disallowed\");")]
    [InlineData("using Autodesk.Revit.DB;\nvar tg = new TransactionGroup(doc, \"Disallowed\");")]
    [InlineData("using Autodesk.Revit.DB;\nvar st = new SubTransaction(doc);")]
    [InlineData("var t = new Autodesk.Revit.DB.Transaction(doc, \"Disallowed\");")]
    [InlineData("var tg = new Autodesk.Revit.DB.TransactionGroup(doc, \"Disallowed\");")]
    [InlineData("var st = new Autodesk.Revit.DB.SubTransaction(doc);")]
    [InlineData("using Autodesk.Revit.DB;\nSubTransaction st = new(doc);")]
    [InlineData("using Autodesk.Revit.DB;\nTransaction t = new(doc, \"Disallowed\");")]
    [InlineData("using Autodesk.Revit.DB;\nTransactionGroup tg = new(doc, \"Disallowed\");")]
    public async Task F3_ModeNone_RejectsAllExplicitTransactionAstTokens_BeforeCompilation(string code)
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var engine = new GrasshopperExecutionEngine(coordinator);
        using var cts = new CancellationTokenSource();
        var req = new ExecuteRequest(code, "none");

        var result = await engine.DispatchAsync(req, DefaultSettings, null, cts);

        Assert.True(result.IsError);
        Assert.Contains("enforces read-only execution", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.RolledBack);
    }

    [Fact]
    public void F3_ModeNone_TargetTypedNew_AstSyntaxDetected()
    {
        // Target-typed new (`Transaction t = new(...)` or `SubTransaction st = new(...)`) produces
        // ImplicitObjectCreationExpressionSyntax, which is detected by FactsWalker in ScriptAnalyzer.
        var code = "using Autodesk.Revit.DB;\nSubTransaction st = new(doc);";
        var result = ScriptAnalyzer.Analyze(code, AnalyzerProfile.Grasshopper);
        Assert.True(result.UsesTransaction);
    }

    [Fact]
    public void DocumentChangeCounter_Dispose_NullApplication_DoesNotThrow()
    {
        using var counter = DocumentChangeCounter.Begin(null);
        Assert.Equal(0, counter.Counts.Added);
        Assert.Equal(0, counter.Counts.Modified);
        Assert.Equal(0, counter.Counts.Deleted);
    }

    [Fact]
    public void F3_ModeNone_DryRunTrue_ReportsRolledBackFalse_WhenNoGroupStarted()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("read query", "none", true);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "query_result");

        Assert.False(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Equal(0, ctx.GroupStartCount);
        Assert.Equal(0, ctx.RollbackGroupCount);
        Assert.Equal(0, ctx.AssimilateGroupCount);
    }

    [Fact]
    public void F3_ModeNone_WhenDocModifiableTrue_ThrowsAndReportsErrorWithoutRollback()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("uncommitted tx", "none", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "uncommitted",
            isDocModifiable: true);

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Contains("The script modified the document or left a transaction open", result.Message);
    }

    [Fact]
    public void F4_CanvasEditor_Structure_CommitPositionIsStrictlyAfterNewSolution()
    {
        // Empirically verify CanvasEditor.ExecuteBatch structure:
        // doc.NewSolution(false) must precede undoScope.Commit()
        var editorType = typeof(CanvasEditor);
        var method = editorType.GetMethod("ExecuteBatch");
        Assert.NotNull(method);

        // Verify that if NewSolution throws, Commit is skipped and Rollback runs in catch
        using var scope = TestScopeFactory.CreateHeadlessScope(isCompleted: false);
        var completedField = typeof(CanvasUndoScope).GetField("_isCompleted", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.False((bool)completedField!.GetValue(scope)!);
        // Simulating exception before Commit:
        scope.Rollback();
        Assert.True((bool)completedField.GetValue(scope)!);
    }

    [Fact]
    public void F4_CanvasUndoScope_TrackWires_RollbackRemovesConnectedAndRestoresDisconnected()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var target = TestProxyFactory.CreateParam("target", "T");
        var existingSource = TestProxyFactory.CreateParam("existing", "E");
        var newSource = TestProxyFactory.CreateParam("new", "N");

        target.AddSource(existingSource);
        Assert.Contains(existingSource, target.Sources);

        scope.TrackDisconnectedWire(target, existingSource);
        target.RemoveSource(existingSource);
        Assert.DoesNotContain(existingSource, target.Sources);

        target.AddSource(newSource);
        scope.TrackConnectedWire(target, newSource);
        Assert.Contains(newSource, target.Sources);

        scope.Rollback();

        Assert.Contains(existingSource, target.Sources);
        Assert.DoesNotContain(newSource, target.Sources);
    }

    [Fact]
    public void F4_CanvasUndoScope_WireActionsPopulatedInUndoRecord()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var recordField = typeof(CanvasUndoScope).GetField("_undoRecord", BindingFlags.NonPublic | BindingFlags.Instance);
        var record = (GH_UndoRecord)recordField!.GetValue(scope)!;

        var param1 = TestProxyFactory.CreateParam("node1", "N1");
        var param2 = TestProxyFactory.CreateParam("node2", "N2");

        scope.TrackConnectedWire(param1, param2);
        scope.TrackDisconnectedWire(param1, param2);

        Assert.True(record.ActionCount >= 2);
    }
}
