using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>What the AI sees when Revit is closed: a plain, actionable tool error and a server that keeps answering.</summary>
public sealed class BridgeUnavailableTests
{
    private static RevitBridgeClient ClientFor(string pipeName) =>
        new RevitBridgeClient(
            Options.Create(new BridgeOptions { PipeName = pipeName, ConnectTimeoutMs = 300, MaxReconnectAttempts = 0 }),
            NullLogger<RevitBridgeClient>.Instance);

    [Fact]
    public async Task Missing_pipe_throws_unavailable_with_pipe_name_and_client_stays_usable()
    {
        await using var client = ClientFor("hprebar-mcp-nobody-" + Guid.NewGuid().ToString("N"));

        var first = await Assert.ThrowsAsync<BridgeUnavailableException>(() =>
            client.SendAsync<BridgePingResult>(JsonRpcMethods.Ping, null, TimeSpan.FromSeconds(2), null, TestContext.Current.CancellationToken));
        var second = await Assert.ThrowsAsync<BridgeUnavailableException>(() =>
            client.SendAsync<BridgePingResult>(JsonRpcMethods.Ping, null, TimeSpan.FromSeconds(2), null, TestContext.Current.CancellationToken));

        Assert.Contains("Revit bridge not connected", first.Message);
        Assert.Contains("hprebar-mcp-nobody-", first.Message);
        Assert.Equal(first.Message, second.Message);
        Assert.False(client.IsConnected);
    }

    [Fact]
    public async Task Formatter_maps_unavailable_to_tool_error_without_throwing()
    {
        var formatter = new ResultFormatter();

        var result = await formatter.RunAsync(() => throw new BridgeUnavailableException("Revit bridge not connected. Open Revit 2026 (pipe hprebar-mcp-r2026)."));

        Assert.True(result.IsError);
        Assert.Contains("hprebar-mcp-r2026", Assert.IsType<TextContentBlock>(result.Content[0]).Text);
    }

    [Fact]
    public async Task Formatter_strips_machine_paths_and_keeps_actionable_bridge_errors_as_tool_errors()
    {
        var formatter = new ResultFormatter();

        var busy = await formatter.RunAsync(() => throw new BridgeErrorException(BridgeErrorCode.Busy, "Another script is still running."));
        Assert.True(busy.IsError);

        var execute = formatter.FromExecute(ExecuteResult.Failure(@"Could not open C:\Users\someone\model.rvt for writing"));
        var text = Assert.IsType<TextContentBlock>(execute.Content[0]).Text;
        Assert.DoesNotContain(@"C:\Users", text);
        Assert.Contains("<path>", text);

        await Assert.ThrowsAsync<ModelContextProtocol.McpException>(() =>
            formatter.RunAsync(() => throw new BridgeErrorException(BridgeErrorCode.InternalError, "boom")));
    }
}
