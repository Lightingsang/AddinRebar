namespace HPBim.Client.Tests.Fakes;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Recovery;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     In-memory fake implementation of IRevitPipeClient for deterministic offline testing.
/// </summary>
public sealed class FakeRevitPipeClient : IRevitPipeClient
{
    private readonly List<ExecuteRequest> _executedRequests = new();

    public bool IsConnected { get; set; } = true;
    public string PipeName { get; set; } = "fake-rebar-pipe";
    public IReadOnlyList<ExecuteRequest> ExecutedRequests => _executedRequests;

    public Func<ExecuteRequest, ExecuteResult>? CustomExecuteHandler { get; set; }
    public IReadOnlyList<string> SimulatedWarnings { get; set; } = Array.Empty<string>();
    public bool ShouldFailConnect { get; set; }
    public bool PingResult { get; set; } = true;
    public JsonElement? ContextResult { get; set; }

    public Dictionary<Guid, CoordinationReceiptRecord> SimulatedReceipts { get; } = new();
    public void AddSimulatedReceipt(CoordinationReceiptRecord receipt) =>
        SimulatedReceipts[receipt.OperationId] = receipt;
    public bool AutoEnrichRecords { get; set; } = true;
    public bool ShouldDisconnectOnExecute { get; set; }
    public bool ShouldTimeoutOnExecute { get; set; }
    public HPBim.Core.Execution.DocumentIdentitySnapshot? SimulatedActiveDocument { get; set; } = new()
    {
        Title = "FakeProject.rvt", PathName = @"C:\FakeProject.rvt",
        VersionGuid = Guid.Parse("11111111-2222-3333-4444-555555555555"), NumberOfSaves = 1
    };

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (ShouldFailConnect) throw new RevitPipeException("Simulated connection failure.");
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
        ValidateExecutePreconditions();
        _executedRequests.Add(request);

        if (CustomExecuteHandler != null)
        {
            return Task.FromResult(HandleCustomExecution(request));
        }

        if (request.Label == "Query Active Document Identity")
        {
            return Task.FromResult(HandleQueryActiveDocumentIdentity(request));
        }

        if (request.Label == "Query Extensible Storage Receipts")
        {
            return Task.FromResult(HandleQueryExtensibleStorageReceipts(request));
        }

        return Task.FromResult(CreateDefaultExecutionResult(request));
    }

    private void ValidateExecutePreconditions()
    {
        if (!IsConnected) throw new RevitPipeException("Client is not connected.");
        if (ShouldDisconnectOnExecute)
        {
            IsConnected = false;
            throw new IOException("Simulated pipe disconnection during execution.");
        }
        if (ShouldTimeoutOnExecute) throw new RevitPipeException("Simulated execution timeout.", isTimeout: true);
    }

    private ExecuteResult HandleCustomExecution(ExecuteRequest request)
    {
        var customResult = CustomExecuteHandler!(request);
        if (request.Label == "Query Active Document Identity" && !customResult.IsError && customResult.Value == null)
        {
            return HandleQueryActiveDocumentIdentity(request);
        }
        if (request.Label == "Query Extensible Storage Receipts" && !customResult.IsError && customResult.Value == null)
        {
            return HandleQueryExtensibleStorageReceipts(request);
        }
        AutoEnrichResponseRecords(request, ref customResult);
        return customResult;
    }

    private ExecuteResult CreateDefaultExecutionResult(ExecuteRequest request) =>
        new()
        {
            IsError = false,
            Value = GenerateDefaultRecordsPayload(request),
            Changed = new ChangedCounts(1, 0, 0),
            RolledBack = request.DryRun,
            DurationMs = 15,
            Warnings = SimulatedWarnings
        };

    private void AutoEnrichResponseRecords(ExecuteRequest request, ref ExecuteResult customResult)
    {
        if (!AutoEnrichRecords || customResult.IsError) return;

        if (customResult.Value.HasValue &&
            customResult.Value.Value.ValueKind == JsonValueKind.Object &&
            customResult.Value.Value.TryGetProperty("records", out _)) return;

        bool needsRecords = !customResult.Value.HasValue ||
            customResult.Value.Value.ValueKind == JsonValueKind.Undefined;

        if (needsRecords)
        {
            var enrichedValue = GenerateDefaultRecordsPayload(request);
            if (enrichedValue.HasValue) customResult.Value = enrichedValue;
        }
    }

    private JsonElement? GenerateDefaultRecordsPayload(ExecuteRequest request)
    {
        if (request.Args.HasValue && request.Args.Value.ValueKind == JsonValueKind.Object &&
            request.Args.Value.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
        {
            var simulatedRecords = new List<object>();
            long idCounter = 1000;
            foreach (var item in itemsProp.EnumerateArray())
            {
                string sk = item.TryGetProperty("sourceKey", out var skp) ? skp.GetString() ?? "" : "";
                string cat = item.TryGetProperty("category", out var cp) ? cp.GetString() ?? "" : "";
                string mark = item.TryGetProperty("mark", out var mp) ? mp.GetString() ?? "" : "";
                string targetUid = item.TryGetProperty("targetUniqueId", out var tup) ? tup.GetString() ?? "" : "";
                bool isUpdate = !string.IsNullOrEmpty(targetUid);
                simulatedRecords.Add(new
                {
                    sourceKey = sk,
                    category = cat,
                    mark = mark,
                    status = isUpdate ? "Updated" : "Created",
                    targetUniqueId = isUpdate ? targetUid : $"fake-uid-{idCounter}",
                    targetElementId = idCounter++
                });
            }
            var payload = new
            {
                planId = request.Args.Value.TryGetProperty("planId", out var pip) ? pip.GetString() : "",
                total = simulatedRecords.Count,
                records = simulatedRecords
            };
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
            using var doc = JsonDocument.Parse(jsonBytes);
            return doc.RootElement.Clone();
        }
        return null;
    }

    private ExecuteResult HandleQueryActiveDocumentIdentity(ExecuteRequest request)
    {
        var doc = SimulatedActiveDocument ?? new HPBim.Core.Execution.DocumentIdentitySnapshot
        {
            Title = "DefaultProject.rvt",
            PathName = @"C:\DefaultProject.rvt",
            VersionGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            NumberOfSaves = 1
        };

        var payload = new
        {
            title = doc.Title,
            pathName = doc.PathName,
            versionGuid = doc.VersionGuid.ToString("D"),
            numberOfSaves = doc.NumberOfSaves,
            centralGuid = (doc.IsWorkshared && doc.CentralGuid.HasValue ? doc.CentralGuid.Value : Guid.Empty).ToString("D"),
            activeViewName = doc.ActiveViewName ?? "{3D}"
        };

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        using var jsonDoc = JsonDocument.Parse(jsonBytes);
        return new ExecuteResult
        {
            IsError = false,
            Value = jsonDoc.RootElement.Clone(),
            RolledBack = request.DryRun,
            DurationMs = 5
        };
    }

    private ExecuteResult HandleQueryExtensibleStorageReceipts(ExecuteRequest request)
    {
        var args = request.Args.HasValue ? request.Args.Value : default;
        string? targetOpId = args.ValueKind == JsonValueKind.Object && args.TryGetProperty("targetOperationId", out var opProp)
            ? opProp.GetString()
            : null;
        string? targetPlanHash = args.ValueKind == JsonValueKind.Object && args.TryGetProperty("targetPlanHash", out var phProp)
            ? phProp.GetString()
            : null;

        var matched = SimulatedReceipts.Values
            .Where(r => (string.IsNullOrEmpty(targetOpId) || r.OperationId.ToString("D") == targetOpId) &&
                        (string.IsNullOrEmpty(targetPlanHash) || string.Equals(r.PlanHash, targetPlanHash, StringComparison.OrdinalIgnoreCase)))
            .Select(r => new
            {
                operationId = r.OperationId.ToString("D"),
                planHash = r.PlanHash,
                targetDocId = r.TargetDocId ?? string.Empty,
                targetDocTitle = !string.IsNullOrEmpty(r.TargetDocTitle) ? r.TargetDocTitle : (r.TargetDocId ?? string.Empty),
                targetDocPath = r.TargetDocPath ?? string.Empty,
                targetVersionGuid = r.TargetVersionGuid ?? string.Empty,
                targetCentralGuid = r.TargetCentralGuid ?? string.Empty,
                committedAtUtc = r.CommittedAtUtc,
                elementMapJson = r.ElementMap != null ? JsonSerializer.Serialize(r.ElementMap) : null
            }).ToList();

        var payload = new { count = matched.Count, receipts = matched };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        using var doc = JsonDocument.Parse(jsonBytes);

        return new ExecuteResult
        {
            IsError = false,
            Value = doc.RootElement.Clone(),
            DurationMs = 5
        };
    }

    public Task<bool> PingAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(IsConnected && PingResult);

    public bool CancelResult { get; set; } = true;
    public int CancelCount { get; private set; }

    public Task<bool> CancelAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new RevitPipeException("Client is not connected.");
        }

        CancelCount++;
        return Task.FromResult(CancelResult);
    }

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
