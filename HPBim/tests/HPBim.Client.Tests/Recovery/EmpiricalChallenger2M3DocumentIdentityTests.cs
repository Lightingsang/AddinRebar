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

public sealed class EmpiricalChallenger2M3DocumentIdentityTests : IDisposable
{
    private readonly string _journalPath;

    public EmpiricalChallenger2M3DocumentIdentityTests()
    {
        _journalPath = Path.Combine(Path.GetTempPath(), $"emp_ch2_m3_{Guid.NewGuid():N}.jsonl");
    }

    public void Dispose()
    {
        if (File.Exists(_journalPath))
        {
            File.Delete(_journalPath);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CentralGuid_NonEmptyInIntent_EmptyOrWhitespaceInReceipt_PreservesOutcomeUnknown(string? receiptGuid)
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();
        var central = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-cg-empty",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Model.rvt",
                CentralGuid = central
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-cg-empty",
            TargetDocTitle = "Model.rvt",
            TargetCentralGuid = receiptGuid ?? string.Empty,
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "uid-1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task CentralGuid_NonEmptyInIntent_ReceiptHasAllZeroGuid_PreservesOutcomeUnknown()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-cg-zeros",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Model.rvt",
                CentralGuid = Guid.NewGuid()
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-cg-zeros",
            TargetDocTitle = "Model.rvt",
            TargetCentralGuid = Guid.Empty.ToString("D"),
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "uid-1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task CentralGuid_BracedFormatInReceipt_MatchesAndCommits()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();
        var central = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-cg-braced",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Model.rvt",
                CentralGuid = central
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-cg-braced",
            TargetDocTitle = "Model.rvt",
            TargetCentralGuid = central.ToString("B"),
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "uid-1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Single(ledger.Mappings);
    }

    [Fact]
    public async Task MixedSlashPath_BackslashVsForwardSlash_RejectedByOrdinalStringEquals()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-path-slash",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Model.rvt",
                PathName = @"C:\Bim\Folder\Model.rvt"
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-path-slash",
            TargetDocTitle = "Model.rvt",
            TargetDocPath = "C:/Bim/Folder/Model.rvt",
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "uid-1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Theory]
    [InlineData(@"C:\Bim\Model.rvt", "")]
    [InlineData(@"C:\Bim\Model.rvt", "   ")]
    [InlineData(@"C:\Bim\Model.rvt", null)]
    public async Task EmptyOrWhitespacePath_InReceiptWhenIntentSpecifiesPath_RejectsReceipt(string intentPath, string? receiptPath)
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-path-empty",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Model.rvt",
                PathName = intentPath
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-path-empty",
            TargetDocTitle = "Model.rvt",
            TargetDocPath = receiptPath ?? string.Empty,
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:C1"] = "uid-1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task VersionGuid_MutationPostCommit_DoesNotRejectValidReceipt()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();
        var preFlightVer = Guid.NewGuid();
        var postCommitVer = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-ver-mutation",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Tower.rvt",
                VersionGuid = preFlightVer
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-ver-mutation",
            TargetDocTitle = "Tower.rvt",
            TargetVersionGuid = postCommitVer.ToString("D"),
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Beam]:B1"] = "uid-b1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Single(ledger.Mappings);
        Assert.Equal("uid-b1", ledger.Mappings["[Doc]#[Beam]:B1"].TargetUniqueId);
    }

    [Fact]
    public void DocSnapshot_ProjectGuidDistinctFromCentralGuid_ReceiptMatchesCentralGuid_FailsDueToLine92ArgDuplication()
    {
        var central = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var project = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var snap = new DocSnapshot
        {
            Title = "Model.rvt",
            CentralGuid = central,
            ProjectGuid = project
        };

        var receipt = new CoordinationReceiptRecord
        {
            TargetDocTitle = "Model.rvt",
            TargetCentralGuid = central.ToString("D")
        };

        bool matched = ReconcileService.MatchTargetDocSnapshot(snap, receipt);

        Assert.False(matched);
    }
}
