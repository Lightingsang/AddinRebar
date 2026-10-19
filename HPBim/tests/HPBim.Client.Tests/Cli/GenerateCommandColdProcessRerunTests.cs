namespace HPBim.Client.Tests.Cli;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Commands;
using HPBim.Cli.Formatting;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Serialization;
using HPBim.Core.Tests.Fixtures;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class GenerateCommandColdProcessRerunTests : IDisposable
{
    private readonly string _tempDir;

    public GenerateCommandColdProcessRerunTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cold_process_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { /* best effort cleanup */ }
        }
    }

    [Fact]
    public async Task GenerateCommand_ColdProcessRerun_PersistsLedgerAndProduces100PercentNoOp()
    {
        string modelPath = Path.Combine(_tempDir, "model.json");
        string ledgerPath = Path.Combine(_tempDir, "state", "bim_coordination_ledger.json");

        var model = NhaPho5x18mFixture.CreateModel();
        await BimModelSerializer.SerializeToFileAsync(model, modelPath);

        var revitElementStore = new Dictionary<string, (string SourceKey, string Category, string Mark, long ElementId)>();
        long[] elementIdCounter = [1000];

        int totalCreatedInRun1 = await RunProcess1Async(modelPath, ledgerPath, revitElementStore, elementIdCounter);
        await RunProcess2Async(modelPath, ledgerPath, revitElementStore, elementIdCounter, totalCreatedInRun1);
    }

    private static async Task<int> RunProcess1Async(
        string modelPath, string ledgerPath,
        Dictionary<string, (string SourceKey, string Category, string Mark, long ElementId)> store,
        long[] counter)
    {
        var fakePipe1 = new FakeRevitPipeClient();
        fakePipe1.CustomExecuteHandler = req => SimulateRevitExecution(req, store, ref counter[0]);

        using var sw1 = new StringWriter();
        var cmd1 = new GenerateCommand(new ConsoleFormatter(sw1), fakePipe1);
        var args1 = CliParser.Parse(["generate", "--input", modelPath, "--ledger", ledgerPath]);

        int exit1 = await cmd1.ExecuteAsync(args1);
        Assert.Equal(0, exit1);
        Assert.True(File.Exists(ledgerPath), "Ledger file must be durably written to disk on Process 1 completion.");

        var diskLedger1 = BimCoordinationLedgerSerializer.DeserializeFromFile(ledgerPath);
        Assert.NotNull(diskLedger1.Mappings);
        Assert.NotEmpty(diskLedger1.Mappings);
        Assert.True(store.Count > 0, "Elements should have been created in Process 1.");
        return store.Count;
    }

    private static async Task RunProcess2Async(
        string modelPath, string ledgerPath,
        Dictionary<string, (string SourceKey, string Category, string Mark, long ElementId)> store,
        long[] counter, int totalCreatedInRun1)
    {
        var fakePipe2 = new FakeRevitPipeClient();
        var capturedRequests = new List<ExecuteRequest>();
        fakePipe2.CustomExecuteHandler = req =>
        {
            capturedRequests.Add(req);
            return SimulateRevitExecution(req, store, ref counter[0]);
        };

        using var sw2 = new StringWriter();
        var cmd2 = new GenerateCommand(new ConsoleFormatter(sw2), fakePipe2);
        var args2 = CliParser.Parse(["generate", "--input", modelPath, "--ledger", ledgerPath]);

        int exit2 = await cmd2.ExecuteAsync(args2);
        Assert.Equal(0, exit2);
        Assert.Contains("DỰNG HÌNH HOÀN TẤT", sw2.ToString());
        Assert.Equal(totalCreatedInRun1, store.Count);

        VerifyDispatchedTargetUniqueIds(capturedRequests, store);
    }

    private static void VerifyDispatchedTargetUniqueIds(
        List<ExecuteRequest> requests,
        Dictionary<string, (string SourceKey, string Category, string Mark, long ElementId)> store)
    {
        var stageRequests = requests.Where(r => r.Label == "HPBim Execution Plan").ToList();
        Assert.NotEmpty(stageRequests);

        foreach (var req in stageRequests)
        {
            Assert.True(req.Args.HasValue);
            var root = req.Args.Value;
            Assert.True(root.TryGetProperty("items", out var itemsProp));
            foreach (var item in itemsProp.EnumerateArray())
            {
                string targetUid = item.TryGetProperty("targetUniqueId", out var tup) ? tup.GetString() ?? "" : "";
                Assert.False(string.IsNullOrEmpty(targetUid), "TargetUniqueId must be resolved from disk ledger for all items in Process 2.");
                Assert.True(store.ContainsKey(targetUid), $"TargetUniqueId '{targetUid}' must exist in Revit element store.");
            }
        }
    }

    [Fact]
    public async Task GenerateCommand_ColdProcessRerun_DefaultLedgerPath_CreatesDirectoryAndPersists()
    {
        string modelPath = Path.Combine(_tempDir, "model.json");
        string defaultLedgerPath = Path.Combine(_tempDir, "state", "bim_coordination_ledger.json");

        var model = NhaPho5x18mFixture.CreateModel();
        await BimModelSerializer.SerializeToFileAsync(model, modelPath);

        var revitElementStore = new Dictionary<string, (string SourceKey, string Category, string Mark, long ElementId)>();
        long elementIdCounter = 5000;

        var fakePipe = new FakeRevitPipeClient();
        fakePipe.CustomExecuteHandler = req => SimulateRevitExecution(req, revitElementStore, ref elementIdCounter);

        using var sw = new StringWriter();
        var cmd = new GenerateCommand(new ConsoleFormatter(sw), fakePipe);
        var args = CliParser.Parse(["generate", "--input", modelPath, "--ledger", defaultLedgerPath]);

        int exit = await cmd.ExecuteAsync(args);
        Assert.Equal(0, exit);
        Assert.True(File.Exists(defaultLedgerPath));
    }

    [Fact]
    public async Task DurableLedgerFirst_WhenLedgerPersistenceFails_PreservesOutcomeUnknownAndRefusesCommitted()
    {
        string journalPath = Path.Combine(_tempDir, $"journal_{Guid.NewGuid():N}.jsonl");
        using var journal = new HPBim.Client.Connection.IntentJournal(journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();

        // Point LedgerFilePath to an invalid directory path that throws on write
        string invalidLedgerPath = Path.Combine(_tempDir, "invalid\0path", "ledger.json");

        var engine = new HPBim.Client.Pipeline.BimPipelineEngine(fakePipe, intentJournal: journal)
        {
            LedgerService = ledger,
            LedgerFilePath = invalidLedgerPath
        };

        var model = NhaPho5x18mFixture.CreateModel();
        var plan = HPBim.Client.Pipeline.ExecutionPlanBuilder.Build(model, options: new HPBim.Core.Execution.ExecutionOptions { DryRun = false });

        var result = await engine.ExecutePlanJournaledAsync(plan, ledger, default);

        Assert.False(result.Success);
        Assert.True(result.IsOutcomeUnknown);

        var intentRecord = await journal.GetIntentAsync(plan.Options.OperationId);
        Assert.NotNull(intentRecord);
        Assert.Equal(HPBim.Client.Connection.IntentStatus.OutcomeUnknown, intentRecord.Status);
        Assert.Contains("Failed to persist coordination ledger to disk", intentRecord.ErrorReason ?? "");
    }

    private static ExecuteResult SimulateRevitExecution(
        ExecuteRequest request,
        Dictionary<string, (string SourceKey, string Category, string Mark, long ElementId)> store,
        ref long nextId)
    {
        if (request.Label == "Query Active Document Identity")
        {
            return CreateSimulatedActiveDocIdentity();
        }

        if (request.Args.HasValue && request.Args.Value.ValueKind == JsonValueKind.Object &&
            request.Args.Value.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
        {
            return SimulatePlanItemsExecution(request, itemsProp, store, ref nextId);
        }

        return new ExecuteResult { IsError = false, DurationMs = 5 };
    }

    private static ExecuteResult CreateSimulatedActiveDocIdentity()
    {
        var docPayload = new
        {
            title = "TestModel.rvt", pathName = @"C:\TestModel.rvt",
            versionGuid = Guid.Parse("11111111-2222-3333-4444-555555555555").ToString("D"),
            numberOfSaves = 1, centralGuid = Guid.Empty.ToString("D"), activeViewName = "{3D}"
        };
        return new ExecuteResult
        {
            IsError = false,
            Value = JsonDocument.Parse(JsonSerializer.Serialize(docPayload)).RootElement.Clone(),
            DurationMs = 5
        };
    }

    private static ExecuteResult SimulatePlanItemsExecution(
        ExecuteRequest request,
        JsonElement itemsProp,
        Dictionary<string, (string SourceKey, string Category, string Mark, long ElementId)> store,
        ref long nextId)
    {
        var simulatedRecords = new List<object>();
        int addedCount = 0;
        foreach (var item in itemsProp.EnumerateArray())
        {
            simulatedRecords.Add(CreateSimulatedItemRecord(item, store, ref nextId, ref addedCount));
        }

        var payload = new
        {
            planId = request.Args!.Value.TryGetProperty("planId", out var pip) ? pip.GetString() : "",
            total = simulatedRecords.Count,
            records = simulatedRecords
        };

        return new ExecuteResult
        {
            IsError = false,
            Value = JsonDocument.Parse(JsonSerializer.Serialize(payload)).RootElement.Clone(),
            Changed = new ChangedCounts(addedCount, 0, 0),
            RolledBack = request.DryRun,
            DurationMs = 20
        };
    }

    private static object CreateSimulatedItemRecord(
        JsonElement item,
        Dictionary<string, (string SourceKey, string Category, string Mark, long ElementId)> store,
        ref long nextId,
        ref int addedCount)
    {
        string sk = item.TryGetProperty("sourceKey", out var skp) ? skp.GetString() ?? "" : "";
        string cat = item.TryGetProperty("category", out var cp) ? cp.GetString() ?? "" : "";
        string mark = item.TryGetProperty("mark", out var mp) ? mp.GetString() ?? "" : "";
        string targetUid = item.TryGetProperty("targetUniqueId", out var tup) ? tup.GetString() ?? "" : "";

        if (!string.IsNullOrEmpty(targetUid) && store.TryGetValue(targetUid, out var existing))
        {
            return new { sourceKey = sk, category = cat, mark = mark, status = "NoOp", targetUniqueId = targetUid, targetElementId = existing.ElementId };
        }

        long newId = ++nextId;
        string newUid = $"rvt-uid-{mark}-{newId}";
        store[newUid] = (sk, cat, mark, newId);
        addedCount++;
        return new { sourceKey = sk, category = cat, mark = mark, status = "Created", targetUniqueId = newUid, targetElementId = newId };
    }
}
