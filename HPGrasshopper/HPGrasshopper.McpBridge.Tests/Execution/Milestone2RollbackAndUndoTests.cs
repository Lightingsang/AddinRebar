namespace HPGrasshopper.McpBridge.Tests.Execution;

using System;
using System.Collections;
using System.Reflection;
using System.Text.Json;
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
using Xunit;

public sealed class Milestone2RollbackAndUndoTests
{
    private static readonly BridgeSettings DefaultSettings = new() { ExecutionEnabled = true };
    private readonly CanvasEditor _editor = new();

    [Fact]
    public void CanvasUndoScope_AddAction_AddsNativeUndoActionsToRecord()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var type = typeof(CanvasUndoScope);
        var recordField = type.GetField("_undoRecord", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(recordField);

        var record = (GH_UndoRecord?)recordField!.GetValue(scope);
        Assert.NotNull(record);

        var param = TestProxyFactory.CreateParam("param1", "P1");
        scope.TrackAddedObject(param.InstanceGuid);
        scope.TrackDisconnectedWire(param, TestProxyFactory.CreateParam("source", "S"));

        Assert.True(record.ActionCount > 0);
    }

    [Fact]
    public void CanvasUndoScope_TrackRemovedObject_StoresArchiveBytes()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var type = typeof(CanvasUndoScope);
        var removedField = type.GetField("_removedSnapshots", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(removedField);

        var param = TestProxyFactory.CreateParam("toRemove", "TR");
        scope.TrackRemovedObject(param);

        var list = (IList?)removedField!.GetValue(scope);
        Assert.NotNull(list);
        Assert.Single(list);

        var snapshot = list[0];
        Assert.NotNull(snapshot);
        var archiveProp = snapshot.GetType().GetProperty("ArchiveBytes");
        Assert.NotNull(archiveProp);
        var bytes = (byte[]?)archiveProp.GetValue(snapshot);
        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task DispatchAsync_TransactionNone_RejectsTransactionTokensBeforeExecution()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var engine = new GrasshopperExecutionEngine(coordinator);
        using var cts = new CancellationTokenSource();

        var codeWithTx = "using Autodesk.Revit.DB;\nvar t = new Transaction(doc, \"Disallowed\");";
        var request = new ExecuteRequest(codeWithTx, "none");

        var result = await engine.DispatchAsync(request, DefaultSettings, null, cts);

        Assert.True(result.IsError);
        Assert.Contains("enforces read-only execution", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.RolledBack);
    }

    [Fact]
    public async Task DispatchAsync_TransactionNone_RejectsTransactionGroupTokensBeforeExecution()
    {
        var coordinator = GrasshopperBridgeCoordinator.Instance;
        var engine = new GrasshopperExecutionEngine(coordinator);
        using var cts = new CancellationTokenSource();

        var codeWithTg = "using Autodesk.Revit.DB;\nvar tg = new TransactionGroup(doc, \"GroupDisallowed\");";
        var request = new ExecuteRequest(codeWithTg, "none");

        var result = await engine.DispatchAsync(request, DefaultSettings, null, cts);

        Assert.True(result.IsError);
        Assert.Contains("enforces read-only execution", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.RolledBack);
    }

    [Fact]
    public void PlayerRunner_Run_PreCanceledToken_ThrowsOperationCanceledException()
    {
        var runner = new PlayerRunner();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => runner.Run(string.Empty, null, null, true, cts.Token));
    }

    [Fact]
    public void CanvasEditor_ExecuteBatch_NullDocument_FailsCleanlyAndDoesNotCommit()
    {
        using var jsonDoc = JsonDocument.Parse("{\"operations\": []}");

        var result = _editor.ExecuteBatch(null, jsonDoc.RootElement, dryRun: true);

        Assert.NotNull(result);
        var json = JsonSerializer.Serialize(result);
        using var parsed = JsonDocument.Parse(json);
        Assert.False(parsed.RootElement.GetProperty("success").GetBoolean());
        Assert.Contains("No active Grasshopper document", parsed.RootElement.GetProperty("error").GetString());
    }
}
