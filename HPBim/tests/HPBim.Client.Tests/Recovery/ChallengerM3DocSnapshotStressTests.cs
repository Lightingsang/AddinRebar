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

public sealed class ChallengerM3DocSnapshotStressTests : IDisposable
{
    private readonly string _journalPath;

    public ChallengerM3DocSnapshotStressTests()
    {
        _journalPath = Path.Combine(Path.GetTempPath(), $"challenger_m3_stress_{Guid.NewGuid():N}.jsonl");
    }

    public void Dispose()
    {
        if (File.Exists(_journalPath))
        {
            File.Delete(_journalPath);
        }
    }

    [Theory]
    [InlineData("11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", 1)]
    [InlineData("00000000-0000-0000-0000-000000000000", "33333333-3333-3333-3333-333333333333", 0)]
    [InlineData("44444444-4444-4444-4444-444444444444", "", 5)]
    [InlineData("55555555-5555-5555-5555-555555555555", "invalid-guid-string", 10)]
    public async Task DynamicVersionGuidMutation_VariousPermutations_RecoverySucceedsCommitted(
        string intentVerGuidStr, string receiptVerGuidStr, int intentSaves)
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();
        var central = Guid.NewGuid();
        var intentVerGuid = Guid.TryParse(intentVerGuidStr, out var g) ? g : Guid.Empty;

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId, PlanHash = "hash-dyn-perm", Status = IntentStatus.OutcomeUnknown, ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Hospital.rvt", PathName = @"D:\Projects\Hospital.rvt",
                CentralGuid = central, VersionGuid = intentVerGuid, NumberOfSaves = intentSaves
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId, PlanHash = "hash-dyn-perm", TargetDocTitle = "Hospital.rvt",
            TargetDocPath = @"D:\Projects\Hospital.rvt", TargetCentralGuid = central.ToString("D"),
            TargetVersionGuid = receiptVerGuidStr,
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Column]:Col_A1"] = "uid-col-a1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Single(ledger.Mappings);
        Assert.Equal("uid-col-a1", ledger.Mappings["[Doc]#[Column]:Col_A1"].TargetUniqueId);

        var updatedIntent = await journal.GetIntentAsync(opId);
        Assert.NotNull(updatedIntent);
        Assert.Equal(IntentStatus.Committed, updatedIntent.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(@"D:\Projects\WrongPath.rvt")]
    public async Task StaticIdentity_PathNameMismatchOrMissing_StrictlyPreservesOutcomeUnknown(string? receiptPath)
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-path-check",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Tower.rvt",
                PathName = @"D:\Projects\Tower.rvt"
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-path-check",
            TargetDocTitle = "Tower.rvt",
            TargetDocPath = receiptPath ?? string.Empty,
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Beam]:B1"] = "uid-b1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-valid-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")]
    public async Task StaticIdentity_CentralGuidMismatchOrMissing_StrictlyPreservesOutcomeUnknown(string? receiptCentral)
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();
        var expectedCentral = Guid.Parse("99999999-9999-9999-9999-999999999999");

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-central-check",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "Stadium.rvt",
                CentralGuid = expectedCentral
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-central-check",
            TargetDocTitle = "Stadium.rvt",
            TargetCentralGuid = receiptCentral ?? string.Empty,
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Slab]:S1"] = "uid-s1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.StillUnknown, result.Status);
        Assert.Empty(ledger.Mappings);
    }

    [Fact]
    public async Task StaticIdentity_CaseInsensitivePathsAndGuids_SuccessfullyMatches()
    {
        using var journal = new IntentJournal(_journalPath);
        var fakePipe = new FakeRevitPipeClient();
        var ledger = new BimCoordinationLedgerService();
        var opId = Guid.NewGuid();
        var central = Guid.NewGuid();

        await journal.RecordIntentAsync(new IntentJournalRecord
        {
            OperationId = opId,
            PlanHash = "hash-case-check",
            Status = IntentStatus.OutcomeUnknown,
            ItemCount = 1,
            TargetDoc = new DocumentIdentitySnapshot
            {
                Title = "model_alpha.rvt",
                PathName = @"c:\bim_models\model_alpha.rvt",
                CentralGuid = central
            }
        });

        fakePipe.AddSimulatedReceipt(new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "hash-case-check",
            TargetDocTitle = "MODEL_ALPHA.RVT",
            TargetDocPath = @"C:\BIM_MODELS\MODEL_ALPHA.RVT",
            TargetCentralGuid = central.ToString("D").ToUpperInvariant(),
            ElementMap = new Dictionary<string, string> { ["[Doc]#[Wall]:W1"] = "uid-w1" }
        });

        var service = new ReconcileService(journal, fakePipe, ledger);
        var result = await service.ReconcileIntentAsync(opId);

        Assert.Equal(ReconcileStatus.Committed, result.Status);
        Assert.Single(ledger.Mappings);
    }

    [Fact]
    public void DocSnapshot_FromAndToDocumentIdentitySnapshot_PreservesAllFields()
    {
        var central = Guid.NewGuid();
        var version = Guid.NewGuid();

        var original = new DocumentIdentitySnapshot
        {
            Title = "Bridge.rvt",
            PathName = @"E:\Models\Bridge.rvt",
            CentralGuid = central,
            VersionGuid = version,
            NumberOfSaves = 42
        };

        var snapshot = DocSnapshot.FromDocumentIdentitySnapshot(original);

        Assert.Equal("Bridge.rvt", snapshot.Title);
        Assert.Equal(@"E:\Models\Bridge.rvt", snapshot.PathName);
        Assert.Equal(central, snapshot.CentralGuid);
        Assert.Equal(version, snapshot.VersionGuid);
        Assert.Equal(42, snapshot.NumberOfSaves);
        Assert.True(snapshot.HasStaticIdentity);

        var roundTripped = snapshot.ToDocumentIdentitySnapshot();

        Assert.Equal(original.Title, roundTripped.Title);
        Assert.Equal(original.PathName, roundTripped.PathName);
        Assert.Equal(original.CentralGuid, roundTripped.CentralGuid);
        Assert.Equal(original.VersionGuid, roundTripped.VersionGuid);
        Assert.Equal(original.NumberOfSaves, roundTripped.NumberOfSaves);
    }

    [Fact]
    public void DocSnapshot_MatchesStaticIdentity_StrictNullAndEmptyRejections()
    {
        var central = Guid.NewGuid();
        var proj = Guid.NewGuid();

        var snap = new DocSnapshot
        {
            Title = "Base.rvt",
            PathName = @"C:\Base.rvt",
            CentralGuid = central,
            ProjectGuid = proj
        };

        Assert.False(snap.MatchesStaticIdentity("Base.rvt", @"C:\Base.rvt", central.ToString("D"), null));
        Assert.False(snap.MatchesStaticIdentity("Base.rvt", @"C:\Base.rvt", central.ToString("D"), ""));
        Assert.False(snap.MatchesStaticIdentity("Base.rvt", @"C:\Base.rvt", null, proj.ToString("D")));
        Assert.False(snap.MatchesStaticIdentity("Base.rvt", null, central.ToString("D"), proj.ToString("D")));
        Assert.False(snap.MatchesStaticIdentity(null!, @"C:\Base.rvt", central.ToString("D"), proj.ToString("D")));
        Assert.True(snap.MatchesStaticIdentity("Base.rvt", @"C:\Base.rvt", central.ToString("D"), proj.ToString("D")));
    }

    [Fact]
    public void DocSnapshot_ProjectGuidEvaluation_ShowsCentralGuidPassedTwice()
    {
        var central = Guid.NewGuid();
        var proj = Guid.NewGuid();
        var snapDistinct = new DocSnapshot { Title = "M.rvt", CentralGuid = central, ProjectGuid = proj };
        var snapSame = new DocSnapshot { Title = "M.rvt", CentralGuid = central, ProjectGuid = central };
        var receipt = new CoordinationReceiptRecord { TargetDocTitle = "M.rvt", TargetCentralGuid = central.ToString("D") };

        Assert.False(ReconcileService.MatchTargetDocSnapshot(snapDistinct, receipt));
        Assert.True(ReconcileService.MatchTargetDocSnapshot(snapSame, receipt));
    }
}

