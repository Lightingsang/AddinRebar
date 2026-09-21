using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPExcel.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using Xunit;

namespace HPExcel.Mcp.Server.Tests;

/// <summary>
///     Adversarial challenge tests for Excel seed tools and ExecuteCodeService IPC round-trip:
///     1. Cancellation propagation across named pipe: triggers TryCancelInRevit and FakeRevitExecutor.Cancel().
///     2. Timeout clamping: low bound (<=5s) and upper bound (600s).
///     3. Input validation: empty code, whitespace code, invalid transaction modes, invalid args types, oversized payload.
///     4. Bridge error mapping and error responses.
/// </summary>
public sealed class ExcelSeedToolsRoundTripAdversarialTests : IAsyncLifetime
{
    private const string DisabledText =
        "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPExcel MCP Bridge window.";

    private readonly string _pipeName = "hpexcel-adv-trip-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new();
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };

    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ExecuteCodeService _execute = null!;
    private ResultFormatter _formatter = null!;
    private IOptions<BridgeOptions> _options = null!;

    public ValueTask InitializeAsync()
    {
        var dispatcher = new RequestDispatcher(
            _executor,
            _settings,
            "2026",
            "Excel",
            DisabledText);

        _listener = new PipeListener(_pipeName, dispatcher);
        _listener.Start();

        _options = Options.Create(new BridgeOptions
        {
            HostId = "excel",
            HostVersion = 2026,
            PipeName = _pipeName,
            ConnectTimeoutMs = 3000,
            PingIntervalSeconds = 60
        });

        _client = new RevitBridgeClient(_options, NullLogger<RevitBridgeClient>.Instance, ExcelHostProfile.Instance);
        _formatter = new ResultFormatter();
        _execute = new ExecuteCodeService(_client, _formatter, _options);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _listener.StopAsync();
        _listener.Dispose();
    }

    private static string TextOf(CallToolResult result) => ((TextContentBlock)result.Content[0]).Text;

    #region 1. Cancellation Token Propagation

    [Fact]
    public async Task Cancellation_DuringExecution_PropagatesCancelAcrossPipe_AndCallsExecutorCancel()
    {
        // Setup executor with multiple steps and delay so we can cancel mid-execution
        _executor.ProgressSteps = 10;
        _executor.ProgressDelayMs = 200;

        using var cts = new CancellationTokenSource();

        // Cancel after 100ms (during step 1)
        cts.CancelAfter(100);

        // ExecuteAsync should throw OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _execute.ExecuteAsync(
                code: "return 1;",
                transaction: TransactionModes.None,
                dryRun: false,
                timeoutSeconds: 30,
                label: "cancel_test",
                args: null,
                progress: null,
                cancellationToken: cts.Token);
        });

        // Allow pipe message for TryCancelInRevit to arrive at the listener
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < deadline && _executor.CancelCalls == 0)
        {
            await Task.Delay(50);
        }

        // FakeRevitExecutor.Cancel() should have been called via excel.cancel
        Assert.True(_executor.CancelCalls >= 1, "Cancel() was not called on the executor when client cancelled.");
    }

    #endregion

    #region 2. Timeout Clamping Bounds

    [Theory]
    [InlineData(-10, 5)]
    [InlineData(0, 5)]
    [InlineData(1, 5)]
    [InlineData(4, 5)]
    [InlineData(5, 5)]
    [InlineData(30, 30)]
    [InlineData(600, 600)]
    [InlineData(601, 600)]
    [InlineData(9999, 600)]
    public async Task TimeoutClamping_EnforcesMin5AndMax600(int requestedTimeout, int expectedClamped)
    {
        var result = await _execute.ExecuteAsync(
            code: "return 42;",
            transaction: TransactionModes.None,
            dryRun: false,
            timeoutSeconds: requestedTimeout,
            label: "timeout_test",
            args: null,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.NotNull(_executor.LastExecuteRequest);
        Assert.Equal(expectedClamped, _executor.LastExecuteRequest.TimeoutSeconds);
    }

    #endregion

    #region 3. Input Validation Without Hitting Pipe

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public async Task EmptyOrWhitespaceCode_ReturnsErrorWithoutDispatchingToBridge(string emptyCode)
    {
        var requestBefore = _executor.LastExecuteRequest;

        var result = await _execute.ExecuteAsync(
            code: emptyCode,
            transaction: TransactionModes.None,
            dryRun: false,
            timeoutSeconds: 15,
            label: "empty_test",
            args: null,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("code is empty", text);
        // Ensure request was never dispatched down the pipe
        Assert.Same(requestBefore, _executor.LastExecuteRequest);
    }

    [Fact]
    public async Task InvalidTransactionMode_ReturnsErrorWithoutDispatchingToBridge()
    {
        var requestBefore = _executor.LastExecuteRequest;

        var result = await _execute.ExecuteAsync(
            code: "return 1;",
            transaction: "unsupported_mode",
            dryRun: false,
            timeoutSeconds: 15,
            label: "invalid_tx",
            args: null,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("transaction must be one of", text);
        Assert.Same(requestBefore, _executor.LastExecuteRequest);
    }

    [Theory]
    [InlineData("auto", TransactionModes.Auto)]
    [InlineData("AUTO", TransactionModes.Auto)]
    [InlineData("manual", TransactionModes.Manual)]
    [InlineData("MANUAL", TransactionModes.Manual)]
    [InlineData("none", TransactionModes.None)]
    [InlineData("None", TransactionModes.None)]
    public async Task ValidTransactionModes_AreNormalizedCorrectly(string inputTx, string expectedTx)
    {
        var result = await _execute.ExecuteAsync(
            code: "return 1;",
            transaction: inputTx,
            dryRun: false,
            timeoutSeconds: 15,
            label: "tx_norm",
            args: null,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(expectedTx, _executor.LastExecuteRequest!.Transaction);
    }

    [Fact]
    public async Task InvalidArgsType_ArrayInsteadOfObject_ReturnsErrorWithoutDispatching()
    {
        var requestBefore = _executor.LastExecuteRequest;
        var arrayArgs = JsonSerializer.SerializeToElement(new[] { "item1", "item2" });

        var result = await _execute.ExecuteAsync(
            code: "return 1;",
            transaction: TransactionModes.None,
            dryRun: false,
            timeoutSeconds: 15,
            label: "bad_args",
            args: arrayArgs,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("args must be a JSON object", text);
        Assert.Same(requestBefore, _executor.LastExecuteRequest);
    }

    [Fact]
    public async Task InvalidArgsType_NumberInsteadOfObject_ReturnsErrorWithoutDispatching()
    {
        var numArgs = JsonSerializer.SerializeToElement(12345);

        var result = await _execute.ExecuteAsync(
            code: "return 1;",
            transaction: TransactionModes.None,
            dryRun: false,
            timeoutSeconds: 15,
            label: "bad_args_num",
            args: numArgs,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("args must be a JSON object", text);
    }

    [Fact]
    public async Task OversizedSourceCode_ExceedingMaxSourceBytes_ReturnsError()
    {
        var hugeCode = "// padding\n" + new string('x', 600_000);

        var result = await _execute.ExecuteAsync(
            code: hugeCode,
            transaction: TransactionModes.None,
            dryRun: false,
            timeoutSeconds: 15,
            label: "huge_code",
            args: null,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("bytes; the limit is", text);
    }

    #endregion

    #region 4. Error Responses & Diagnostics Handling

    [Fact]
    public async Task BridgeExecutionError_ReturnsToolResultWithIsErrorTrue()
    {
        _executor.ExecuteHandler = _ => new ExecuteResult
        {
            IsError = true,
            Message = "Excel runtime error: worksheet 'Sheet99' not found",
            Diagnostics = new[] { new ScriptDiagnostic(12, 5, "EXCEL_ERR", "Unknown sheet") }
        };

        var result = await _execute.ExecuteAsync(
            code: "return 1;",
            transaction: TransactionModes.None,
            dryRun: false,
            timeoutSeconds: 15,
            label: "error_test",
            args: null,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("worksheet 'Sheet99' not found", text);
        Assert.Contains("EXCEL_ERR", text);
    }

    #endregion
}
