namespace HPGeo.Core.Kml;

/// <summary>What the KMZ contains: survey markers, boundary polygons/lines, or both.</summary>
public enum KmlOutput
{
    Both,
    Points,
    Boundaries,
}

/// <summary>
/// Presentation options for the KML document. Colours are aabbggrr and validated by <see cref="KmlColor"/>.
/// </summary>
public sealed record KmlExportOptions(string DocumentName)
{
    public KmlOutput Output { get; init; } = KmlOutput.Both;
    public string PointColor { get; init; } = KmlColor.DefaultPoint;
    public string LineColor { get; init; } = KmlColor.DefaultLine;

    /// <summary>Document description; the reference tool states the source system and the data vintage.</summary>
    public string Description { get; init; } = "Chuyển đổi từ VN-2000 sang WGS84 · TM-3 · dữ liệu kinh tuyến trục cập nhật 2025";

    public static KmlOutput ParseOutput(string? text) => (text ?? "").Trim().ToLowerInvariant() switch
    {
        "" or "both" or "all" => KmlOutput.Both,
        "points" or "point" or "diem" => KmlOutput.Points,
        "boundaries" or "boundary" or "polygon" or "ranh" => KmlOutput.Boundaries,
        var other => throw new ArgumentException($"Kiểu xuất '{other}' không hợp lệ (both | points | boundaries)."),
    };
}
