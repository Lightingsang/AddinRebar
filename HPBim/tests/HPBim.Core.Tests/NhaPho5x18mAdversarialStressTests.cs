namespace HPBim.Core.Tests;

using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using HPBim.Core.Serialization;
using HPBim.Core.Tests.Fixtures;
using Xunit;

public sealed class NhaPho5x18mAdversarialStressTests
{
    private static BimModel CloneBenchmarkModel()
    {
        var original = NhaPho5x18mFixture.CreateModel();
        return BimModelSerializer.Deserialize(BimModelSerializer.Serialize(original, indented: false));
    }

    [Fact]
    public void Adversarial_ShiftColumnToClashOpening_CaughtByQcEngine()
    {
        var model = CloneBenchmarkModel();
        // Shift column C_3_A from (9500, 0) to (9500, 900) penetrating door W1_06_D4 (at Y=900)
        var modifiedCols = model.Columns.Select(c => c.Id == "C_3_A"
            ? new ColumnDef { Id = c.Id, CenterLocation = new Point2D(9500, 900), WidthMm = c.WidthMm, DepthMm = c.DepthMm, BaseLevel = c.BaseLevel }
            : c).ToList();

        var brokenModel = new BimModel { Columns = modifiedCols, Walls = model.Walls };
        var report = new QcEngine().Validate(brokenModel);

        Assert.False(report.IsValid);
        Assert.NotEmpty(report.Clashes);
        var clash = Assert.Single(report.Clashes, c => c.ClashType == "ColumnVsOpening");
        Assert.Equal("C_3_A", clash.ElementIdA);
        Assert.Equal("W1_06_D4", clash.ElementIdB);
    }

    [Fact]
    public void Adversarial_ViolateConcreteCover_CaughtByQcEngine()
    {
        var model = CloneBenchmarkModel();
        // Violate cover: set cover to 8.0mm (< 15mm TCVN minimum for slab/general)
        var brokenRebars = model.Rebars.Select(r => r.Id == "RB-FL-09"
            ? new RebarDef { Id = r.Id, HostElementId = r.HostElementId, BarMark = r.BarMark, DiameterMm = r.DiameterMm, LengthMm = r.LengthMm, Quantity = r.Quantity, CoverMm = 8.0 }
            : r).ToList();

        var report = new QcEngine().Validate(new BimModel { Rebars = brokenRebars });

        Assert.False(report.IsValid);
        Assert.NotEmpty(report.Errors);
        Assert.Contains(report.Errors, e => e.RuleId == "TCVN-COVER-001");
    }

    [Fact]
    public void Adversarial_SecondaryBeamSlicesSlab_CaughtByQcEngine()
    {
        var model = CloneBenchmarkModel();
        // Insert a secondary beam slicing undivided slab S3_LAU 1 (5500,0 to 9500,5000)
        var newBeam = new BeamDef { Id = "D_SLICE_01", StartPoint = new Point2D(5500, 2500), EndPoint = new Point2D(9500, 2500), WidthMm = 200, HeightMm = 300, Level = "LAU 1", IsTieBeam = false };
        var brokenModel = new BimModel { Beams = model.Beams.Concat([newBeam]).ToList(), Slabs = model.Slabs };
        var report = new QcEngine().Validate(brokenModel);

        Assert.False(report.IsValid);
        Assert.NotEmpty(report.Clashes);
        Assert.Contains(report.Clashes, c => c.ClashType == "SecondaryBeamSlicesUndividedSlab" && c.ElementIdA == "D_SLICE_01");
    }

    [Fact]
    public void Adversarial_OpeningClearanceViolation_CaughtByQcEngine()
    {
        var model = CloneBenchmarkModel();
        // Shift opening W1_06_D4 to 150mm (distance to column face = 150 - 110 = 40mm < 100mm)
        var wall = model.Walls.First(w => w.Id == "W1_06");
        var shiftedOps = wall.Openings.Select(o => o.Id == "W1_06_D4"
            ? new OpeningDef { Id = o.Id, DistanceAlongWallMm = 150.0, WidthMm = o.WidthMm, HeightMm = o.HeightMm, Kind = o.Kind }
            : o).ToList();
        var modifiedWalls = model.Walls.Select(w => w.Id == "W1_06"
            ? new WallDef { Id = w.Id, StartPoint = w.StartPoint, EndPoint = w.EndPoint, ThicknessMm = w.ThicknessMm, BaseLevel = w.BaseLevel, Openings = shiftedOps }
            : w).ToList();

        var report = new QcEngine().Validate(new BimModel { Columns = model.Columns, Walls = modifiedWalls });

        Assert.NotEmpty(report.Warnings);
        Assert.Contains(report.Warnings, w => w.RuleId == "CLR-001" && w.ElementId == "W1_06_D4");
    }

    [Fact]
    public void Adversarial_DoorSwingClashFixture_CaughtByQcEngine()
    {
        var model = CloneBenchmarkModel();
        // Place sanitary fixture right at door swing trajectory: hinge at (9500, 500), door width 800mm
        var clashingFixture = new MepFixtureDef { Id = "FX_CLASH_01", FamilyName = "Lavabo", Location = new Point3D(9600, 600, 0), Category = MepCategory.Sanitary };
        var brokenModel = new BimModel { Walls = model.Walls, Fixtures = model.Fixtures.Concat([clashingFixture]).ToList() };
        var report = new QcEngine().Validate(brokenModel);

        Assert.NotEmpty(report.Warnings);
        Assert.Contains(report.Warnings, w => w.RuleId == "CLR-003" && w.ElementId == "W1_06_D4");
    }

    [Fact]
    public void Adversarial_NonStandardWallThickness_CaughtByQcEngine()
    {
        var model = CloneBenchmarkModel();
        var brokenWalls = model.Walls.Select(w => w.Id == "W1_01"
            ? new WallDef { Id = w.Id, StartPoint = w.StartPoint, EndPoint = w.EndPoint, ThicknessMm = 150.0, BaseLevel = w.BaseLevel }
            : w).ToList();

        var report = new QcEngine().Validate(new BimModel { Walls = brokenWalls });

        Assert.NotEmpty(report.Warnings);
        Assert.Contains(report.Warnings, w => w.RuleId == "TCVN-WALL-001" && w.ElementId == "W1_01");
    }

    [Fact]
    public void Adversarial_MultipleCompoundErrors_CaughtSimultaneously()
    {
        var model = CloneBenchmarkModel();
        var brokenRebars = model.Rebars.Select(r => r.Id == "RB-FL-09"
            ? new RebarDef { Id = r.Id, CoverMm = 5.0, DiameterMm = r.DiameterMm, LengthMm = r.LengthMm, Quantity = r.Quantity }
            : r).ToList();
        var brokenCols = model.Columns.Select(c => c.Id == "C_3_A"
            ? new ColumnDef { Id = c.Id, CenterLocation = new Point2D(9500, 900), WidthMm = 220, DepthMm = 220, BaseLevel = c.BaseLevel }
            : c).ToList();
        var brokenWalls = model.Walls.Select(w => w.Id == "W1_01"
            ? new WallDef { Id = w.Id, StartPoint = w.StartPoint, EndPoint = w.EndPoint, ThicknessMm = 180.0, BaseLevel = w.BaseLevel }
            : w).ToList();

        var report = new QcEngine().Validate(new BimModel { Columns = brokenCols, Walls = brokenWalls, Rebars = brokenRebars });

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, e => e.RuleId == "TCVN-COVER-001");
        Assert.Contains(report.Clashes, c => c.ClashType == "ColumnVsOpening");
        Assert.Contains(report.Warnings, w => w.RuleId == "TCVN-WALL-001");
    }

    [Fact]
    public void BenchmarkModel_JsonSerialization_DeepRoundTripFidelity()
    {
        var original = NhaPho5x18mFixture.CreateModel();
        string json = BimModelSerializer.Serialize(original, indented: true);
        var restored = BimModelSerializer.Deserialize(json);

        Assert.Equal(original.Levels.Count, restored.Levels.Count);
        Assert.Equal(original.Grids.Count, restored.Grids.Count);
        Assert.Equal(original.Columns.Count, restored.Columns.Count);
        Assert.Equal(original.Walls.Count, restored.Walls.Count);
        Assert.Equal(original.Rooms.Count, restored.Rooms.Count);
        Assert.Equal(original.Beams.Count, restored.Beams.Count);
        Assert.Equal(original.Slabs.Count, restored.Slabs.Count);
        Assert.Equal(original.Rebars.Count, restored.Rebars.Count);
        Assert.Equal(original.Fixtures.Count, restored.Fixtures.Count);
        Assert.Equal(original.Pipes.Count, restored.Pipes.Count);
        Assert.Equal(7966.5, Math.Round(restored.TotalRebarWeightKg, 1));
    }

    [Fact]
    public void BenchmarkModel_JsonSerialization_CompactVsIndentedEquivalence()
    {
        var original = NhaPho5x18mFixture.CreateModel();
        string compact = BimModelSerializer.Serialize(original, indented: false);
        string indented = BimModelSerializer.Serialize(original, indented: true);

        Assert.True(compact.Length < indented.Length);
        Assert.DoesNotContain("\n  ", compact);

        var fromCompact = BimModelSerializer.Deserialize(compact);
        var fromIndented = BimModelSerializer.Deserialize(indented);

        Assert.Equal(fromCompact.Columns.Count, fromIndented.Columns.Count);
        Assert.Equal(fromCompact.Walls.Count, fromIndented.Walls.Count);
        Assert.Equal(fromCompact.TotalRebarWeightKg, fromIndented.TotalRebarWeightKg, precision: 3);
    }

    [Fact]
    public void BenchmarkModel_JsonSerialization_MultiCycleIdempotency()
    {
        var modelA = NhaPho5x18mFixture.CreateModel();
        string json1 = BimModelSerializer.Serialize(modelA, indented: false);
        var modelB = BimModelSerializer.Deserialize(json1);
        string json2 = BimModelSerializer.Serialize(modelB, indented: false);
        var modelC = BimModelSerializer.Deserialize(json2);

        Assert.Equal(json1, json2);
        Assert.Equal(modelB.TotalRebarWeightKg, modelC.TotalRebarWeightKg, precision: 4);
    }

    [Fact]
    public void BenchmarkModel_JsonSerialization_ScaledVolumePerformance()
    {
        var original = NhaPho5x18mFixture.CreateModel();
        var scaledColumns = Enumerable.Range(0, 10).SelectMany(i => original.Columns.Select(c => new ColumnDef
        {
            Id = $"{c.Id}_{i}",
            CenterLocation = new Point2D(c.CenterLocation.X + i * 20000, c.CenterLocation.Y),
            WidthMm = c.WidthMm,
            DepthMm = c.DepthMm
        })).ToList();

        var scaledRebars = Enumerable.Range(0, 10).SelectMany(i => original.Rebars.Select(r => new RebarDef
        {
            Id = $"{r.Id}_{i}",
            DiameterMm = r.DiameterMm,
            LengthMm = r.LengthMm,
            Quantity = r.Quantity,
            CoverMm = r.CoverMm
        })).ToList();

        var sw = Stopwatch.StartNew();
        string json = BimModelSerializer.Serialize(new BimModel { Columns = scaledColumns, Rebars = scaledRebars }, indented: false);
        var restored = BimModelSerializer.Deserialize(json);
        sw.Stop();

        Assert.Equal(100, restored.Columns.Count);
        Assert.Equal(330, restored.Rebars.Count);
        Assert.True(sw.ElapsedMilliseconds < 500, $"Scaled serialization took {sw.ElapsedMilliseconds}ms (> 500ms)");
    }

    [Fact]
    public void BenchmarkModel_JsonSerialization_AdversarialInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => BimModelSerializer.Serialize(null!));
        Assert.Throws<ArgumentException>(() => BimModelSerializer.Deserialize(string.Empty));
        Assert.Throws<ArgumentException>(() => BimModelSerializer.Deserialize("   "));
        Assert.Throws<JsonException>(() => BimModelSerializer.Deserialize("{ invalid json: 123 }"));

        var minimal = BimModelSerializer.Deserialize("{\"projectName\":\"Test\"}");
        Assert.NotNull(minimal);
        Assert.Equal("Test", minimal.ProjectName);
        Assert.NotNull(minimal.Columns);
        Assert.Empty(minimal.Columns);
    }
}
