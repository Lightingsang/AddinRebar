using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Geometry;
using HPAutoCad.Core.HPGeoLink.Import;
using HPAutoCad.Core.HPGeoLink.Kml;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Text;
using HPAutoCad.Core.HPGeoLink.Units;
using Xunit;

namespace HPAutoCad.Tests.HPGeoLink;

/// <summary>
/// Pins the findings of the first code review: arcs flattened to a tolerance instead of a fixed count,
/// row numbers in pasted lists, KML tuples with spaces, the script-argument contract, wildcards, file names.
/// </summary>
public sealed class ReviewRegressionTests
{
    [Theory]
    [InlineData(5, 90)]
    [InlineData(50, 90)]
    [InlineData(200, 180)]
    [InlineData(500, 90)]
    [InlineData(300, 270)]
    public void Arc_chords_stay_within_5_mm_of_the_arc(double radius, double angleDeg)
    {
        var theta = angleDeg * Math.PI / 180;
        var bulge = Math.Tan(theta / 4);
        // Arc from (r,0) counter-clockwise by theta around the origin: end point on the circle.
        var from = new BulgeTessellator.Vertex(radius, 0, bulge);
        var to = new BulgeTessellator.Vertex(radius * Math.Cos(theta), radius * Math.Sin(theta), 0);
        var points = BulgeTessellator.Tessellate(new[] { from, to }, closed: false);
        Assert.True(points.Count >= BulgeTessellator.MinSegmentsPerArc + 1);

        // Every chord midpoint must lie within the tolerance of the circle.
        var worst = 0.0;
        for (var i = 1; i < points.Count; i++)
        {
            var mx = (points[i - 1].Easting + points[i].Easting) / 2;
            var my = (points[i - 1].Northing + points[i].Northing) / 2;
            worst = Math.Max(worst, Math.Abs(radius - Math.Sqrt(mx * mx + my * my)));
            // and every vertex is on the circle
            Assert.Equal(radius, Math.Sqrt(points[i].Easting * points[i].Easting + points[i].Northing * points[i].Northing), 6);
        }
        Assert.True(worst <= BulgeTessellator.DefaultToleranceM + 1e-9, $"r {radius} θ {angleDeg}°: sagitta {worst:F4} m with {points.Count - 1} chords");
    }

    [Fact]
    public void Negative_bulge_goes_clockwise_and_chord_count_is_clamped()
    {
        var cw = BulgeTessellator.Tessellate(new[] { new BulgeTessellator.Vertex(0, 0, -1), new BulgeTessellator.Vertex(2, 0, 0) }, closed: false);
        Assert.True(cw.Skip(1).Take(cw.Count - 2).All(p => p.Northing > 0), "a clockwise semicircle from (0,0) to (2,0) passes above the chord");
        Assert.Equal(BulgeTessellator.MaxSegmentsPerArc, BulgeTessellator.ChordCount(100000, 2 * Math.PI, 0.0001));
        Assert.Equal(BulgeTessellator.MinSegmentsPerArc, BulgeTessellator.ChordCount(0.01, Math.PI / 2, 0.005));
        Assert.Equal(BulgeTessellator.MinSegmentsPerArc, BulgeTessellator.ChordCount(1, 0, 0.005));
    }

    [Theory]
    [InlineData("1;10.77;106.70", "1", 10.77, 106.70)]
    [InlineData("3\t11.1355893\t106.6684375", "3", 11.1355893, 106.6684375)]
    [InlineData("12 11.1355893, 106.6684375", "12", 11.1355893, 106.6684375)]
    [InlineData("11.1355893 106.6684375", "1", 11.1355893, 106.6684375)]
    public void Leading_row_number_is_the_label_not_a_coordinate(string line, string label, double lat, double lon)
    {
        var p = Assert.Single(CoordinateTextParser.Parse(line, PastedCoordinateKind.Wgs84));
        Assert.Equal(label, p.Label);
        Assert.Equal(lat, p.Geo!.Value.LatDeg, 9);
        Assert.Equal(lon, p.Geo!.Value.LonDeg, 9);
        Assert.False(p.Ambiguous);
    }

    [Fact]
    public void Leading_row_number_works_for_vn2000_lists_too()
    {
        var parsed = CoordinateTextParser.Parse("1\t600125.887\t1231608.428\n2\t600124.894\t1231587.765", PastedCoordinateKind.Vn2000);
        Assert.Equal(2, parsed.Count);
        Assert.Equal("2", parsed[1].Label);
        Assert.Equal(600124.894, parsed[1].Grid!.Value.Easting, 6);
        Assert.Equal(1231587.765, parsed[1].Grid!.Value.Northing, 6);
    }

    [Fact]
    public void Kml_tuples_with_spaces_after_commas_are_read()
    {
        const string kml = """
            <kml xmlns="http://www.opengis.net/kml/2.2"><Placemark><name>A</name><Point><coordinates>106.6684375, 11.1355893, 0</coordinates></Point></Placemark>
            <Placemark><name>L</name><LineString><coordinates>106.6684, 11.1355, 0 106.6690, 11.1340, 0</coordinates></LineString></Placemark></kml>
            """;
        var features = KmlReader.Read(kml);
        Assert.Equal(2, features.Count);
        Assert.Equal(11.1355893, features[0].Vertices[0].LatDeg, 9);
        Assert.Equal(2, features[1].Vertices.Count);
    }

    [Fact]
    public void Malformed_kml_is_an_error_not_a_crash()
    {
        Assert.ThrowsAny<Exception>(() => KmlReader.Read("<kml><Placemark><Point><coordinates>1,2"));
        Assert.Empty(KmlReader.Read("<kml><Placemark><name>x</name><Point><coordinates>abc</coordinates></Point></Placemark></kml>"));
        Assert.Empty(KmlReader.Read("<kml/>"));
    }

    [Fact]
    public void Script_arguments_follow_the_documented_contract()
    {
        var a = ExportArguments.Parse(@"cm=105-45 type=points out=""C:\a b\site"" unit=mm layer=RANH*,DIEM-?? name=Lo1 pcolor=FF00FFFF");
        Assert.Equal(105.75, a.CentralMeridianDeg);
        Assert.Equal(KmlOutput.Points, a.Output);
        Assert.Equal(@"C:\a b\site.kmz", a.OutputPath);
        Assert.Equal(DrawingUnit.Millimeters, a.UnitOverride);
        Assert.Equal("RANH*,DIEM-??", a.LayerFilter);
        Assert.Equal("Lo1", a.DocumentName);
        Assert.Equal("ff00ffff", a.PointColor);
        Assert.Equal(TmParametersDefaults.K0, a.ScaleFactor);

        Assert.Contains("cm=", Assert.Throws<ArgumentException>(() => ExportArguments.Parse("out=x.kmz")).Message);
        Assert.Contains("out=", Assert.Throws<ArgumentException>(() => ExportArguments.Parse("cm=105.75")).Message);
        Assert.Contains("'foo'", Assert.Throws<ArgumentException>(() => ExportArguments.Parse("cm=105.75 out=x foo=1")).Message);
        Assert.Contains("key=value", Assert.Throws<ArgumentException>(() => ExportArguments.Parse("cm=105.75 out=x junk")).Message);
        Assert.Contains("k0=", Assert.Throws<ArgumentException>(() => ExportArguments.Parse("cm=105.75 out=x k0=abc")).Message);
        Assert.Contains("Đơn vị", Assert.Throws<ArgumentException>(() => ExportArguments.Parse("cm=105.75 out=x unit=yd")).Message);
        Assert.Contains("aabbggrr", Assert.Throws<ArgumentException>(() => ExportArguments.Parse("cm=105.75 out=x lcolor=red")).Message);
    }

    private static class TmParametersDefaults
    {
        public const double K0 = HPAutoCad.Core.HPGeoLink.Projection.TmParameters.Tm3ScaleFactor;
    }

    [Theory]
    [InlineData("RANH*", "RANH-01", true)]
    [InlineData("RANH*", "ranh", true)]
    [InlineData("RANH*", "DIEM", false)]
    [InlineData("DIEM-??", "DIEM-01", true)]
    [InlineData("DIEM-??", "DIEM-001", false)]
    [InlineData("A-###", "A-123", true)]
    [InlineData("A-###", "A-12X", false)]
    [InlineData("RANH*,DIEM*", "DIEM-1", true)]
    [InlineData("~RANH*", "RANH-1", false)]
    [InlineData("~RANH*", "DIEM", true)]
    public void Layer_wildcards_follow_autocad_rules(string pattern, string layer, bool expected) =>
        Assert.Equal(expected, new WildcardPattern(pattern).IsMatch(layer));

    [Theory]
    [InlineData("CON", "CON_")]
    [InlineData("con.kmz", "con_.kmz")]
    [InlineData("site.", "site")]
    [InlineData("a\u0001b", "a_b")]
    [InlineData("Lô 12/3", "Lô 12_3")]
    public void File_names_avoid_device_names_and_control_characters(string input, string expected) =>
        Assert.Equal(expected, KmzWriter.SafeFileName(input));

    [Fact]
    public void Control_characters_in_labels_never_reach_the_kml()
    {
        var points = new[] { new SurveyPoint(1, "M\u0001\u001f1", new PlanePoint(600125.887, 1231608.428)) };
        var result = new Vn2000Converter().Convert(points, Array.Empty<BoundaryPolyline>(), new ConversionOptions(HPAutoCad.Core.HPGeoLink.Projection.TmParameters.Tm3(105.75), 1.0));
        var kml = KmlDocumentBuilder.Build(result, new KmlExportOptions("D\u0007oc")).Kml;
        System.Xml.Linq.XDocument.Parse(kml); // must be well-formed
        Assert.Contains("<name>POINT M1</name>", kml);
        Assert.Contains("<name>Doc</name>", kml);
    }
}
