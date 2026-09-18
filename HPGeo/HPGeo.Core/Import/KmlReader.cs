using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using HPGeo.Core.Model;

namespace HPGeo.Core.Import;

public enum KmlFeatureKind
{
    Point,
    Line,
    Polygon,
}

/// <summary>One placemark worth importing: a point, an open line or a polygon's outer ring (WGS84).</summary>
public sealed record KmlFeature(KmlFeatureKind Kind, string Name, IReadOnlyList<GeoPoint> Vertices);

/// <summary>
/// Reads the geometry of a KML or KMZ (Google Earth "Save Place As…", or a file this tool wrote): Point,
/// LineString, Polygon outer ring, MultiGeometry and nested Folders/Documents. A MultiGeometry that holds a
/// Point is a point (this tool's own survey marker: triangle + ring + hidden exact Point); the number-label
/// placemarks this tool writes (<c>#ptLabelStyle</c>) are skipped so a round trip does not double every point.
/// </summary>
public static class KmlReader
{
    private const string LabelStyleUrl = "#ptLabelStyle";

    /// <summary>A survey KML is kilobytes; anything past this is not one and would inflate inside acad.exe.</summary>
    public const long MaxKmlBytes = 64L * 1024 * 1024;

    public static IReadOnlyList<KmlFeature> ReadFile(string path)
    {
        if (path.EndsWith(".kmz", StringComparison.OrdinalIgnoreCase))
        {
            using var zip = ZipFile.OpenRead(path);
            var entry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase))
                        ?? throw new InvalidDataException($"{Path.GetFileName(path)} không chứa file .kml nào.");
            if (entry.Length > MaxKmlBytes) throw new InvalidDataException($"{entry.FullName} quá lớn ({entry.Length / (1024 * 1024)} MB > {MaxKmlBytes / (1024 * 1024)} MB).");
            using var stream = entry.Open();
            return Read(XDocument.Load(stream));
        }
        var length = new FileInfo(path).Length;
        if (length > MaxKmlBytes) throw new InvalidDataException($"{Path.GetFileName(path)} quá lớn ({length / (1024 * 1024)} MB > {MaxKmlBytes / (1024 * 1024)} MB).");
        return Read(XDocument.Load(path));
    }

    public static IReadOnlyList<KmlFeature> Read(string kml) => Read(XDocument.Parse(kml));

    public static IReadOnlyList<KmlFeature> Read(XDocument document)
    {
        var root = document.Root ?? throw new InvalidDataException("KML rỗng.");
        // Google Earth writes the 2.2 namespace; older files may use 2.0/2.1 or none — match on local names.
        var features = new List<KmlFeature>();
        foreach (var placemark in root.Descendants().Where(e => e.Name.LocalName == "Placemark"))
        {
            var style = placemark.Elements().FirstOrDefault(e => e.Name.LocalName == "styleUrl")?.Value.Trim();
            if (style == LabelStyleUrl) continue;
            var name = placemark.Elements().FirstOrDefault(e => e.Name.LocalName == "name")?.Value.Trim() ?? "";
            features.AddRange(ReadGeometry(placemark, name));
        }
        return features;
    }

    private static IEnumerable<KmlFeature> ReadGeometry(XElement placemark, string name)
    {
        var geometries = placemark.Descendants().Where(e => e.Name.LocalName is "Point" or "LineString" or "Polygon").ToList();
        var point = geometries.FirstOrDefault(g => g.Name.LocalName == "Point");
        if (point is not null)
        {
            // A MultiGeometry with a Point is a point placemark (our marker keeps its exact position there).
            var coords = ParseCoordinates(point);
            if (coords.Count > 0) yield return new KmlFeature(KmlFeatureKind.Point, name, new[] { coords[0] });
            yield break;
        }
        var index = 0;
        foreach (var g in geometries)
        {
            index++;
            var suffix = geometries.Count > 1 ? $" {index}" : "";
            if (g.Name.LocalName == "LineString")
            {
                var coords = ParseCoordinates(g);
                if (coords.Count >= 2) yield return new KmlFeature(KmlFeatureKind.Line, name + suffix, coords);
            }
            else
            {
                var outer = g.Descendants().FirstOrDefault(e => e.Name.LocalName == "outerBoundaryIs")
                            ?? g.Descendants().FirstOrDefault(e => e.Name.LocalName == "LinearRing");
                if (outer is null) continue;
                var coords = ParseCoordinates(outer);
                if (coords.Count >= 2 && SamePosition(coords[0], coords[^1])) coords.RemoveAt(coords.Count - 1); // the ring repeats its first vertex
                if (coords.Count >= 3) yield return new KmlFeature(KmlFeatureKind.Polygon, name + suffix, coords);
            }
        }
    }

    /// <summary>
    /// KML coordinates are "lon,lat[,alt]" tuples separated by whitespace. Hand-written and some exported files put a
    /// space after the commas ("106.6, 11.1, 0"), which Google Earth accepts, so the separators are normalised first.
    /// </summary>
    private static List<GeoPoint> ParseCoordinates(XElement geometry)
    {
        var text = geometry.Descendants().FirstOrDefault(e => e.Name.LocalName == "coordinates")?.Value ?? "";
        text = System.Text.RegularExpressions.Regex.Replace(text, @",\s+", ",");
        var result = new List<GeoPoint>();
        foreach (var tuple in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = tuple.Split(',');
            if (parts.Length < 2) continue;
            if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                result.Add(new GeoPoint(lat, lon));
        }
        return result;
    }

    private static bool SamePosition(GeoPoint a, GeoPoint b) => Math.Abs(a.LatDeg - b.LatDeg) < 1e-12 && Math.Abs(a.LonDeg - b.LonDeg) < 1e-12;
}
