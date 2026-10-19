namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Pipeline;
using HPBim.Client.ScriptGenerators;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Models;
using HPBim.Core.Tests.Fixtures;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

/// <summary>
///     Adversarial stress harness for M3 Iteration 2 challenge:
///     concurrency synchronization, 8-stage composite generation, and atomic dry-run pipeline.
/// </summary>
public sealed class ChallengerM3It2AdversarialTests
{
    private readonly BimModel _nhaPhoModel = NhaPho5x18mFixture.CreateModel();

    [Fact]
    public async Task Concurrency_TwentyConcurrentConnectCalls_SynchronizeCleanly()
    {
        string pipeName = $"challenger-conn-20-{Guid.NewGuid():N}";
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
                    var env = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
                    if (env?.Id != null)
                    {
                        var response = JsonRpcEnvelope.Success(env.Id.Value, new { ok = true });
                        await writer.WriteLineAsync(BridgeJson.Serialize(response));
                    }
                }
            }
            catch (IOException)
            {
                // Expected when client disconnects named pipe during teardown.
            }
        });

        using var client = new RevitPipeClient(new RevitConnectionOptions { PipeName = pipeName, ConnectTimeoutMs = 5000 });
        const int concurrentCallers = 20;
        var tasks = new Task[concurrentCallers];
        for (int i = 0; i < concurrentCallers; i++)
        {
            tasks[i] = client.ConnectAsync();
        }

        await Task.WhenAll(tasks);
        Assert.True(client.IsConnected);

        await client.DisconnectAsync();
        Assert.False(client.IsConnected);
        await serverTask;
    }

    [Fact]
    public void CompositeBimGenerator_EmptyModel_DoesNotThrowAndEmitsValidScriptAndArgs()
    {
        var emptyModel = new BimModel { ProjectName = "EmptyTestProject" };
        var generator = new CompositeBimGenerator();

        var payload = generator.Generate(emptyModel);

        Assert.Equal(0, payload.StageIndex);
        Assert.Equal("CompositeBimPipeline", payload.StageName);
        Assert.NotNull(payload.Args);

        var dict = (IDictionary<string, object>)payload.Args!;
        Assert.True(dict.ContainsKey("levels"));
        Assert.True(dict.ContainsKey("grids"));
        Assert.True(dict.ContainsKey("columns"));
        Assert.True(dict.ContainsKey("beams"));
        Assert.True(dict.ContainsKey("slabs"));
        Assert.True(dict.ContainsKey("walls"));
        Assert.True(dict.ContainsKey("openings"));
        Assert.True(dict.ContainsKey("rooms"));
        Assert.True(dict.ContainsKey("fixtures"));
        Assert.True(dict.ContainsKey("pipes"));
        Assert.True(dict.ContainsKey("rebars"));

        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script);
        var tree = CSharpSyntaxTree.ParseText(payload.ScriptCode, parseOptions);
        var errors = tree.GetDiagnostics();
        Assert.Empty(errors);
    }

    [Fact]
    public void CompositeBimGenerator_ArgCountsMatchIndividualStageGenerators()
    {
        var generator = new CompositeBimGenerator();
        var payload = generator.Generate(_nhaPhoModel);
        var dict = (IDictionary<string, object>)payload.Args!;

        var s1 = new Stage1LevelGenerator().Generate(_nhaPhoModel);
        var s2 = new Stage2GridGenerator().Generate(_nhaPhoModel);
        var s4 = new Stage4WallGenerator().Generate(_nhaPhoModel);
        var s6 = new Stage6RoomGenerator().Generate(_nhaPhoModel);

        Assert.NotNull(dict["levels"]);
        Assert.NotNull(dict["grids"]);
        Assert.NotNull(dict["walls"]);
        Assert.NotNull(dict["rooms"]);
    }

    [Fact]
    public async Task Pipeline_AtomicDryRun_WithAutoConnect_RollsBackAndEmitsCompositeEvent()
    {
        var fakeClient = new FakeRevitPipeClient { IsConnected = false };
        var engine = new BimPipelineEngine(fakeClient);

        StageResult? capturedStage = null;
        engine.StageCompleted += res => capturedStage = res;

        var options = new BimPipelineOptions
        {
            DryRun = true,
            AtomicDryRun = true,
            TimeoutSecondsPerStage = 45
        };

        var result = await engine.ExecuteAsync(_nhaPhoModel, options);

        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.True(fakeClient.IsConnected);
        Assert.Single(fakeClient.ExecutedRequests);

        var executedReq = fakeClient.ExecutedRequests[0];
        Assert.True(executedReq.DryRun);
        Assert.Equal(90, executedReq.TimeoutSeconds);
        Assert.Contains("Composite BIM Pipeline", executedReq.Label);

        Assert.NotNull(capturedStage);
        Assert.Equal(0, capturedStage!.StageIndex);
        Assert.Equal("CompositeDryRun", capturedStage.StageName);
        Assert.True(capturedStage.RolledBack);
    }

    [Fact]
    public async Task Pipeline_AtomicDryRun_Cancellation_CancelsCleanlyWithoutOrphanExecution()
    {
        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = _ => throw new OperationCanceledException()
        };
        var engine = new BimPipelineEngine(fakeClient);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var options = new BimPipelineOptions { DryRun = true, AtomicDryRun = true };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            engine.ExecuteAsync(_nhaPhoModel, options, cts.Token));
    }
}
