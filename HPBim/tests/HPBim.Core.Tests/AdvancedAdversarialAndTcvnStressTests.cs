namespace HPBim.Core.Tests;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using HPBim.Core.Tcvn;
using Xunit;

public sealed class AdvancedAdversarialAndTcvnStressTests
{
    [Theory]
    [InlineData(6, 0.222)]
    [InlineData(8, 0.395)]
    [InlineData(10, 0.617)]
    [InlineData(12, 0.888)]
    [InlineData(14, 1.208)]
    [InlineData(16, 1.578)]
    [InlineData(18, 1.998)]
    [InlineData(20, 2.466)]
    [InlineData(22, 2.984)]
    [InlineData(25, 3.853)]
    [InlineData(28, 4.834)]
    [InlineData(32, 6.313)]
    public void TcvnUnitWeight_StandardDiameters_MatchTcvn5574(double diameterMm, double expectedWeightKgPerM)
    {
        // TCVN 5574:2018 formula: W = D^2 / 162.2
        var rebar = new RebarDef
        {
            DiameterMm = diameterMm,
            LengthMm = 1000.0,
            Quantity = 1
        };

        Assert.Equal(expectedWeightKgPerM, rebar.UnitWeightKgPerM, tolerance: 0.005);
        Assert.Equal(expectedWeightKgPerM, rebar.TotalWeightKg, tolerance: 0.005);
    }

    [Theory]
    [InlineData(StructuralElementKind.Footing, 50.0)]
    [InlineData(StructuralElementKind.Beam, 25.0)]
    [InlineData(StructuralElementKind.Column, 25.0)]
    [InlineData(StructuralElementKind.Stair, 20.0)]
    [InlineData(StructuralElementKind.Slab, 15.0)]
    [InlineData(StructuralElementKind.Wall, 20.0)]
    public void ConcreteCoverRules_MinimumCovers_StrictlyFollowTable19(
        StructuralElementKind kind, double expectedMinCover)
    {
        Assert.Equal(expectedMinCover, ConcreteCoverRules.GetMinimumCoverMm(kind));
        Assert.True(ConcreteCoverRules.IsCoverCompliant(kind, expectedMinCover));
        Assert.True(ConcreteCoverRules.IsCoverCompliant(kind, expectedMinCover + 1.0));
        Assert.False(ConcreteCoverRules.IsCoverCompliant(kind, expectedMinCover - 0.5));

        var valResult = ConcreteCoverRules.ValidateCover(kind, expectedMinCover - 1.0);
        Assert.False(valResult.IsCompliant);
        Assert.Equal(expectedMinCover, valResult.MinimumCoverMm);
    }

    [Fact]
    public void Vector2D_AdversarialMathAndDegeneracies()
    {
        var zero = Vector2D.Zero;
        Assert.True(zero.IsZero());

        // Normalizing zero vector throws
        Assert.Throws<InvalidOperationException>(() => zero.Normalize());
        Assert.False(zero.TryNormalize(out var unit));
        Assert.Equal(Vector2D.Zero, unit);

        // Division by zero throws
        var v = new Vector2D(3, 4);
        Assert.Throws<DivideByZeroException>(() => _ = v / 0.0);

        // Perpendicular normals
        var left = v.LeftNormal;
        var right = v.RightNormal;
        Assert.Equal(0.0, v.Dot(left), precision: 5);
        Assert.Equal(0.0, v.Dot(right), precision: 5);
        Assert.Equal(-25.0, left.Dot(right), precision: 5);

        // AngleRad and AngleDeg
        Assert.Equal(0.0, Vector2D.UnitX.AngleDeg, precision: 5);
        Assert.Equal(90.0, Vector2D.UnitY.AngleDeg, precision: 5);
        Assert.Equal(180.0, Math.Abs((-Vector2D.UnitX).AngleDeg), precision: 5);
        Assert.Equal(-90.0, (-Vector2D.UnitY).AngleDeg, precision: 5);
    }

    [Fact]
    public void BoundingBox2D_IntersectionsUnionsAndDeflate()
    {
        var boxA = new BoundingBox2D(0, 0, 100, 100);
        var boxB = new BoundingBox2D(50, 50, 150, 150);

        Assert.True(boxA.Intersects(boxB));
        var inter = boxA.Intersection(boxB);
        Assert.NotNull(inter);
        Assert.Equal(new BoundingBox2D(50, 50, 100, 100), inter.Value);

        var union = boxA.Union(boxB);
        Assert.Equal(new BoundingBox2D(0, 0, 150, 150), union);

        // Disjoint boxes
        var boxC = new BoundingBox2D(200, 200, 300, 300);
        Assert.False(boxA.Intersects(boxC));
        Assert.Null(boxA.Intersection(boxC));

        // Inflate / Deflate
        var inflated = boxA.Inflate(10, 20);
        Assert.Equal(new BoundingBox2D(-10, -20, 110, 120), inflated);

        var deflated = boxA.Deflate(0.2); // 20% deflation
        Assert.Equal(new BoundingBox2D(10, 10, 90, 90), deflated);
    }

    [Fact]
    public void ClearanceChecker_DoorSwingIntoRoomTagOrMepFixture()
    {
        // Wall from (0,0) to (5000, 0), direction (1, 0)
        // Door at offset 1000, width 900 -> hinge at (1000, 0), swing radius = 900
        var wall = new WallDef
        {
            Id = "W1",
            StartPoint = new Point2D(0, 0),
            EndPoint = new Point2D(5000, 0),
            BaseLevel = "Level 1",
            Openings = new[]
            {
                new OpeningDef
                {
                    Id = "D1",
                    DistanceAlongWallMm = 1000,
                    WidthMm = 900,
                    Kind = OpeningKind.Door
                }
            }
        };

        // 1. Room tag inside swing radius (distance to hinge = 500 < 810)
        var roomInside = new RoomDef
        {
            Name = "Room A",
            Level = "Level 1",
            TagLocation = new Point2D(1000, 500)
        };

        // 2. Room tag on different level (Level 2) -> ignored!
        var roomDiffLevel = new RoomDef
        {
            Name = "Room B",
            Level = "Level 2",
            TagLocation = new Point2D(1000, 500)
        };

        // 3. MEP Fixture inside swing radius (distance to hinge = 400 < 720)
        var fixInside = new MepFixtureDef
        {
            Id = "FX1",
            FamilyName = "Sink",
            Location = new Point3D(1000, 400, 0)
        };

        var checker = new ClearanceChecker();
        var violations = checker.CheckDoorSwingClearances(
            new[] { wall },
            new[] { fixInside },
            new[] { roomInside, roomDiffLevel });

        // Exactly 2 warnings: 1 for room tag on Level 1 (CLR-002), 1 for fixture (CLR-003)
        Assert.Equal(2, violations.Count);
        Assert.Contains(violations, v => v.RuleId == "CLR-002");
        Assert.Contains(violations, v => v.RuleId == "CLR-003");
    }

    [Fact]
    public void QcReport_FormatSummary_VietnameseReportOutputFidelity()
    {
        var errors = new[]
        {
            new QcViolation(QcSeverity.Error, "TCVN-COVER-001", "Lỗi lớp bảo vệ.")
        };
        var warnings = new[]
        {
            new QcViolation(QcSeverity.Warning, "TCVN-WALL-001", "Cảnh báo tường non-standard.")
        };
        var clashes = new[]
        {
            new QcClash("C1", "D1", "ColumnVsOpening", "Đè lên cột", new Point2D(100, 200), 50.0)
        };

        var report = new QcReport(errors, warnings, clashes, 6, TimeSpan.FromMilliseconds(42.5));
        Assert.False(report.IsValid);

        string summary = report.FormatSummary();
        Assert.Contains("KẾT QUẢ SOÁT LỖI QC OFFLINE", summary);
        Assert.Contains("TRẠNG THÁI: KHÔNG HỢP LỆ", summary);
        Assert.Contains("VA CHẠM CẤU KIỆN (CLASHES)", summary);
        Assert.Contains("ColumnVsOpening: C1 vs D1", summary);
        Assert.Contains("LỖI QUY CHUẨN (ERRORS)", summary);
        Assert.Contains("[TCVN-COVER-001] Lỗi lớp bảo vệ.", summary);
        Assert.Contains("CẢNH BÁO (WARNINGS)", summary);
        Assert.Contains("[TCVN-WALL-001] Cảnh báo tường non-standard.", summary);
    }

    [Fact]
    public void QcEngine_HighScaleModel_ExecutesInMilliseconds()
    {
        // Stress test: 100 columns, 100 walls with openings across 5 levels
        var columns = new List<ColumnDef>();
        var walls = new List<WallDef>();
        var rebars = new List<RebarDef>();

        for (int l = 1; l <= 5; l++)
        {
            string lvl = $"Level_{l}";
            for (int i = 0; i < 20; i++)
            {
                double x = i * 2000.0;
                columns.Add(new ColumnDef
                {
                    Id = $"C_{l}_{i}",
                    CenterLocation = new Point2D(x, 0),
                    WidthMm = 220,
                    BaseLevel = lvl
                });

                walls.Add(new WallDef
                {
                    Id = $"W_{l}_{i}",
                    StartPoint = new Point2D(x, 1000),
                    EndPoint = new Point2D(x + 1800, 1000),
                    BaseLevel = lvl,
                    ThicknessMm = 220,
                    Openings = new[]
                    {
                        new OpeningDef { Id = $"OP_{l}_{i}", DistanceAlongWallMm = 500, WidthMm = 800 }
                    }
                });

                rebars.Add(new RebarDef
                {
                    Id = $"RB_{l}_{i}",
                    BarMark = i,
                    CoverMm = 25.0,
                    DiameterMm = 16.0,
                    LengthMm = 2000.0
                });
            }
        }

        var model = new BimModel
        {
            ProjectName = "HighScaleBuilding",
            Columns = columns,
            Walls = walls,
            Rebars = rebars
        };

        var engine = new QcEngine();
        var report = engine.Validate(model);

        Assert.Equal(6, report.TotalChecksRun);
        Assert.True(report.IsValid);
        Assert.Empty(report.Errors);
        Assert.Empty(report.Clashes);
        // Execution should be well under 1 second (1000ms)
        Assert.True(report.Duration.TotalMilliseconds < 1000.0, $"Took {report.Duration.TotalMilliseconds}ms");
    }
}
