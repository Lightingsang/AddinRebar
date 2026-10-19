namespace HPDynamo.McpBridge.Tests.Coordinator;

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Dynamo.ViewModels;
using HPDynamo.McpBridge.Execution;
using HPDynamo.McpBridge.Tests.Fakes;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

/// <summary>
///     Adversarial stress-test suite for CanvasGraphRunner decomposition and guardrails.
///     Empirically validates parameter edge cases, missing file paths, cancellation tokens,
///     and RevitTransactionScope rollback on exceptions.
/// </summary>
public sealed class CanvasGraphRunnerStressTests
{
    private static DynamoViewModel CreateUninitializedViewModel() =>
        (DynamoViewModel)RuntimeHelpers.GetUninitializedObject(typeof(DynamoViewModel));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t  ")]
    public void ExecuteDesignScript_InvalidCode_ThrowsArgumentException(string? invalidCode)
    {
        var runner = new CanvasGraphRunner();
        var vm = CreateUninitializedViewModel();

        Assert.Throws<ArgumentException>(() =>
            runner.ExecuteDesignScript(vm, invalidCode!, null, false, CancellationToken.None, _ => { }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("G:\\Definitely\\NonExistent\\Graph_12345.dyn")]
    public void RunDynFile_InvalidOrMissingFilePath_ThrowsFileNotFoundException(string? invalidPath)
    {
        var runner = new CanvasGraphRunner();
        var vm = CreateUninitializedViewModel();

        Assert.Throws<FileNotFoundException>(() =>
            runner.RunDynFile(vm, invalidPath!, null, false, CancellationToken.None, _ => { }));
    }

    [Fact]
    public void DecomposedPrivateHelperMethods_ExistWithExpectedSignatures()
    {
        var runnerType = typeof(CanvasGraphRunner);

        var loadWs = runnerType.GetMethod("LoadWorkspaceFromFile", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(loadWs);
        Assert.Equal(4, loadWs.GetParameters().Length);

        var placeNode = runnerType.GetMethod("PlaceCodeBlockNode", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(placeNode);
        Assert.Equal(4, placeNode.GetParameters().Length);

        var evalNode = runnerType.GetMethod("EvaluateDesignScriptNode", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(evalNode);
        Assert.Equal(5, evalNode.GetParameters().Length);

        var applyOverrides = runnerType.GetMethod("ApplyParameterOverrides", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(applyOverrides);
        Assert.Equal(3, applyOverrides.GetParameters().Length);

        var extractOutputs = runnerType.GetMethod("ExtractOutputs", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(extractOutputs);
        Assert.Equal(2, extractOutputs.GetParameters().Length);
    }

    [Fact]
    public void RevitTransactionScope_WhenExecuteDesignScriptThrowsArgumentException_RollsBackTransactions()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("", "auto");
        using var cts = new CancellationTokenSource();
        var runner = new CanvasGraphRunner();
        var vm = CreateUninitializedViewModel();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, new BridgeSettings(), null, cts,
            (linkedToken, log) =>
            {
                return runner.ExecuteDesignScript(vm, "", null, false, linkedToken, log);
            });

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("ArgumentException", result.Message);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void RevitTransactionScope_WhenRunDynFileThrowsFileNotFound_RollsBackTransactions()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("test.dyn", "auto");
        using var cts = new CancellationTokenSource();
        var runner = new CanvasGraphRunner();
        var vm = CreateUninitializedViewModel();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, new BridgeSettings(), null, cts,
            (linkedToken, log) =>
            {
                return runner.RunDynFile(vm, "missing.dyn", null, false, linkedToken, log);
            });

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("FileNotFoundException", result.Message);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void RevitTransactionScope_WhenRunActiveGraphThrowsArgumentNull_RollsBackTransactions()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("active", "auto");
        using var cts = new CancellationTokenSource();
        var runner = new CanvasGraphRunner();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, new BridgeSettings(), null, cts,
            (linkedToken, log) =>
            {
                return runner.RunActiveGraph(null!, false, linkedToken, log);
            });

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("ArgumentNullException", result.Message);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void RevitTransactionScope_WhenCancellationExceptionThrown_RollsBackWithoutTimeoutFlag()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("code", "auto");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, new BridgeSettings(), null, cts,
            (linkedToken, _) =>
            {
                throw new OperationCanceledException("Dynamo active graph evaluation was cancelled.", linkedToken);
            });

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.False(result.TimedOut);
        Assert.Contains("cancelled", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }
}
