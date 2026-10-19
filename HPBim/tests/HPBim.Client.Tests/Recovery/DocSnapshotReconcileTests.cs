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
using HPBim.Core.Models;
using Xunit;

public sealed class DocSnapshotReconcileTests : IDisposable
{
    private readonly string _journalPath;

    public DocSnapshotReconcileTests()
    {
        _journalPath = Path.Combine(Path.GetTempPath(), $"doc_snap_{Guid.NewGuid():N}.jsonl");
    }

    public void Dispose()
    {
        if (File.Exists(_journalPath))
        {
            File.Delete(_journalPath);
        }
    }

    [Fact]
    public async Task DynamicVersionGuidChange_DoesNotRejectValidReceipt_ReconcileSucceeds()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();
        var central = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-dyn-1",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "BlockA.rvt",
                PathName = @"C:\Bim\BlockA.rvt",
                CentralGuid = central,
                VersionGuid = Guid.NewGuid()
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-dyn-1",
            TargetDocTitle = "BlockA.rvt",
            TargetDocPath = @"C:\Bim\BlockA.rvt",
            TargetCentralGuid = central.ToString("D"),
            TargetVersionGuid = Guid.NewGuid().ToString("D"),
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "uid-col-1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Single(ledger.Mappings);
        Assert.Equal("uid-col-1", ledger.Mappings["[Doc]#[Column]:C1"].TargetUniqueId);
        var updated = await journal.GetIntentAsync(opId);
        Assert.NotNull(updated);
        Assert.Equal(IntentStatus.Committed, updated.Status);
    }

    [Fact]
    public async Task MultipleElementMutations_VersionGuidAdvancedMultipleTimes_RecoveryStillSucceeds()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-multi",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 2,
            TargetDoc = new DocumentIdentitySnapshot { Title = "Tower.rvt", VersionGuid = Guid.NewGuid() }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-multi",
            TargetDocTitle = "Tower.rvt",
            TargetVersionGuid = Guid.NewGuid().ToString("D"),
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Beam]:B1"] = "u1", ["[Doc]#[Beam]:B2"] = "u2" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Equal(2, ledger.Mappings.Count);
    }

    [Fact]
    public async Task StrictStaticIdentity_TitleMismatch_RejectsReceipt_PreservesOutcomeUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-bad-title",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot { Title = "Struct.rvt" }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-bad-title",
            TargetDocTitle = "Arch.rvt",
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "u1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task StrictStaticIdentity_MissingPathInReceipt_RejectsReceipt_NoFailOpen()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-miss-path",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot { Title = "M.rvt", PathName = @"C:\Prod\M.rvt" }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-miss-path",
            TargetDocTitle = "M.rvt",
            TargetDocPath = string.Empty,
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "u1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task StrictStaticIdentity_MissingCentralGuidInReceipt_RejectsReceipt_NoFailOpen()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-miss-central",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot { Title = "M.rvt", CentralGuid = Guid.NewGuid() }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-miss-central",
            TargetDocTitle = "M.rvt",
            TargetCentralGuid = string.Empty,
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "u1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task StrictStaticIdentity_InvalidCentralGuidFormat_RejectsReceipt_NoFailOpen()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-invalid-guid",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot { Title = "M.rvt", CentralGuid = Guid.NewGuid() }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-invalid-guid",
            TargetDocTitle = "M.rvt",
            TargetCentralGuid = "invalid-guid",
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "u1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public void DocSnapshot_DirectMatch_StaticIdentityMatches_IgnoresVersionGuidDifference()
    {
        var central = Guid.NewGuid();
        var snap = new DocSnapshot
        {
            Title = "ModelA.rvt",
            PathName = @"C:\Bim\ModelA.rvt",
            CentralGuid = central,
            VersionGuid = Guid.NewGuid()
        };

        var receipt = new CoordinationReceiptRecord
        {
            TargetDocTitle = "ModelA.rvt",
            TargetDocPath = @"C:\Bim\ModelA.rvt",
            TargetCentralGuid = central.ToString("D"),
            TargetVersionGuid = Guid.NewGuid().ToString("D")
        };

        Assert.True(ReconcileService.MatchTargetDocSnapshot(snap, receipt));
    }

    [Fact]
    public void DocSnapshot_DirectMatch_MissingPathWhenSpecified_ReturnsFalse()
    {
        var snap = new DocSnapshot { Title = "ModelA.rvt", PathName = @"C:\Bim\ModelA.rvt" };
        var receipt = new CoordinationReceiptRecord { TargetDocTitle = "ModelA.rvt", TargetDocPath = string.Empty };

        Assert.False(ReconcileService.MatchTargetDocSnapshot(snap, receipt));
    }
}
