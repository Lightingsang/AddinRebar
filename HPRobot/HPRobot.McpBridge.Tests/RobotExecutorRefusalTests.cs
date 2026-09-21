using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HPRobot.McpBridge.Com;
using HPRobot.McpBridge.Host;
using HPRobot.McpBridge.Safety;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

namespace HPRobot.McpBridge.Tests;

public sealed class RobotExecutorRefusalTests : IDisposable
{
    private readonly string _tempDir;
    private readonly RobotAttachment _attachment;
    private readonly RobotStaWorker _staWorker;
    private readonly RobotSafetyGuard _guard;
    private readonly RobotSnapshotManager _snapshots;
    private readonly RobotBridgeExecutor _executor;

    public RobotExecutorRefusalTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "HPRobot_ExecRefusal_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _attachment = new RobotAttachment();
        _staWorker = new RobotStaWorker();
        _staWorker.Start();
        _guard = new RobotSafetyGuard();
        _snapshots = new RobotSnapshotManager(Path.Combine(_tempDir, "snaps"));

        _executor = new RobotBridgeExecutor(
            _attachment,
            _staWorker,
            _guard,
            _snapshots);
    }

    public void Dispose()
    {
        _executor.Dispose();
        _staWorker.Dispose();
        _attachment.Dispose();

        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    private Task<ExecuteResult> Execute(string code, bool dryRun = false, string transaction = TransactionModes.Auto) =>
        _executor.ExecuteAsync(new ExecuteRequest(code, transaction, dryRun, 10, "test"), null, CancellationToken.None);

    [Fact]
    public async Task ExecuteAsync_WhenExecutionDisabled_ThrowsBridgeRequestException_WithCode32001()
    {
        _guard.IsExecutionEnabled = false;

        var ex = await Assert.ThrowsAsync<BridgeRequestException>(() =>
            Execute("var count = structure.Nodes.Count; return count;"));

        Assert.Equal(BridgeErrorCode.ExecutionDisabled, ex.Code);
        Assert.Equal(-32001, ex.Code);
        Assert.Equal(RobotSafetyGuard.ExecutionDisabledMessage, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHeavyDisabled_TierDScriptThrowsBridgeRequestException_WithCode32001()
    {
        _guard.IsExecutionEnabled = true;
        _guard.IsHeavyOperationsEnabled = false;

        var ex = await Assert.ThrowsAsync<BridgeRequestException>(() =>
            Execute("structure.CalcEngine.Calculate();"));

        Assert.Equal(BridgeErrorCode.ExecutionDisabled, ex.Code);
        Assert.Equal(-32001, ex.Code);
        Assert.Equal(RobotSafetyGuard.HeavyOperationsDisabledMessage, ex.Message);
        Assert.Contains("Heavy operations", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHeavyDisabled_StructuralDeleteThrowsBridgeRequestException_WithCode32001()
    {
        _guard.IsExecutionEnabled = true;
        _guard.IsHeavyOperationsEnabled = false;

        var ex = await Assert.ThrowsAsync<BridgeRequestException>(() =>
            Execute("structure.Bars.Delete(1);"));

        Assert.Equal(BridgeErrorCode.ExecutionDisabled, ex.Code);
        Assert.Equal(-32001, ex.Code);
        Assert.Equal(RobotSafetyGuard.HeavyOperationsDisabledMessage, ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_DryRunTierRead_ReturnsStaticPreviewWithoutExecuting()
    {
        _guard.IsExecutionEnabled = true;
        _guard.IsHeavyOperationsEnabled = false;

        // TransactionModes.None ensures read-only tier is preserved
        var result = await Execute("var count = structure.Nodes.GetAll().Count;", dryRun: true, transaction: TransactionModes.None);

        Assert.False(result.IsError);
        Assert.NotNull(result.Message);
        Assert.Contains("Tier Read", result.Message);
        Assert.Contains("Static preview", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_DryRunTierDeleteHeavy_WhenHeavyEnabled_ReturnsStaticPreview()
    {
        _guard.IsExecutionEnabled = true;
        _guard.IsHeavyOperationsEnabled = true;

        // Valid RobotOM script that compiles and invokes Tier D member Delete
        var result = await Execute("structure.Bars.Delete(1);", dryRun: true);

        Assert.False(result.IsError);
        Assert.NotNull(result.Message);
        Assert.Contains("Tier DeleteHeavy", result.Message);
        Assert.Contains("Delete", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ScriptGuardViolation_ReturnsGuardDiagnosticBeforeTierOrExecution()
    {
        _guard.IsExecutionEnabled = true;

        var result = await Execute("System.Diagnostics.Process.Start(\"notepad.exe\");");

        Assert.True(result.IsError);
        Assert.Contains("violates Robot safety guard", result.Message);
        Assert.NotNull(result.Diagnostics);
        Assert.Contains(result.Diagnostics, d => d.Id == "GUARD");
    }

    [Fact]
    public async Task ExecuteAsync_ForbiddenDirectives_ReturnsGuardDiagnostic()
    {
        _guard.IsExecutionEnabled = true;

        var result = await Execute("#r \"forbidden.dll\"\nreturn 1;");

        Assert.True(result.IsError);
        Assert.Contains(result.Diagnostics, d => d.Id == "GUARD");
    }
}
