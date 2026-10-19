namespace HPBim.Core.Tests;

using System;
using System.Collections.Generic;
using HPBim.Core.Geometry;
using Xunit;

public class GradeBeamGridSnapperTests
{
    private static readonly double[] XGrids = { 0, 1300, 6900, 9900, 13900, 16500, 23700, 29050, 35750, 38300, 40800, 45000, 48900 };
    private static readonly double[] YGrids = { 0, 2800, 4650, 7150, 9950, 12750, 13950, 15150, 17950, 20000, 20750, 23250, 25100, 27900 };

    [Fact]
    public void SnapAll_HorizontalBeamNearGridC_SnapsToGridIntersections()
    {
        // Raw beam from CAD: started near Grid 11 (X=40800) and Grid 12 (X=45000), along Y=4650
        var raw = new List<BeamRawSpan>
        {
            new("GM-3", 300, 700, 40918.5, 4650.0, 44218.5, 4650.0)
        };

        var snapped = GradeBeamGridSnapper.SnapAll(raw, XGrids, YGrids);

        Assert.Single(snapped);
        var b = snapped[0];
        Assert.Equal(4650.0, b.StartPoint.Y);
        Assert.Equal(4650.0, b.EndPoint.Y);
        Assert.Equal(40800.0, b.StartPoint.X); // Snapped to Grid 11
        Assert.Equal(45000.0, b.EndPoint.X); // Snapped to Grid 12
    }

    [Fact]
    public void SnapAll_BoundaryBeamTrụcA_AssignsPositiveYOffset()
    {
        var raw = new List<BeamRawSpan>
        {
            new("GM-1", 300, 700, 1300.0, 0.0, 6900.0, 0.0)
        };

        var snapped = GradeBeamGridSnapper.SnapAll(raw, XGrids, YGrids);

        Assert.Single(snapped);
        Assert.Equal(150.0, snapped[0].YOffsetMm); // 300mm / 2 = 150mm offset
    }

    [Fact]
    public void SnapAll_VerticalBeamBetweenFootings_ExtendsIntoGridIntersections()
    {
        // Raw beam from CAD between ĐC-3-07 and ĐC-3-09:
        // Raw Y from 13500 to 14400 (900mm clear span).
        // Must extend to Grid F (Y=12750) and Grid H (Y=15150) inside the footings!
        var raw = new List<BeamRawSpan>
        {
            new("GM-18a", 300, 400, 16518.6, 13500.0, 16518.6, 14400.0)
        };

        var snapped = GradeBeamGridSnapper.SnapAll(raw, XGrids, YGrids);

        Assert.Single(snapped);
        var b = snapped[0];
        Assert.Equal(16500.0, b.StartPoint.X); // Snapped to Grid 6
        Assert.Equal(16500.0, b.EndPoint.X);
        Assert.Equal(12750.0, b.StartPoint.Y); // Extended into ĐC-3-07 to Grid F
        Assert.Equal(15150.0, b.EndPoint.Y); // Extended into ĐC-3-09 to Grid H
        Assert.Equal(2400.0, b.StartPoint.DistanceTo(b.EndPoint));
    }

    [Fact]
    public void SnapAll_StrayCantilever_IsFilteredOut()
    {
        var raw = new List<BeamRawSpan>
        {
            new("GM-Stray", 300, 400, -4200.0, 4650.0, -3800.0, 4650.0)
        };

        var snapped = GradeBeamGridSnapper.SnapAll(raw, XGrids, YGrids);

        Assert.Empty(snapped);
    }

    [Fact]
    public void SnapAll_WithExtractedCadSpans_SnapsAllValidBeams()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), ".agents", "teamwork", "worker_m3_foundation", "cad_extracted_beam_spans.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "..", ".agents", "teamwork", "worker_m3_foundation", "cad_extracted_beam_spans.json");
        }
        Assert.True(File.Exists(path), $"Path does not exist: {path}");

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
        var rawList = new List<BeamRawSpan>();
        foreach (var b in doc.RootElement.EnumerateArray())
        {
            string name = b.GetProperty("name").GetString() ?? "GM";
            double bMm = b.GetProperty("bMm").GetDouble();
            double hMm = b.GetProperty("hMm").GetDouble();
            double x1 = b.GetProperty("x1").GetDouble();
            double y1 = b.GetProperty("y1").GetDouble();
            double x2 = b.GetProperty("x2").GetDouble();
            double y2 = b.GetProperty("y2").GetDouble();
            rawList.Add(new BeamRawSpan(name, bMm, hMm, x1, y1, x2, y2));
        }

        var snapped = GradeBeamGridSnapper.SnapAll(rawList, XGrids, YGrids);
        Assert.Equal(188, snapped.Count);

        foreach (var b in snapped)
        {
            double dist = b.StartPoint.DistanceTo(b.EndPoint);
            Assert.True(dist >= 100.0, $"Beam {b.Name} collapsed to length {dist:F1}mm");
        }
    }
}
