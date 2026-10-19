namespace HPBim.Client.Tests.Recovery;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Recovery;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using Xunit;

/// <summary>
/// Empirical adversarial verification harness for Milestone M2 (Receipt Completeness & Reconcile Resilience).
/// Stress-tests corrupt JSON, truncated payloads, noise, cardinality mismatch, and target document divergence.
/// </summary>
public sealed class ChallengerM2StressTests : IDisposable
{
    private readonly string _journalPath;

    public ChallengerM2StressTests()
    {
        _journalPath = Path.Combine(Path.GetTempPath(), $"challenger_m2_stress_{Guid.NewGuid():N}.jsonl");
    }

    public void Dispose()
    {
        if (File.Exists(_journalPath))
        {
            File.Delete(_journalPath);
        }
    }

    [Theory]
    [InlineData("{ unclosed json")]
    [InlineData("{\"Doc#Column:C1\": \"uid-1\"")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\0\u0001\u0002\u00FF\uFEFF!@#$%^&*()")]
    [InlineData("12345")]
    [InlineData("true")]
    [InlineData("[1, 2, 3]")]
    [InlineData("{\"Doc#Column:C1\": 12345}")]
    public async Task CorruptJsonAndNoise_ReceiptStorage_PreservesOutcomeUnknown_RefusesCommitted(string rawJson)
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "plan-noise-test",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1
        });

        var parsedMap = ExtensibleStorageReceiptReader.ParseElementMap(rawJson);
        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "plan-noise-test",
            ElementMap = parsedMap
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);

        var updatedIntent = await journal.GetIntentAsync(opId);
        Assert.NotNull(updatedIntent);
        Assert.Equal(IntentStatus.OutcomeUnknown, updatedIntent.Status);
    }

    [Theory]
    [InlineData(5, 1)] // 1 when intent expects 5
    [InlineData(5, 0)] // 0 when intent expects 5
    [InlineData(1, 5)] // 5 when intent expects 1
    [InlineData(4, 3)] // 3 when intent expects 4
    [InlineData(2, 0)] // empty map when intent expects 2
    public async Task IncompleteReceipt_CardinalityMismatch_PreservesOutcomeUnknown_RefusesCommitted(int expectedCount, int actualCount)
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "plan-cardinality",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = expectedCount
        });

        var map = new Dictionary<string, string>();
        for (int i = 0; i < actualCount; i++)
        {
            map[$"[Doc]#[Column]:C{i + 1}"] = $"uid-{i + 1}";
        }

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "plan-cardinality",
            ElementMap = map
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
    public async Task TargetDocumentMismatch_SameTitleDifferentPathName_PreservesOutcomeUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "plan-doc-path-mismatch",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "TowerA.rvt",
                PathName = @"D:\Projects\Phase1\TowerA.rvt"
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "plan-doc-path-mismatch",
            TargetDocTitle = "TowerA.rvt",
            TargetDocPath = @"D:\Clones\TowerA_Copy.rvt",
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "uid-c1" }
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
    public async Task TargetDocumentVersionGuidChange_DoesNotFailRecovery_SucceedsAndMarksCommitted()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        var expectedGuid = Guid.NewGuid();
        var receiptGuid = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "plan-doc-ver-mismatch",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "TowerA.rvt",
                VersionGuid = expectedGuid
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "plan-doc-ver-mismatch",
            TargetDocTitle = "TowerA.rvt",
            TargetVersionGuid = receiptGuid.ToString("D"),
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "uid-c1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Single(ledger.Mappings);
        var record = await journal.GetIntentAsync(opId);
        Assert.NotNull(record);
        Assert.Equal(IntentStatus.Committed, record.Status);
    }

    [Theory]
    [InlineData("InvalidKeyFormatWithoutDocOrCategory", "uid-1")]
    [InlineData("[Doc]#[Column]:C1", "")]
    [InlineData("[Doc]#[Column]:C1", "   ")]
    public async Task InvalidSourceKeyOrEmptyUniqueId_PreservesOutcomeUnknown_RefusesCommitted(string key, string uid)
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "plan-bad-keys",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "plan-bad-keys",
            ElementMap = new Dictionary<string, string> { [key] = uid }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task AllFactorsAndPayloadValid_SuccessfullyCommitsAndPopulatesLedger()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();
        var docGuid = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "plan-happy-path",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 2,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "TowerA.rvt",
                PathName = @"D:\Projects\TowerA.rvt",
                VersionGuid = docGuid
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "plan-happy-path",
            TargetDocTitle = "TowerA.rvt",
            TargetDocPath = @"D:\Projects\TowerA.rvt",
            TargetVersionGuid = docGuid.ToString("D"),
            ElementMap = new Dictionary<string, string>
            {
                ["[Doc]#[Column]:C1"] = "revit-unique-id-1",
                ["[Doc]#[Beam]:B1"] = "revit-unique-id-2"
            }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Equal(2, ledger.Mappings.Count);
        var rec = await journal.GetIntentAsync(opId);
        Assert.NotNull(rec);
        Assert.Equal(IntentStatus.Committed, rec.Status);
    }
}
