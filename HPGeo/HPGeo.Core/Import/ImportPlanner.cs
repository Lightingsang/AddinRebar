using HPGeo.Core.Catalog;
using HPGeo.Core.Conversion;
using HPGeo.Core.Model;
using HPGeo.Core.Projection;
using HPGeo.Core.Validation;

namespace HPGeo.Core.Import;

/// <summary>A point to draw, in drawing units (X = Easting, Y = Northing) plus what it came from.</summary>
public sealed record ImportPoint(string Label, PlanePoint DrawingXY, PlanePoint GridM, GeoPoint? Wgs84, string? Note);

/// <summary>A polyline to draw, in drawing units.</summary>
public sealed record ImportPolyline(string Name, IReadOnlyList<PlanePoint> DrawingVertices, bool Closed);

/// <summary>What the drawing writer will create, with everything the user should know first.</summary>
public sealed class ImportPlan
{
    public ImportPlan(IReadOnlyList<ImportPoint> points, IReadOnlyList<ImportPolyline> polylines, IReadOnlyList<ConversionIssue> issues, ConversionOptions options)
    {
        Points = points;
        Polylines = polylines;
        Issues = issues;
        Options = options;
    }

    public IReadOnlyList<ImportPoint> Points { get; }
    public IReadOnlyList<ImportPolyline> Polylines { get; }
    public IReadOnlyList<ConversionIssue> Issues { get; }
    public ConversionOptions Options { get; }
    public bool Success => Issues.All(i => i.Severity != IssueSeverity.Error) && (Points.Count > 0 || Polylines.Count > 0);
    public IEnumerable<ConversionIssue> Errors => Issues.Where(i => i.Severity == IssueSeverity.Error);
}

/// <summary>
/// The reverse direction: WGS84 features (from a KML/KMZ or pasted lat,lon) or VN-2000 pairs (pasted E,N)
/// become drawing coordinates for the chosen central meridian and drawing unit. WGS84 input outside Viet
/// Nam and grid results outside the plausible VN-2000 ranges are errors — a wrong zone must never be drawn
/// silently. Never touches a drawing; the host writes the plan in one transaction.
/// </summary>
public sealed class ImportPlanner
{
    private readonly Vn2000Wgs84Transform _transform;

    public ImportPlanner() : this(Vn2000Wgs84Transform.Default) { }

    public ImportPlanner(Vn2000Wgs84Transform transform) => _transform = transform;

    public ImportPlan FromKml(IReadOnlyList<KmlFeature> features, ConversionOptions options)
    {
        var issues = new List<ConversionIssue>();
        var points = new List<ImportPoint>();
        var polylines = new List<ImportPolyline>();
        var setup = Setup(options, issues);
        if (setup is null) return new ImportPlan(points, polylines, issues, options);

        var index = 0;
        foreach (var f in features)
        {
            if (f.Kind == KmlFeatureKind.Point)
            {
                index++;
                var label = f.Name.Length == 0 ? index.ToString() : StripPointPrefix(f.Name);
                var p = ToPoint(label, f.Vertices[0], options, setup.Value, issues);
                if (p is not null) points.Add(p);
                continue;
            }
            var vertices = new List<PlanePoint>();
            var ok = true;
            for (var i = 0; i < f.Vertices.Count; i++)
            {
                var p = ToPoint($"{f.Name} đỉnh {i + 1}", f.Vertices[i], options, setup.Value, issues);
                if (p is null) { ok = false; break; }
                vertices.Add(p.DrawingXY);
            }
            if (ok) polylines.Add(new ImportPolyline(f.Name, vertices, f.Kind == KmlFeatureKind.Polygon));
        }
        if (features.Count == 0) issues.Add(new ConversionIssue(IssueSeverity.Error, "NO_INPUT", "File KML không có Placemark nào có toạ độ."));
        return new ImportPlan(points, polylines, issues, options);
    }

    public ImportPlan FromPasted(IReadOnlyList<PastedCoordinate> pasted, ConversionOptions options, bool asPolyline, bool closed)
    {
        var issues = new List<ConversionIssue>();
        var points = new List<ImportPoint>();
        var polylines = new List<ImportPolyline>();
        var setup = Setup(options, issues);
        if (setup is null) return new ImportPlan(points, polylines, issues, options);
        if (pasted.Count == 0)
        {
            issues.Add(new ConversionIssue(IssueSeverity.Error, "NO_INPUT", "Không tìm thấy tọa độ hợp lệ. Kiểm tra định dạng dữ liệu."));
            return new ImportPlan(points, polylines, issues, options);
        }

        foreach (var c in pasted)
        {
            ImportPoint? p;
            if (c.Geo is { } geo)
            {
                p = ToPoint(c.Label, geo, options, setup.Value, issues);
            }
            else
            {
                var grid = c.Grid!.Value;
                var hint = PlausibilityCheck.Diagnose(grid);
                if (hint is not null) issues.Add(new ConversionIssue(IssueSeverity.Warning, "IMPLAUSIBLE_EN", $"Dòng {c.LineNumber} ({c.Label}): {hint}."));
                var wgs = _transform.ToWgs84(grid, options.Tm);
                if (!VietnamEnvelope.Contains(wgs))
                {
                    issues.Add(new ConversionIssue(IssueSeverity.Error, "OUTSIDE_VIETNAM",
                        $"Dòng {c.LineNumber} ({c.Label}) E={grid.Easting:F3}, N={grid.Northing:F3} với KTT hiện tại rơi ngoài Việt Nam (Lat {wgs.LatDeg:F6}, Lon {wgs.LonDeg:F6}). Kiểm tra thứ tự E/N ↔ X/Y và KTT."));
                    p = null;
                }
                else
                {
                    p = new ImportPoint(c.Label, ToDrawing(grid, setup.Value), grid, wgs, c.Ambiguous ? c.Mode : null);
                }
            }
            if (c.Ambiguous && p is not null)
                issues.Add(new ConversionIssue(IssueSeverity.Warning, "AMBIGUOUS_ORDER", $"Dòng {c.LineNumber} ({c.Label}): {c.Mode}."));
            if (p is not null) points.Add(p);
        }

        if (asPolyline)
        {
            if (closed && points.Count < 3)
                issues.Add(new ConversionIssue(IssueSeverity.Error, "RING_TOO_SHORT", "Ranh đóng cần ít nhất 3 điểm."));
            else if (!closed && points.Count < 2)
                issues.Add(new ConversionIssue(IssueSeverity.Error, "LINE_TOO_SHORT", "Đường cần ít nhất 2 điểm."));
            else
                polylines.Add(new ImportPolyline("Ranh", points.Select(p => p.DrawingXY).ToList(), closed));
        }
        return new ImportPlan(points, polylines, issues, options);
    }

    private static string StripPointPrefix(string name) =>
        name.StartsWith("POINT ", StringComparison.OrdinalIgnoreCase) ? name[6..].Trim() : name;

    private ImportPoint? ToPoint(string label, GeoPoint geo, ConversionOptions options, double metersPerUnit, List<ConversionIssue> issues)
    {
        if (!VietnamEnvelope.Contains(geo))
        {
            issues.Add(new ConversionIssue(IssueSeverity.Error, "OUTSIDE_VIETNAM", $"{label}: Lat {geo.LatDeg:F6}, Lon {geo.LonDeg:F6} nằm ngoài Việt Nam."));
            return null;
        }
        var grid = _transform.ToVn2000(geo, options.Tm);
        var hint = PlausibilityCheck.Diagnose(grid);
        if (hint is not null)
        {
            issues.Add(new ConversionIssue(IssueSeverity.Error, "OUTSIDE_ZONE",
                $"{label}: E={grid.Easting:F3}, N={grid.Northing:F3} ngoài dải VN-2000 thông thường — KTT {CentralMeridian.Format(options.Tm.CentralMeridianDeg)} có đúng với vị trí này không?"));
            return null;
        }
        return new ImportPoint(label, ToDrawing(grid, metersPerUnit), grid, geo, null);
    }

    /// <summary>Validates the options once; returns metres-per-unit or null after recording the error.</summary>
    private static double? Setup(ConversionOptions options, List<ConversionIssue> issues)
    {
        if (!double.IsFinite(options.Tm.CentralMeridianDeg))
        {
            issues.Add(new ConversionIssue(IssueSeverity.Error, "NO_CENTRAL_MERIDIAN", "Chưa xác định KINH TUYẾN TRỤC. Hãy chọn tỉnh/thành và KTT trước khi nhập."));
            return null;
        }
        if (!options.Tm.IsValid)
        {
            issues.Add(new ConversionIssue(IssueSeverity.Error, "INVALID_PROJECTION", "Thông số hệ chiếu không hợp lệ (k0, FE, FN)."));
            return null;
        }
        if (!double.IsFinite(options.MetersPerDrawingUnit) || options.MetersPerDrawingUnit <= 0)
        {
            issues.Add(new ConversionIssue(IssueSeverity.Error, "UNKNOWN_UNIT", "Chưa xác định đơn vị bản vẽ. Hãy chọn đơn vị."));
            return null;
        }
        return options.MetersPerDrawingUnit;
    }

    private static PlanePoint ToDrawing(PlanePoint gridM, double metersPerUnit) =>
        new(gridM.Easting / metersPerUnit, gridM.Northing / metersPerUnit);
}
