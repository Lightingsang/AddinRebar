namespace HPBim.Core.Tests;

using HPBim.Core.Geometry;
using Xunit;

public class TriangularFootingGeneratorTests
{
    [Fact]
    public void Generate_UpwardPointingTriangle_GeneratesValid6VertexPolygon()
    {
        // 3 piles of ĐC-3-01: (6400, -400), (7300, -400), (6850, 400)
        var p1 = new Point2D(6400, -400);
        var p2 = new Point2D(7300, -400);
        var p3 = new Point2D(6850, 400);

        var poly = TriangularFootingGenerator.Generate(p1, p2, p3, 350.0);

        Assert.Equal(6, poly.VertexCount);
        Assert.True(poly.Contains(p1));
        Assert.True(poly.Contains(p2));
        Assert.True(poly.Contains(p3));
        Assert.True(poly.IsCounterClockwise);

        // Check canonical area: 2.0 m^2 = 2,000,000 mm^2 (tolerance 1e-3)
        Assert.InRange(poly.Area, 1999999.0, 2000001.0);

        // Check pile clearance to boundary >= 350mm (minus 1mm numerical tolerance)
        Assert.True(poly.DistanceToBoundary(p1) >= 349.0);
        Assert.True(poly.DistanceToBoundary(p2) >= 349.0);
        Assert.True(poly.DistanceToBoundary(p3) >= 349.0);
    }

    [Fact]
    public void Generate_LeftwardPointingTriangle_GeneratesValid6VertexPolygon()
    {
        // 3 piles of ĐC-3-06: (45400, 9500), (45400, 10400), (44600, 9950)
        var p1 = new Point2D(45400, 9500);
        var p2 = new Point2D(45400, 10400);
        var p3 = new Point2D(44600, 9950);

        var poly = TriangularFootingGenerator.Generate(p1, p2, p3, 350.0);

        Assert.Equal(6, poly.VertexCount);
        Assert.True(poly.Contains(p1));
        Assert.True(poly.Contains(p2));
        Assert.True(poly.Contains(p3));

        Assert.True(poly.DistanceToBoundary(p1) >= 349.0);
        Assert.True(poly.DistanceToBoundary(p2) >= 349.0);
        Assert.True(poly.DistanceToBoundary(p3) >= 349.0);
    }

    [Fact]
    public void Generate_DownwardPointingTriangle_GeneratesValid6VertexPolygon()
    {
        // 3 piles of ĐC-3-11: (28650, 23450), (29550, 23450), (29100, 22650)
        var p1 = new Point2D(28650, 23450);
        var p2 = new Point2D(29550, 23450);
        var p3 = new Point2D(29100, 22650);

        var poly = TriangularFootingGenerator.Generate(p1, p2, p3, 350.0);

        Assert.Equal(6, poly.VertexCount);
        Assert.True(poly.Contains(p1));
        Assert.True(poly.Contains(p2));
        Assert.True(poly.Contains(p3));

        Assert.True(poly.DistanceToBoundary(p1) >= 349.0);
        Assert.True(poly.DistanceToBoundary(p2) >= 349.0);
        Assert.True(poly.DistanceToBoundary(p3) >= 349.0);
    }

    [Fact]
    public void Generate_All17ThreePileFootings_FromMappingJson_ProduceValidPolygons()
    {
        string? current = AppDomain.CurrentDomain.BaseDirectory;
        string? path = null;
        for (int i = 0; i < 8 && current != null; i++)
        {
            string candidate = System.IO.Path.Combine(current, ".agents", "teamwork", "worker_m3_foundation", "exact_196_pile_mapping.json");
            if (System.IO.File.Exists(candidate))
            {
                path = candidate;
                break;
            }
            current = System.IO.Path.GetDirectoryName(current);
        }
        if (path == null) return;

        using var doc = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(path));
        var pilesByFooting = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Point2D>>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in doc.RootElement.EnumerateArray())
        {
            string fId = p.GetProperty("footing_id").GetString() ?? "";
            string normId = fId.Replace("DC-", "ĐC-");
            double px = p.GetProperty("x").GetDouble();
            double py = p.GetProperty("y").GetDouble();
            if (!pilesByFooting.TryGetValue(normId, out var list))
            {
                list = new System.Collections.Generic.List<Point2D>();
                pilesByFooting[normId] = list;
            }
            pListAdd(list, px, py);
        }

        int count3Pile = 0;
        foreach (var kvp in pilesByFooting)
        {
            if (kvp.Value.Count == 3)
            {
                count3Pile++;
                var p = kvp.Value;
                var poly = TriangularFootingGenerator.Generate(p[0], p[1], p[2], 350.0);
                Assert.Equal(6, poly.VertexCount);
                Assert.True(poly.Contains(p[0]));
                Assert.True(poly.Contains(p[1]));
                Assert.True(poly.Contains(p[2]));
                Assert.True(poly.DistanceToBoundary(p[0]) >= 349.0);
                Assert.True(poly.DistanceToBoundary(p[1]) >= 349.0);
                Assert.True(poly.DistanceToBoundary(p[2]) >= 349.0);
            }
        }

        Assert.Equal(17, count3Pile);
    }

    private static void pListAdd(System.Collections.Generic.List<Point2D> list, double x, double y)
    {
        list.Add(new Point2D(x, y));
    }
}
