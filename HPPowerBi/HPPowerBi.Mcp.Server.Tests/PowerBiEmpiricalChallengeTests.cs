using System.Diagnostics;
using System.Text.Json;
using HPPowerBi.Mcp.Server.Hosts;
using HPPowerBi.Mcp.Server.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using Xunit;

namespace HPPowerBi.Mcp.Server.Tests;

/// <summary>
///     Empirical challenge test suite covering:
///     1. Cloud REST Tools error envelopes (401/403, invalid GUIDs, empty query, unconfigured client)
///     2. Named pipe communication & timeout clamping (>600s clamped to 600s, <5s clamped to 5s)
///     3. CancellationToken cancellation propagation and powerbi.cancel signaling
///     4. Pipe broken mid-request and bridge disconnected error hints
///     5. End-to-end Stdio MCP server process survival (does not crash on 401/403 or unhandled exceptions)
/// </summary>
public sealed class PowerBiEmpiricalChallengeTests : IAsyncLifetime
{
    private readonly string _pipeName = "hppowerbi-mcp-challenge-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new FakeRevitExecutor();
    private readonly BridgeSettings _settings = new BridgeSettings { ExecutionEnabled = true };

    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ExecuteCodeService _execute = null!;
    private ResultFormatter _formatter = null!;
    private IOptions<BridgeOptions> _options = null!;

    public Func<long, JsonRpcEnvelope, Task<JsonRpcEnvelope?>>? CustomHandler { get; set; }
    public string? LastDispatchedMethod { get; private set; }
    public JsonElement? LastDispatchedParams { get; private set; }

    public ValueTask InitializeAsync()
    {
        var dispatcher = new RequestDispatcher(
            _executor,
            _settings,
            "2026",
            "Power BI",
            "Code execution disabled",
            customHandler: (id, req, writer, ct) =>
            {
                LastDispatchedMethod = req.Method;
                LastDispatchedParams = req.Params;
                if (CustomHandler != null)
                {
                    return CustomHandler(id, req);
                }
                return Task.FromResult<JsonRpcEnvelope?>(null);
            });

        _listener = new PipeListener(_pipeName, dispatcher);
        _listener.Start();

        _options = Options.Create(new BridgeOptions
        {
            HostId = "powerbi",
            HostVersion = 2026,
            PipeName = _pipeName,
            ConnectTimeoutMs = 3000,
            PingIntervalSeconds = 60
        });

        _client = new RevitBridgeClient(_options, NullLogger<RevitBridgeClient>.Instance, PowerBiHostProfile.Instance);
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

    #region 1. Cloud REST Tools Challenges

    [Fact]
    public async Task CloudWorkspaces_WhenBridgeReturns401Unauthorized_ThrowsMcpExceptionWithDetailedError()
    {
        // Simulate bridge returning 401 Unauthorized
        CustomHandler = (id, req) =>
        {
            var suffix = JsonRpcMethods.Suffix(req.Method ?? string.Empty);
            if (suffix == "cloud.workspaces")
            {
                return Task.FromResult<JsonRpcEnvelope?>(
                    JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, "Power BI API error (Unauthorized): 401 Unauthorized"));
            }
            return Task.FromResult<JsonRpcEnvelope?>(null);
        };

        var tool = new PowerBiCloudListWorkspacesTool(_client, _formatter);

        // Non-actionable code (-32000) causes ResultFormatter to surface it as an McpException
        var ex = await Assert.ThrowsAsync<McpException>(() => tool.ListWorkspacesAsync(TestContext.Current.CancellationToken));
        Assert.Contains("-32000", ex.Message);
        Assert.Contains("401 Unauthorized", ex.Message);
    }

    [Fact]
    public async Task CloudWorkspaces_WhenBridgeReturns403Forbidden_ThrowsMcpExceptionWithDetailedError()
    {
        // Simulate bridge returning 403 Forbidden
        CustomHandler = (id, req) =>
        {
            var suffix = JsonRpcMethods.Suffix(req.Method ?? string.Empty);
            if (suffix == "cloud.workspaces")
            {
                return Task.FromResult<JsonRpcEnvelope?>(
                    JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, "Power BI API error (Forbidden): 403 Access Denied to Workspace"));
            }
            return Task.FromResult<JsonRpcEnvelope?>(null);
        };

        var tool = new PowerBiCloudListWorkspacesTool(_client, _formatter);

        var ex = await Assert.ThrowsAsync<McpException>(() => tool.ListWorkspacesAsync(TestContext.Current.CancellationToken));
        Assert.Contains("-32000", ex.Message);
        Assert.Contains("403 Access Denied", ex.Message);
    }

    [Fact]
    public async Task CloudTriggerRefresh_WithInvalidGuids_BridgeReturnsFailureEnvelope_SurfacesInvalidRequest()
    {
        CustomHandler = (id, req) =>
        {
            var suffix = JsonRpcMethods.Suffix(req.Method ?? string.Empty);
            if (suffix == "cloud.refresh")
            {
                return Task.FromResult<JsonRpcEnvelope?>(
                    JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, "DatasetId 'not-a-valid-guid' is not a valid GUID format."));
            }
            return Task.FromResult<JsonRpcEnvelope?>(null);
        };

        var tool = new PowerBiCloudTriggerRefreshTool(_client, _formatter);

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            tool.TriggerRefreshAsync("not-a-valid-guid", "bad-workspace-guid", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("-32600", ex.Message);
        Assert.Contains("not a valid GUID format", ex.Message);
    }

    [Fact]
    public async Task CloudTriggerRefresh_WithInvalidGuids_BridgeReturnsApiNotFound_ReturnsResultWithFailureStatus()
    {
        CustomHandler = (id, req) =>
        {
            var suffix = JsonRpcMethods.Suffix(req.Method ?? string.Empty);
            if (suffix == "cloud.refresh")
            {
                return Task.FromResult<JsonRpcEnvelope?>(
                    JsonRpcEnvelope.Success(id, new
                    {
                        Success = false,
                        DatasetId = "invalid-guid",
                        RequestId = (string?)null,
                        Status = "NotFound",
                        ErrorMessage = "Power BI API error (NotFound): Dataset not found"
                    }));
            }
            return Task.FromResult<JsonRpcEnvelope?>(null);
        };

        var tool = new PowerBiCloudTriggerRefreshTool(_client, _formatter);
        var result = await tool.TriggerRefreshAsync("invalid-guid", cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var text = TextOf(result);
        Assert.Contains("NotFound", text);
        Assert.Contains("Dataset not found", text);
    }

    [Fact]
    public async Task CloudExecuteDax_WithEmptyQuery_BridgeRejectsWithInvalidRequest()
    {
        CustomHandler = (id, req) =>
        {
            var suffix = JsonRpcMethods.Suffix(req.Method ?? string.Empty);
            if (suffix == "cloud.dax")
            {
                var query = req.Params?.GetProperty("query").GetString();
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Task.FromResult<JsonRpcEnvelope?>(
                        JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, "DatasetId and Query are required."));
                }
            }
            return Task.FromResult<JsonRpcEnvelope?>(null);
        };

        var tool = new PowerBiCloudExecuteDaxTool(_client, _formatter);

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            tool.ExecuteCloudDaxAsync("ds-001", "", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("-32600", ex.Message);
        Assert.Contains("DatasetId and Query are required", ex.Message);
    }

    [Fact]
    public async Task CloudExecuteDax_WithUnconfiguredCloudClient_BridgeRejectsWithInternalError()
    {
        CustomHandler = (id, req) =>
        {
            var suffix = JsonRpcMethods.Suffix(req.Method ?? string.Empty);
            if (suffix == "cloud.dax")
            {
                return Task.FromResult<JsonRpcEnvelope?>(
                    JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, "TenantId and ClientId must be configured to acquire a Power BI cloud token."));
            }
            return Task.FromResult<JsonRpcEnvelope?>(null);
        };

        var tool = new PowerBiCloudExecuteDaxTool(_client, _formatter);

        var ex = await Assert.ThrowsAsync<McpException>(() =>
            tool.ExecuteCloudDaxAsync("ds-001", "EVALUATE TopN(5, Sales)", cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("-32000", ex.Message);
        Assert.Contains("TenantId and ClientId must be configured", ex.Message);
    }

    #endregion

    #region 2. Named Pipe & Timeout Clamping Challenges

    [Theory]
    [InlineData(601, 600)]
    [InlineData(999, 600)]
    [InlineData(10000, 600)]
    [InlineData(600, 600)]
    [InlineData(120, 120)]
    [InlineData(30, 30)]
    [InlineData(5, 5)]
    [InlineData(2, 5)]
    [InlineData(-10, 5)]
    public async Task ExecutePowerBiCodeTool_TimeoutClamping_EnforcesRange_Between5And600(int requestedTimeout, int expectedClamped)
    {
        var tool = new ExecutePowerBiCodeTool(_execute);
        var result = await tool.ExecuteAsync("return 42;", timeoutSeconds: requestedTimeout, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(expectedClamped, _executor.LastExecuteRequest!.TimeoutSeconds);
    }

    [Fact]
    public async Task CancellationToken_WhenFiredMidRequest_CancelsRequestAndSendsPowerBiCancelToBridge()
    {
        // Custom handler that pauses on powerbi.dax until cancellation occurs
        var requestStarted = new TaskCompletionSource<bool>();
        CustomHandler = async (id, req) =>
        {
            var suffix = JsonRpcMethods.Suffix(req.Method ?? string.Empty);
            if (suffix == "dax")
            {
                requestStarted.TrySetResult(true);
                // Hang to allow cancellation token to trigger
                await Task.Delay(10000);
                return JsonRpcEnvelope.Success(id, "delayed result");
            }
            return null;
        };

        var tool = new PowerBiEvaluateDaxTool(_client, _formatter);
        using var cts = new CancellationTokenSource();

        var callTask = tool.EvaluateDaxAsync("EVALUATE LongRunningTable", cancellationToken: cts.Token);

        // Wait until bridge has received the request
        await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        // Cancel the CTS
        cts.Cancel();

        // The tool call must throw OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => callTask);

        // Wait briefly for the fire-and-forget TryCancelInRevit to deliver powerbi.cancel to the bridge
        await Task.Delay(200, TestContext.Current.CancellationToken);

        // Verify that the bridge executor received the cancellation request
        Assert.True(_executor.CancelCalls >= 1, $"Expected CancelCalls >= 1 but got {_executor.CancelCalls}");
    }

    [Fact]
    public async Task BridgeDisconnect_MidRequest_ReturnsToolErrorIndicatingBridgeDisconnected()
    {
        var requestReceived = new TaskCompletionSource<bool>();
        CustomHandler = async (id, req) =>
        {
            var suffix = JsonRpcMethods.Suffix(req.Method ?? string.Empty);
            if (suffix == "dax")
            {
                requestReceived.TrySetResult(true);
                // Simulate abrupt bridge crash/disconnect by stopping listener without responding
                _ = Task.Run(async () =>
                {
                    await Task.Delay(30);
                    await _listener.StopAsync();
                });
                // Hold request open on the dying connection
                await Task.Delay(5000);
            }
            return null;
        };

        var tool = new PowerBiEvaluateDaxTool(_client, _formatter);
        var callTask = tool.EvaluateDaxAsync("EVALUATE Sales", cancellationToken: TestContext.Current.CancellationToken);

        await requestReceived.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        // ResultFormatter catches BridgeUnavailableException and formats it as a tool error
        var result = await callTask;
        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("bridge disconnected while the request was running", text);
    }

    [Fact]
    public async Task BridgeNotRunning_InitialRequest_ReturnsToolErrorWithBridgeNotConnectedHint()
    {
        var deadPipeOptions = Options.Create(new BridgeOptions
        {
            HostId = "powerbi",
            HostVersion = 2026,
            PipeName = "nonexistent-pipe-" + Guid.NewGuid().ToString("N"),
            ConnectTimeoutMs = 200, // fast fail
            PingIntervalSeconds = 60
        });

        await using var deadClient = new RevitBridgeClient(deadPipeOptions, NullLogger<RevitBridgeClient>.Instance, PowerBiHostProfile.Instance);
        var tool = new PowerBiCloudListWorkspacesTool(deadClient, _formatter);

        var result = await tool.ListWorkspacesAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("Power BI bridge not connected", text);
        Assert.Contains("Start HPPowerBi.McpBridge.exe", text);
        Assert.Contains("Allow Model Modifications / DAX Execution", text);
    }

    #endregion

    #region 3. Stdio Server Process Survival Challenge

    [Fact]
    public async Task StdioServerProcess_WhenBridgeReturns401_ReturnsStructuredJsonRpcError_AndProcessDoesNotCrash()
    {
        // 1. Setup a dedicated named pipe for the child stdio server process
        var stdioPipeName = "hppowerbi-stdio-test-" + Guid.NewGuid().ToString("N");
        var childExecutor = new FakeRevitExecutor();
        var childSettings = new BridgeSettings { ExecutionEnabled = true };

        var childDispatcher = new RequestDispatcher(
            childExecutor,
            childSettings,
            "2026",
            "Power BI",
            "Code execution disabled",
            customHandler: (id, req, writer, ct) =>
            {
                var suffix = JsonRpcMethods.Suffix(req.Method ?? string.Empty);
                if (suffix == "cloud.workspaces")
                {
                    // Bridge returns 401 Unauthorized
                    return Task.FromResult<JsonRpcEnvelope?>(
                        JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, "Power BI API error (Unauthorized): 401 Unauthorized"));
                }
                return Task.FromResult<JsonRpcEnvelope?>(null);
            });

        using var childListener = new PipeListener(stdioPipeName, childDispatcher);
        childListener.Start();

        // 2. Find HPPowerBi.Mcp.Server.exe
        var serverExe = Path.Combine(AppContext.BaseDirectory, "HPPowerBi.Mcp.Server.exe");
        Assert.True(File.Exists(serverExe), $"Server exe not found at: {serverExe}");

        var psi = new ProcessStartInfo(serverExe)
        {
            Arguments = $"--Bridge:PipeName {stdioPipeName} --Bridge:ConnectTimeoutMs 3000",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)!;
        Assert.NotNull(proc);

        try
        {
            // 3. Send initialize
            var initMsg = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "initialize",
                @params = new
                {
                    protocolVersion = "2025-06-18",
                    capabilities = new { },
                    clientInfo = new { name = "test_client", version = "1.0" }
                }
            });
            await proc.StandardInput.WriteLineAsync(initMsg.AsMemory(), TestContext.Current.CancellationToken);
            await proc.StandardInput.FlushAsync(TestContext.Current.CancellationToken);

            var initResponseLine = await proc.StandardOutput.ReadLineAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(initResponseLine);
            using var initDoc = JsonDocument.Parse(initResponseLine);
            Assert.Equal(1, initDoc.RootElement.GetProperty("id").GetInt64());

            // 4. Send initialized notification
            var notifMsg = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                method = "notifications/initialized"
            });
            await proc.StandardInput.WriteLineAsync(notifMsg.AsMemory(), TestContext.Current.CancellationToken);
            await proc.StandardInput.FlushAsync(TestContext.Current.CancellationToken);

            // 5. Send tools/call for powerbi_cloud_list_workspaces
            var callMsg = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/call",
                @params = new
                {
                    name = "powerbi_cloud_list_workspaces",
                    arguments = new { }
                }
            });
            await proc.StandardInput.WriteLineAsync(callMsg.AsMemory(), TestContext.Current.CancellationToken);
            await proc.StandardInput.FlushAsync(TestContext.Current.CancellationToken);

            // 6. Read tool response
            var callResponseLine = await proc.StandardOutput.ReadLineAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(callResponseLine);

            using var callDoc = JsonDocument.Parse(callResponseLine);
            var root = callDoc.RootElement;
            Assert.Equal(2, root.GetProperty("id").GetInt64());

            // Either the SDK surfaces it as an error envelope (root.error) or as result with isError = true
            var hasErrorProp = root.TryGetProperty("error", out var errElement);
            var hasResultProp = root.TryGetProperty("result", out var resElement);
            Assert.True(hasErrorProp || hasResultProp, "Response must contain either 'error' or 'result'.");

            if (hasErrorProp)
            {
                var errMsg = errElement.GetProperty("message").GetString();
                Assert.Contains("401 Unauthorized", errMsg);
            }
            else
            {
                Assert.True(resElement.GetProperty("isError").GetBoolean());
                var contentText = resElement.GetProperty("content")[0].GetProperty("text").GetString();
                Assert.Contains("401 Unauthorized", contentText);
            }

            // 7. CRUCIAL CHECK: Verify the stdio process is STILL ALIVE and did NOT crash
            Assert.False(proc.HasExited, "The stdio server process must NOT crash when an MCP tool fails with 401!");

            // 8. Verify the server can successfully handle subsequent requests
            var listMsg = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = 3,
                method = "tools/list",
                @params = new { }
            });
            await proc.StandardInput.WriteLineAsync(listMsg.AsMemory(), TestContext.Current.CancellationToken);
            await proc.StandardInput.FlushAsync(TestContext.Current.CancellationToken);

            var listResponseLine = await proc.StandardOutput.ReadLineAsync(TestContext.Current.CancellationToken);
            Assert.NotNull(listResponseLine);
            using var listDoc = JsonDocument.Parse(listResponseLine);
            Assert.Equal(3, listDoc.RootElement.GetProperty("id").GetInt64());
            var tools = listDoc.RootElement.GetProperty("result").GetProperty("tools");
            Assert.Equal(22, tools.GetArrayLength());
        }
        finally
        {
            if (!proc.HasExited)
            {
                try
                {
                    proc.Kill();
                    await proc.WaitForExitAsync(TestContext.Current.CancellationToken);
                }
                catch { }
            }
            await childListener.StopAsync();
        }
    }

    #endregion
}
