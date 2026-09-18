using System.Globalization;
using System.Xml.Linq;
using HPGeo.Core.Conversion;
using HPGeo.Core.Geometry;
using HPGeo.Core.Kml;
using HPGeo.Core.Model;
using HPGeo.Core.Projection;
using Xunit;

namespace HPGeo.Tests;

/// <summary>The KML document is the reference tool's shape and survives a Vietnamese locale and a KMZ round trip.</summary>
public sealed class KmlTests
{
    private static readonly XNamespace Kml = "http://www.opengis.net/kml/2.2";
    private static readonly Vn2000Converter Converter = new();
    private static readonly TmParameters Hcm = TmParameters.Tm3(105.75);

    private static ConversionResult ThirteenPoints(bool withRing = false)
    {
        var raw = new (double E, double N)[]
        {
            (600125.887, 1231608.428), (600124.894, 1231587.765), (600130.542, 1231543.79), (600138.537, 1231528.803),
            (600154.577, 1231483.188), (600157.772, 1231465.515), (600167.147, 1231439.669), (600185.982, 1231422.961),
            (600172.355, 1231418.421), (600131.909, 1231412.989), (600135.362, 1231392), (600138.509, 1231379.42),
            (600102.308, 1231385.196),
        };
        var points = raw.Select((p, i) => new SurveyPoint(i + 1, (i + 1).ToString(), new PlanePoint(p.E, p.N))).ToList();
        var rings = withRing
            ? new[] { new BoundaryPolyline("Ranh", raw.Select(p => new PlanePoint(p.E, p.N)).ToList(), Closed: true) }
            : Array.Empty<BoundaryPolyline>();
        return Converter.Convert(points, rings, new ConversionOptions(Hcm, 1.0));
    }

    [Fact]
    public void Thirteen_points_give_thirteen_markers_thirteen_labels_and_a_boundary_from_the_points()
    {
        var built = KmlDocumentBuilder.Build(ThirteenPoints(), new KmlExportOptions("Test"));
        Assert.Equal(13, built.MarkerCount);
        Assert.Equal(1, built.BoundaryCount);
        Assert.True(built.BoundaryFromPoints);

        var doc = XDocument.Parse(built.Kml);
        var placemarks = doc.Descendants(Kml + "Placemark").ToList();
        Assert.Equal(13 * 2 + 1, placemarks.Count);
        Assert.Equal(13, placemarks.Count(p => (string?)p.Element(Kml + "styleUrl") == "#ptVectorStyle"));
        Assert.Equal(13, placemarks.Count(p => (string?)p.Element(Kml + "styleUrl") == "#ptLabelStyle"));
        Assert.Equal("POINT 1", (string?)placemarks[0].Element(Kml + "name"));
        Assert.Contains("VN2000: X(N)=1231608.428, Y(E)=600125.887", (string?)placemarks[0].Element(Kml + "description"));

        var ring = doc.Descendants(Kml + "LinearRing").Single().Element(Kml + "coordinates")!.Value
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(14, ring.Length);
        Assert.Equal(ring[0], ring[^1]);
        Assert.Equal(2, ring[0].Count(c => c == ','));
        Assert.EndsWith(",0", ring[0]);
    }

    [Fact]
    public void Styles_and_colours_follow_the_reference_tool()
    {
        var kml = KmlDocumentBuilder.Build(ThirteenPoints(), new KmlExportOptions("T") { PointColor = "FF00FFFF", LineColor = "ff0000ff" }).Kml;
        Assert.Contains("<Style id=\"ptVectorStyle\">", kml);
        Assert.Contains("<color>ff00ffff</color><width>3.0</width>", kml);
        Assert.Contains("<LabelStyle><color>ff00ffff</color><scale>1.35</scale></LabelStyle>", kml);
        Assert.Contains("<color>ff0000ff</color><width>1.6</width>", kml);
        Assert.Contains("<PolyStyle><color>180000ff</color></PolyStyle>", kml);
    }

    [Fact]
    public void Invalid_colour_is_refused()
    {
        var ex = Assert.Throws<ArgumentException>(() => KmlDocumentBuilder.Build(ThirteenPoints(), new KmlExportOptions("T") { PointColor = "yellow" }));
        Assert.Contains("aabbggrr", ex.Message);
        Assert.False(KmlColor.IsValid("ff00ff"));
        Assert.True(KmlColor.IsValid("18ff00ff"));
    }

    [Fact]
    public void Output_points_only_has_no_polygon_and_boundaries_only_has_no_marker()
    {
        var points = KmlDocumentBuilder.Build(ThirteenPoints(true), new KmlExportOptions("T") { Output = KmlOutput.Points });
        Assert.Equal(0, points.BoundaryCount);
        Assert.DoesNotContain("<Polygon>", points.Kml);

        var bounds = KmlDocumentBuilder.Build(ThirteenPoints(true), new KmlExportOptions("T") { Output = KmlOutput.Boundaries });
        Assert.Equal(0, bounds.MarkerCount);
        Assert.Equal(1, bounds.BoundaryCount);
        Assert.False(bounds.BoundaryFromPoints);
        Assert.Contains("<name>Ranh</name>", bounds.Kml);
    }

    [Fact]
    public void Open_polyline_becomes_a_LineString()
    {
        var line = new BoundaryPolyline("Tim", new[] { new PlanePoint(600000, 1231000), new PlanePoint(600100, 1231050), new PlanePoint(600200, 1231000) }, Closed: false);
        var r = Converter.Convert(Array.Empty<SurveyPoint>(), new[] { line }, new ConversionOptions(Hcm, 1.0));
        var kml = KmlDocumentBuilder.Build(r, new KmlExportOptions("T")).Kml;
        var doc = XDocument.Parse(kml);
        Assert.Empty(doc.Descendants(Kml + "Polygon"));
        var coords = doc.Descendants(Kml + "LineString").Single().Element(Kml + "coordinates")!.Value
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(3, coords.Length);
    }

    [Fact]
    public void Coordinates_use_a_dot_under_a_Vietnamese_locale()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("vi-VN");
            var kml = KmlDocumentBuilder.Build(ThirteenPoints(), new KmlExportOptions("T")).Kml;
            var doc = XDocument.Parse(kml);
            foreach (var c in doc.Descendants(Kml + "coordinates"))
            {
                foreach (var triple in c.Value.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var parts = triple.Split(',');
                    Assert.Equal(3, parts.Length);
                    Assert.True(double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _), triple);
                    Assert.True(double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out _), triple);
                }
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Kmz_round_trips_the_document()
    {
        var kml = KmlDocumentBuilder.Build(ThirteenPoints(), new KmlExportOptions("Round trip")).Kml;
        var path = Path.Combine(Path.GetTempPath(), $"hpgeo-{Guid.NewGuid():N}.kmz");
        try
        {
            KmzWriter.WriteFile(kml, path);
            Assert.Equal(kml, KmzWriter.ReadKml(path));
            using var zip = System.IO.Compression.ZipFile.OpenRead(path);
            Assert.Equal(KmzWriter.KmlEntryName, zip.Entries[0].FullName);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Names_are_xml_escaped()
    {
        var kml = KmlDocumentBuilder.Build(ThirteenPoints(), new KmlExportOptions("A & B <C>")).Kml;
        Assert.Contains("<name>A &amp; B &lt;C&gt;</name>", kml);
        Assert.Equal("A & B <C>", XDocument.Parse(kml).Root!.Element(Kml + "Document")!.Element(Kml + "name")!.Value);
    }

    [Theory]
    [InlineData("VN2000_Points", "VN2000_Points")]
    [InlineData("  ", "VN2000_Points")]
    [InlineData("a:b/c", "a_b_c")]
    public void File_names_are_sanitised(string input, string expected) => Assert.Equal(expected, KmzWriter.SafeFileName(input));

    [Fact]
    public void Bulge_semicircle_passes_below_the_chord_and_zero_bulge_stays_straight()
    {
        var arc = BulgeTessellator.Tessellate(new[] { new BulgeTessellator.Vertex(0, 0, 1), new BulgeTessellator.Vertex(2, 0, 0) }, closed: false);
        var segments = arc.Count - 1;
        Assert.Equal(0, segments % 2);
        var mid = arc[segments / 2];
        Assert.Equal(1.0, mid.Easting, 9);
        Assert.Equal(-1.0, mid.Northing, 9);
        Assert.Equal(new PlanePoint(2, 0), arc[^1]);

        var straight = BulgeTessellator.Tessellate(new[] { new BulgeTessellator.Vertex(0, 0, 0), new BulgeTessellator.Vertex(2, 0, 0) }, closed: false);
        Assert.Equal(2, straight.Count);

        var ring = BulgeTessellator.Tessellate(new[] { new BulgeTessellator.Vertex(0, 0, 0), new BulgeTessellator.Vertex(1, 0, 0), new BulgeTessellator.Vertex(1, 1, 0) }, closed: true);
        Assert.Equal(3, ring.Count);
    }
}
