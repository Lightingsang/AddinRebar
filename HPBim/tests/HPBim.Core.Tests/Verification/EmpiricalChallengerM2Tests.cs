namespace HPBim.Core.Tests.Verification;

using System;
using System.Collections.Generic;
using System.Linq;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Qc;
using HPBim.Core.Verification;
using Xunit;

/// <summary>
/// Empirical Challenger M2 Suite:
/// Rigorously validates tolerance thresholds, Rule 0 tri-state missing ground-truth,
/// and Step 8 ledger state audit.
/// </summary>
public class EmpiricalChallengerM2Tests
{
    private static ElementPlanItem CreateColumnItem(string mark, double x, double y, double width = 220, double height = 220) => new()
    {
        SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, mark),
        Category = CoordinationCategory.Column,
        Mark = mark,
        Geometry = new PointGeometrySpec { Location = new Point3D(x, y, 0) },
        Resource = new ResourceResolutionSpec
        {
            DimensionParametersMm = new Dictionary<string, double> { ["b"] = width, ["h"] = height }
        }
    };

    private static ElementPlanItem CreateBeamItem(string mark, double x1, double x2, double z, double width = 300, double height = 500) => new()
    {
        SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, mark),
        Category = CoordinationCategory.Beam,
        Mark = mark,
        Geometry = new LineGeometrySpec
        {
            Start = new Point3D(x1, 0, z),
            End = new Point3D(x2, 0, z),
            ElevationMm = z
        },
        Resource = new ResourceResolutionSpec
        {
            DimensionParametersMm = new Dictionary<string, double> { ["b"] = width, ["h"] = height }
        }
    };

    private static BimCoordinationLedger CreateTestLedger(
        int piles = 196,
        int footings = 72,
        int beams = 192,
        ElementTechnicalGroup group = ElementTechnicalGroup.Group1_ReadyToModel,
        RfiStatus? rfiStatus = null)
    {
        var elements = new List<CoordinationElementRecord>(piles + footings + beams);
        elements.AddRange(Enumerable.Range(1, piles).Select(i => new CoordinationElementRecord
        {
            Id = $"P_{i}", Category = CoordinationCategory.Pile, Code = $"P{i}", TechnicalGroup = group
        }));
        elements.AddRange(Enumerable.Range(1, footings).Select(i => new CoordinationElementRecord
        {
            Id = $"FT_{i}", Category = CoordinationCategory.Footing, Code = $"DC-{i}", TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel
        }));
        elements.AddRange(Enumerable.Range(1, beams).Select(i => new CoordinationElementRecord
        {
            Id = $"BM_{i}", Category = CoordinationCategory.GradeBeam, Code = $"GM-{i}", TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel
        }));

        var rfis = rfiStatus.HasValue
            ? new[] { new RfiRecord { RfiId = "RFI-TEST-01", Status = rfiStatus.Value, Title = "Test Pending RFI" } }
            : Array.Empty<RfiRecord>();

        return new BimCoordinationLedger { ActivePhase = "PHAN_MONG", Elements = elements, Rfis = rfis };
    }

    [Fact]
    public void Challenger_100PercentCountMatch_FailsIfSingleColumnDimensionExceedsZeroMm()
    {
        var items = new List<ElementPlanItem>();
        var actuals = new Dictionary<string, ActualElementGeometry>();

        for (int i = 1; i <= 100; i++)
        {
            string mark = $"C{i}";
            items.Add(CreateColumnItem(mark, i * 1000, 0, 220, 220));

            // Element 42 has a 0.05mm width deviation (> 0.0mm tolerance)
            double width = (i == 42) ? 220.05 : 220.0;
            actuals[$"Doc#Column:{mark}"] = new ActualElementGeometry
            {
                Key = $"Doc#Column:{mark}",
                CategoryName = "Column",
                HasGeometry = true,
                Centroid = new Point3D(i * 1000, 0, 0),
                WidthMm = width,
                HeightMm = 220.0
            };
        }

        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "hash-100cols", Items = items };
        var engine = new MeasuredGeometricEngine();
        var report = engine.AuditPlan(plan, actuals);

        Assert.Equal(100, report.TotalPlannedElements);
        Assert.Equal(100, report.TotalMeasuredElements);
        Assert.Equal(99, report.VerifiedCount);
        Assert.Equal(1, report.DefectCount);
        Assert.False(report.OverallPassed);

        var defect = report.Audits.First(a => a.Mark == "C42");
        Assert.Equal(GeometricVerificationStatus.Defect, defect.Status);
        Assert.True(defect.IsDimensionOutOfTolerance);
        Assert.Equal(0.05, defect.DeltaWidthMm, 3);
    }

    [Fact]
    public void Challenger_100PercentCountMatch_FailsIfSingleBeamElevationExceedsOneMm()
    {
        var items = new List<ElementPlanItem>();
        var actuals = new Dictionary<string, ActualElementGeometry>();

        for (int i = 1; i <= 100; i++)
        {
            string mark = $"B{i}";
            items.Add(CreateBeamItem(mark, (i - 1) * 4000, i * 4000, 3800, 300, 500));

            // Beam 77 has elevation deviation of 1.05mm (> 1.0mm tolerance)
            double elev = (i == 77) ? 3801.05 : 3800.0;
            actuals[$"Doc#Beam:{mark}"] = new ActualElementGeometry
            {
                Key = $"Doc#Beam:{mark}",
                CategoryName = "Beam",
                HasGeometry = true,
                CurveStart = new Point3D((i - 1) * 4000, 0, elev),
                CurveEnd = new Point3D(i * 4000, 0, elev),
                TopElevationMm = elev,
                WidthMm = 300.0,
                HeightMm = 500.0
            };
        }

        var plan = new BimExecutionPlan { PlanId = Guid.NewGuid(), PlanHash = "hash-100beams", Items = items };
        var engine = new MeasuredGeometricEngine();
        var report = engine.AuditPlan(plan, actuals);

        Assert.Equal(100, report.TotalPlannedElements);
        Assert.Equal(100, report.TotalMeasuredElements);
        Assert.Equal(99, report.VerifiedCount);
        Assert.Equal(1, report.DefectCount);
        Assert.False(report.OverallPassed);

        var defect = report.Audits.First(a => a.Mark == "B77");
        Assert.Equal(GeometricVerificationStatus.Defect, defect.Status);
        Assert.True(defect.IsElevationOutOfTolerance);
        Assert.Equal(1.05, defect.DeltaElevationMm, 3);
    }

    [Fact]
    public void Challenger_MissingGroundTruth_ReturnsInconclusiveAndBlocksAllPassed()
    {
        var engine = new FoundationQcEngine();
        var piles = new List<PileAuditItem> { new(1, "P1", 1000, 1000, -2700.0, "P400") };
        var footings = new List<FootingAuditItem>();

        // Positional audit without CAD ground truth
        var step3 = engine.AuditPositions(piles, footings, groundTruthPiles: null, groundTruthFootings: null);
        Assert.Equal(FoundationStepStatus.Inconclusive, step3.Status);
        Assert.False(step3.Passed);
        Assert.True(step3.IsInconclusive);

        // Grids & Levels audit without expected dimensions
        var grids = new List<GridAuditItem>();
        for (int i = 1; i <= 27; i++) { grids.Add(new GridAuditItem($"G{i}", 0, 0, 1000, 0, true)); }
        var levels = new List<LevelAuditItem>();
        for (int i = 1; i <= 10; i++) { levels.Add(new LevelAuditItem($"L{i}", i * 1000, true)); }

        var step1 = engine.AuditGridsAndLevels(grids, levels, null, null, null);
        Assert.Equal(FoundationStepStatus.Inconclusive, step1.Status);
        Assert.False(step1.Passed);

        // Full audit context with missing ground truth: must prevent AllPassed
        var beams = new List<BeamAuditItem>();
        var ctx = new FoundationAuditContext(
            grids, levels, piles, footings, beams,
            ExpectedXGrids: null,
            GroundTruthPiles: null,
            GroundTruthFootings: null,
            Ledger: CreateTestLedger());

        var fullReport = engine.ExecuteFullAudit(ctx);
        Assert.False(fullReport.AllPassed);
        Assert.True(fullReport.InconclusiveStepsCount > 0);
        Assert.NotEqual(FoundationStepStatus.Passed, fullReport.OverallStatus);
    }

    [Fact]
    public void Challenger_Step8_FailsIfLedgerMissing()
    {
        var auditor = new FoundationLedgerAuditor();
        var priorSteps = Enumerable.Range(1, 7)
            .Select(i => new FoundationStepResult(i, $"Step {i}", FoundationStepStatus.Passed, "OK", Array.Empty<string>()))
            .ToList();

        var result = auditor.AuditLedgerAndDossier(priorSteps, ledger: null);

        Assert.Equal(FoundationStepStatus.Inconclusive, result.Status);
        Assert.False(result.Passed);
        Assert.True(result.IsInconclusive);
        Assert.Contains("BimCoordinationLedger is null", result.Errors[0]);
    }

    [Fact]
    public void Challenger_Step8_FailsIfUnreadyTechnicalGroupsExist()
    {
        var auditor = new FoundationLedgerAuditor();
        var priorSteps = Enumerable.Range(1, 7)
            .Select(i => new FoundationStepResult(i, $"Step {i}", FoundationStepStatus.Passed, "OK", Array.Empty<string>()))
            .ToList();

        // 1 pile is unready (Group2_ConflictingInfo)
        var ledger = CreateTestLedger(group: ElementTechnicalGroup.Group2_ConflictingInfo);
        var result = auditor.AuditLedgerAndDossier(priorSteps, ledger);

        Assert.Equal(FoundationStepStatus.Failed, result.Status);
        Assert.False(result.Passed);
        Assert.Contains("Nhóm 2/Nhóm 3", result.Errors.First(e => e.Contains("Nhóm 2")));
    }

    [Fact]
    public void Challenger_Step8_FailsIfPendingRfisExist()
    {
        var auditor = new FoundationLedgerAuditor();
        var priorSteps = Enumerable.Range(1, 7)
            .Select(i => new FoundationStepResult(i, $"Step {i}", FoundationStepStatus.Passed, "OK", Array.Empty<string>()))
            .ToList();

        // 1 pending RFI
        var ledger = CreateTestLedger(rfiStatus: RfiStatus.PendingChiefEngineer);
        var result = auditor.AuditLedgerAndDossier(priorSteps, ledger);

        Assert.Equal(FoundationStepStatus.Failed, result.Status);
        Assert.False(result.Passed);
        Assert.Contains("RFI", result.Errors.First(e => e.Contains("RFI")));
    }

    [Fact]
    public void Challenger_Step8_FailsEarlyIfPriorStepsContainFailure()
    {
        var auditor = new FoundationLedgerAuditor();
        var priorSteps = new List<FoundationStepResult>
        {
            new(1, "Step 1", FoundationStepStatus.Passed, "OK", Array.Empty<string>()),
            new(2, "Step 2", FoundationStepStatus.Failed, "Fail count", new[] { "Count mismatch" }),
            new(3, "Step 3", FoundationStepStatus.Passed, "OK", Array.Empty<string>())
        };

        var ledger = CreateTestLedger();
        var result = auditor.AuditLedgerAndDossier(priorSteps, ledger);

        Assert.Equal(FoundationStepStatus.Failed, result.Status);
        Assert.False(result.Passed);
        Assert.Contains("thất bại", result.Summary);
    }
}
