namespace HPBim.Core.Tests;

using System;
using System.Linq;
using HPBim.Core.Geometry;
using HPBim.Core.Models;
using HPBim.Core.Qc;
using HPBim.Core.Serialization;
using HPBim.Core.Tests.Fixtures;
using Xunit;

public sealed class NhaPho5x18mTests
{
    private readonly BimModel _model = NhaPho5x18mFixture.CreateModel();

    [Fact]
    public void Benchmark_Levels_MatchExactElevationsAndCount()
    {
        Assert.Equal(5, _model.Levels.Count);

        var tret = _model.GetLevel("TRET");
        Assert.NotNull(tret);
        Assert.Equal(0.150, tret.ElevationM, precision: 3);
        Assert.Equal(3650.0, tret.HeightMm);
        Assert.Equal(1, tret.FloorNumber);

        var lau1 = _model.GetLevel("LAU 1");
        Assert.NotNull(lau1);
        Assert.Equal(3.800, lau1.ElevationM, precision: 3);
        Assert.Equal(3400.0, lau1.HeightMm);
        Assert.Equal(2, lau1.FloorNumber);

        var lau2 = _model.GetLevel("LAU 2");
        Assert.NotNull(lau2);
        Assert.Equal(7.200, lau2.ElevationM, precision: 3);
        Assert.Equal(3400.0, lau2.HeightMm);
        Assert.Equal(3, lau2.FloorNumber);

        var sanThuong = _model.GetLevel("SAN THUONG");
        Assert.NotNull(sanThuong);
        Assert.Equal(10.600, sanThuong.ElevationM, precision: 3);
        Assert.Equal(2800.0, sanThuong.HeightMm);
        Assert.Equal(4, sanThuong.FloorNumber);

        var maiChe = _model.GetLevel("MAI CHE THANG");
        Assert.NotNull(maiChe);
        Assert.Equal(13.400, maiChe.ElevationM, precision: 3);
        Assert.Equal(5, maiChe.FloorNumber);
    }

    [Fact]
    public void Benchmark_Grids_MatchExactCoordinatesAndCount()
    {
        Assert.Equal(7, _model.Grids.Count);

        var xGrids = _model.Grids.Where(g => g.Direction == GridDirection.X).OrderBy(g => g.CoordinateMm).ToList();
        Assert.Equal(5, xGrids.Count);
        double[] expectedX = [0.0, 5500.0, 9500.0, 13000.0, 18000.0];
        string[] expectedNames = ["1", "2", "3", "4", "5"];

        for (int i = 0; i < expectedX.Length; i++)
        {
            Assert.Equal(expectedNames[i], xGrids[i].Name);
            Assert.Equal(expectedX[i], xGrids[i].CoordinateMm, precision: 1);
        }

        var yGrids = _model.Grids.Where(g => g.Direction == GridDirection.Y).OrderBy(g => g.CoordinateMm).ToList();
        Assert.Equal(2, yGrids.Count);
        Assert.Equal("A", yGrids[0].Name);
        Assert.Equal(0.0, yGrids[0].CoordinateMm, precision: 1);
        Assert.Equal("B", yGrids[1].Name);
        Assert.Equal(5000.0, yGrids[1].CoordinateMm, precision: 1);
    }

    [Fact]
    public void Benchmark_Columns_MatchExactCountAndSection()
    {
        Assert.Equal(10, _model.Columns.Count);

        foreach (var col in _model.Columns)
        {
            Assert.Equal(220.0, col.WidthMm);
            Assert.Equal(220.0, col.DepthMm);
            Assert.Equal("TRET", col.BaseLevel);
            Assert.Equal("SAN THUONG", col.TopLevel);
        }

        var locations = _model.Columns.Select(c => (c.CenterLocation.X, c.CenterLocation.Y)).ToHashSet();
        double[] xs = [0.0, 5500.0, 9500.0, 13000.0, 18000.0];
        double[] ys = [0.0, 5000.0];

        foreach (double x in xs)
        {
            foreach (double y in ys)
            {
                Assert.Contains((x, y), locations);
            }
        }
    }

    [Fact]
    public void Benchmark_Walls_MatchExactCountAndThickness()
    {
        Assert.Equal(26, _model.Walls.Count);

        var t1Walls = _model.GetWallsByLevel("TRET").ToList();
        var t2Walls = _model.GetWallsByLevel("LAU 1").ToList();
        var t3Walls = _model.GetWallsByLevel("LAU 2").ToList();

        Assert.Equal(8, t1Walls.Count);
        Assert.Equal(9, t2Walls.Count);
        Assert.Equal(9, t3Walls.Count);

        foreach (var w in _model.Walls)
        {
            Assert.True(Math.Abs(w.ThicknessMm - 110.0) < 1e-3 || Math.Abs(w.ThicknessMm - 220.0) < 1e-3);
        }
    }

    [Fact]
    public void Benchmark_Openings_MatchExactCountsAndHosts()
    {
        int totalOpenings = _model.Walls.Sum(w => w.Openings?.Count ?? 0);
        Assert.Equal(23, totalOpenings);

        int t1Openings = _model.GetWallsByLevel("TRET").Sum(w => w.Openings.Count);
        int t2Openings = _model.GetWallsByLevel("LAU 1").Sum(w => w.Openings.Count);
        int t3Openings = _model.GetWallsByLevel("LAU 2").Sum(w => w.Openings.Count);

        Assert.Equal(7, t1Openings);
        Assert.Equal(8, t2Openings);
        Assert.Equal(8, t3Openings);
    }

    [Fact]
    public void Benchmark_Rooms_MatchExactCountAndAreas()
    {
        Assert.Equal(16, _model.Rooms.Count);

        var t1Rooms = _model.Rooms.Where(r => r.Level == "TRET").ToList();
        var t2Rooms = _model.Rooms.Where(r => r.Level == "LAU 1").ToList();
        var t3Rooms = _model.Rooms.Where(r => r.Level == "LAU 2").ToList();

        Assert.Equal(5, t1Rooms.Count);
        Assert.Equal(5, t2Rooms.Count);
        Assert.Equal(6, t3Rooms.Count);

        var gara = t1Rooms.First(r => r.Name == "Gara o to");
        Assert.Equal(26.4, gara.TheoreticalAreaM2);

        var bed1 = t2Rooms.First(r => r.Name == "Phong ngu master 1");
        Assert.Equal(30.5, bed1.TheoreticalAreaM2);
    }

    [Fact]
    public void Benchmark_BeamsAndSlabs_MatchStructuralGrid()
    {
        Assert.Equal(24, _model.Beams.Count);
        Assert.Equal(12, _model.Slabs.Count);

        foreach (var beam in _model.Beams)
        {
            Assert.Equal(200.0, beam.WidthMm);
            Assert.True(beam.HeightMm == 300.0 || beam.HeightMm == 400.0);
        }

        foreach (var slab in _model.Slabs)
        {
            Assert.Equal(100.0, slab.ThicknessMm);
        }
    }

    [Fact]
    public void Benchmark_MepFixturesAndPipes_MatchLayout()
    {
        Assert.Equal(26, _model.Fixtures.Count);
        Assert.Equal(3, _model.Pipes.Count);

        int sanitaryCount = _model.Fixtures.Count(f => f.Category == MepCategory.Sanitary);
        Assert.Equal(11, sanitaryCount);
    }

    [Fact]
    public void Benchmark_RebarTotalWeight_MatchesGroundTruth7966_5Kg()
    {
        Assert.Equal(33, _model.Rebars.Count);

        double totalWeight = _model.TotalRebarWeightKg;
        Assert.Equal(7966.5, Math.Round(totalWeight, 1));

        double foundationWeight = _model.Rebars.Where(r => r.Zone == "Foundation").Sum(r => r.TotalWeightKg);
        Assert.Equal(748.9, Math.Round(foundationWeight, 1));

        double superWeight = _model.Rebars.Where(r => r.Zone == "Superstructure").Sum(r => r.TotalWeightKg);
        Assert.Equal(6846.5, Math.Round(superWeight, 1));

        double stairWeight = _model.Rebars.Where(r => r.Zone == "Stair").Sum(r => r.TotalWeightKg);
        Assert.Equal(371.1, Math.Round(stairWeight, 1));

        double dSmallWeight = _model.Rebars.Where(r => r.Zone == "Superstructure" && r.DiameterMm <= 10).Sum(r => r.TotalWeightKg);
        double stairSmallWeight = _model.Rebars.Where(r => r.Zone == "Stair" && r.DiameterMm <= 10).Sum(r => r.TotalWeightKg);
        Assert.Equal(4120.9, Math.Round(dSmallWeight + stairSmallWeight, 1));

        double dLargeWeight = _model.Rebars.Where(r => r.Zone == "Superstructure" && r.DiameterMm > 10).Sum(r => r.TotalWeightKg);
        double stairLargeWeight = _model.Rebars.Where(r => r.Zone == "Stair" && r.DiameterMm > 10).Sum(r => r.TotalWeightKg);
        Assert.Equal(3096.7, Math.Round(dLargeWeight + stairLargeWeight, 1));
    }

    [Fact]
    public void Benchmark_QcEngine_ValidatesCleanModel()
    {
        var qcEngine = new QcEngine();
        var report = qcEngine.Validate(_model);

        Assert.True(report.IsValid);
        Assert.Empty(report.Errors);
    }

    [Fact]
    public void Benchmark_JsonSerialization_RoundTripsAccurately()
    {
        string json = BimModelSerializer.Serialize(_model);
        Assert.False(string.IsNullOrWhiteSpace(json));

        var deserialized = BimModelSerializer.Deserialize(json);
        Assert.NotNull(deserialized);
        Assert.Equal(_model.Levels.Count, deserialized.Levels.Count);
        Assert.Equal(_model.Grids.Count, deserialized.Grids.Count);
        Assert.Equal(_model.Columns.Count, deserialized.Columns.Count);
        Assert.Equal(_model.Walls.Count, deserialized.Walls.Count);
        Assert.Equal(_model.Rooms.Count, deserialized.Rooms.Count);
        Assert.Equal(_model.Rebars.Count, deserialized.Rebars.Count);
        Assert.Equal(7966.5, Math.Round(deserialized.TotalRebarWeightKg, 1));
    }
}
