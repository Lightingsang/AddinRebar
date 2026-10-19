namespace HPBim.Client.Tests.Connection;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class RevitPipeClientAdversarialTests
{
    [Fact]
    public async Task ConnectAsync_NonExistentPipe_TimesOutAndThrowsRevitPipeException()
    {
        string deadPipe = $"nonexistent-pipe-{Guid.NewGuid():N}";
        var options = new RevitConnectionOptions { PipeName = deadPipe, ConnectTimeoutMs = 150 };
        using var client = new RevitPipeClient(options);
        var ex = await Assert.ThrowsAsync<RevitPipeException>(() => client.ConnectAsync());
        Assert.True(ex.IsTimeout);
        Assert.Contains(deadPipe, ex.Message);
        Assert.Contains("150ms", ex.Message);
        Assert.False(client.IsConnected);
    }

    [Fact]
    public async Task ConnectAsync_WithCancelledToken_ThrowsRevitPipeException()
    {
        string deadPipe = $"cancelled-pipe-{Guid.NewGuid():N}";
        var options = new RevitConnectionOptions { PipeName = deadPipe, ConnectTimeoutMs = 5000 };
        using var client = new RevitPipeClient(options);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<RevitPipeException>(() => client.ConnectAsync(cts.Token));
        Assert.False(client.IsConnected);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        var client = new RevitPipeClient();
        client.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.ExecuteAsync(new ExecuteRequest("code")));
    }

    [Fact]
    public async Task ConnectAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        var client = new RevitPipeClient();
        client.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.ConnectAsync());
    }

    [Fact]
    public async Task ExecuteAsync_ServerReturnsRpcError_ReturnsFailureResult()
    {
        string pipeName = $"rpc-err-test-{Guid.NewGuid():N}";
        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();
            using var reader = new StreamReader(server, Encoding.UTF8, false, 4096, leaveOpen: true);
            using var writer = new StreamWriter(server, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };

            string? line = await reader.ReadLineAsync();
            if (line != null)
            {
                var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
                if (envelope?.Id != null)
                {
                    var response = JsonRpcEnvelope.Failure(envelope.Id.Value, -32603, "Internal crash in host bridge");
                    await writer.WriteLineAsync(BridgeJson.Serialize(response));
                }
            }
        });

        using var client = new RevitPipeClient(new RevitConnectionOptions { PipeName = pipeName, ConnectTimeoutMs = 3000 });
        await client.ConnectAsync();
        var result = await client.ExecuteAsync(new ExecuteRequest("test()"));
        Assert.True(result.IsError);
        Assert.Contains("-32603", result.Message);
        Assert.Contains("Internal crash in host bridge", result.Message);
        await client.DisconnectAsync();
        await serverTask;
    }

    [Fact]
    public async Task GetContextAsync_ServerReturnsRpcError_ThrowsRevitPipeExceptionWithErrorCode()
    {
        string pipeName = $"ctx-err-test-{Guid.NewGuid():N}";
        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();
            using var reader = new StreamReader(server, Encoding.UTF8, false, 4096, leaveOpen: true);
            using var writer = new StreamWriter(server, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };

            string? line = await reader.ReadLineAsync();
            if (line != null)
            {
                var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
                if (envelope?.Id != null)
                {
                    var response = JsonRpcEnvelope.Failure(envelope.Id.Value, -32001, "No active document open");
                    await writer.WriteLineAsync(BridgeJson.Serialize(response));
                }
            }
        });

        using var client = new RevitPipeClient(new RevitConnectionOptions { PipeName = pipeName, ConnectTimeoutMs = 3000 });
        await client.ConnectAsync();
        var ex = await Assert.ThrowsAsync<RevitPipeException>(() => client.GetContextAsync());
        Assert.True(ex.IsBridgeError);
        Assert.Equal(-32001, ex.ErrorCode);
        Assert.Contains("No active document open", ex.Message);
        await client.DisconnectAsync();
        await serverTask;
    }

    [Fact]
    public async Task ExecuteAsync_ServerClosesAbruptly_ThrowsRevitPipeException()
    {
        string pipeName = $"abrupt-close-{Guid.NewGuid():N}";
        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();
            using var reader = new StreamReader(server, Encoding.UTF8, false, 4096, leaveOpen: true);
            await reader.ReadLineAsync();
            server.Disconnect();
        });

        using var client = new RevitPipeClient(new RevitConnectionOptions { PipeName = pipeName, ConnectTimeoutMs = 3000 });
        await client.ConnectAsync();
        await Assert.ThrowsAsync<RevitPipeException>(() => client.ExecuteAsync(new ExecuteRequest("hang()")));
        await client.DisconnectAsync();
        await serverTask;
    }

    [Fact]
    public async Task ExecuteAsync_RequestTimeout_ThrowsRevitPipeExceptionWithIsTimeoutTrue()
    {
        string pipeName = $"req-timeout-{Guid.NewGuid():N}";
        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();
            using var reader = new StreamReader(server, Encoding.UTF8, false, 4096, leaveOpen: true);
            await reader.ReadLineAsync();
            await Task.Delay(3000);
        });

        using var client = new RevitPipeClient(new RevitConnectionOptions { PipeName = pipeName, ConnectTimeoutMs = 3000, DefaultExecuteTimeoutSeconds = 1 });
        await client.ConnectAsync();
        var req = new ExecuteRequest("slow()") { TimeoutSeconds = 1 };
        var ex = await Assert.ThrowsAsync<RevitPipeException>(() => client.ExecuteAsync(req));
        Assert.True(ex.IsTimeout);
        Assert.Contains("timed out", ex.Message);
        await client.DisconnectAsync();
        await serverTask;
    }

    [Fact]
    public async Task ConcurrentExecuteAsync_HandlesConcurrentRequestsSafely()
    {
        string pipeName = $"concurrent-test-{Guid.NewGuid():N}";
        const int requestCount = 5;

        var serverTask = Task.Run(async () =>
        {
            using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();
            using var reader = new StreamReader(server, Encoding.UTF8, false, 4096, leaveOpen: true);
            using var writer = new StreamWriter(server, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };

            for (int i = 0; i < requestCount; i++)
            {
                string? line = await reader.ReadLineAsync();
                if (line == null) break;
                var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
                if (envelope?.Id != null)
                {
                    var res = new ExecuteResult { IsError = false, Changed = new ChangedCounts(1, 0, 0), DurationMs = 5 };
                    var response = JsonRpcEnvelope.Success(envelope.Id.Value, res);
                    await writer.WriteLineAsync(BridgeJson.Serialize(response));
                }
            }
        });

        using var client = new RevitPipeClient(new RevitConnectionOptions { PipeName = pipeName, ConnectTimeoutMs = 3000 });
        await client.ConnectAsync();

        var tasks = new List<Task<ExecuteResult>>();
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(client.ExecuteAsync(new ExecuteRequest($"cmd_{i}")));
        }

        var results = await Task.WhenAll(tasks);
        Assert.Equal(requestCount, results.Length);
        foreach (var r in results)
        {
            Assert.False(r.IsError);
            Assert.Equal(1, r.Changed.Added);
        }

        await client.DisconnectAsync();
        await serverTask;
    }

    [Fact]
    public async Task ConnectAsync_ConcurrentCalls_SynchronizesWithoutThrowingOrLeaking()
    {
        string pipeName = $"concurrent-conn-{Guid.NewGuid():N}";
        var serverTask = Task.Run(async () =>
        {
            try
            {
                using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync();
                using var reader = new StreamReader(server, Encoding.UTF8, false, 4096, leaveOpen: true);
                using var writer = new StreamWriter(server, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };

                while (server.IsConnected)
                {
                    string? line = await reader.ReadLineAsync();
                    if (line == null) break;
                    var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
                    if (envelope?.Id != null)
                    {
                        var response = JsonRpcEnvelope.Success(envelope.Id.Value, new { ok = true });
                        await writer.WriteLineAsync(BridgeJson.Serialize(response));
                    }
                }
            }
            catch (IOException)
            {
                // Expected when client disconnects
            }
        });

        using var client = new RevitPipeClient(new RevitConnectionOptions { PipeName = pipeName, ConnectTimeoutMs = 3000 });
        var tasks = new Task[5];
        for (int i = 0; i < 5; i++) tasks[i] = client.ConnectAsync();

        await Task.WhenAll(tasks);
        Assert.True(client.IsConnected);
        await client.DisconnectAsync();
        await serverTask;
    }
}
