namespace HPBim.Client.Tests.Cli;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Commands;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Serialization;
using HPBim.Core.Tests.Fixtures;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class EmpiricalChallengerColdProcessRerunStressTests : IDisposable
{
    private readonly string _tempDir;

    public EmpiricalChallengerColdProcessRerunStressTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"emp_challenger_cold_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); }
            catch (IOException) { /* best-effort cleanup */ }
        }
    }

    [Fact]
    public async Task ColdRerun_Produces100PercentNoOp_ZeroCreated_AcrossThreeColdProcesses()
    {
        string modelPath = Path.Combine(_tempDir, "model.json");
        string ledgerPath = Path.Combine(_tempDir, "state", "bim_coordination_ledger.json");
        await BimModelSerializer.SerializeToFileAsync(NhaPho5x18mFixture.CreateModel(), modelPath);

        var revitStore = new Dictionary<string, (string SourceKey, string Cat, string Mark, long Id)>();
        long counter = 2000;

        // Process 1: Initial Generation
        var fakePipe1 = new FakeRevitPipeClient { CustomExecuteHandler = req => HandleRevit(req, revitStore, ref counter) };
        using var sw1 = new StringWriter();
        var cmd1 = new GenerateCommand(new ConsoleFormatter(sw1), fakePipe1);
        int exit1 = await cmd1.ExecuteAsync(CliParser.Parse(["generate", "--input", modelPath, "--ledger", ledgerPath]));
        Assert.Equal(0, exit1);
        int initialCount = revitStore.Count;
        Assert.True(initialCount > 0);

        // Process 2: First Cold Rerun
        var captured2 = new List<ExecuteResult>();
        var fakePipe2 = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req =>
            {
                var res = HandleRevit(req, revitStore, ref counter);
                if (req.Label == "HPBim Execution Plan") captured2.Add(res);
                return res;
            }
        };
        using var sw2 = new StringWriter();
        var cmd2 = new GenerateCommand(new ConsoleFormatter(sw2), fakePipe2);
        int exit2 = await cmd2.ExecuteAsync(CliParser.Parse(["generate", "--input", modelPath, "--ledger", ledgerPath]));
        Assert.Equal(0, exit2);
        Assert.Equal(initialCount, revitStore.Count);
        string out2 = sw2.ToString();
        Assert.Contains("Tổng element tạo mới", out2);
        Assert.Contains("| 0", out2);

        // Process 3: Second Cold Rerun
        var captured3 = new List<ExecuteResult>();
        var fakePipe3 = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req =>
            {
                var res = HandleRevit(req, revitStore, ref counter);
                if (req.Label == "HPBim Execution Plan") captured3.Add(res);
                return res;
            }
        };
        using var sw3 = new StringWriter();
        var cmd3 = new GenerateCommand(new ConsoleFormatter(sw3), fakePipe3);
        int exit3 = await cmd3.ExecuteAsync(CliParser.Parse(["generate", "--input", modelPath, "--ledger", ledgerPath]));
        Assert.Equal(0, exit3);
        Assert.Equal(initialCount, revitStore.Count);
        AssertAllRecordsAreNoOp(captured3, initialCount);
    }

    [Fact]
    public async Task DurableLedgerFirst_StrictOrderingProof_LedgerSavedBeforeJournalCommitted()
    {
        string journalPath = Path.Combine(_tempDir, $"journal_{Guid.NewGuid():N}.jsonl");
        string ledgerPath = Path.Combine(_tempDir, "state", "ledger_ordering_proof.json");
        using var baseJournal = new IntentJournal(journalPath);
        var trackingJournal = new OrderingTrackingJournal(baseJournal, ledgerPath);

        var revitStore = new Dictionary<string, (string SourceKey, string Cat, string Mark, long Id)>();
        long counter = 3000;
        var fakePipe = new FakeRevitPipeClient { CustomExecuteHandler = req => HandleRevit(req, revitStore, ref counter) };

        var ledgerService = new BimCoordinationLedgerService();
        ledgerService.Initialize("Ordering Proof Project");

        var engine = new BimPipelineEngine(fakePipe, intentJournal: trackingJournal)
        {
            LedgerService = ledgerService,
            LedgerFilePath = ledgerPath
        };

        var model = NhaPho5x18mFixture.CreateModel();
        var plan = ExecutionPlanBuilder.Build(model, options: new ExecutionOptions { DryRun = false });

        var result = await engine.ExecutePlanJournaledAsync(plan, ledgerService, default);

        Assert.True(result.Success);
        Assert.True(trackingJournal.LedgerExistedAtCommitTime, "Ledger file MUST exist on disk when journal commit happens!");
        Assert.True(trackingJournal.LedgerMappingCountAtCommitTime > 0, "Ledger MUST be populated on disk BEFORE journal Committed!");
        Assert.Contains("JournalCommitted", trackingJournal.EventOrder);
    }

    [Fact]
    public async Task ColdRerun_WithDryRun_DoesNotModifyLedgerFileOrCorruptMappings()
    {
        string modelPath = Path.Combine(_tempDir, "model.json");
        string ledgerPath = Path.Combine(_tempDir, "state", "bim_coordination_ledger.json");
        var model = NhaPho5x18mFixture.CreateModel();
        await BimModelSerializer.SerializeToFileAsync(model, modelPath);

        var revitStore = new Dictionary<string, (string SourceKey, string Cat, string Mark, long Id)>();
        long counter = 4000;

        // Run 1: Apply to create ledger
        var fakePipe1 = new FakeRevitPipeClient { CustomExecuteHandler = req => HandleRevit(req, revitStore, ref counter) };
        using var sw1 = new StringWriter();
        var cmd1 = new GenerateCommand(new ConsoleFormatter(sw1), fakePipe1);
        await cmd1.ExecuteAsync(CliParser.Parse(["generate", "--input", modelPath, "--ledger", ledgerPath]));

        var ledgerBefore = BimCoordinationLedgerSerializer.DeserializeFromFile(ledgerPath);
        var writeTimeBefore = File.GetLastWriteTimeUtc(ledgerPath);

        // Run 2: Dry Run cold invocation
        await Task.Delay(50); // slight time gap
        var fakePipe2 = new FakeRevitPipeClient { CustomExecuteHandler = req => HandleRevit(req, revitStore, ref counter) };
        using var sw2 = new StringWriter();
        var cmd2 = new GenerateCommand(new ConsoleFormatter(sw2), fakePipe2);
        int dryExit = await cmd2.ExecuteAsync(CliParser.Parse(["generate", "--input", modelPath, "--ledger", ledgerPath, "--dry-run"]));

        Assert.Equal(0, dryExit);
        var ledgerAfter = BimCoordinationLedgerSerializer.DeserializeFromFile(ledgerPath);
        var writeTimeAfter = File.GetLastWriteTimeUtc(ledgerPath);

        Assert.Equal(ledgerBefore.Mappings.Count, ledgerAfter.Mappings.Count);
        Assert.Equal(writeTimeBefore, writeTimeAfter);
    }

    private static void AssertAllRecordsAreNoOp(List<ExecuteResult> captured, int expectedCount)
    {
        Assert.NotEmpty(captured);
        int verifiedRecords = 0;
        foreach (var execRes in captured)
        {
            Assert.True(execRes.Value.HasValue);
            var root = execRes.Value.Value;
            Assert.True(root.TryGetProperty("records", out var recsProp));
            foreach (var rec in recsProp.EnumerateArray())
            {
                string status = rec.GetProperty("status").GetString() ?? "";
                Assert.Equal("NoOp", status);
                verifiedRecords++;
            }
        }
        Assert.Equal(expectedCount, verifiedRecords);
    }

    private static ExecuteResult HandleRevit(
        ExecuteRequest req,
        Dictionary<string, (string SourceKey, string Cat, string Mark, long Id)> store,
        ref long nextId)
    {
        if (req.Label == "Query Active Document Identity")
        {
            var doc = new
            {
                title = "Model.rvt",
                pathName = @"C:\Model.rvt",
                versionGuid = Guid.NewGuid().ToString("D"),
                numberOfSaves = 1,
                centralGuid = Guid.Empty.ToString("D"),
                activeViewName = "{3D}"
            };
            return new ExecuteResult { IsError = false, Value = JsonDocument.Parse(JsonSerializer.Serialize(doc)).RootElement.Clone() };
        }

        if (req.Args.HasValue && req.Args.Value.TryGetProperty("items", out var items))
        {
            var records = new List<object>();
            int added = 0;
            foreach (var it in items.EnumerateArray())
            {
                string sk = it.TryGetProperty("sourceKey", out var p1) ? p1.GetString() ?? "" : "";
                string cat = it.TryGetProperty("category", out var p2) ? p2.GetString() ?? "" : "";
                string mark = it.TryGetProperty("mark", out var p3) ? p3.GetString() ?? "" : "";
                string targetUid = it.TryGetProperty("targetUniqueId", out var p4) ? p4.GetString() ?? "" : "";

                if (!string.IsNullOrEmpty(targetUid) && store.TryGetValue(targetUid, out var existing))
                {
                    records.Add(new { sourceKey = sk, category = cat, mark = mark, status = "NoOp", targetUniqueId = targetUid, targetElementId = existing.Id });
                }
                else
                {
                    long id = ++nextId;
                    string uid = $"uid-{mark}-{id}";
                    store[uid] = (sk, cat, mark, id);
                    added++;
                    records.Add(new { sourceKey = sk, category = cat, mark = mark, status = "Created", targetUniqueId = uid, targetElementId = id });
                }
            }
            var payload = new { planId = "plan-1", total = records.Count, records };
            return new ExecuteResult
            {
                IsError = false,
                Value = JsonDocument.Parse(JsonSerializer.Serialize(payload)).RootElement.Clone(),
                Changed = new ChangedCounts(added, 0, 0),
                RolledBack = req.DryRun
            };
        }

        return new ExecuteResult { IsError = false };
    }

    private sealed class OrderingTrackingJournal : IIntentJournal
    {
        private readonly IIntentJournal _inner;
        private readonly string _ledgerFilePath;
        public bool LedgerExistedAtCommitTime { get; private set; }
        public int LedgerMappingCountAtCommitTime { get; private set; }
        public List<string> EventOrder { get; } = new();

        public OrderingTrackingJournal(IIntentJournal inner, string ledgerFilePath)
        {
            _inner = inner;
            _ledgerFilePath = ledgerFilePath;
        }

        public string JournalFilePath => _inner.JournalFilePath;

        public Task RecordIntentAsync(IntentJournalRecord record, CancellationToken ct = default)
        {
            EventOrder.Add("JournalInFlight");
            return _inner.RecordIntentAsync(record, ct);
        }

        public async Task UpdateStatusAsync(Guid opId, IntentStatus status, string? error = null, CancellationToken ct = default)
        {
            if (status == IntentStatus.Committed && File.Exists(_ledgerFilePath))
            {
                LedgerExistedAtCommitTime = true;
                try
                {
                    var ledger = BimCoordinationLedgerSerializer.DeserializeFromFile(_ledgerFilePath);
                    LedgerMappingCountAtCommitTime = ledger.Mappings?.Count ?? 0;
                }
                catch (Exception) { LedgerMappingCountAtCommitTime = -1; }
                EventOrder.Add("JournalCommitted");
            }
            await _inner.UpdateStatusAsync(opId, status, error, ct);
        }

        public Task<IReadOnlyList<IntentJournalRecord>> GetIncompleteIntentsAsync(CancellationToken ct = default) => _inner.GetIncompleteIntentsAsync(ct);
        public Task<IntentJournalRecord?> GetIntentAsync(Guid opId, CancellationToken ct = default) => _inner.GetIntentAsync(opId, ct);
        public Task<int> PruneOldEntriesAsync(TimeSpan olderThan, CancellationToken ct = default) => _inner.PruneOldEntriesAsync(olderThan, ct);
        public ValueTask DisposeAsync() => _inner.DisposeAsync();
        public void Dispose() => _inner.Dispose();
    }
}
