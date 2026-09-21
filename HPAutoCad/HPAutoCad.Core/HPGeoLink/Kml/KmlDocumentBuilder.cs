using System.Globalization;
using System.Security;
using System.Text;
using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Model;

namespace HPAutoCad.Core.HPGeoLink.Kml;

public sealed record KmlBuildResult(string Kml, int MarkerCount, int BoundaryCount, bool BoundaryFromPoints);

/// <summary>
/// Writes the KML 2.2 document the reference tool produces: each survey point as a crisp vector marker
/// (hollow triangle + small ring at the exact position, plus a hidden Point carrying the exact coordinates
/// so the file re-imports without loss) with its number to the upper right, every closed boundary as a
/// Polygon (open ones as a LineString), styles and colours identical. When there is no
/// boundary at all but at least three points, the points in order form the boundary — the reference tool's
/// behaviour for a pasted list of corner points.
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
        sb.Append("  <description>").Append(Escape(options.Description)).Append("</description>\n");
        AppendStyles(sb, pointColor, lineColor);

        var markers = 0;
        if (options.Output != KmlOutput.Boundaries)
        {
            foreach (var p in result.Points)
            {
                AppendMarker(sb, p);
                markers++;
            }
        }

        var boundaries = 0;
        var fromPoints = false;
        if (options.Output != KmlOutput.Points)
        {
            foreach (var b in result.Boundaries)
            {
                AppendBoundary(sb, b.Source.Name, b.Wgs84, b.Source.Closed);
                boundaries++;
            }
            if (result.Boundaries.Count == 0 && result.Points.Count >= 3)
            {
                AppendBoundary(sb, "Boundary", result.Points.Select(p => p.Wgs84).ToList(), closed: true);
                boundaries++;
                fromPoints = true;
            }
        }

        sb.Append("</Document>\n</kml>\n");
        return new KmlBuildResult(sb.ToString(), markers, boundaries, fromPoints);
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
        sb.Append("    <IconStyle><scale>0</scale></IconStyle>\n");
        sb.Append("    <LabelStyle><color>").Append(pointColor).Append("</color><scale>1.35</scale></LabelStyle>\n  </Style>\n");
        sb.Append("  <Style id=\"polyStyle\">\n");
        sb.Append("    <LineStyle><color>").Append(lineColor).Append("</color><width>1.6</width></LineStyle>\n");
        sb.Append("    <PolyStyle><color>").Append(KmlColor.PolygonFillFrom(lineColor)).Append("</color></PolyStyle>\n  </Style>\n");
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
            sb.Append("    <Polygon>\n      <outerBoundaryIs><LinearRing><coordinates>\n            ");
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

    /// <summary>XML 1.0 forbids most control characters even when escaped; they are dropped, the rest is entity-escaped.</summary>
    private static string Escape(string text)
    {
        var clean = new string(text.Where(c => c is '\t' or '\n' or '\r' || c >= ' ').ToArray());
        return SecurityElement.Escape(clean) ?? "";
    }
}
