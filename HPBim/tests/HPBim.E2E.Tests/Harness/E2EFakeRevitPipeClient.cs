namespace HPBim.E2E.Tests.Harness;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
/// Deterministic offline in-memory fake implementation of IRevitPipeClient for E2E acceptance tests.
/// </summary>
public sealed class E2EFakeRevitPipeClient : IRevitPipeClient
{
    private readonly List<ExecuteRequest> _executedRequests = new();

    public bool IsConnected { get; set; } = true;
    public string PipeName { get; set; } = "e2e-fake-rebar-pipe";
    public IReadOnlyList<ExecuteRequest> ExecutedRequests => _executedRequests;

    public Func<ExecuteRequest, ExecuteResult>? CustomExecuteHandler { get; set; }
    public IReadOnlyList<string> SimulatedWarnings { get; set; } = Array.Empty<string>();
    public bool ShouldFailConnect { get; set; }
    public bool PingResult { get; set; } = true;
    public bool CancelResult { get; set; } = true;
    public JsonElement? ContextResult { get; set; }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (ShouldFailConnect)
        {
            throw new RevitPipeException("Simulated connection failure.");
        }
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new RevitPipeException("Client is not connected.");
        }

        _executedRequests.Add(request);

        if (CustomExecuteHandler != null)
        {
            return Task.FromResult(CustomExecuteHandler(request));
        }

        var result = new ExecuteResult
        {
            IsError = false,
            Changed = new ChangedCounts(1, 0, 0),
            RolledBack = request.DryRun,
            DurationMs = 25,
            Warnings = SimulatedWarnings
        };

        return Task.FromResult(result);
    }

    public Task<bool> PingAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(IsConnected && PingResult);

    public Task<bool> CancelAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(IsConnected && CancelResult);

    public Task<JsonElement?> GetContextAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected) throw new RevitPipeException("Client is not connected.");
        return Task.FromResult(ContextResult);
    }

    public void Dispose() => IsConnected = false;
    public ValueTask DisposeAsync()
    {
        IsConnected = false;
        return ValueTask.CompletedTask;
    }
}
