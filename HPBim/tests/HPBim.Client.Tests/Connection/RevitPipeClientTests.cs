namespace HPBim.Client.Tests.Connection;

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class RevitPipeClientTests
{
    [Fact]
    public void RevitConnectionOptions_Defaults_AreValid()
    {
        var options = new RevitConnectionOptions();
        options.Validate();

        Assert.Equal("hprebar-mcp-r2026", options.PipeName);
        Assert.Equal(5000, options.ConnectTimeoutMs);
        Assert.Equal(120, options.DefaultExecuteTimeoutSeconds);
        Assert.Equal(10 * 1024 * 1024, options.MaxMessageBytes);
    }

    [Fact]
    public void RevitConnectionOptions_InvalidPipeName_ThrowsArgumentException()
    {
        var options = new RevitConnectionOptions { PipeName = "   " };
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void RevitConnectionOptions_InvalidTimeout_ThrowsArgumentOutOfRangeException()
    {
        var options = new RevitConnectionOptions { ConnectTimeoutMs = 0 };
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotConnected_ThrowsRevitPipeException()
    {
        using var client = new RevitPipeClient();
        var request = new ExecuteRequest("return 1;");

        await Assert.ThrowsAsync<RevitPipeException>(() => client.ExecuteAsync(request));
    }

    [Fact]
    public async Task PingAsync_WhenNotConnected_ReturnsFalse()
    {
        using var client = new RevitPipeClient();
        bool result = await client.PingAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task ConnectAndRoundTrip_WithLocalPipeServer_Succeeds()
    {
        string pipeName = $"hpbim-test-{Guid.NewGuid():N}";
        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await server.WaitForConnectionAsync();
            using var reader = new StreamReader(server, Encoding.UTF8, false, 4096, leaveOpen: true);
            using var writer = new StreamWriter(server, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };

            // Request 1: Ping
            string? pingLine = await reader.ReadLineAsync();
            if (pingLine != null)
            {
                var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(pingLine);
                if (envelope?.Id != null)
                {
                    var response = JsonRpcEnvelope.Success(envelope.Id.Value, new { pong = true });
                    await writer.WriteLineAsync(BridgeJson.Serialize(response));
                }
            }

            // Request 2: Execute
            string? execLine = await reader.ReadLineAsync();
            if (execLine != null)
            {
                var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(execLine);
                if (envelope?.Id != null)
                {
                    var execResult = new ExecuteResult
                    {
                        IsError = false,
                        Changed = new ChangedCounts(7, 0, 0),
                        DurationMs = 25
                    };
                    var response = JsonRpcEnvelope.Success(envelope.Id.Value, execResult);
                    await writer.WriteLineAsync(BridgeJson.Serialize(response));
                }
            }
        });

        var options = new RevitConnectionOptions
        {
            PipeName = pipeName,
            ConnectTimeoutMs = 3000
        };

        using var client = new RevitPipeClient(options);
        await client.ConnectAsync();
        Assert.True(client.IsConnected);

        bool pingOk = await client.PingAsync();
        Assert.True(pingOk);

        var request = new ExecuteRequest("return 42;");
        var result = await client.ExecuteAsync(request);

        Assert.False(result.IsError);
        Assert.Equal(7, result.Changed.Added);

        await client.DisconnectAsync();
        Assert.False(client.IsConnected);

        await serverTask;
    }

    [Fact]
    public async Task Dispose_MultipleTimes_DoesNotThrow()
    {
        var client = new RevitPipeClient();
        client.Dispose();
        client.Dispose();
        await client.DisposeAsync();
    }
}
