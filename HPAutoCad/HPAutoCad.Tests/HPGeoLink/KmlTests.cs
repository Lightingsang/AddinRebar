using System.Globalization;
using System.Xml.Linq;
using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Geometry;
using HPAutoCad.Core.HPGeoLink.Kml;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Projection;
using Xunit;

namespace HPAutoCad.Tests.HPGeoLink;

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
        Assert.Contains("<color>ff0000ff</color><width>3.5</width>", kml);
        Assert.Contains("<PolyStyle><color>180000ff</color><fill>1</fill><outline>1</outline></PolyStyle>", kml);
        Assert.Contains("<LookAt>", kml);
        Assert.Contains("<open>1</open>", kml);
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
    public void Presets_24_colours_are_correct_and_convert_consistently()
    {
        Assert.Equal(24, KmlColor.Presets.Count);

        // Verify specific boundary and key colors
        var red = KmlColor.Presets[0];
        Assert.Equal(1, red.Stt);
        Assert.Equal("Đỏ", red.Name);
        Assert.Equal("#FF0000", red.HexRgb);
        Assert.Equal("ff0000ff", red.KmlHex);

        var yellow = KmlColor.Presets[3];
        Assert.Equal(4, yellow.Stt);
        Assert.Equal("Vàng", yellow.Name);
        Assert.Equal("#FFFF00", yellow.HexRgb);
        Assert.Equal("ff00ffff", yellow.KmlHex);

        var pink = KmlColor.Presets[23];
        Assert.Equal(24, pink.Stt);
        Assert.Equal("Hồng", pink.Name);
        Assert.Equal("#FF69B4", pink.HexRgb);
        Assert.Equal("ffb469ff", pink.KmlHex);

        // Every preset round-trips consistently
        foreach (var p in KmlColor.Presets)
        {
            Assert.Equal(p.KmlHex, KmlColor.Normalize(p.HexRgb, p.Name));
            Assert.Equal(p.HexRgb, KmlColor.KmlToRgbHex(p.KmlHex));
        }
    }

    [Fact]
    public void Hex_input_is_supported_and_normalizes_to_kml()
    {
        Assert.True(KmlColor.IsValid("#FF0000"));
        Assert.Equal("ff0000ff", KmlColor.Normalize("#FF0000", "điểm"));
        Assert.Equal("#FF0000", KmlColor.KmlToRgbHex("#FF0000"));

        Assert.True(KmlColor.IsValid("#00FF00"));
        Assert.Equal("ff00ff00", KmlColor.Normalize("#00FF00", "ranh"));

        Assert.Equal("180000ff", KmlColor.PolygonFillFrom("#FF0000"));
    }


    [Fact]
    public void Output_points_only_has_no_polygon_and_boundaries_only_has_no_marker()
    {
        var points = KmlDocumentBuilder.Build(ThirteenPoints(true), new KmlExportOptions("T") { Output = KmlOutput.Points });
        Assert.Equal(0, points.BoundaryCount);
        Assert.DoesNotContain("<Polygon>", points.Kml);
        Assert.Equal(13, points.VertexCount);
        Assert.Contains("<name>Mốc đỉnh ranh</name>", points.Kml);

        var bounds = KmlDocumentBuilder.Build(ThirteenPoints(true), new KmlExportOptions("T") { Output = KmlOutput.Boundaries });
        Assert.Equal(0, bounds.MarkerCount);
        Assert.Equal(0, bounds.VertexCount);
        Assert.DoesNotContain("<name>Mốc đỉnh ranh</name>", bounds.Kml);
        Assert.Equal(1, bounds.BoundaryCount);
        Assert.False(bounds.BoundaryFromPoints);
        Assert.Contains("<name>Ranh</name>", bounds.Kml);
    }

    [Fact]
    public void Boundary_only_selection_with_output_points_exports_boundary_vertices()
    {
        var raw = new (double E, double N)[] { (600100, 1231000), (600200, 1231000), (600150, 1231100) };
        var rings = new[] { new BoundaryPolyline("Ranh", raw.Select(p => new PlanePoint(p.E, p.N)).ToList(), Closed: true) };
        var r = Converter.Convert(Array.Empty<SurveyPoint>(), rings, new ConversionOptions(Hcm, 1.0));
        var built = KmlDocumentBuilder.Build(r, new KmlExportOptions("T") { Output = KmlOutput.Points });
        Assert.Equal(0, built.MarkerCount);
        Assert.Equal(0, built.BoundaryCount);
        Assert.Equal(3, built.VertexCount);
        Assert.Contains("<name>Mốc đỉnh ranh</name>", built.Kml);
        Assert.Contains("P1", built.Kml);
        Assert.Contains("P2", built.Kml);
        Assert.Contains("P3", built.Kml);
        Assert.DoesNotContain("<Polygon>", built.Kml);
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

    [Fact]
    public void Boundary_vertices_are_exported_into_separate_folder_when_enabled()
    {
        var options = new KmlExportOptions("TestBoundaryVertices")
        {
            ExportBoundaryVertices = true,
            MarkerStyle = BoundaryMarkerStyle.Triangle,
            PopupTemplate = BoundaryPopupTemplate.Cadastral,
        };
        var built = KmlDocumentBuilder.Build(ThirteenPoints(withRing: true), options);
        Assert.Equal(13, built.VertexCount);

        var doc = XDocument.Parse(built.Kml);
        var folders = doc.Descendants(Kml + "Folder").ToList();
        Assert.Contains(folders, f => (string?)f.Element(Kml + "name") == "Đường ranh đất");
        Assert.Contains(folders, f => (string?)f.Element(Kml + "name") == "Mốc đỉnh ranh");

        var vertexFolder = folders.Single(f => (string?)f.Element(Kml + "name") == "Mốc đỉnh ranh");
        var vertexPlacemarks = vertexFolder.Elements(Kml + "Placemark").ToList();
        // Triangle has vector placemark + label placemark per vertex (13 * 2 = 26)
        Assert.Equal(26, vertexPlacemarks.Count);
        Assert.Equal("P1", (string?)vertexPlacemarks[0].Element(Kml + "name"));
        Assert.Contains("THÔNG TIN ĐỈNH RANH: P1", (string?)vertexPlacemarks[0].Element(Kml + "description"));
        Assert.Contains("P1 → P2:", (string?)vertexPlacemarks[0].Element(Kml + "description"));
    }

    [Theory]
    [InlineData(BoundaryMarkerStyle.Pushpin, "#bndPushpinStyle")]
    [InlineData(BoundaryMarkerStyle.Circle, "#bndCircleStyle")]
    [InlineData(BoundaryMarkerStyle.LabelOnly, "#bndLabelOnlyStyle")]
    public void Boundary_point_marker_styles_produce_single_placemark_per_vertex(BoundaryMarkerStyle style, string expectedStyleUrl)
    {
        var options = new KmlExportOptions("TestStyles")
        {
            ExportBoundaryVertices = true,
            MarkerStyle = style,
        };
        var built = KmlDocumentBuilder.Build(ThirteenPoints(withRing: true), options);
        var doc = XDocument.Parse(built.Kml);
        var vertexFolder = doc.Descendants(Kml + "Folder").Single(f => (string?)f.Element(Kml + "name") == "Mốc đỉnh ranh");
        var placemarks = vertexFolder.Elements(Kml + "Placemark").ToList();

        // Pushpin, Circle, LabelOnly have 1 placemark per vertex (13 total)
        Assert.Equal(13, placemarks.Count);
        Assert.All(placemarks, p => Assert.Equal(expectedStyleUrl, (string?)p.Element(Kml + "styleUrl")));
    }

    [Fact]
    public void Boundary_popup_templates_generate_expected_html()
    {
        // Technical DMS template
        var techOptions = new KmlExportOptions("Tech")
        {
            ExportBoundaryVertices = true,
            PopupTemplate = BoundaryPopupTemplate.Technical,
            MarkerStyle = BoundaryMarkerStyle.Pushpin,
        };
        var techKml = KmlDocumentBuilder.Build(ThirteenPoints(withRing: true), techOptions).Kml;
        Assert.Contains("MỐC RANH: P1", techKml);
        Assert.Contains("WGS84 Lat (DMS)", techKml);
        Assert.Contains("WGS84 Lon (DMS)", techKml);
        Assert.Contains("°", techKml);
        Assert.Contains("N", techKml);
        Assert.Contains("E", techKml);

        // Simple text template
        var simpleOptions = new KmlExportOptions("Simple")
        {
            ExportBoundaryVertices = true,
            PopupTemplate = BoundaryPopupTemplate.Simple,
            MarkerStyle = BoundaryMarkerStyle.Pushpin,
        };
        var simpleKml = KmlDocumentBuilder.Build(ThirteenPoints(withRing: true), simpleOptions).Kml;
        Assert.Contains("<b>Đỉnh ranh: P1</b>", simpleKml);
        Assert.Contains("<b>VN-2000:</b>", simpleKml);
        Assert.Contains("<b>WGS-84:</b>", simpleKml);
        Assert.Contains("<b>Cạnh:</b>", simpleKml);
    }

    [Theory]
    [InlineData(10.7922639, true, "10°47'32.15\" N")]
    [InlineData(-10.5, true, "10°30'00.00\" S")]
    [InlineData(106.6709556, false, "106°40'15.44\" E")]
    [InlineData(-120.0, false, "120°00'00.00\" W")]
    public void FormatDms_converts_decimal_degrees_correctly(double deg, bool isLat, string expected)
    {
        Assert.Equal(expected, KmlDocumentBuilder.FormatDms(deg, isLat));
    }
}
