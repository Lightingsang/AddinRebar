namespace HPBim.Core.Tests;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using Xunit;

public sealed class QcEngineTests
{
    [Fact]
    public void QcEngine_Validate_CleanModel_PassesWithZeroErrorsAndClashes()
    {
        var model = new BimModel
        {
            ProjectName = "Clean Model Test",
            Columns =
            [
                new ColumnDef { Id = "C1", CenterLocation = new Point2D(0, 0), BaseLevel = "T1" }
            ],
            Walls =
            [
                new WallDef
                {
                    Id = "W1",
                    StartPoint = new Point2D(0, 0),
                    EndPoint = new Point2D(5000, 0),
                    ThicknessMm = 220.0,
                    BaseLevel = "T1",
                    Openings =
                    [
                        // Opening offset 1000mm from (0,0), safe distance from column
                        new OpeningDef { Id = "D1", DistanceAlongWallMm = 1000, WidthMm = 900, HeightMm = 2200, Kind = OpeningKind.Door }
                    ]
                }
            ],
            Rebars =
            [
                new RebarDef { Id = "R1", BarMark = 1, DiameterMm = 16, LengthMm = 5000, Quantity = 4, CoverMm = 25.0 }
            ]
        };

        var engine = new QcEngine();
        var report = engine.Validate(model);

        Assert.True(report.IsValid);
        Assert.Empty(report.Errors);
        Assert.Empty(report.Clashes);
        Assert.True(report.TotalChecksRun >= 6);
    }

    [Fact]
    public void QcEngine_Validate_CoverViolations_FailsWithErrors()
    {
        var model = new BimModel
        {
            Rebars =
            [
                new RebarDef { Id = "R1", BarMark = 1, DiameterMm = 16, LengthMm = 4000, Quantity = 2, CoverMm = 10.0 } // < 15mm TCVN
            ]
        };

        var engine = new QcEngine();
        var report = engine.Validate(model);

        Assert.False(report.IsValid);
        Assert.Single(report.Errors);
        Assert.Equal("TCVN-COVER-001", report.Errors[0].RuleId);
        Assert.Equal(QcSeverity.Error, report.Errors[0].Severity);
    }

    [Fact]
    public void QcEngine_Validate_ColumnOpeningClash_FailsWithClashes()
    {
        var model = new BimModel
        {
            Columns =
            [
                new ColumnDef { Id = "C1", CenterLocation = new Point2D(0, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "T1" }
            ],
            Walls =
            [
                new WallDef
                {
                    Id = "W1",
                    StartPoint = new Point2D(0, 0),
                    EndPoint = new Point2D(5000, 0),
                    ThicknessMm = 220,
                    BaseLevel = "T1",
                    Openings =
                    [
                        // Opening overlaps column footprint directly: start at 10mm, width 800mm
                        new OpeningDef { Id = "D1", DistanceAlongWallMm = 10, WidthMm = 800, HeightMm = 2200, Kind = OpeningKind.Door }
                    ]
                }
            ]
        };

        var engine = new QcEngine();
        var report = engine.Validate(model);

        Assert.False(report.IsValid);
        Assert.NotEmpty(report.Clashes);
        var clash = Assert.Single(report.Clashes);
        Assert.Equal("ColumnVsOpening", clash.ClashType);
        Assert.Equal("C1", clash.ElementIdA);
        Assert.Equal("D1", clash.ElementIdB);
    }

    [Fact]
    public void QcEngine_Validate_BeamSlabClash_FlagsSecondaryBeamSlicingSlab()
    {
        var model = new BimModel
        {
            Slabs =
            [
                new SlabDef
                {
                    Id = "S1",
                    Level = "L1",
                    Boundary = new Polygon2D([
                        new Point2D(0, 0),
                        new Point2D(6000, 0),
                        new Point2D(6000, 6000),
                        new Point2D(0, 6000)
                    ])
                }
            ],
            Beams =
            [
                // Secondary beam cuts across the middle of undivided slab
                new BeamDef
                {
                    Id = "B_SEC",
                    Level = "L1",
                    StartPoint = new Point2D(0, 3000),
                    EndPoint = new Point2D(6000, 3000),
                    WidthMm = 200,
                    HeightMm = 300,
                    IsTieBeam = false
                },
                // Tie beam does NOT trigger clash even if under slab
                new BeamDef
                {
                    Id = "B_TIE",
                    Level = "L1",
                    StartPoint = new Point2D(0, 4000),
                    EndPoint = new Point2D(6000, 4000),
                    WidthMm = 250,
                    HeightMm = 400,
                    IsTieBeam = true
                }
            ]
        };

        var engine = new QcEngine();
        var report = engine.Validate(model);

        Assert.False(report.IsValid);
        var clash = Assert.Single(report.Clashes);
        Assert.Equal("SecondaryBeamSlicesUndividedSlab", clash.ClashType);
        Assert.Equal("B_SEC", clash.ElementIdA);
    }

    [Fact]
    public void QcEngine_Validate_ClearanceViolations_GeneratesWarnings()
    {
        var model = new BimModel
        {
            Columns =
            [
                new ColumnDef { Id = "C1", CenterLocation = new Point2D(0, 0), WidthMm = 220, DepthMm = 220, BaseLevel = "T1" }
            ],
            Walls =
            [
                new WallDef
                {
                    Id = "W1",
                    StartPoint = new Point2D(0, 0),
                    EndPoint = new Point2D(5000, 0),
                    ThicknessMm = 150.0, // Non-standard thickness -> TCVN-WALL-001
                    BaseLevel = "T1",
                    Openings =
                    [
                        // Opening with edge clearance 50mm from column edge (< 100mm) -> CLR-001
                        // Col half-width = 110mm. DistanceAlongWall = 160mm. Edge gap = 160 - 110 = 50mm.
                        new OpeningDef { Id = "D1", DistanceAlongWallMm = 160, WidthMm = 800, HeightMm = 2100, Kind = OpeningKind.Door }
                    ]
                }
            ],
            Rooms =
            [
                // Room tag within door swing radius -> CLR-002
                new RoomDef { Name = "WC", Level = "T1", TagLocation = new Point2D(200, 100) }
            ],
            Fixtures =
            [
                // Fixture within door swing radius -> CLR-003
                new MepFixtureDef { Id = "FX1", FamilyName = "LAVABO", Level = "T1", Location = new Point3D(200, 200, 0) }
            ]
        };

        var engine = new QcEngine();
        var report = engine.Validate(model);

        Assert.True(report.IsValid); // Warnings do NOT invalidate model
        Assert.Empty(report.Errors);
        Assert.Empty(report.Clashes);
        Assert.True(report.Warnings.Count >= 4);

        var ruleIds = new HashSet<string>();
        foreach (var w in report.Warnings)
        {
            ruleIds.Add(w.RuleId);
        }

        Assert.Contains("CLR-001", ruleIds);
        Assert.Contains("CLR-002", ruleIds);
        Assert.Contains("CLR-003", ruleIds);
        Assert.Contains("TCVN-WALL-001", ruleIds);
    }

    [Fact]
    public void QcEngine_Validate_QcOptions_ControlsActiveCheckCount()
    {
        var model = new BimModel();
        var engine = new QcEngine();

        var noneOpts = new QcOptions(CheckClashes: false, CheckClearances: false, CheckTcvnCompliance: false);
        var repNone = engine.Validate(model, noneOpts);
        Assert.Equal(0, repNone.TotalChecksRun);

        var clashOnly = new QcOptions(CheckClashes: true, CheckClearances: false, CheckTcvnCompliance: false);
        var repClash = engine.Validate(model, clashOnly);
        Assert.Equal(2, repClash.TotalChecksRun);

        var clrOnly = new QcOptions(CheckClashes: false, CheckClearances: true, CheckTcvnCompliance: false);
        var repClr = engine.Validate(model, clrOnly);
        Assert.Equal(3, repClr.TotalChecksRun);

        var tcvnOnly = new QcOptions(CheckClashes: false, CheckClearances: false, CheckTcvnCompliance: true);
        var repTcvn = engine.Validate(model, tcvnOnly);
        Assert.Equal(1, repTcvn.TotalChecksRun);
    }

    [Fact]
    public void QcEngine_Validate_FormatSummary_ProducesDetailedVietnameseOutput()
    {
        var cleanReport = new QcEngine().Validate(new BimModel());
        string cleanSummary = cleanReport.FormatSummary();
        Assert.Contains("=== KẾT QUẢ SOÁT LỖI QC OFFLINE", cleanSummary);
        Assert.Contains("TRẠNG THÁI: SẠCH", cleanSummary);

        var dirtyModel = new BimModel
        {
            Rebars = [new RebarDef { BarMark = 1, CoverMm = 10.0 }]
        };
        var dirtyReport = new QcEngine().Validate(dirtyModel);
        string dirtySummary = dirtyReport.FormatSummary();
        Assert.Contains("TRẠNG THÁI: KHÔNG HỢP LỆ", dirtySummary);
        Assert.Contains("TCVN-COVER-001", dirtySummary);
    }

    [Fact]
    public void QcEngine_Validate_HandlesNullModelDefensively()
    {
        var engine = new QcEngine();
        Assert.Throws<ArgumentNullException>(() => engine.Validate(null!));
    }
}
