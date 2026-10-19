namespace HPBim.Client.Tests.Recovery;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Recovery;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

/// <summary>
/// Additional adversarial payload tests for ExtensibleStorageReceiptReader and ReconcileService.
/// </summary>
public sealed class ChallengerM2AdversarialPayloadTests : IDisposable
{
    private readonly string _journalPath;

    public ChallengerM2AdversarialPayloadTests()
    {
        _journalPath = Path.Combine(Path.GetTempPath(), $"adv_payload_{Guid.NewGuid():N}.jsonl");
    }

    public void Dispose()
    {
        if (File.Exists(_journalPath))
        {
            File.Delete(_journalPath);
        }
    }

    [Theory]
    [InlineData("{\"key\": \"\\u12\"}")] // invalid unicode escape
    [InlineData("{\"key\": [1, 2, 3]}")] // array value
    [InlineData("{\"key\": {\"nested\": \"val\"}}")] // nested object
    [InlineData("/* comment */ {\"key\": \"val\"}")] // comments not allowed
    [InlineData("{\"key\": true}")] // boolean value
    [InlineData("{'key': 'single-quotes'}")] // single quotes
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    public void ParseElementMap_ExoticMalformedJson_ReturnsNull(string invalidJson)
    {
        var result = ExtensibleStorageReceiptReader.ParseElementMap(invalidJson);
        Assert.Null(result);
    }

    [Fact]
    public void ParseElementMap_TruncatedSurrogate_ReturnsNull()
    {
        string truncatedSurrogate = "{\"key\": \"\uD83D\"}"; // unpaired high surrogate
        var result = ExtensibleStorageReceiptReader.ParseElementMap(truncatedSurrogate);
        Assert.Null(result);
    }

    [Fact]
    public void ParseReceipts_WithNonStringPropertyValues_SafelyDefaultsWithoutThrowing()
    {
        string raw = """
        {
            "receipts": [
                {
                    "operationId": 12345,
                    "planHash": true,
                    "elementMapJson": { "nested": "object" }
                }
            ]
        }
        """;
        using var doc = JsonDocument.Parse(raw);
        var receipts = ExtensibleStorageReceiptReader.ParseReceipts(doc.RootElement);
        Assert.NotNull(receipts);
        Assert.Single(receipts);
        Assert.Equal(Guid.Empty, receipts[0].OperationId);
        Assert.Equal(string.Empty, receipts[0].PlanHash);
        Assert.Null(receipts[0].ElementMap);
    }


    [Fact]
    public async Task ReconcileIntent_WhenTargetCentralGuidMismatched_PreservesOutcomeUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();
        var central1 = Guid.NewGuid();
        var central2 = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "plan-central-mismatch",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot { Title = "TowerB.rvt", CentralGuid = central1 }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "plan-central-mismatch",
            TargetDocTitle = "TowerB.rvt",
            TargetCentralGuid = central2.ToString("D"),
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "uid-1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
    }

    [Fact]
    public async Task ReconcileIntent_PlanHashCaseInsensitiveMatch_Succeeds()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "HASH-ABC-123",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-abc-123", // lower case
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "uid-1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Single(ledger.Mappings);
    }

    [Fact]
    public async Task ReconcileIntent_ZeroItemPlan_WithZeroItemReceipt_MatchesAndCommits()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "plan-zero",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 0
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "plan-zero",
            ElementMap = new Dictionary<string, string>() // empty map
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task ReconcileIntent_ZeroItemPlan_WithOneItemReceipt_MismatchesAndRefusesCommitted()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "plan-zero-mismatch",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 0
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "plan-zero-mismatch",
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "uid-1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task ReconcileIntent_PipeReturnsCorruptedSurrogateJson_PreservesOutcomeUnknownWithoutThrowing()
    {
        using var journal = new IntentJournal(_journalPath);
        var opId = Guid.NewGuid();
        var fakePipe = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req => req.Label == "Query Extensible Storage Receipts"
                ? CreateCorruptedSurrogateResult(opId)
                : new ExecuteResult { IsError = false }
        };

        var ledger = new BimCoordinationLedgerService();
        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "plan-surrogate",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.OutcomeUnknown, record.Status);
    }

    private static ExecuteResult CreateCorruptedSurrogateResult(Guid opId)
    {
        string rawJson = $$"""
        {
            "count": 1,
            "receipts": [{ "operationId": "{{opId}}", "planHash": "plan-surrogate", "elementMapJson": "{\"key\": \"\uD83D\"}" }]
        }
        """;
        using var doc = JsonDocument.Parse(rawJson);
        return new ExecuteResult { IsError = false, Value = doc.RootElement.Clone() };
    }
}
