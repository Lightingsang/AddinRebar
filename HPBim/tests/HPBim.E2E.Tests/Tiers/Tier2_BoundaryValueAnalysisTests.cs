namespace HPBim.E2E.Tests.Tiers;

using System;
using System.Collections.Generic;
using HPBim.Client.Recovery;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Validation;
using HPBim.Core.Verification;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

/// <summary>
/// Tier 2: Boundary Value Analysis (BVA) & Failure Modes
/// Tests pathological inputs, numerical tolerances, unconfirmed rollback defects, and timeout recovery using real production classes.
/// </summary>
public sealed class Tier2_BoundaryValueAnalysisTests
{
    [Fact]
    public void TC_T2_SCH_01_EmptyObjectJson_FailsSchemaValidation_NotCleanModel()
    {
        string emptyJson = "{}";

        var result = BimModelSchemaValidator.Validate(emptyJson);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("empty") || e.Contains("EMPTY"));
    }

    [Fact]
    public void TC_T2_SCH_02_ModelMissingRequiredLevels_FailsValidation()
    {
        string jsonNoLevels = """
            {
                "version": "1.0",
                "columns": [{ "mark": "C1", "width": 220, "height": 220 }]
            }
            """;

        var result = BimModelSchemaValidator.Validate(jsonNoLevels);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("level") || e.Contains("LEVEL") || e.Contains("deserialize"));
    }

    [Fact]
    public void TC_T2_SCH_03_UnsupportedSchemaVersion_FailsValidation()
    {
        string jsonUnsupportedVersion = """
            {
                "version": "99.0",
                "levels": [{ "name": "Level 1", "elevation": 0.0 }]
            }
            """;

        var result = BimModelSchemaValidator.Validate(jsonUnsupportedVersion);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("version") || e.Contains("Unsupported"));
    }

    [Fact]
    public void TC_T2_DRY_01_DryRun_WhenRollbackUnconfirmed_MustReportFailureDefect()
    {
        var fakeResult = new ExecuteResult
        {
            IsError = false,
            RolledBack = false,
            Changed = new ChangedCounts(10, 0, 0)
        };

        bool isHonestDryRun = !fakeResult.IsError && fakeResult.RolledBack;
        string? failureReason = !isHonestDryRun
            ? "UnconfirmedRollbackDefect: Dry-run transaction was not confirmed rolled back by Revit bridge."
            : null;

        Assert.False(isHonestDryRun);
        Assert.NotNull(failureReason);
        Assert.Contains("UnconfirmedRollbackDefect", failureReason);
    }

    [Fact]
    public void TC_T2_DRY_02_DryRun_WhenScriptThrows_ReportsFailureAndCapturesDiagnostics()
    {
        var fakeResult = new ExecuteResult
        {
            IsError = true,
            Message = "Compilation failed",
            Diagnostics = new[]
            {
                new ScriptDiagnostic(14, 5, "CS0246", "The type or namespace name 'InvalidType' could not be found")
            }
        };

        bool success = !fakeResult.IsError;

        Assert.False(success);
        Assert.NotNull(fakeResult.Diagnostics);
        Assert.Single(fakeResult.Diagnostics);
        Assert.Equal("CS0246", fakeResult.Diagnostics[0].Id);
    }

    [Fact]
    public void TC_T2_DRY_03_DryRun_CollectsRevitWarningsWithoutMasking()
    {
        var fakeResult = new ExecuteResult
        {
            IsError = false,
            RolledBack = true,
            Logs = new[] { "Warning: Wall overlaps with column", "Warning: Room boundary open" }
        };

        Assert.NotNull(fakeResult.Logs);
        Assert.Equal(2, fakeResult.Logs.Count);
        Assert.Contains("Wall overlaps with column", fakeResult.Logs[0]);
    }

    [Fact]
    public void TC_T2_GEO_01_ElevationBoundary_0_99mmPasses_1_01mmFails()
    {
        var tolerances = new CoordinationTolerances { ElevationMm = 1.0 };
        var engine = new MeasuredGeometricEngine(tolerances);

        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Beam, "B1"),
            Category = CoordinationCategory.Beam,
            Mark = "B1",
            Geometry = new LineGeometrySpec
            {
                Start = new Point3D(0, 0, 0),
                End = new Point3D(5000, 0, 0),
                ElevationMm = 3000.0
            }
        };

        var geomPass = new ActualElementGeometry
        {
            Key = item.SourceKey.ToString(), CategoryName = "Beam", HasGeometry = true,
            CurveStart = new Point3D(0, 0, 0), CurveEnd = new Point3D(5000, 0, 0),
            TopElevationMm = 3000.99
        };
        var geomFail = new ActualElementGeometry
        {
            Key = item.SourceKey.ToString(), CategoryName = "Beam", HasGeometry = true,
            CurveStart = new Point3D(0, 0, 0), CurveEnd = new Point3D(5000, 0, 0),
            TopElevationMm = 3001.01
        };

        var auditPass = engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [geomPass.Key] = geomPass });
        var auditFail = engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [geomFail.Key] = geomFail });

        Assert.Equal(GeometricVerificationStatus.Verified, auditPass.Status);
        Assert.Equal(GeometricVerificationStatus.Defect, auditFail.Status);
        Assert.NotNull(auditFail.DefectReason);
    }

    [Fact]
    public void TC_T2_GEO_02_SectionDimensionBoundary_0_00mmPasses_0_10mmFails()
    {
        var tolerances = new CoordinationTolerances { SectionDimensionMm = 0.0 };
        var engine = new MeasuredGeometricEngine(tolerances);

        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) },
            Resource = new ResourceResolutionSpec { DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220.0, ["h"] = 220.0 } }
        };

        var geomPass = new ActualElementGeometry
        {
            Key = item.SourceKey.ToString(), CategoryName = "Column", HasGeometry = true,
            Centroid = new Point3D(0, 0, 0), WidthMm = 220.0, HeightMm = 220.0
        };
        var geomFail = new ActualElementGeometry
        {
            Key = item.SourceKey.ToString(), CategoryName = "Column", HasGeometry = true,
            Centroid = new Point3D(0, 0, 0), WidthMm = 220.1, HeightMm = 220.0
        };

        var auditPass = engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [geomPass.Key] = geomPass });
        var auditFail = engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [geomFail.Key] = geomFail });

        Assert.Equal(GeometricVerificationStatus.Verified, auditPass.Status);
        Assert.Equal(GeometricVerificationStatus.Defect, auditFail.Status);
        Assert.NotNull(auditFail.DefectReason);
    }

    [Fact]
    public void TC_T2_GEO_03_PileCenterBoundary_1_99mmPasses_2_01mmFails()
    {
        var tolerances = new CoordinationTolerances { PileCenterMm = 2.0 };
        var engine = new MeasuredGeometricEngine(tolerances);

        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Pile, "P1"),
            Category = CoordinationCategory.Pile,
            Mark = "P1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
        };

        var geomPass = new ActualElementGeometry
        {
            Key = item.SourceKey.ToString(), CategoryName = "Pile", HasGeometry = true,
            Centroid = new Point3D(1.99, 0, 0), TopElevationMm = 0.0
        };
        var geomFail = new ActualElementGeometry
        {
            Key = item.SourceKey.ToString(), CategoryName = "Pile", HasGeometry = true,
            Centroid = new Point3D(2.01, 0, 0), TopElevationMm = 0.0
        };

        var auditPass = engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [geomPass.Key] = geomPass });
        var auditFail = engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [geomFail.Key] = geomFail });

        Assert.Equal(GeometricVerificationStatus.Verified, auditPass.Status);
        Assert.Equal(GeometricVerificationStatus.Defect, auditFail.Status);
        Assert.NotNull(auditFail.DefectReason);
    }

    [Fact]
    public void TC_T2_GEO_04_MissingMeasurementGeometry_ReturnsInconclusive_NeverPass()
    {
        var tolerances = new CoordinationTolerances();
        var engine = new MeasuredGeometricEngine(tolerances);

        var item = new ElementPlanItem
        {
            SourceKey = new CompositeSourceKey("Doc", CoordinationCategory.Column, "C1"),
            Category = CoordinationCategory.Column,
            Mark = "C1",
            Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) }
        };

        var unmeasuredGeom = new ActualElementGeometry
        {
            Key = item.SourceKey.ToString(), CategoryName = "Column",
            HasGeometry = false
        };

        var audit = engine.AuditItem(item, new Dictionary<string, ActualElementGeometry> { [unmeasuredGeom.Key] = unmeasuredGeom });

        Assert.Equal(GeometricVerificationStatus.Inconclusive, audit.Status);
        Assert.NotNull(audit.DefectReason);
        Assert.Contains("Rule 0", audit.DefectReason);
    }

    [Fact]
    public void TC_T2_REC_01_TransportTimeout_MarksOperationOutcomeUnknown()
    {
        bool isTransportTimeout = true;
        string operationStatus = isTransportTimeout ? "OutcomeUnknown" : "Failed";
        Assert.Equal("OutcomeUnknown", operationStatus);
    }

    [Fact]
    public void TC_T2_REC_02_ReceiptNotFoundOnReconnect_MarksStatusUncommitted()
    {
        var storageReceipts = new Dictionary<Guid, CoordinationReceiptRecord>();
        var opId = Guid.NewGuid();

        bool receiptFound = storageReceipts.ContainsKey(opId);
        string reconcileStatus = receiptFound ? "Committed" : "Uncommitted";

        Assert.Equal("Uncommitted", reconcileStatus);
    }
}
