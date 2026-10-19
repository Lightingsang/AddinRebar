namespace HPBim.Core.Tests.Verification;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Qc;
using HPBim.Core.Verification;
using Xunit;

/// <summary>
/// Additional adversarial suite for FoundationQcEngine & tolerance boundaries.
/// Strictly satisfies Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines per method).
/// </summary>
public class EmpiricalChallengerM2FoundationTests
{
    [Fact]
    public void BoundaryTolerances_SectionDimensionEpsilonEnforcement()
    {
        var tolerances = new CoordinationTolerances { SectionDimensionMm = 0.0 };
        var engine = new MeasuredGeometricEngine(tolerances);

        var colItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) },
            Resource = new ResourceResolutionSpec
            {
                DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220.0, ["h"] = 220.0 }
            }
        };

        // Exact match (0.0mm diff) -> Verified
        var actExact = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Column:C1"] = new() { Key = "Doc#Column:C1", HasGeometry = true, Centroid = new Point3D(0, 0, 0), WidthMm = 220.0, HeightMm = 220.0 }
        };
        Assert.Equal(GeometricVerificationStatus.Verified, engine.AuditItem(colItem, actExact).Status);

        // 0.001mm diff -> Defect
        var actEpsilonDim = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Column:C1"] = new() { Key = "Doc#Column:C1", HasGeometry = true, Centroid = new Point3D(0, 0, 0), WidthMm = 220.001, HeightMm = 220.0 }
        };
        Assert.Equal(GeometricVerificationStatus.Defect, engine.AuditItem(colItem, actEpsilonDim).Status);
    }

    [Fact]
    public void BoundaryTolerances_ElevationEpsilonEnforcement()
    {
        var tolerances = new CoordinationTolerances { ElevationMm = 1.0 };
        var engine = new MeasuredGeometricEngine(tolerances);

        var beamItem = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B1"),
            Category = CoordinationCategory.Beam,
            Mark = "B1",
            Geometry = new LineGeometrySpec { Start = new Point3D(0, 0, 0), End = new Point3D(1000, 0, 0), ElevationMm = 0.0 }
        };

        // Elevation exact 1.0mm diff -> Verified
        var actElev1 = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B1"] = new() { Key = "Doc#Beam:B1", HasGeometry = true, CurveStart = new Point3D(0, 0, 1.0), CurveEnd = new Point3D(1000, 0, 1.0), TopElevationMm = 1.0 }
        };
        Assert.Equal(GeometricVerificationStatus.Verified, engine.AuditItem(beamItem, actElev1).Status);

        // Elevation 1.001mm diff -> Defect
        var actElevOver = new Dictionary<string, ActualElementGeometry>
        {
            ["Doc#Beam:B1"] = new() { Key = "Doc#Beam:B1", HasGeometry = true, CurveStart = new Point3D(0, 0, 1.001), CurveEnd = new Point3D(1000, 0, 1.001), TopElevationMm = 1.001 }
        };
        Assert.Equal(GeometricVerificationStatus.Defect, engine.AuditItem(beamItem, actElevOver).Status);
    }

    [Fact]
    public void FoundationQcEngine_100PercentCount_StillFailsIfFootingThicknessDeviates()
    {
        var engine = new FoundationQcEngine();
        var poly = new Polygon2D(new[] { new Point2D(0, 0), new Point2D(1000, 0), new Point2D(1000, 1000), new Point2D(0, 1000) });

        var footings = new List<FootingAuditItem>();
        for (int i = 1; i <= 72; i++)
        {
            double thk = (i == 10) ? 1250.0 : 1200.0;
            footings.Add(new FootingAuditItem(i, $"ĐC-4-{i:D2}", "ĐC-4", -1600.0, -2800.0, thk, new Point2D(500, 500), poly));
        }

        var beams = new List<BeamAuditItem>();
        for (int i = 1; i <= 192; i++)
        {
            beams.Add(new BeamAuditItem(i, $"GM-{i}", "B_300x700", 300, 700, -1600, new Point2D(0, 0), new Point2D(1000, 0)));
        }

        var res = engine.AuditDimensions(footings, beams);

        Assert.False(res.Passed);
        Assert.Equal(FoundationStepStatus.Failed, res.Status);
        Assert.Single(res.Errors);
        Assert.Contains("ĐC-4-10", res.Errors[0]);
    }

    [Fact]
    public void FoundationQcEngine_100PercentCount_StillFailsIfBeamElevationDeviates()
    {
        var engine = new FoundationQcEngine();
        var piles = new List<PileAuditItem>();
        var footings = new List<FootingAuditItem>();
        var poly = new Polygon2D(new[] { new Point2D(0, 0), new Point2D(1000, 0), new Point2D(1000, 1000), new Point2D(0, 1000) });

        for (int i = 1; i <= 72; i++)
        {
            footings.Add(new FootingAuditItem(i, $"ĐC-4-{i:D2}", "ĐC-4", -1600.0, -2800.0, 1200.0, new Point2D(500, 500), poly));
        }

        var beams = new List<BeamAuditItem>();
        for (int i = 1; i <= 192; i++)
        {
            double topZ = (i == 15) ? -1595.0 : -1600.0;
            beams.Add(new BeamAuditItem(i, $"GM-{i}", "B_300x700", 300, 700, topZ, new Point2D(0, 0), new Point2D(1000, 0)));
        }

        var res = engine.AuditElevations(piles, footings, beams);

        Assert.False(res.Passed);
        Assert.Equal(FoundationStepStatus.Failed, res.Status);
        Assert.Single(res.Errors);
        Assert.Contains("GM-15", res.Errors[0]);
    }

    [Fact]
    public void FoundationLedgerAuditor_FailsIfLedgerHasGroup3MissingInfoOrWrongPhaseOrCount()
    {
        var auditor = new FoundationLedgerAuditor();
        var priorSteps = new List<FoundationStepResult>();
        for (int i = 1; i <= 7; i++)
        {
            priorSteps.Add(new FoundationStepResult(i, $"Step {i}", FoundationStepStatus.Passed, "OK", Array.Empty<string>()));
        }

        var elements = new List<CoordinationElementRecord>
        {
            new() { Id = "P1", Category = CoordinationCategory.Pile, TechnicalGroup = ElementTechnicalGroup.Group3_MissingInfo }
        };
        var ledger = new BimCoordinationLedger { ActivePhase = "PHAN_MONG", Elements = elements };
        var resGroup3 = auditor.AuditLedgerAndDossier(priorSteps, ledger);
        Assert.Equal(FoundationStepStatus.Failed, resGroup3.Status);
        Assert.Contains("Nhóm 2/Nhóm 3", resGroup3.Errors.First(e => e.Contains("Nhóm 2")));

        var ledgerWrongPhase = new BimCoordinationLedger { ActivePhase = "TANG_1", Elements = elements };
        var resPhase = auditor.AuditLedgerAndDossier(priorSteps, ledgerWrongPhase);
        Assert.Equal(FoundationStepStatus.Failed, resPhase.Status);
        Assert.Contains("TANG_1", resPhase.Errors.First(e => e.Contains("TANG_1")));

        Assert.Contains(resGroup3.Errors, e => e.Contains("cọc"));
    }
}
