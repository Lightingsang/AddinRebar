using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Import;
using HPAutoCad.Core.HPGeoLink.Kml;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Projection;
using Xunit;

namespace HPAutoCad.Tests.HPGeoLink;

/// <summary>The reverse direction: pasted text, KML/KMZ files, and the export → import round trip.</summary>
public sealed class ImportTests
{
    private static readonly TmParameters Hcm = TmParameters.Tm3(105.75);
    private static readonly ConversionOptions Metres = new(Hcm, 1.0);
    private static readonly ImportPlanner Planner = new();

    private static readonly (double E, double N)[] Raw =
    {
        (600125.887, 1231608.428), (600124.894, 1231587.765), (600130.542, 1231543.79), (600138.537, 1231528.803),
        (600154.577, 1231483.188), (600157.772, 1231465.515), (600167.147, 1231439.669), (600185.982, 1231422.961),
        (600172.355, 1231418.421), (600131.909, 1231412.989), (600135.362, 1231392), (600138.509, 1231379.42),
        (600102.308, 1231385.196),
    };

    private static ConversionResult Exported()
    {
        var points = Raw.Select((p, i) => new SurveyPoint(i + 1, (i + 1).ToString(), new PlanePoint(p.E, p.N))).ToList();
        var ring = new BoundaryPolyline("Ranh", Raw.Select(p => new PlanePoint(p.E, p.N)).ToList(), Closed: true);
        return new Vn2000Converter().Convert(points, new[] { ring }, Metres);
    }

    [Fact]
    public void Exported_kmz_imports_back_within_a_millimetre()
    {
        var kml = KmlDocumentBuilder.Build(Exported(), new KmlExportOptions("RT")).Kml;
        var path = Path.Combine(Path.GetTempPath(), $"hpgeo-rt-{Guid.NewGuid():N}.kmz");
        try
        {
            KmzWriter.WriteFile(kml, path);
            var features = KmlReader.ReadFile(path);
            Assert.Equal(13, features.Count(f => f.Kind == KmlFeatureKind.Point));
            Assert.Equal(1, features.Count(f => f.Kind == KmlFeatureKind.Polygon));

            var plan = Planner.FromKml(features, Metres);
            Assert.True(plan.Success, string.Join(" | ", plan.Issues.Select(i => i.Message)));
            Assert.Equal(13, plan.Points.Count);
            Assert.Equal("1", plan.Points[0].Label);
            var worst = 0.0;
            for (var i = 0; i < 13; i++)
            {
                var d = Math.Sqrt(Math.Pow(plan.Points[i].DrawingXY.Easting - Raw[i].E, 2) + Math.Pow(plan.Points[i].DrawingXY.Northing - Raw[i].N, 2));
                worst = Math.Max(worst, d);
            }
            var ring = Assert.Single(plan.Polylines);
            Assert.True(ring.Closed);
            Assert.Equal(13, ring.DrawingVertices.Count);
            for (var i = 0; i < 13; i++)
            {
                var d = Math.Sqrt(Math.Pow(ring.DrawingVertices[i].Easting - Raw[i].E, 2) + Math.Pow(ring.DrawingVertices[i].Northing - Raw[i].N, 2));
                worst = Math.Max(worst, d);
            }
            Assert.True(worst <= 0.001, $"worst {worst:E2} m");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(BoundaryMarkerStyle.Triangle)]
    [InlineData(BoundaryMarkerStyle.Pushpin)]
    [InlineData(BoundaryMarkerStyle.Circle)]
    [InlineData(BoundaryMarkerStyle.LabelOnly)]
    public void Kmz_with_boundary_vertices_imports_back_without_duplicate_points(BoundaryMarkerStyle style)
    {
        var options = new KmlExportOptions("RT_With_Vertices")
        {
            ExportBoundaryVertices = true,
            MarkerStyle = style,
            PopupTemplate = BoundaryPopupTemplate.Cadastral,
        };
        var kml = KmlDocumentBuilder.Build(Exported(), options).Kml;
        var path = Path.Combine(Path.GetTempPath(), $"hpgeo-rt-vert-{Guid.NewGuid():N}.kmz");
        try
        {
            KmzWriter.WriteFile(kml, path);
            var features = KmlReader.ReadFile(path);
            // Must have only the 13 survey points, NOT polluted by boundary vertices
            Assert.Equal(13, features.Count(f => f.Kind == KmlFeatureKind.Point));
            Assert.Equal(1, features.Count(f => f.Kind == KmlFeatureKind.Polygon));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Google_earth_style_kml_reads_point_line_and_polygon()
    {
        const string kml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <kml xmlns="http://www.opengis.net/kml/2.2"><Document><Folder>
              <Placemark><name>Mốc A</name><Point><coordinates>106.6684375,11.1355893,0</coordinates></Point></Placemark>
              <Placemark><name>Đường</name><LineString><coordinates>
                106.6684,11.1355,0 106.6690,11.1340,0
              </coordinates></LineString></Placemark>
              <Placemark><name>Thửa</name><Polygon><outerBoundaryIs><LinearRing><coordinates>
                106.668,11.135,0 106.669,11.135,0 106.669,11.134,0 106.668,11.135,0
              </coordinates></LinearRing></outerBoundaryIs></Polygon></Placemark>
            </Folder></Document></kml>
            """;
        var features = KmlReader.Read(kml);
        Assert.Equal(3, features.Count);
        Assert.Equal(KmlFeatureKind.Point, features[0].Kind);
        Assert.Equal("Mốc A", features[0].Name);
        Assert.Equal(11.1355893, features[0].Vertices[0].LatDeg, 9);
        Assert.Equal(KmlFeatureKind.Line, features[1].Kind);
        Assert.Equal(2, features[1].Vertices.Count);
        Assert.Equal(KmlFeatureKind.Polygon, features[2].Kind);
        Assert.Equal(3, features[2].Vertices.Count); // closing vertex dropped

        var plan = Planner.FromKml(features, new ConversionOptions(Hcm, 0.001)); // millimetre drawing
        Assert.True(plan.Success);
        Assert.InRange(plan.Points[0].DrawingXY.Easting, 600125.887 * 1000 - 5, 600125.887 * 1000 + 5);
        Assert.Equal(2, plan.Polylines.Count);
        Assert.False(plan.Polylines[0].Closed);
        Assert.True(plan.Polylines[1].Closed);
    }

    [Fact]
    public void Kml_outside_vietnam_or_wrong_zone_is_refused()
    {
        var paris = new[] { new KmlFeature(KmlFeatureKind.Point, "Paris", new[] { new GeoPoint(48.85, 2.35) }) };
        var plan = Planner.FromKml(paris, Metres);
        Assert.False(plan.Success);
        Assert.Contains(plan.Errors, e => e.Code == "OUTSIDE_VIETNAM");

        // Hà Nội on the TP.HCM zone: 105°45' vs 105°00' is fine, but 108°30' puts it 300 km west of the zone.
        var hanoi = new[] { new KmlFeature(KmlFeatureKind.Point, "HN", new[] { new GeoPoint(21.0285, 105.8542) }) };
        var wrongZone = Planner.FromKml(hanoi, new ConversionOptions(TmParameters.Tm3(103.0), 1.0));
        Assert.True(wrongZone.Success); // still inside the plausible band
        var farZone = Planner.FromKml(hanoi, new ConversionOptions(new TmParameters(120, 0.9999, 500000, 0), 1.0));
        Assert.False(farZone.Success);
        Assert.Contains(farZone.Errors, e => e.Code == "OUTSIDE_ZONE");
    }

    [Fact]
    public void Missing_meridian_and_empty_kml_are_errors()
    {
        Assert.Equal("NO_CENTRAL_MERIDIAN", Assert.Single(Planner.FromKml(Array.Empty<KmlFeature>(), new ConversionOptions(TmParameters.Tm3(double.NaN), 1.0)).Issues).Code);
        Assert.Equal("NO_INPUT", Assert.Single(Planner.FromKml(Array.Empty<KmlFeature>(), Metres).Issues).Code);
    }

    [Theory]
    [InlineData("POINT 600125.887,1231608.428", 600125.887, 1231608.428, false, "POINT")]
    [InlineData("600125.887 1231608.428", 600125.887, 1231608.428, false, "Easting")]
    [InlineData("1231608.428;600125.887", 600125.887, 1231608.428, false, "Northing")]
    [InlineData("M1: 600125.887, 1231608.428", 600125.887, 1231608.428, false, "Easting")]
    public void Pasted_vn2000_pairs_follow_the_reference_classifier(string line, double e, double n, bool ambiguous, string modeContains)
    {
        var parsed = Assert.Single(CoordinateTextParser.Parse(line, PastedCoordinateKind.Vn2000));
        Assert.Equal(e, parsed.Grid!.Value.Easting, 6);
        Assert.Equal(n, parsed.Grid!.Value.Northing, 6);
        Assert.Equal(ambiguous, parsed.Ambiguous);
        Assert.Contains(modeContains, parsed.Mode);
    }

    [Fact]
    public void Pasted_labels_and_orders_are_honoured()
    {
        var parsed = CoordinateTextParser.Parse("M1: 600125.887, 1231608.428\r\n\r\nM2 600124.894 1231587.765\n# comment\n", PastedCoordinateKind.Vn2000);
        Assert.Equal(2, parsed.Count);
        Assert.Equal("M1", parsed[0].Label);
        Assert.Equal("M2", parsed[1].Label);
        Assert.Equal(1, parsed[0].LineNumber);
        Assert.Equal(3, parsed[1].LineNumber);

        var xy = Assert.Single(CoordinateTextParser.Parse("600125.887 1231608.428", PastedCoordinateKind.Vn2000, PairOrder.CadastralXY));
        Assert.Equal(1231608.428, xy.Grid!.Value.Easting, 6); // forced X,Y: first = Northing

        var ambiguous = Assert.Single(CoordinateTextParser.Parse("850000 850000", PastedCoordinateKind.Vn2000));
        Assert.True(ambiguous.Ambiguous);
        Assert.Equal(850000, ambiguous.Grid!.Value.Easting);
    }

    [Fact]
    public void Pasted_wgs84_pairs_detect_lat_lon_order()
    {
        var parsed = CoordinateTextParser.Parse("11.1355893, 106.6684375\n106.6684279 11.1354026\nP3 48.85 2.35", PastedCoordinateKind.Wgs84);
        Assert.Equal(3, parsed.Count);
        Assert.Equal(11.1355893, parsed[0].Geo!.Value.LatDeg, 9);
        Assert.Equal(11.1354026, parsed[1].Geo!.Value.LatDeg, 9); // swapped back
        Assert.Contains("đảo", parsed[1].Mode);
        Assert.True(parsed[2].Ambiguous);

        var plan = Planner.FromPasted(parsed.Take(2).ToList(), Metres, asPolyline: true, closed: false);
        Assert.True(plan.Success);
        Assert.Equal(2, plan.Points.Count);
        Assert.InRange(plan.Points[0].DrawingXY.Easting, 600125.887 - 0.01, 600125.887 + 0.01);
        Assert.Single(plan.Polylines);
        Assert.False(Planner.FromPasted(parsed, Metres, false, false).Success); // Paris is an error
    }

    [Fact]
    public void Pasted_vn2000_pairs_are_drawn_without_conversion_and_diagnosed()
    {
        var parsed = CoordinateTextParser.Parse("600125.887 1231608.428\n600125887 1231608428", PastedCoordinateKind.Vn2000, PairOrder.EastingNorthing);
        var plan = Planner.FromPasted(parsed, Metres, asPolyline: false, closed: false);
        Assert.False(plan.Success); // the mm-looking pair lands near the equator
        Assert.Contains(plan.Issues, i => i.Code == "IMPLAUSIBLE_EN");
        Assert.Contains(plan.Issues, i => i.Code == "OUTSIDE_VIETNAM");
        Assert.Single(plan.Points);
        Assert.Equal(600125.887, plan.Points[0].DrawingXY.Easting);

        var ring = Planner.FromPasted(parsed.Take(1).ToList(), Metres, asPolyline: true, closed: true);
        Assert.Contains(ring.Issues, i => i.Code == "RING_TOO_SHORT");
        Assert.Empty(ring.Polylines);
    }
}
