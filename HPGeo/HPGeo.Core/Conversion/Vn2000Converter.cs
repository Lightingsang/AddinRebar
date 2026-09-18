using HPGeo.Core.Catalog;
using HPGeo.Core.Model;
using HPGeo.Core.Projection;
using HPGeo.Core.Validation;

namespace HPGeo.Core.Conversion;

/// <summary>
/// Converts survey points and boundaries from a drawing (drawing units, X = Easting) to WGS84 and reports
/// what the user must know: the checks are the reference tool's, in the same order — no central meridian,
/// invalid projection parameters, nothing to convert, a result outside Viet Nam — plus plausibility hints
/// and a size warning. Never mutates its inputs.
/// </summary>
public sealed class Vn2000Converter
{
    private readonly Vn2000Wgs84Transform _transform;

    public Vn2000Converter() : this(Vn2000Wgs84Transform.Default) { }

    public Vn2000Converter(Vn2000Wgs84Transform transform) => _transform = transform;

    public ConversionResult Convert(IReadOnlyList<SurveyPoint> points, IReadOnlyList<BoundaryPolyline> boundaries, ConversionOptions options)
    {
        var issues = new List<ConversionIssue>();
        var convertedPoints = new List<ConvertedPoint>();
        var convertedBoundaries = new List<ConvertedBoundary>();

        var setupError = CheckSetup(points, boundaries, options);
        if (setupError is not null)
        {
            issues.Add(setupError);
            return new ConversionResult(convertedPoints, convertedBoundaries, issues, options);
        }
        if (points.Count > ConversionOptions.PointCountWarningThreshold)
        {
            issues.Add(new ConversionIssue(IssueSeverity.Warning, "MANY_POINTS",
                $"{points.Count} điểm — nhiều hơn {ConversionOptions.PointCountWarningThreshold}; KMZ sẽ lớn và Google Earth có thể chậm."));
        }

        var scale = options.MetersPerDrawingUnit;
        var meridianLabel = CentralMeridian.Format(options.Tm.CentralMeridianDeg);

        foreach (var p in points)
        {
            var grid = new PlanePoint(p.DrawingXY.Easting * scale, p.DrawingXY.Northing * scale);
            var wgs = _transform.ToWgs84(grid, options.Tm);
            var hint = PlausibilityCheck.Diagnose(grid);
            if (hint is not null)
                issues.Add(new ConversionIssue(IssueSeverity.Warning, "IMPLAUSIBLE_EN", $"Điểm {p.Label}: {hint}.", p.Index));
            if (!VietnamEnvelope.Contains(wgs))
                issues.Add(OutsideVietnam(p.Label, p.Index, grid, wgs, meridianLabel));
            convertedPoints.Add(new ConvertedPoint(p, grid, wgs, hint));
        }

        foreach (var b in boundaries)
        {
            if (b.Closed && b.DrawingVertices.Count < 3)
            {
                issues.Add(new ConversionIssue(IssueSeverity.Error, "RING_TOO_SHORT",
                    $"Ranh '{b.Name}' đóng nhưng chỉ có {b.DrawingVertices.Count} đỉnh (cần ≥ 3)."));
                continue;
            }
            if (b.DrawingVertices.Count < 2)
            {
                issues.Add(new ConversionIssue(IssueSeverity.Error, "LINE_TOO_SHORT",
                    $"Ranh '{b.Name}' chỉ có {b.DrawingVertices.Count} đỉnh (cần ≥ 2)."));
                continue;
            }
            var grid = b.DrawingVertices.Select(v => new PlanePoint(v.Easting * scale, v.Northing * scale)).ToList();
            var wgs = grid.Select(g => _transform.ToWgs84(g, options.Tm)).ToList();
            for (var i = 0; i < wgs.Count; i++)
            {
                if (VietnamEnvelope.Contains(wgs[i])) continue;
                issues.Add(OutsideVietnam($"{b.Name} đỉnh {i + 1}", null, grid[i], wgs[i], meridianLabel));
                break; // one message per boundary is enough to tell the user the setup is wrong
            }
            convertedBoundaries.Add(new ConvertedBoundary(b, grid, wgs));
        }

        return new ConversionResult(convertedPoints, convertedBoundaries, issues, options);
    }

    private static ConversionIssue? CheckSetup(IReadOnlyList<SurveyPoint> points, IReadOnlyList<BoundaryPolyline> boundaries, ConversionOptions options)
    {
        if (!double.IsFinite(options.Tm.CentralMeridianDeg))
            return new ConversionIssue(IssueSeverity.Error, "NO_CENTRAL_MERIDIAN",
                "Chưa xác định KINH TUYẾN TRỤC. Hãy chọn tỉnh/thành và KTT đúng của hồ sơ (hoặc nhập KTT thủ công) trước khi chuyển đổi.");
        if (!options.Tm.IsValid)
            return new ConversionIssue(IssueSeverity.Error, "INVALID_PROJECTION", "Thông số hệ chiếu không hợp lệ (k0, FE, FN).");
        if (!double.IsFinite(options.MetersPerDrawingUnit) || options.MetersPerDrawingUnit <= 0)
            return new ConversionIssue(IssueSeverity.Error, "UNKNOWN_UNIT",
                "Chưa xác định đơn vị bản vẽ (INSUNITS = 0 hoặc không hỗ trợ). Hãy chọn đơn vị.");
        if (points.Count == 0 && boundaries.Count == 0)
            return new ConversionIssue(IssueSeverity.Error, "NO_INPUT", "Không có điểm hoặc ranh nào để chuyển đổi.");
        return null;
    }

    private static ConversionIssue OutsideVietnam(string label, int? index, PlanePoint grid, GeoPoint wgs, string meridianLabel) =>
        new(IssueSeverity.Error, "OUTSIDE_VIETNAM",
            $"Kết quả điểm {label} nằm ngoài phạm vi hợp lý của Việt Nam (Lat {wgs.LatDeg:F6}, Lon {wgs.LonDeg:F6}). " +
            $"Tool đang hiểu E={grid.Easting:F3}, N={grid.Northing:F3} và KTT {meridianLabel}. " +
            "Hãy kiểm tra lại thứ tự tọa độ E/N ↔ X/Y, đơn vị bản vẽ và kinh tuyến trục.",
            index);
}
