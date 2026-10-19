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

public sealed class ReconcileDocumentIdentityTests : IDisposable
{
    private readonly string _journalPath;

    public ReconcileDocumentIdentityTests()
    {
        _journalPath = Path.Combine(Path.GetTempPath(), $"doc_identity_{Guid.NewGuid():N}.jsonl");
    }

    public void Dispose()
    {
        if (File.Exists(_journalPath))
        {
            File.Delete(_journalPath);
        }
    }

    [Fact]
    public async Task MatchDocumentIdentity_SameTitleDifferentPath_ReturnsFalse_PreservesOutcomeUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-4",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Project.rvt",
                PathName = @"C:\Production\Project.rvt"
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-4",
            TargetDocTitle = "Project.rvt",
            TargetDocPath = @"C:\Backups\Project.rvt",
            ElementMap = new Dictionary<string, string>
            {
                ["Doc#Column:C1"] = "uid-1"
            }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task MatchDocumentIdentity_SameTitleDifferentVersionGuid_SucceedsAndMarksCommitted()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        var ver1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var ver2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-5",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Project.rvt",
                VersionGuid = ver1
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-5",
            TargetDocTitle = "Project.rvt",
            TargetVersionGuid = ver2.ToString("D"),
            ElementMap = new Dictionary<string, string>
            {
                ["Doc#Column:C1"] = "uid-1"
            }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Single(ledger.Mappings);
        var updated = await journal.GetIntentAsync(opId);
        Assert.NotNull(updated);
        Assert.Equal(IntentStatus.Committed, updated.Status);
    }

    [Fact]
    public async Task MatchDocumentIdentity_SameTitleDifferentCentralGuid_ReturnsFalse_PreservesOutcomeUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        var central1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var central2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-6",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Project.rvt",
                CentralGuid = central1
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-6",
            TargetDocTitle = "Project.rvt",
            TargetCentralGuid = central2.ToString("D"),
            ElementMap = new Dictionary<string, string>
            {
                ["Doc#Column:C1"] = "uid-1"
            }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task MatchDocumentIdentity_AllVectorsMatch_SucceedsAndMarksCommitted()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        var ver = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var central = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-7",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 2,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Project.rvt",
                PathName = @"C:\Bim\Project.rvt",
                VersionGuid = ver,
                CentralGuid = central
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-7",
            TargetDocTitle = "Project.rvt",
            TargetDocPath = @"C:\Bim\Project.rvt",
            TargetVersionGuid = ver.ToString("D"),
            TargetCentralGuid = central.ToString("D"),
            ElementMap = new Dictionary<string, string>
            {
                ["Doc#Column:C1"] = "uid-c1",
                ["Doc#Column:C2"] = "uid-c2"
            }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Equal(2, ledger.Mappings.Count);

        var updated = await journal.GetIntentAsync(opId);
        Assert.NotNull(updated);
        Assert.Equal(IntentStatus.Committed, updated.Status);
    }
}
