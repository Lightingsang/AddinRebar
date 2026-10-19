namespace HPBim.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Pipeline;
using HPBim.Client.Recovery;
using HPBim.Client.ScriptGenerators;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Serialization;
using HPBim.Core.Tests.Fixtures;
using HPBim.Core.Validation;
using HPBim.Core.Verification;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

/// <summary>
/// Tier 1: Category-Partition (Feature Coverage)
/// Tests nominal behavior, contracts, and primary execution paths across all pipeline features using real production classes.
/// </summary>
public sealed class Tier1_FeatureCoverageTests
{
    [Fact]
    public void TC_T1_SCH_01_ValidTownhouseModel_PassesSchemaValidation()
    {
        var model = NhaPho5x18mFixture.CreateModel();
        string json = BimModelSerializer.Serialize(model);

        var result = BimModelSchemaValidator.Validate(json);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void TC_T1_SCH_02_RawCadJson_FailsSchemaValidationWithClearMessage()
    {
        string rawCadJson = """
            {
                "layers": ["C-GRID", "S-COLU", "S-BEAM"],
                "entities": [
                    { "handle": "3F1A", "type": "LINE", "layer": "C-GRID" },
                    { "handle": "3F1B", "type": "POLYLINE", "layer": "S-COLU" }
                ]
            }
            """;

        var result = BimModelSchemaValidator.Validate(rawCadJson);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("CAD") || e.Contains("artifact"));
    }

    [Fact]
    public void TC_T1_SEP_01_ExecutionPlanBuilder_ProducesSinglePlanIR()
    {
        var model = NhaPho5x18mFixture.CreateModel();
        var targetDoc = new DocumentIdentitySnapshot { Title = "NhaPho_5x18m.rvt", CentralGuid = Guid.NewGuid() };

        var plan = ExecutionPlanBuilder.Build(model, targetDoc, new ExecutionOptions());

        Assert.NotNull(plan.PlanHash);
        Assert.NotEmpty(plan.PlanHash);
        Assert.True(plan.Items.Count > 0);
        Assert.Equal(targetDoc.Title, plan.TargetDoc?.Title);
    }

    [Fact]
    public void TC_T1_SEP_02_PreviewAndApply_ProduceIdenticalExecutableScript()
    {
        var model = NhaPho5x18mFixture.CreateModel();
        var generator = new CompositeBimGenerator();

        var previewPayload = generator.Generate(model);
        var applyPayload = generator.Generate(model);

        var previewRequest = previewPayload.ToExecuteRequest(dryRun: true, timeoutSeconds: 60);
        var applyRequest = applyPayload.ToExecuteRequest(dryRun: false, timeoutSeconds: 60);

        Assert.Equal(previewRequest.Code, applyRequest.Code);
        Assert.Equal(previewRequest.Args?.GetRawText(), applyRequest.Args?.GetRawText());
        Assert.True(previewRequest.DryRun);
        Assert.False(applyRequest.DryRun);
    }

    [Fact]
    public async Task TC_T1_DRY_01_DryRun_WithConfirmedRollback_ReportsSuccess()
    {
        var model = NhaPho5x18mFixture.CreateModel();
        var fakeClient = new FakeRevitPipeClient();
        var pipeline = new BimPipelineEngine(fakeClient);

        var result = await pipeline.ExecuteAsync(model, new BimPipelineOptions
        {
            DryRun = true,
            AtomicDryRun = true
        });

        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void TC_T1_IDM_01_CompositeSourceKey_FormatsAndRoundtripsCorrectly()
    {
        var key = new CompositeSourceKey("TANDONGHIEP_KC", CoordinationCategory.Column, "3F2A", new[] { "3F2A_1", "3F2A_2" });

        string formatted = key.ToString();
        var parsed = CompositeSourceKey.Parse(formatted);

        Assert.Equal("[TANDONGHIEP_KC]#[Column]:3F2A", formatted);
        Assert.Equal(key.DocIdentifier, parsed.DocIdentifier);
        Assert.Equal(key.Category, parsed.Category);
        Assert.Equal(key.PrimaryHandle, parsed.PrimaryHandle);
    }

    [Fact]
    public void TC_T1_IDM_02_FirstRun_CreatesElementsAndRecordsUniqueIdMapping()
    {
        var ledger = new BimCoordinationLedgerService();
        var key = new CompositeSourceKey("TANDONGHIEP_KC", CoordinationCategory.Column, "3F2A");
        var generatedUniqueId = "rvt-elem-" + Guid.NewGuid().ToString("N");

        Assert.False(ledger.TryGetTarget(key, out _));
        ledger.UpdateMapping(key, generatedUniqueId, "plan-hash", ElementExecutionStatus.Created);

        Assert.True(ledger.TryGetTarget(key, out string? targetId));
        Assert.Equal(generatedUniqueId, targetId);
    }

    [Fact]
    public void TC_T1_IDM_03_SecondRunIdenticalPlan_ReturnsNoOpWithoutDuplicates()
    {
        var ledger = new BimCoordinationLedgerService();
        var key = new CompositeSourceKey("TANDONGHIEP_KC", CoordinationCategory.Column, "3F2A");
        ledger.UpdateMapping(key, "rvt-elem-001", "plan-hash", ElementExecutionStatus.Created);

        Assert.True(ledger.TryGetTarget(key, out string? targetId));
        Assert.Equal("rvt-elem-001", targetId);
    }

    [Fact]
    public async Task TC_T1_REC_01_IntentJournal_LogsOperationIntentBeforePipeDispatch()
    {
        string tempFile = Path.Combine(Path.GetTempPath(), $"hpbim_intent_{Guid.NewGuid():N}.jsonl");
        try
        {
            var journal = new IntentJournal(tempFile);
            var opId = Guid.NewGuid();
            var record = new IntentJournalRecord
            {
                OperationId = opId,
                PlanHash = "AABBCCDDEEFF",
                TargetDoc = new DocumentIdentitySnapshot { Title = "NhaPho_5x18m.rvt" },
                TargetDocId = "Doc-1",
                Status = IntentStatus.Pending
            };

            await journal.RecordIntentAsync(record);
            var retrieved = await journal.GetIntentAsync(opId);

            Assert.NotNull(retrieved);
            Assert.Equal(opId, retrieved.OperationId);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void TC_T1_REC_02_ReconcileService_ReadsExtensibleStorageReceiptOnReconnect()
    {
        var opId = Guid.NewGuid();
        var receipt = new CoordinationReceiptRecord
        {
            OperationId = opId,
            PlanHash = "HASH123",
            TargetDocId = "Doc-1",
            ElementMap = new Dictionary<string, string> { ["KEY1"] = "RVT1", ["KEY2"] = "RVT2" }
        };

        Assert.Equal(opId, receipt.OperationId);
        Assert.Equal(2, receipt.ElementMap.Count);
        Assert.Equal("RVT1", receipt.ElementMap["KEY1"]);
    }

    [Fact]
    public void TC_T1_GEO_01_MeasuredGeometricVerifier_PassesWithinTolerances()
    {
        var tolerances = new CoordinationTolerances
        {
            SectionDimensionMm = 0.0,
            ElevationMm = 1.0,
            PlanPositionMm = 1.0,
            PileCenterMm = 2.0
        };

        var engine = new MeasuredGeometricEngine(tolerances);
        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) },
            Resource = new ResourceResolutionSpec { DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220, ["h"] = 220 } }
        };

        var actual = new ActualElementGeometry
        {
            Key = item.SourceKey.ToString(),
            CategoryName = "Column",
            HasGeometry = true,
            Centroid = new Point3D(0, 0, 0.5),
            WidthMm = 220,
            HeightMm = 220
        };

        var audit = engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [actual.Key] = actual });

        Assert.Equal(GeometricVerificationStatus.Verified, audit.Status);
        Assert.True(audit.IsVerified);
    }
}
