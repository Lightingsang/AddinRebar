namespace HPBim.Core.Tests;

using System;
using System.Collections.Generic;
using HPBim.Core.Coordination;
using HPBim.Core.Geometry;
using HPBim.Core.Qc;
using Xunit;

public class FoundationQcEngineTests
{
    private static BimCoordinationLedger CreateValidLedger(int piles = 196, int footings = 72, int beams = 192)
    {
        var elements = new List<CoordinationElementRecord>();
        for (int i = 1; i <= piles; i++)
        {
            elements.Add(new CoordinationElementRecord
            {
                Id = $"PILE_{i}",
                Category = CoordinationCategory.Pile,
                Code = $"P{i}",
                TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel
            });
        }
        for (int i = 1; i <= footings; i++)
        {
            elements.Add(new CoordinationElementRecord
            {
                Id = $"FT_{i}",
                Category = CoordinationCategory.Footing,
                Code = $"ĐC-4-{i:D2}",
                TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel
            });
        }
        for (int i = 1; i <= beams; i++)
        {
            elements.Add(new CoordinationElementRecord
            {
                Id = $"BM_{i}",
                Category = CoordinationCategory.GradeBeam,
                Code = $"GM-{i}",
                TechnicalGroup = ElementTechnicalGroup.Group1_ReadyToModel
            });
        }

        return new BimCoordinationLedger
        {
            ActivePhase = "PHAN_MONG",
            Elements = elements,
            Rfis = Array.Empty<RfiRecord>()
        };
    }

    [Fact]
    public void DistanceToBoundary_PointInsideSquare_ReturnsCorrectDistance()
    {
        var poly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(1000, 0),
            new Point2D(1000, 1000),
            new Point2D(0, 1000)
        });

        double dist = poly.DistanceToBoundary(new Point2D(500, 500));
        Assert.Equal(500.0, dist, 3);

        double distNear = poly.DistanceToBoundary(new Point2D(500, 100));
        Assert.Equal(100.0, distNear, 3);
    }

    [Fact]
    public void PileContainmentChecker_ValidPiles_ReportsAllContained()
    {
        var footingPoly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(2000, 0),
            new Point2D(2000, 2000),
            new Point2D(0, 2000)
        });

        var footings = new List<FootingAuditItem>
        {
            new(101, "ĐC-4-01", "ĐC-4", -1600.0, -2800.0, 1200.0, new Point2D(1000, 1000), footingPoly)
        };

        var piles = new List<PileAuditItem>
        {
            new(1, "P1", 500, 500, -2700.0, "P400"),
            new(2, "P2", 1500, 500, -2700.0, "P400"),
            new(3, "P3", 500, 1500, -2700.0, "P400"),
            new(4, "P4", 1500, 1500, -2700.0, "P400")
        };

        var checker = new PileContainmentChecker(150.0, 1.0);
        var summary = checker.CheckAll(piles, footings);

        Assert.True(summary.AllContained);
        Assert.Equal(4, summary.TotalPiles);
        Assert.Equal(4, summary.ContainedPiles);
        Assert.Empty(summary.Violations);
    }

    [Fact]
    public void PileContainmentChecker_PileOutsideFooting_ReportsViolation()
    {
        var footingPoly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(1000, 0),
            new Point2D(1000, 1000),
            new Point2D(0, 1000)
        });

        var footings = new List<FootingAuditItem>
        {
            new(101, "ĐC-1-01", "ĐC-1", -1600.0, -2600.0, 1000.0, new Point2D(500, 500), footingPoly)
        };

        var piles = new List<PileAuditItem>
        {
            new(1, "P1", 1500, 500, -2500.0, "P400")
        };

        var checker = new PileContainmentChecker(150.0, 1.0);
        var summary = checker.CheckAll(piles, footings);

        Assert.False(summary.AllContained);
        Assert.Equal(0, summary.ContainedPiles);
        Assert.Single(summary.Violations);
        Assert.Contains("nằm ngoài", summary.Violations[0].Reason);
    }

    [Fact]
    public void BeamBearingChecker_SupportedAndFloatingBeams_DetectedCorrectly()
    {
        var f1Poly = new Polygon2D(new[]
        {
            new Point2D(0, 0),
            new Point2D(1000, 0),
            new Point2D(1000, 1000),
            new Point2D(0, 1000)
        });
        var f2Poly = new Polygon2D(new[]
        {
            new Point2D(4000, 0),
            new Point2D(5000, 0),
            new Point2D(5000, 1000),
            new Point2D(4000, 1000)
        });

        var footings = new List<FootingAuditItem>
        {
            new(1, "F1", "ĐC-1", -1600, -2600, 1000, new Point2D(500, 500), f1Poly),
            new(2, "F2", "ĐC-1", -1600, -2600, 1000, new Point2D(4500, 500), f2Poly)
        };

        var beams = new List<BeamAuditItem>
        {
            new(101, "GM-1", "B_300x700", 300, 700, -1600, new Point2D(500, 500), new Point2D(4500, 500)),
            new(102, "GM-2", "B_300x400", 300, 400, -1600, new Point2D(500, 500), new Point2D(-3000, 500))
        };

        var checker = new BeamBearingChecker(50.0, 250.0);
        var summary = checker.CheckAll(beams, footings);

        Assert.False(summary.AllSupported);
        Assert.Equal(1, summary.ValidBeams);
        Assert.Single(summary.Violations);
        Assert.Equal(102, summary.Violations[0].BeamId);
    }

    [Fact]
    public void FoundationQcEngine_MissingGroundTruth_ReturnsInconclusive_NeverPassed()
    {
        var engine = new FoundationQcEngine();
        var piles = new List<PileAuditItem> { new(1, "P1", 100, 100, -2700.0, "P400") };
        var footings = new List<FootingAuditItem>();

        var res = engine.AuditPositions(piles, footings, groundTruthPiles: null, groundTruthFootings: null);

        Assert.Equal(FoundationStepStatus.Inconclusive, res.Status);
        Assert.False(res.Passed);
        Assert.True(res.IsInconclusive);
        Assert.Contains("Chưa đủ cơ sở", res.Summary);
    }

    [Fact]
    public void FoundationQcEngine_PendingRfiInLedger_CausesStep8ToFail()
    {
        var engine = new FoundationQcEngine();
        var priorSteps = new List<FoundationStepResult>();
        for (int i = 1; i <= 7; i++)
        {
            priorSteps.Add(new FoundationStepResult(i, $"Step {i}", FoundationStepStatus.Passed, "OK", Array.Empty<string>()));
        }

        var ledger = CreateValidLedger();
        ledger = ledger with
        {
            Rfis = new[]
            {
                new RfiRecord
                {
                    RfiId = "RFI-01",
                    Status = RfiStatus.PendingChiefEngineer,
                    Title = "Xung đột cao độ"
                }
            }
        };

        var step8 = engine.AuditLedgerAndDossier(priorSteps, ledger);

        Assert.Equal(FoundationStepStatus.Failed, step8.Status);
        Assert.False(step8.Passed);
        Assert.Contains("RFI", step8.Summary);
    }

    [Fact]
    public void FoundationQcEngine_FullAudit_WithValidGroundTruthAndLedger_PassesAll8Steps()
    {
        var engine = new FoundationQcEngine();
        var ctx = CreateValidFoundationAuditContext();

        var report = engine.ExecuteFullAudit(ctx);

        Assert.True(report.AllPassed);
        Assert.Equal(8, report.Steps.Count);
        Assert.Equal(8, report.PassedStepsCount);
        Assert.Equal(FoundationStepStatus.Passed, report.OverallStatus);
    }

    private static FoundationAuditContext CreateValidFoundationAuditContext()
    {
        var grids = Enumerable.Range(1, 27).Select(i => new GridAuditItem($"G{i}", 0, 0, 1000, 0, true)).ToList();
        var levels = Enumerable.Range(1, 10).Select(i => new LevelAuditItem($"L{i}", i * 1000, true)).ToList();

        var fPoly = new Polygon2D([new(0, 0), new(2000, 0), new(2000, 2000), new(0, 2000)]);
        var footings = new List<FootingAuditItem>();
        var gtFootings = new Dictionary<string, Point2D>();
        for (int i = 1; i <= 72; i++)
        {
            string mark = $"ĐC-4-{i:D2}";
            footings.Add(new FootingAuditItem(i, mark, "ĐC-4", -1600.0, -2800.0, 1200.0, new Point2D(1000, 1000), fPoly));
            gtFootings[mark] = new Point2D(1000, 1000);
        }

        var piles = Enumerable.Range(1, 196).Select(i => new PileAuditItem(i, $"P{i}", 1000, 1000, -2700.0, "P400")).ToList();
        var gtPiles = Enumerable.Range(1, 196).ToDictionary(i => (long)i, _ => new Point2D(1000, 1000));
        var beams = Enumerable.Range(1, 192).Select(i => new BeamAuditItem(i, $"GM-{i}", "B_300x700", 300, 700, -1600, new Point2D(1000, 1000), new Point2D(1000, 1000))).ToList();

        return new FoundationAuditContext(
            grids, levels, piles, footings, beams,
            ExpectedXGrids: new Dictionary<string, double>(),
            GroundTruthPiles: gtPiles,
            GroundTruthFootings: gtFootings,
            Ledger: CreateValidLedger(196, 72, 192));
    }
}
