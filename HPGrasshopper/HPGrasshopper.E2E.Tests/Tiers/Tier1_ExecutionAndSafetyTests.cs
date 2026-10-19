namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

public sealed class Tier1_ExecutionAndSafetyTests
{
    [Fact]
    public async Task TC_T1_EXC_01_PureCSharpExpressionExecution()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var req = new GrasshopperExecutionRequest("int a = 10; int b = 20; return a + b;");

        var result = await bridge.ExecuteAsync(req);
        Assert.True(result.Success);
        Assert.False(result.RolledBack);
    }

    [Fact]
    public void TC_T1_EXC_02_StandardGlobalsAccessibility()
    {
        var globals = GrasshopperE2EContracts.RequiredGlobals;
        Assert.Contains("doc", globals);
        Assert.Contains("canvas", globals);
        Assert.Contains("rhinoDoc", globals);
        Assert.Contains("revitDoc", globals);
        Assert.Contains("ct", globals);
        Assert.Contains("args", globals);
    }

    [Fact]
    public async Task TC_T1_EXC_03_ReadOnlyTransactionModeNone()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var req = new GrasshopperExecutionRequest("return doc.Name;", Transaction: "none");

        var result = await bridge.ExecuteAsync(req);
        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(0, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T1_EXC_04_AutoTransactionModeAuto()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var req = new GrasshopperExecutionRequest("Ledger.CreateElement();", Transaction: "auto", DryRun: false);

        var result = await bridge.ExecuteAsync(req);
        Assert.True(result.Success);
        Assert.False(result.RolledBack);
        Assert.NotEmpty(result.CreatedElementIds);
        Assert.Equal(1, bridge.GetCommittedElementCount());
    }

    [Fact]
    public void TC_T1_EXC_05_ScriptArgsParameterParsing()
    {
        var argsJson = "{\"Scale\": 2.5, \"Prefix\": \"GH_\", \"Count\": 10, \"Enabled\": true}";
        using var doc = JsonDocument.Parse(argsJson);
        var args = new ScriptArgs(doc.RootElement);

        Assert.True(args.Has("Scale"));
        Assert.Equal(2.5, args.Double("Scale"));
        Assert.Equal("GH_", args.Str("Prefix"));
        Assert.Equal("GH_", args.Require("Prefix"));
        Assert.Equal(10, args.Int("Count"));
        Assert.True(args.Bool("Enabled"));
    }

    [Fact]
    public async Task TC_T1_DLS_01_DryRunRevitTransactionGroupRollback()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var initialCommitted = bridge.GetCommittedElementCount();

        var req = new GrasshopperExecutionRequest("Ledger.CreateElement();", DryRun: true);
        var result = await bridge.ExecuteAsync(req);

        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(initialCommitted, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T1_DLS_02_DryRunCanvasUndoScopeCanvasRollback()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var initialComponents = bridge.ActiveCanvas.Components.Count;

        var req = new GrasshopperExecutionRequest("Ledger.CreateElement();", DryRun: true);
        var result = await bridge.ExecuteAsync(req);

        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(initialComponents, bridge.ActiveCanvas.Components.Count);
    }

    [Fact]
    public async Task TC_T1_DLS_03_CommitExecutionDualPersistence()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var req = new GrasshopperExecutionRequest("Ledger.CreateElement();", DryRun: false);

        var result = await bridge.ExecuteAsync(req);
        Assert.True(result.Success);
        Assert.False(result.RolledBack);
        Assert.Equal(1, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T1_DLS_04_ScriptExceptionRollbackSafety()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var initialCommitted = bridge.GetCommittedElementCount();

        var req = new GrasshopperExecutionRequest("throw new InvalidOperationException(\"Intentional failure\");");
        var result = await bridge.ExecuteAsync(req);

        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(initialCommitted, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T1_DLS_05_CancellationCompensatingRollback()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var req = new GrasshopperExecutionRequest("Ledger.CreateElement();");
        var result = await bridge.ExecuteAsync(req, cts.Token);

        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(-32003, result.ErrorCode);
    }
}
