using System.Globalization;
using System.Security;
using System.Text;
using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Model;

namespace HPAutoCad.Core.HPGeoLink.Kml;

public sealed record KmlBuildResult(string Kml, int MarkerCount, int BoundaryCount, bool BoundaryFromPoints, int VertexCount = 0);

/// <summary>
/// Writes the KML 2.2 document the reference tool produces: each survey point as a crisp vector marker
/// (hollow triangle + small ring at the exact position, plus a hidden Point carrying the exact coordinates
/// so the file re-imports without loss) with its number to the upper right, every closed boundary as a
/// Polygon (open ones as a LineString), styles and colours identical. Boundary vertices can be exported
/// with selectable symbol styles (Triangle, Pushpin, Circle, LabelOnly) and HTML popup templates (Cadastral,
/// Technical DMS, Simple). Elements are grouped into Folders for convenient toggling in Google Earth.
/// </summary>
public static class KmlDocumentBuilder
{
    // Marker proportions of the reference KMZ, in metres around the survey point.
    private static readonly (double East, double North)[] Triangle = { (-1.05, -0.60), (1.05, -0.60), (0.00, 1.20), (-1.05, -0.60) };
    private const double RingRadiusM = 0.16;
    private const int RingSegments = 32;
    private static readonly (double East, double North) LabelOffset = (2.30, 0.40);
    private const int MarkerDecimals = 8;
    // The reference tool prints 7 decimals (1 cm); 9 keeps a re-import within a millimetre.
    private const int RingDecimals = 9;
    private const int ExactDecimals = 9;

    public static KmlBuildResult Build(ConversionResult result, KmlExportOptions options)
    {
        var pointColor = KmlColor.Normalize(options.PointColor, "điểm");
        var lineColor = KmlColor.Normalize(options.LineColor, "đường");
        var sb = new StringBuilder();

        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        sb.Append("<kml xmlns=\"http://www.opengis.net/kml/2.2\">\n<Document>\n");
        sb.Append("  <name>").Append(Escape(options.DocumentName)).Append("</name>\n");
        sb.Append("  <open>1</open>\n");
        sb.Append("  <description>").Append(Escape(options.Description)).Append("</description>\n");

        var allWgs = result.Points.Select(p => p.Wgs84)
            .Concat(result.Boundaries.SelectMany(b => b.Wgs84))
            .ToList();

        if (allWgs.Count > 0)
        {
            var center = result.Center ?? allWgs[0];
            var minLat = allWgs.Min(p => p.LatDeg);
            var maxLat = allWgs.Max(p => p.LatDeg);
            var minLon = allWgs.Min(p => p.LonDeg);
            var maxLon = allWgs.Max(p => p.LonDeg);
            var dLatM = (maxLat - minLat) * 111320.0;
            var dLonM = (maxLon - minLon) * 111320.0 * Math.Cos(center.LatDeg * Math.PI / 180.0);
            var diagM = Math.Sqrt(dLatM * dLatM + dLonM * dLonM);
            var range = Math.Max(diagM * 1.6, 250.0);

            var ci = CultureInfo.InvariantCulture;
            sb.Append("  <LookAt>\n");
            sb.Append("    <longitude>").Append(center.LonDeg.ToString("F7", ci)).Append("</longitude>\n");
            sb.Append("    <latitude>").Append(center.LatDeg.ToString("F7", ci)).Append("</latitude>\n");
            sb.Append("    <altitude>0</altitude>\n");
            sb.Append("    <heading>0</heading>\n");
            sb.Append("    <tilt>0</tilt>\n");
            sb.Append("    <range>").Append(range.ToString("F1", ci)).Append("</range>\n");
            sb.Append("    <altitudeMode>relativeToGround</altitudeMode>\n");
            sb.Append("  </LookAt>\n");
        }

        AppendStyles(sb, pointColor, lineColor);

        var markers = 0;
        if (options.Output != KmlOutput.Boundaries && result.Points.Count > 0)
        {
            sb.Append("  <Folder>\n    <name>Điểm khảo sát</name>\n    <open>1</open>\n");
            foreach (var p in result.Points)
            {
                AppendMarker(sb, p);
                markers++;
            }
            sb.Append("  </Folder>\n");
        }

        var boundaries = 0;
        var vertices = 0;
        var fromPoints = false;
        if (options.Output != KmlOutput.Points)
        {
            if (result.Boundaries.Count > 0)
            {
                sb.Append("  <Folder>\n    <name>Đường ranh đất</name>\n    <open>1</open>\n");
                foreach (var b in result.Boundaries)
                {
                    AppendBoundary(sb, b.Source.Name, b.Wgs84, b.Source.Closed);
                    boundaries++;
                }
                sb.Append("  </Folder>\n");
            }
            else if (result.Points.Count >= 3)
            {
                AppendBoundary(sb, "Boundary", result.Points.Select(p => p.Wgs84).ToList(), closed: true);
                boundaries++;
                fromPoints = true;
            }
        }

        if ((options.ExportBoundaryVertices || options.Output == KmlOutput.Points) && options.Output != KmlOutput.Boundaries && result.Boundaries.Count > 0)
        {
            sb.Append("  <Folder>\n    <name>Mốc đỉnh ranh</name>\n    <open>1</open>\n");
            var totalBoundaries = result.Boundaries.Count;
            foreach (var b in result.Boundaries)
            {
                var gridPts = b.CadGridM;
                var wgsPts = b.CadWgs84;
                var vertexCount = gridPts.Count;
                for (var i = 0; i < vertexCount; i++)
                {
                    var vertexLabel = totalBoundaries == 1 ? $"P{i + 1}" : $"{b.Source.Name}-P{i + 1}";
                    double? segmentDist = null;
                    string? nextLabel = null;
                    if (b.Source.Closed)
                    {
                        var nextIdx = (i + 1) % vertexCount;
                        var currentPt = gridPts[i];
                        var nextPt = gridPts[nextIdx];
                        var dx = nextPt.Easting - currentPt.Easting;
                        var dy = nextPt.Northing - currentPt.Northing;
                        segmentDist = Math.Sqrt(dx * dx + dy * dy);
                        nextLabel = totalBoundaries == 1 ? $"P{nextIdx + 1}" : $"{b.Source.Name}-P{nextIdx + 1}";
                    }
                    else if (i < vertexCount - 1)
                    {
                        var nextIdx = i + 1;
                        var currentPt = gridPts[i];
                        var nextPt = gridPts[nextIdx];
                        var dx = nextPt.Easting - currentPt.Easting;
                        var dy = nextPt.Northing - currentPt.Northing;
                        segmentDist = Math.Sqrt(dx * dx + dy * dy);
                        nextLabel = totalBoundaries == 1 ? $"P{nextIdx + 1}" : $"{b.Source.Name}-P{nextIdx + 1}";
                    }

                    var balloon = BuildBalloon(options.PopupTemplate, vertexLabel, b.Source.Name, gridPts[i], wgsPts[i], nextLabel, segmentDist);
                    AppendBoundaryVertex(sb, vertexLabel, wgsPts[i], balloon, options.MarkerStyle);
                    vertices++;
                }
            }
            sb.Append("  </Folder>\n");
        }

        sb.Append("</Document>\n</kml>\n");
        return new KmlBuildResult(sb.ToString(), markers, boundaries, fromPoints, vertices);
    }

    private static void AppendStyles(StringBuilder sb, string pointColor, string lineColor)
    {
        sb.Append("  <!-- Reference-style survey point: crisp vector triangle + center ring -->\n");
        sb.Append("  <Style id=\"ptVectorStyle\">\n");
        sb.Append("    <LineStyle><color>").Append(pointColor).Append("</color><width>3.0</width></LineStyle>\n");
        sb.Append("    <IconStyle><scale>0</scale></IconStyle>\n"); // the marker carries an exact Point, kept invisible
        sb.Append("    <LabelStyle><scale>0</scale></LabelStyle>\n  </Style>\n");
        sb.Append("  <!-- Number to the right of each survey marker -->\n");
        sb.Append("  <Style id=\"ptLabelStyle\">\n");
        sb.Append("    <IconStyle><scale>0.001</scale><Icon><href>https://maps.google.com/mapfiles/kml/shapes/placemark_circle.png</href></Icon></IconStyle>\n");
        sb.Append("    <LabelStyle><color>").Append(pointColor).Append("</color><scale>1.35</scale></LabelStyle>\n  </Style>\n");
        sb.Append("  <Style id=\"polyStyle\">\n");
        sb.Append("    <LineStyle><color>").Append(lineColor).Append("</color><width>3.5</width></LineStyle>\n");
        sb.Append("    <PolyStyle><color>").Append(KmlColor.PolygonFillFrom(lineColor)).Append("</color><fill>1</fill><outline>1</outline></PolyStyle>\n  </Style>\n");

        sb.Append("  <!-- Boundary vertex styles: Triangle, Pushpin, Circle, LabelOnly -->\n");
        sb.Append("  <Style id=\"bndTriangleStyle\">\n");
        sb.Append("    <LineStyle><color>ffffffff</color><width>3.5</width></LineStyle>\n");
        sb.Append("    <IconStyle><scale>0</scale></IconStyle>\n");
        sb.Append("    <LabelStyle><scale>0</scale></LabelStyle>\n  </Style>\n");
        sb.Append("  <Style id=\"bndLabelStyle\">\n");
        sb.Append("    <IconStyle><scale>0.001</scale><Icon><href>https://maps.google.com/mapfiles/kml/shapes/placemark_circle.png</href></Icon></IconStyle>\n");
        sb.Append("    <LabelStyle><color>").Append(pointColor).Append("</color><scale>1.25</scale></LabelStyle>\n  </Style>\n");
        sb.Append("  <Style id=\"bndPushpinStyle\">\n");
        sb.Append("    <IconStyle><color>ffffffff</color><scale>1.0</scale><Icon><href>https://maps.google.com/mapfiles/kml/pushpin/wht-pushpin.png</href></Icon><hotSpot x=\"20\" y=\"2\" xunits=\"pixels\" yunits=\"pixels\"/></IconStyle>\n");
        sb.Append("    <LabelStyle><color>").Append(pointColor).Append("</color><scale>1.15</scale></LabelStyle>\n  </Style>\n");
        sb.Append("  <Style id=\"bndCircleStyle\">\n");
        sb.Append("    <IconStyle><color>ffffffff</color><scale>0.9</scale><Icon><href>https://maps.google.com/mapfiles/kml/shapes/placemark_circle.png</href></Icon></IconStyle>\n");
        sb.Append("    <LabelStyle><color>").Append(pointColor).Append("</color><scale>1.15</scale></LabelStyle>\n  </Style>\n");
        sb.Append("  <Style id=\"bndLabelOnlyStyle\">\n");
        sb.Append("    <IconStyle><scale>0.001</scale><Icon><href>https://maps.google.com/mapfiles/kml/shapes/placemark_circle.png</href></Icon></IconStyle>\n");
        sb.Append("    <LabelStyle><color>").Append(pointColor).Append("</color><scale>1.2</scale></LabelStyle>\n  </Style>\n");
    }

    private static void AppendMarker(StringBuilder sb, ConvertedPoint p)
    {
        var at = p.Wgs84;
        var label = KmlGeoMath.Offset(at, LabelOffset.East, LabelOffset.North);
        var ci = CultureInfo.InvariantCulture;

        sb.Append("  <Placemark>\n    <name>POINT ").Append(Escape(p.Source.Label)).Append("</name>\n");
        sb.Append("    <description>VN2000: X(N)=").Append(p.GridM.Northing.ToString("0.###", ci))
          .Append(", Y(E)=").Append(p.GridM.Easting.ToString("0.###", ci))
          .Append("&#xa;WGS84: Lat=").Append(at.LatDeg.ToString("F7", ci))
          .Append(", Lon=").Append(at.LonDeg.ToString("F7", ci)).Append("</description>\n");
        sb.Append("    <styleUrl>#ptVectorStyle</styleUrl>\n    <MultiGeometry>\n");
        sb.Append("      <LineString><tessellate>1</tessellate><coordinates>");
        sb.Append(string.Join(' ', Triangle.Select(t => KmlGeoMath.Coordinate(KmlGeoMath.Offset(at, t.East, t.North), MarkerDecimals))));
        sb.Append("</coordinates></LineString>\n");
        sb.Append("      <LineString><tessellate>1</tessellate><coordinates>");
        sb.Append(string.Join(' ', Ring(at)));
        sb.Append("</coordinates></LineString>\n");
        // The exact position, invisible (IconStyle scale 0): what a re-import reads instead of the marker shapes.
        sb.Append("      <Point><coordinates>").Append(KmlGeoMath.Coordinate(at, ExactDecimals)).Append("</coordinates></Point>\n");
        sb.Append("    </MultiGeometry>\n  </Placemark>\n");

        sb.Append("  <Placemark>\n    <name>").Append(Escape(p.Source.Label)).Append("</name>\n");
        sb.Append("    <styleUrl>#ptLabelStyle</styleUrl>\n    <Point><coordinates>")
          .Append(KmlGeoMath.Coordinate(label, MarkerDecimals)).Append("</coordinates></Point>\n  </Placemark>\n");
    }

    private static IEnumerable<string> Ring(GeoPoint at)
    {
        for (var j = 0; j <= RingSegments; j++)
        {
            var a = Math.PI * 2 * j / RingSegments;
            yield return KmlGeoMath.Coordinate(KmlGeoMath.Offset(at, RingRadiusM * Math.Cos(a), RingRadiusM * Math.Sin(a)), MarkerDecimals);
        }
    }

    private static void AppendBoundary(StringBuilder sb, string name, IReadOnlyList<GeoPoint> vertices, bool closed)
    {
        var coords = vertices.Select(v => KmlGeoMath.Coordinate(v, RingDecimals)).ToList();
        sb.Append("  <Placemark>\n    <name>").Append(Escape(name)).Append("</name>\n    <styleUrl>#polyStyle</styleUrl>\n");
        if (closed)
        {
            coords.Add(KmlGeoMath.Coordinate(vertices[0], RingDecimals)); // a LinearRing repeats its first vertex
            sb.Append("    <Polygon>\n      <tessellate>1</tessellate>\n      <outerBoundaryIs><LinearRing><coordinates>\n            ");
            sb.Append(string.Join("\n            ", coords));
            sb.Append("\n      </coordinates></LinearRing></outerBoundaryIs>\n    </Polygon>\n");
        }
        else
        {
            sb.Append("    <LineString><tessellate>1</tessellate><coordinates>\n            ");
            sb.Append(string.Join("\n            ", coords));
            sb.Append("\n    </coordinates></LineString>\n");
        }
        sb.Append("  </Placemark>\n");
    }

    private static void AppendBoundaryVertex(StringBuilder sb, string vLabel, GeoPoint at, string balloon, BoundaryMarkerStyle style)
    {
        switch (style)
        {
            case BoundaryMarkerStyle.Pushpin:
                AppendVertexPointMarker(sb, vLabel, at, balloon, "#bndPushpinStyle");
                break;
            case BoundaryMarkerStyle.Circle:
                AppendVertexPointMarker(sb, vLabel, at, balloon, "#bndCircleStyle");
                break;
            case BoundaryMarkerStyle.LabelOnly:
                AppendVertexPointMarker(sb, vLabel, at, balloon, "#bndLabelOnlyStyle");
                break;
            case BoundaryMarkerStyle.Triangle:
            default:
                AppendVertexTriangleMarker(sb, vLabel, at, balloon);
                break;
        }
    }

    private static void AppendVertexPointMarker(StringBuilder sb, string label, GeoPoint at, string html, string styleUrl)
    {
        sb.Append("    <Placemark>\n      <name>").Append(Escape(label)).Append("</name>\n");
        sb.Append("      <description><![CDATA[").Append(html).Append("]]></description>\n");
        sb.Append("      <styleUrl>").Append(styleUrl).Append("</styleUrl>\n");
        sb.Append("      <Point><coordinates>").Append(KmlGeoMath.Coordinate(at, ExactDecimals)).Append("</coordinates></Point>\n");
        sb.Append("    </Placemark>\n");
    }

    private static void AppendVertexTriangleMarker(StringBuilder sb, string label, GeoPoint at, string html)
    {
        var labelOffset = KmlGeoMath.Offset(at, LabelOffset.East, LabelOffset.North);
        sb.Append("    <Placemark>\n      <name>").Append(Escape(label)).Append("</name>\n");
        sb.Append("      <description><![CDATA[").Append(html).Append("]]></description>\n");
        sb.Append("      <styleUrl>#bndTriangleStyle</styleUrl>\n      <MultiGeometry>\n");
        sb.Append("        <LineString><tessellate>1</tessellate><coordinates>");
        sb.Append(string.Join(' ', Triangle.Select(t => KmlGeoMath.Coordinate(KmlGeoMath.Offset(at, t.East, t.North), MarkerDecimals))));
        sb.Append("</coordinates></LineString>\n");
        sb.Append("        <LineString><tessellate>1</tessellate><coordinates>");
        sb.Append(string.Join(' ', Ring(at)));
        sb.Append("</coordinates></LineString>\n");
        sb.Append("        <Point><coordinates>").Append(KmlGeoMath.Coordinate(at, ExactDecimals)).Append("</coordinates></Point>\n");
        sb.Append("      </MultiGeometry>\n    </Placemark>\n");

        sb.Append("    <Placemark>\n      <name>").Append(Escape(label)).Append("</name>\n");
        sb.Append("      <styleUrl>#bndLabelStyle</styleUrl>\n      <Point><coordinates>")
          .Append(KmlGeoMath.Coordinate(labelOffset, MarkerDecimals)).Append("</coordinates></Point>\n    </Placemark>\n");
    }

    public static string FormatDms(double deg, bool isLatitude)
    {
        var hemisphere = isLatitude ? (deg >= 0 ? "N" : "S") : (deg >= 0 ? "E" : "W");
        var abs = Math.Abs(deg);
        var d = (int)Math.Floor(abs);
        var remM = (abs - d) * 60.0;
        var m = (int)Math.Floor(remM);
        var s = (remM - m) * 60.0;
        if (s >= 59.995)
        {
            s = 0;
            m++;
            if (m >= 60)
            {
                m = 0;
                d++;
            }
        }
        return string.Create(CultureInfo.InvariantCulture, $"{d}°{m:D2}'{s:00.00}\" {hemisphere}");
    }

    public static string BuildBalloon(BoundaryPopupTemplate template, string vertexLabel, string boundaryName, PlanePoint grid, GeoPoint geo, string? nextLabel, double? segmentDist)
    {
        return template switch
        {
            BoundaryPopupTemplate.Technical => BuildTechnicalBalloon(vertexLabel, boundaryName, grid, geo, nextLabel, segmentDist),
            BoundaryPopupTemplate.Simple => BuildSimpleBalloon(vertexLabel, boundaryName, grid, geo, nextLabel, segmentDist),
            _ => BuildCadastralBalloon(vertexLabel, boundaryName, grid, geo, nextLabel, segmentDist),
        };
    }

    private static string BuildCadastralBalloon(string vertexLabel, string boundaryName, PlanePoint grid, GeoPoint geo, string? nextLabel, double? segmentDist)
    {
        var ci = CultureInfo.InvariantCulture;
        var segText = segmentDist.HasValue && nextLabel != null
            ? $"{vertexLabel} → {nextLabel}: {segmentDist.Value.ToString("F2", ci)} m"
            : "-";
        return $"""
            <div style="font-family: Arial, sans-serif; font-size: 12px; color: #222; min-width: 250px;">
              <div style="background-color: #1976D2; color: white; padding: 6px 10px; font-weight: bold; border-radius: 3px 3px 0 0;">
                THÔNG TIN ĐỈNH RANH: {Escape(vertexLabel)}
              </div>
              <table style="width: 100%; border-collapse: collapse; margin-top: 4px;" border="1" cellpadding="4" cellspacing="0" bordercolor="#ccc">
                <tr bgcolor="#f5f5f5"><td colspan="2" style="font-weight: bold; color: #1976D2;">Hệ tọa độ VN-2000</td></tr>
                <tr><td style="width: 42%; color: #555;">X (Bắc):</td><td style="font-weight: bold;">{grid.Northing.ToString("F3", ci)} m</td></tr>
                <tr><td style="color: #555;">Y (Đông):</td><td style="font-weight: bold;">{grid.Easting.ToString("F3", ci)} m</td></tr>
                <tr bgcolor="#f5f5f5"><td colspan="2" style="font-weight: bold; color: #1976D2;">Hệ tọa độ WGS-84</td></tr>
                <tr><td style="color: #555;">Vĩ độ (Lat):</td><td>{geo.LatDeg.ToString("F7", ci)}°</td></tr>
                <tr><td style="color: #555;">Kinh độ (Lon):</td><td>{geo.LonDeg.ToString("F7", ci)}°</td></tr>
                <tr bgcolor="#f5f5f5"><td colspan="2" style="font-weight: bold; color: #1976D2;">Thông số ranh đất</td></tr>
                <tr><td style="color: #555;">Ranh đất:</td><td>{Escape(boundaryName)}</td></tr>
                <tr><td style="color: #555;">Cạnh kế tiếp:</td><td style="font-weight: bold; color: #D32F2F;">{segText}</td></tr>
              </table>
            </div>
            """;
    }

    private static string BuildTechnicalBalloon(string vertexLabel, string boundaryName, PlanePoint grid, GeoPoint geo, string? nextLabel, double? segmentDist)
    {
        var ci = CultureInfo.InvariantCulture;
        var segText = segmentDist.HasValue && nextLabel != null
            ? $"{vertexLabel} → {nextLabel}: {segmentDist.Value.ToString("F2", ci)} m"
            : "-";
        var latDms = FormatDms(geo.LatDeg, isLatitude: true);
        var lonDms = FormatDms(geo.LonDeg, isLatitude: false);
        return $"""
            <div style="font-family: 'Consolas', 'Courier New', monospace; font-size: 12px; color: #111; min-width: 270px;">
              <div style="background-color: #37474F; color: white; padding: 6px 10px; font-weight: bold;">
                MỐC RANH: {Escape(vertexLabel)} ({Escape(boundaryName)})
              </div>
              <table style="width: 100%; border-collapse: collapse; margin-top: 4px;" border="1" cellpadding="4" cellspacing="0" bordercolor="#999">
                <tr bgcolor="#ECEFF1"><th style="text-align: left;">Thông số</th><th style="text-align: right;">Giá trị</th></tr>
                <tr><td>VN2000 X (N)</td><td style="text-align: right; font-weight: bold;">{grid.Northing.ToString("F3", ci)} m</td></tr>
                <tr><td>VN2000 Y (E)</td><td style="text-align: right; font-weight: bold;">{grid.Easting.ToString("F3", ci)} m</td></tr>
                <tr><td>WGS84 Lat (deg)</td><td style="text-align: right;">{geo.LatDeg.ToString("F7", ci)}°</td></tr>
                <tr><td>WGS84 Lon (deg)</td><td style="text-align: right;">{geo.LonDeg.ToString("F7", ci)}°</td></tr>
                <tr><td>WGS84 Lat (DMS)</td><td style="text-align: right;">{latDms}</td></tr>
                <tr><td>WGS84 Lon (DMS)</td><td style="text-align: right;">{lonDms}</td></tr>
                <tr><td>Cạnh kế tiếp</td><td style="text-align: right; font-weight: bold; color: #C62828;">{segText}</td></tr>
              </table>
            </div>
            """;
    }

    private static string BuildSimpleBalloon(string vertexLabel, string boundaryName, PlanePoint grid, GeoPoint geo, string? nextLabel, double? segmentDist)
    {
        var ci = CultureInfo.InvariantCulture;
        var segText = segmentDist.HasValue && nextLabel != null
            ? $"{vertexLabel} → {nextLabel}: {segmentDist.Value.ToString("F2", ci)} m"
            : "-";
        return $"""
            <div style="font-family: Arial, sans-serif; font-size: 12px; line-height: 1.5;">
              <b>Đỉnh ranh: {Escape(vertexLabel)}</b> ({Escape(boundaryName)})<br/>
              <b>VN-2000:</b> X={grid.Northing.ToString("F3", ci)} m, Y={grid.Easting.ToString("F3", ci)} m<br/>
              <b>WGS-84:</b> {geo.LatDeg.ToString("F7", ci)}°, {geo.LonDeg.ToString("F7", ci)}°<br/>
              <b>Cạnh:</b> {segText}
            </div>
            """;
    }

    /// <summary>XML 1.0 forbids most control characters even when escaped; they are dropped, the rest is entity-escaped.</summary>
    private static string Escape(string text)
    {
        var clean = new string(text.Where(c => c is '\t' or '\n' or '\r' || c >= ' ').ToArray());
        return SecurityElement.Escape(clean) ?? "";
    }
}
