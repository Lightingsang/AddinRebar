namespace HPBim.Client.Tests.Verification;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Tests.Fakes;
using HPBim.Client.Verification;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPBim.Core.Geometry;
using HPBim.Core.Verification;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public class MeasuredGeometricVerifierTests
{
    private static BimExecutionPlan CreateSamplePlan() => new()
    {
        PlanId = Guid.NewGuid(),
        PlanHash = "sample-hash",
        Items = new[]
        {
            new ElementPlanItem
            {
                SourceKey = new CompositeSourceKey("TestDoc", CoordinationCategory.Column, "C1"),
                Category = CoordinationCategory.Column,
                Mark = "C1",
                Geometry = new PointGeometrySpec { Location = new Point3D(0, 0, 0) },
                Resource = new ResourceResolutionSpec
                {
                    DimensionParametersMm = new Dictionary<string, double> { ["b"] = 220, ["h"] = 220 }
                }
            }
        }
    };

    [Fact]
    public async Task VerifyPlanGeometryAsync_WhenRevitReturnsAccurateGeometry_ReturnsOverallPassedTrue()
    {
        var fake = new FakeRevitPipeClient();
        var payload = new
        {
            total = 1,
            actuals = new[]
            {
                new
                {
                    key = "TestDoc#Column:C1",
                    category = "Column",
                    mark = "C1",
                    hasGeometry = true,
                    centroid = new { x = 0.0, y = 0.0, z = 0.0 },
                    widthMm = 220.0,
                    heightMm = 220.0
                }
            }
        };

        fake.CustomExecuteHandler = req =>
        {
            Assert.Equal(TransactionModes.None, req.Transaction);
            Assert.False(req.DryRun);
            return new ExecuteResult
            {
                IsError = false,
                Value = BridgeJson.ToElement(payload)
            };
        };

        var verifier = new MeasuredGeometricVerifier(fake);
        var report = await verifier.VerifyPlanGeometryAsync(CreateSamplePlan());

        Assert.True(report.OverallPassed);
        Assert.Equal(1, report.VerifiedCount);
        Assert.Equal(0, report.DefectCount);
    }

    [Fact]
    public async Task VerifyPlanGeometryAsync_WhenRevitReturnsDefectiveDimensions_CatchesDefect()
    {
        var fake = new FakeRevitPipeClient();
        var payload = new
        {
            total = 1,
            actuals = new[]
            {
                new
                {
                    key = "TestDoc#Column:C1",
                    category = "Column",
                    mark = "C1",
                    hasGeometry = true,
                    centroid = new { x = 0.0, y = 0.0, z = 0.0 },
                    widthMm = 200.0, // Defect: 200mm != 220mm
                    heightMm = 220.0
                }
            }
        };

        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            Value = BridgeJson.ToElement(payload)
        };

        var verifier = new MeasuredGeometricVerifier(fake);
        var report = await verifier.VerifyPlanGeometryAsync(CreateSamplePlan());

        Assert.False(report.OverallPassed);
        Assert.Equal(1, report.DefectCount);
        Assert.True(report.Audits[0].IsDimensionOutOfTolerance);
    }

    [Fact]
    public async Task VerifyPlanGeometryAsync_WhenRevitPipeFails_ThrowsRevitPipeException()
    {
        var fake = new FakeRevitPipeClient();
        fake.CustomExecuteHandler = req => ExecuteResult.Failure("Revit script crashed with unhandled exception.");

        var verifier = new MeasuredGeometricVerifier(fake);

        var ex = await Assert.ThrowsAsync<RevitPipeException>(() => verifier.VerifyPlanGeometryAsync(CreateSamplePlan()));
        Assert.Contains("script failed", ex.Message);
    }

    [Fact]
    public async Task VerifyPlanGeometryAsync_WhenClientNotConnected_AutomaticallyConnects()
    {
        var fake = new FakeRevitPipeClient { IsConnected = false };
        var payload = new
        {
            total = 1,
            actuals = new[]
            {
                new
                {
                    key = "TestDoc#Column:C1",
                    category = "Column",
                    mark = "C1",
                    hasGeometry = true,
                    centroid = new { x = 0.0, y = 0.0, z = 0.0 },
                    widthMm = 220.0,
                    heightMm = 220.0
                }
            }
        };

        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            Value = BridgeJson.ToElement(payload)
        };

        var verifier = new MeasuredGeometricVerifier(fake);
        var report = await verifier.VerifyPlanGeometryAsync(CreateSamplePlan());

        Assert.True(fake.IsConnected);
        Assert.True(report.OverallPassed);
    }

    [Fact]
    public async Task VerifyPlanGeometryAsync_WithCustomTolerances_AppliesCustomTolerances()
    {
        var fake = new FakeRevitPipeClient();
        var payload = new
        {
            total = 1,
            actuals = new[]
            {
                new
                {
                    key = "TestDoc#Column:C1",
                    category = "Column",
                    mark = "C1",
                    hasGeometry = true,
                    centroid = new { x = 0.0, y = 0.0, z = 0.0 },
                    widthMm = 218.0, // Deviation is 2.0mm
                    heightMm = 220.0
                }
            }
        };

        fake.CustomExecuteHandler = req => new ExecuteResult
        {
            IsError = false,
            Value = BridgeJson.ToElement(payload)
        };

        var verifier = new MeasuredGeometricVerifier(fake);
        var customTolerances = new CoordinationTolerances { SectionDimensionMm = 5.0 }; // Allows 5mm

        var report = await verifier.VerifyPlanGeometryAsync(CreateSamplePlan(), customTolerances);

        Assert.True(report.OverallPassed);
        Assert.Equal(1, report.VerifiedCount);
    }
}
