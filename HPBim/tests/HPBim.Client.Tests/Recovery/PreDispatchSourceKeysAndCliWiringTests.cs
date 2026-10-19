namespace HPBim.Client.Tests.Recovery;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HPBim.Cli.CommandLine;
using HPBim.Cli.Commands;
using HPBim.Cli.Formatting;
using HPBim.Client.Connection;
using HPBim.Client.Pipeline;
using HPBim.Client.Recovery;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using Xunit;

public sealed class PreDispatchSourceKeysAndCliWiringTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _journalPath;
    private readonly string _ledgerPath;

    public PreDispatchSourceKeysAndCliWiringTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hpbim_wire_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _journalPath = Path.Combine(_tempDir, "intent_journal.jsonl");
        _ledgerPath = Path.Combine(_tempDir, "bim_coordination_ledger.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
            // Transient lock cleanup tolerance
        }
    }

    [Fact]
    public async Task PreDispatchIntent_PopulatesSourceKeysFromPlanItems()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient { ShouldTimeoutOnExecute = true };
        var engine = new BimPipelineEngine(fakePipe, intentJournal: journal);

        var key1 = CompositeSourceKey.Create("DocA", CoordinationCategory.Column, "C1");
        var key2 = CompositeSourceKey.Create("DocA", CoordinationCategory.Wall, "W1");
        var plan = CreatePlan(key1, key2);

        var result = await engine.ExecutePlanJournaledAsync(plan);
        Assert.True(result.IsOutcomeUnknown);

        var recorded = await journal.GetIntentAsync(plan.Options.OperationId);
        Assert.NotNull(recorded);
        Assert.Equal(2, recorded.ItemCount);
        Assert.NotNull(recorded.SourceKeys);
        Assert.Equal(2, recorded.SourceKeys.Count);
        Assert.Contains(key1.ToString(), recorded.SourceKeys);
        Assert.Contains(key2.ToString(), recorded.SourceKeys);
    }

    [Fact]
    public async Task RealPipelineIntent_WhenReceiptHasForeignKeys_ReconcileRejectsWithOutcomeUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient { ShouldTimeoutOnExecute = true };
        var ledger = new BimCoordinationLedgerService();
        var engine = new BimPipelineEngine(fakePipe, intentJournal: journal) { LedgerService = ledger };

        var key1 = CompositeSourceKey.Create("DocA", CoordinationCategory.Column, "C1");
        var plan = CreatePlan(key1);
        await engine.ExecutePlanJournaledAsync(plan);

        fakePipe.ShouldTimeoutOnExecute = false;
        var foreignKey = CompositeSourceKey.Create("DocA", CoordinationCategory.Wall, "ForeignW").ToString();
        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = plan.Options.OperationId,
            PlanHash = plan.PlanHash,
            ElementMap = new Dictionary<string, string> { [foreignKey] = "uid-foreign" }
        });

        var reconcileService = new ReconcileService(journal, fakePipe, ledger, ledgerFilePath: _ledgerPath);
        var reconcileResult = await reconcileService.ReconcileIntentAsync(plan.Options.OperationId);

        Assert.Equal(ReconcileStatus.StillUnknown, reconcileResult.Status);
        Assert.Contains("missing=1, foreign=1", reconcileResult.Message);

        var record = await journal.GetIntentAsync(plan.Options.OperationId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
    }

    [Fact]
    public async Task CoordinationCommand_Reconcile_PersistsLedgerViaConfiguredPath()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        ledger.Initialize("Cli Wire Test");
        ledger.SaveToFile(_ledgerPath);

        var key1 = CompositeSourceKey.Create("DocA", CoordinationCategory.Column, "C1");
        var opId = Guid.NewGuid();
        await journal.RecordIntentAsync(CreateIncompleteIntent(opId, "hash-cli-wire", key1.ToString()));
        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-cli-wire",
            ElementMap = new Dictionary<string, string> { [key1.ToString()] = "uid-col-1" }
        });

        using var sw = new StringWriter();
        var cmd = new CoordinationCommand(new ConsoleFormatter(sw), ledger, pipeClient: fakePipe, journal: journal);
        var args = CliParser.Parse(["coord", "reconcile", "--ledger", _ledgerPath, "--journal", _journalPath]);
        int exitCode = await cmd.ExecuteAsync(args);

        Assert.Equal(0, exitCode);
        Assert.Contains("[COMMITTED]", sw.ToString());
        var loadedLedger = ledger.LoadFromFile(_ledgerPath);
        Assert.Single(loadedLedger.Mappings);
        Assert.Equal("uid-col-1", loadedLedger.Mappings.Values.First().TargetUniqueId);

        var finalIntent = await journal.GetIntentAsync(opId);
        Assert.NotNull(finalIntent);
        Assert.Equal(IntentStatus.Committed, finalIntent.Status);
    }

    [Fact]
    public async Task CoordinationCommand_Reconcile_WhenLedgerUnwritable_PreservesOutcomeUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        ledger.Initialize("Readonly Test");
        ledger.SaveToFile(_ledgerPath);

        var key1 = CompositeSourceKey.Create("DocA", CoordinationCategory.Column, "C1");
        var opId = Guid.NewGuid();
        await journal.RecordIntentAsync(CreateIncompleteIntent(opId, "hash-fail-save", key1.ToString()));
        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-fail-save",
            ElementMap = new Dictionary<string, string> { [key1.ToString()] = "uid-1" }
        });

        File.SetAttributes(_ledgerPath, FileAttributes.ReadOnly);
        try
        {
            using var sw = new StringWriter();
            var cmd = new CoordinationCommand(new ConsoleFormatter(sw), ledger, pipeClient: fakePipe, journal: journal);
            var args = CliParser.Parse(["coord", "reconcile", "--ledger", _ledgerPath, "--journal", _journalPath]);
            int exitCode = await cmd.ExecuteAsync(args);

            Assert.Equal(1, exitCode);
            Assert.Contains("[UNKNOWN]", sw.ToString());
            var finalIntent = await journal.GetIntentAsync(opId);
            Assert.NotNull(finalIntent);
            Assert.Equal(IntentStatus.OutcomeUnknown, finalIntent.Status);
            Assert.False(finalIntent.IsTerminal);
        }
        finally
        {
            File.SetAttributes(_ledgerPath, FileAttributes.Normal);
        }
    }

    private static IntentJournalRecord CreateIncompleteIntent(Guid opId, string hash, params string[] keys) =>
        new() { OperationId = opId, PlanHash = hash, Status = IntentStatus.OutcomeUnknown, ItemCount = keys.Length, SourceKeys = keys };

    private static BimExecutionPlan CreatePlan(params CompositeSourceKey[] keys)
    {
        var items = keys.Select(k => new ElementPlanItem
        {
            SourceKey = k,
            Category = k.Category,
            Mark = k.PrimaryHandle,
            TargetLevelName = "Level 1"
        }).ToList();

        return new BimExecutionPlan
        {
            Items = items,
            Options = new ExecutionOptions
            {
                OperationId = Guid.NewGuid(),
                DryRun = false
            }
        };
    }
}
