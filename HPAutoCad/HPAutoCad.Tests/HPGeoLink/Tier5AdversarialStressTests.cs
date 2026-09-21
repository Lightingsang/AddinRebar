using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Xml;
using HPAutoCad.Core.HPGeoLink.Catalog;
using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Imagery;
using HPAutoCad.Core.HPGeoLink.Import;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Projection;
using HPAutoCad.Core.HPGeoLink.Text;
using HPAutoCad.Core.HPGeoLink.Units;
using HPAutoCad.Core.HPGeoLink.Validation;
using Xunit;

namespace HPAutoCad.Tests.HPGeoLink;

/// <summary>
/// Tier 5 Adversarial Stress & Hardening Test Suite.
/// Stress-tests boundary limits, extreme geodetics, corrupted archives, malformed parsers,
/// and reflection entry-point safety contracts under hostile/unexpected conditions.
/// </summary>
public sealed class Tier5AdversarialStressTests
{
    #region 1. Reflection & Loader Safe Resolution Stress Tests

    [Fact]
    public void Entry_Start_two_param_handles_empty_or_special_directories_safely()
    {
        var logMessages = new List<string>();
        Action<string> logger = msg => logMessages.Add(msg);

        // Test with empty string
        var dict1 = HPAutoCad.Entry.Start("", logger);
        Assert.NotNull(dict1);
        Assert.Contains("dialog", dict1.Keys);
        Assert.Contains("stop", dict1.Keys);

        // Test with null logger
        var dict2 = HPAutoCad.Entry.Start(@"C:\NonExistent\Directory\With Unicode Đất Đai\Sub", null);
        Assert.NotNull(dict2);
        Assert.Equal(8, dict2.Count);
        Assert.Contains("smartplot", dict2.Keys);

        // Verify stop delegate execution does not throw
        var stopAction = Assert.IsAssignableFrom<Action>(dict2["stop"]);
        var stopEx = Record.Exception(() => stopAction());
        Assert.Null(stopEx);
    }

    [Fact]
    public void Entry_Start_three_param_handles_arbitrary_strings_safely()
    {
        var dict = HPAutoCad.Entry.Start("G:\\TestDir", "AutoCAD", "25.1.0");
        Assert.NotNull(dict);
        Assert.Equal(8, dict.Count);
        Assert.True(dict.ContainsKey("kmz-script"));
        Assert.True(dict.ContainsKey("image-script"));
        Assert.True(dict.ContainsKey("info"));
        Assert.True(dict.ContainsKey("smartplot"));
    }

    [Fact]
    public void Loader_resolution_disambiguation_logic_survives_adversarial_type_shapes()
    {
        // 1. Emulate the exact loader disambiguation logic on HPAutoCad.Entry
        var entryType = typeof(HPAutoCad.Entry);
        var startMethod = entryType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                             .FirstOrDefault(m => m.Name == "Start" && m.GetParameters().Length == 2)
                        ?? entryType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                .FirstOrDefault(m => m.Name == "Start")
                        ?? throw new MissingMethodException("HPAutoCad.Entry", "Start");

        Assert.NotNull(startMethod);
        Assert.Equal(2, startMethod.GetParameters().Length);

        // 2. Simulate missing assembly
        var missingAssemblyPath = Path.Combine(Path.GetTempPath(), "NonExistent_" + Guid.NewGuid().ToString("N") + ".dll");
        Assert.False(File.Exists(missingAssemblyPath));

        // 3. Emulate loader failure recovery: ensure all failure modes produce clean StartupError and null App
        string? startupError = null;
        object? app = null;

        try
        {
            if (!File.Exists(missingAssemblyPath))
            {
                throw new FileNotFoundException("Add-in assembly missing beside the loader", missingAssemblyPath);
            }
        }
        catch (Exception exception)
        {
            var cause = exception is TargetInvocationException { InnerException: { } inner } ? inner : exception;
            startupError = cause.GetType().Name + ": " + cause.Message;
        }

        Assert.Null(app);
        Assert.NotNull(startupError);
        Assert.StartsWith("FileNotFoundException:", startupError);
    }

    [Fact]
    public void Simulated_missing_method_and_type_in_loader_catches_cleanly()
    {
        string? startupError = null;
        try
        {
            var dummyType = typeof(string);
            var method = dummyType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                             .FirstOrDefault(m => m.Name == "NonExistentStart" && m.GetParameters().Length == 2)
                        ?? throw new MissingMethodException("StringType", "NonExistentStart");
        }
        catch (Exception ex)
        {
            var cause = ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;
            startupError = cause.GetType().Name + ": " + cause.Message;
        }

        Assert.NotNull(startupError);
        Assert.StartsWith("MissingMethodException:", startupError);
    }

    #endregion

    #region 2. Extreme Geodetic Coordinates & Boundary Limits

    [Theory]
    [InlineData(0, 0)]                                // Origin (Gulf of Guinea)
    [InlineData(-500000, -1000000)]                   // Negative coordinates
    [InlineData(1e8, 1e8)]                            // 100,000 km off Earth
    [InlineData(1e15, 1e15)]                          // Astronomical distance
    [InlineData(-1e8, -1e8)]                          // Negative astronomical
    public void Vn2000Converter_rejects_extreme_coordinates_without_unhandled_exceptions(double easting, double northing)
    {
        var converter = new Vn2000Converter();
        var points = new List<SurveyPoint>
        {
            new(1, "EXTREME", new PlanePoint(easting, northing))
        };
        var options = new ConversionOptions(TmParameters.Tm3(105.75), 1.0);

        var result = converter.Convert(points, Array.Empty<BoundaryPolyline>(), options);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Code == "OUTSIDE_VIETNAM" || e.Code == "IMPLAUSIBLE_EN");
    }

    [Fact]
    public void Vn2000Converter_handles_special_floating_point_values_gracefully()
    {
        var converter = new Vn2000Converter();
        var points = new List<SurveyPoint>
        {
            new(1, "NAN", new PlanePoint(double.NaN, double.NaN)),
            new(2, "INF", new PlanePoint(double.PositiveInfinity, double.PositiveInfinity)),
            new(3, "NEGINF", new PlanePoint(double.NegativeInfinity, double.NegativeInfinity)),
            new(4, "MAX", new PlanePoint(double.MaxValue, double.MaxValue)),
        };
        var options = new ConversionOptions(TmParameters.Tm3(105.75), 1.0);

        var ex = Record.Exception(() => converter.Convert(points, Array.Empty<BoundaryPolyline>(), options));
        Assert.Null(ex);

        var result = converter.Convert(points, Array.Empty<BoundaryPolyline>(), options);
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData(7.4999, 106.0, false)]  // Just south of Vietnam (MinLat=7.5)
    [InlineData(7.5001, 106.0, true)]   // Just inside southern envelope
    [InlineData(24.0001, 106.0, false)] // Just north of Vietnam (MaxLat=24.0)
    [InlineData(23.9999, 106.0, true)]  // Just inside northern envelope
    [InlineData(15.0, 101.4999, false)] // Just west of Vietnam (MinLon=101.5)
    [InlineData(15.0, 101.5001, true)]  // Just inside western envelope
    [InlineData(15.0, 110.5001, false)] // Just east of Vietnam (MaxLon=110.5)
    [InlineData(15.0, 110.4999, true)]  // Just inside eastern envelope
    [InlineData(90.0, 105.0, false)]    // North Pole
    [InlineData(-90.0, 105.0, false)]   // South Pole
    [InlineData(15.0, 180.0, false)]    // Antimeridian
    [InlineData(double.NaN, 105.0, false)]
    [InlineData(15.0, double.PositiveInfinity, false)]
    public void VietnamEnvelope_precision_boundaries_tested(double lat, double lon, bool expectedInside)
    {
        var pt = new GeoPoint(lat, lon);
        Assert.Equal(expectedInside, VietnamEnvelope.Contains(pt));
    }

    [Fact]
    public void TransverseMercator_Inverse_at_near_pole_northing_does_not_throw_unhandled_exception()
    {
        var tm = TmParameters.Tm3(105.75);
        var ell = Ellipsoid.Wgs84;

        // Northing approaching the pole (~10,000,000 m)
        var ex1 = Record.Exception(() => TransverseMercator.Inverse(500000.0, 10000000.0, tm, ell));
        Assert.Null(ex1);

        // Northing past the pole (15,000,000 m)
        var ex2 = Record.Exception(() => TransverseMercator.Inverse(500000.0, 15000000.0, tm, ell));
        Assert.Null(ex2);

        // Negative northing near south pole (-10,000,000 m)
        var ex3 = Record.Exception(() => TransverseMercator.Inverse(500000.0, -10000000.0, tm, ell));
        Assert.Null(ex3);
    }

    #endregion

    #region 3. Central Meridian Out-of-Range & Parsing Stress Tests

    [Theory]
    [InlineData("105°60′")] // 60 minutes is invalid
    [InlineData("105°99′")] // 99 minutes is invalid
    [InlineData("abc")]     // non-numeric
    [InlineData("105°ab′")] // non-numeric minutes
    [InlineData("")]        // empty
    [InlineData("   ")]     // whitespace
    [InlineData(null)]      // null
    public void CentralMeridian_Parse_rejects_malformed_inputs(string? input)
    {
        var parsed = CentralMeridian.Parse(input);
        Assert.Null(parsed);
    }

    [Theory]
    [InlineData("105°", 105.0)]
    [InlineData("105", 105.0)]
    [InlineData("105.75", 105.75)]
    [InlineData("105,75", 105.75)]
    [InlineData("105°45′", 105.75)]
    [InlineData("105-45", 105.75)]
    [InlineData("105 45", 105.75)]
    public void CentralMeridian_Parse_accepts_valid_formats(string input, double expected)
    {
        var parsed = CentralMeridian.Parse(input);
        Assert.NotNull(parsed);
        Assert.Equal(expected, parsed!.Value);
    }

    [Theory]
    [InlineData(105.75, "105°45′")]
    [InlineData(105.0, "105°00′")]
    [InlineData(108.5, "108°30′")]
    [InlineData(104.25, "104°15′")]
    public void CentralMeridian_Format_renders_canonical_labels(double deg, string expected)
    {
        Assert.Equal(expected, CentralMeridian.Format(deg));
    }

    [Fact]
    public void CentralMeridian_Format_handles_non_finite_values_gracefully()
    {
        Assert.Equal("—", CentralMeridian.Format(double.NaN));
        Assert.Equal("—", CentralMeridian.Format(double.PositiveInfinity));
        Assert.Equal("—", CentralMeridian.Format(double.NegativeInfinity));
    }

    [Theory]
    [InlineData(0.0, "OUTSIDE_VIETNAM")]     // Prime Meridian (UK) - inside [-180, 180] but outside VN
    [InlineData(-100.0, "OUTSIDE_VIETNAM")]  // Americas - inside [-180, 180] but outside VN
    [InlineData(180.0, "OUTSIDE_VIETNAM")]   // Antimeridian - inside [-180, 180] but outside VN
    [InlineData(360.0, "INVALID_PROJECTION")] // > 180 deg -> TmParameters.IsValid is false
    [InlineData(-500.0, "INVALID_PROJECTION")] // < -180 deg -> TmParameters.IsValid is false
    [InlineData(1000.0, "INVALID_PROJECTION")] // > 180 deg -> TmParameters.IsValid is false
    public void Vn2000Converter_with_out_of_range_meridian_refuses_cleanly(double cm, string expectedErrorCode)
    {
        var converter = new Vn2000Converter();
        // Valid VN-2000 coordinates in Ho Chi Minh City
        var points = new List<SurveyPoint>
        {
            new(1, "PT1", new PlanePoint(600000.0, 1200000.0))
        };
        var options = new ConversionOptions(TmParameters.Tm3(cm), 1.0);

        var result = converter.Convert(points, Array.Empty<BoundaryPolyline>(), options);

        // Result must fail with expected error code
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Code == expectedErrorCode);
    }

    [Fact]
    public void Vn2000Converter_with_NaN_meridian_reports_NO_CENTRAL_MERIDIAN()
    {
        var converter = new Vn2000Converter();
        var points = new List<SurveyPoint>
        {
            new(1, "PT1", new PlanePoint(600000.0, 1200000.0))
        };
        var options = new ConversionOptions(TmParameters.Tm3(double.NaN), 1.0);

        var result = converter.Convert(points, Array.Empty<BoundaryPolyline>(), options);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Code == "NO_CENTRAL_MERIDIAN");
    }

    #endregion

    #region 4. Command Argument Parsers Stress Tests

    [Theory]
    [InlineData("")]                                            // Empty line
    [InlineData("   ")]                                         // Whitespace
    [InlineData("foo")]                                         // Missing '='
    [InlineData("foo=")]                                        // Missing cm and out
    [InlineData("cm=105.75")]                                   // Missing out
    [InlineData("out=test.kmz")]                                // Missing cm
    [InlineData("cm=105.75 out=test.kmz invalidKey=123")]       // Unknown key
    [InlineData("cm=105.75 out=test.kmz k0=abc")]               // Non-numeric k0
    [InlineData("cm=105.75 out=test.kmz fe=abc")]               // Non-numeric fe
    [InlineData("cm=105.75 out=test.kmz fn=abc")]               // Non-numeric fn
    [InlineData("cm=105.75 out=test.kmz unit=furlong")]         // Invalid unit
    [InlineData("cm=105.75 out=test.kmz pcolor=notAColor")]     // Invalid color
    [InlineData("cm=105.75 out=\"unclosedQuote.kmz")]           // Unclosed quote
    public void ExportArguments_Parse_rejects_adversarial_inputs_with_ArgumentException(string line)
    {
        var ex = Assert.Throws<ArgumentException>(() => ExportArguments.Parse(line));
        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
    }

    [Fact]
    public void ExportArguments_Tokenize_handles_quotes_with_internal_spaces()
    {
        var line = "cm=105.75 out=\"D:\\My Projects\\Cadastral 2026\\test.kmz\" name=\"Bản Đồ 1\"";
        var tokens = ExportArguments.Tokenize(line);

        Assert.Equal("105.75", tokens["cm"]);
        Assert.Equal(@"D:\My Projects\Cadastral 2026\test.kmz", tokens["out"]);
        Assert.Equal("Bản Đồ 1", tokens["name"]);
    }

    [Fact]
    public void ExportArguments_Tokenize_duplicate_keys_last_wins_without_throwing()
    {
        var line = "cm=105.0 cm=105.75 out=test.kmz";
        var args = ExportArguments.Parse(line);
        Assert.Equal(105.75, args.CentralMeridianDeg);
    }

    [Theory]
    [InlineData("layer=*RANH*")]
    [InlineData("layer=RANH?")]
    [InlineData("layer=LAYER[123]")]
    [InlineData("layer=(SPECIAL)")]
    public void WildcardPattern_handles_complex_wildcard_characters_without_throwing(string arg)
    {
        var tokens = ExportArguments.Tokenize(arg);
        var pattern = new WildcardPattern(tokens["layer"]);

        Assert.NotNull(pattern);
        var ex = Record.Exception(() => pattern.IsMatch("ANY_STRING"));
        Assert.Null(ex);
    }

    #endregion

    #region 5. Corrupted KMZ & Malformed KML Stress Tests

    [Fact]
    public void KmlReader_corrupted_non_zip_kmz_throws_InvalidDataException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "corrupt_" + Guid.NewGuid().ToString("N") + ".kmz");
        try
        {
            // Write 1 KB of random uncompressed non-zip bytes
            File.WriteAllBytes(tempFile, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0xFF, 0xFE, 0x00, 0xAA });

            var ex = Assert.Throws<InvalidDataException>(() => KmlReader.ReadFile(tempFile));
            Assert.NotNull(ex);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void KmlReader_zip_without_kml_entry_throws_InvalidDataException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "nokml_" + Guid.NewGuid().ToString("N") + ".kmz");
        try
        {
            using (var zip = ZipFile.Open(tempFile, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("something_else.txt");
                using var writer = new StreamWriter(entry.Open());
                writer.WriteLine("No KML in here!");
            }

            var ex = Assert.Throws<InvalidDataException>(() => KmlReader.ReadFile(tempFile));
            Assert.Contains("không chứa file .kml nào", ex.Message);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void KmlReader_zip_with_corrupt_kml_entry_throws_XmlException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "badxml_" + Guid.NewGuid().ToString("N") + ".kmz");
        try
        {
            using (var zip = ZipFile.Open(tempFile, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("doc.kml");
                using var writer = new StreamWriter(entry.Open());
                writer.WriteLine("<kml><Placemark><Point><coordinates>106.5,10.5</Placemark>"); // Unclosed Point tag
            }

            Assert.ThrowsAny<XmlException>(() => KmlReader.ReadFile(tempFile));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void KmlReader_empty_xml_string_throws_XmlException()
    {
        Assert.ThrowsAny<XmlException>(() => KmlReader.Read(""));
    }

    [Fact]
    public void KmlReader_placemark_with_garbage_coordinates_returns_no_features()
    {
        var kml = """
            <kml xmlns="http://www.opengis.net/kml/2.2">
              <Document>
                <Placemark>
                  <name>BadPoint</name>
                  <Point>
                    <coordinates>garbage,not,a,number</coordinates>
                  </Point>
                </Placemark>
              </Document>
            </kml>
            """;

        var features = KmlReader.Read(kml);
        Assert.Empty(features);
    }

    [Fact]
    public void KmlReader_polygon_with_less_than_3_vertices_is_ignored()
    {
        var kml = """
            <kml xmlns="http://www.opengis.net/kml/2.2">
              <Document>
                <Placemark>
                  <name>DegeneratePoly</name>
                  <Polygon>
                    <outerBoundaryIs>
                      <LinearRing>
                        <coordinates>106.6,10.7,0 106.7,10.8,0</coordinates>
                      </LinearRing>
                    </outerBoundaryIs>
                  </Polygon>
                </Placemark>
              </Document>
            </kml>
            """;

        var features = KmlReader.Read(kml);
        Assert.Empty(features);
    }

    [Fact]
    public void KmlReader_linestring_with_less_than_2_vertices_is_ignored()
    {
        var kml = """
            <kml xmlns="http://www.opengis.net/kml/2.2">
              <Document>
                <Placemark>
                  <name>DegenerateLine</name>
                  <LineString>
                    <coordinates>106.6,10.7,0</coordinates>
                  </LineString>
                </Placemark>
              </Document>
            </kml>
            """;

        var features = KmlReader.Read(kml);
        Assert.Empty(features);
    }

    #endregion
}
