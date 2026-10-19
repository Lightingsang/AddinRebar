namespace HPGrasshopper.E2E.Tests.Tiers;

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.E2E.Tests.Contracts;
using HPGrasshopper.E2E.Tests.Harness;
using Xunit;

public sealed class Tier2_ExecutionAndSafetyBoundariesTests
{
    [Fact]
    public void TC_T2_EXC_01_CSharpSyntaxCompilationErrorNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge();
        var code = "int a = ;";

        var quality = bridge.ValidateScript(code);
        Assert.NotNull(quality);
    }

    [Fact]
    public async Task TC_T2_EXC_02_UnhandledRuntimeExceptionNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var req = new GrasshopperExecutionRequest("throw new InvalidOperationException(\"Simulation engine failure\");");

        var result = await bridge.ExecuteAsync(req);
        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(-32000, result.ErrorCode);
    }

    [Fact]
    public void TC_T2_EXC_03_EmptyAndWhitespaceOnlyCodeNegativeTest()
    {
        var bridge = new GrasshopperE2EBridge();
        var quality1 = bridge.ValidateScript("");
        var quality2 = bridge.ValidateScript("   \r\n\t  ");

        Assert.Equal(0, quality1.LineCount);
        Assert.Equal(0, quality2.LineCount);
    }

    [Fact]
    public async Task TC_T2_EXC_04_ExecutionTimeoutExceededCancellation()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var req = new GrasshopperExecutionRequest("int x = 0; while (true) { x++; }");
        var result = await bridge.ExecuteAsync(req, cts.Token);

        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(-32003, result.ErrorCode);
    }

    [Fact]
    public void TC_T2_EXC_05_LargeScriptLineCountLimit301Lines()
    {
        var bridge = new GrasshopperE2EBridge();
        var sb = new StringBuilder();
        for (int i = 0; i < 301; i++) sb.AppendLine($"int x{i} = {i};");

        var quality = bridge.ValidateScript(sb.ToString());
        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("300") || e.Contains("Q-B3"));
    }

    [Fact]
    public async Task TC_T2_DLS_01_PartialScriptFailureRollbackVerification()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var initial = bridge.GetCommittedElementCount();

        var req = new GrasshopperExecutionRequest("Ledger.CreateElement(); throw new InvalidOperationException();");
        var result = await bridge.ExecuteAsync(req);

        Assert.False(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(initial, bridge.GetCommittedElementCount());
    }

    [Fact]
    public void TC_T2_DLS_02_IdempotentDoubleRollback()
    {
        var bridge = new GrasshopperE2EBridge();
        bridge.Ledger.BeginTransaction();
        bridge.Ledger.CreateElement();
        var rolledBack1 = bridge.Ledger.RollbackTransaction();
        var rolledBack2 = bridge.Ledger.RollbackTransaction();

        Assert.Single(rolledBack1);
        Assert.Empty(rolledBack2);
        Assert.Equal(0, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T2_DLS_03_DisposeWithoutCommitTriggersRollback()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var req = new GrasshopperExecutionRequest("Ledger.CreateElement();", DryRun: true);

        var result = await bridge.ExecuteAsync(req);
        Assert.True(result.RolledBack);
        Assert.Equal(0, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T2_DLS_04_ReadOnlyDocumentTransactionPrevention()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var mutatingScript = "Ledger.CreateElement();";
        var request = new GrasshopperExecutionRequest(mutatingScript, Transaction: "none", DryRun: true);

        var result = await bridge.ExecuteAsync(request);
        Assert.True(result.RolledBack);
        Assert.Equal(0, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T2_DLS_05_ZeroDocumentChangeCountOnDryRun()
    {
        var bridge = new GrasshopperE2EBridge { IsExecutionEnabled = true };
        var initial = bridge.GetCommittedElementCount();

        var req = new GrasshopperExecutionRequest("Ledger.CreateElement(); Ledger.CreateElement();", DryRun: true);
        var res = await bridge.ExecuteAsync(req);

        Assert.True(res.Success);
        Assert.True(res.RolledBack);
        Assert.Equal(initial, bridge.GetCommittedElementCount());
    }
}
