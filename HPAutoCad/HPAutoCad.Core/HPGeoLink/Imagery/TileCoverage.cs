using HPAutoCad.Core.HPGeoLink.Conversion;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Projection;
using HPAutoCad.Core.HPGeoLink.Validation;

namespace HPAutoCad.Core.HPGeoLink.Imagery;

/// <summary>Axis-aligned extent in VN-2000 metres.</summary>
public readonly record struct GridBoundingBox(double MinE, double MinN, double MaxE, double MaxN)
{
    public double WidthM => MaxE - MinE;
    public double HeightM => MaxN - MinN;
    public GridBoundingBox Expand(double marginM) => new(MinE - marginM, MinN - marginM, MaxE + marginM, MaxN + marginM);

    public static GridBoundingBox Of(IEnumerable<PlanePoint> points)
    {
        double minE = double.PositiveInfinity, minN = double.PositiveInfinity, maxE = double.NegativeInfinity, maxN = double.NegativeInfinity;
        foreach (var p in points)
        {
            minE = Math.Min(minE, p.Easting); maxE = Math.Max(maxE, p.Easting);
            minN = Math.Min(minN, p.Northing); maxN = Math.Max(maxN, p.Northing);
        }
        return new GridBoundingBox(minE, minN, maxE, maxN);
    }
}

/// <summary>Axis-aligned extent in WGS84 degrees.</summary>
public readonly record struct GeoBoundingBox(double MinLat, double MinLon, double MaxLat, double MaxLon)
{
    public GeoPoint Center => new((MinLat + MaxLat) / 2, (MinLon + MaxLon) / 2);
}

/// <summary>
/// Everything one imagery run needs, decided before a single tile is fetched: the zoom, the tile range, the
/// mosaic size, and the output raster on the VN-2000 grid the mosaic will be warped onto.
/// </summary>
public sealed record TilePlan(
    int Zoom,
    int XMin, int XMax, int YMin, int YMax,
    double ResolutionMPerPx,
    GeoBoundingBox GeoBox,
    OutputRaster Output,
    IReadOnlyList<ConversionIssue> Warnings)
{
    public int TileCountX => XMax - XMin + 1;
    public int TileCountY => YMax - YMin + 1;
    public int TileCount => TileCountX * TileCountY;
    public int MosaicWidthPx => TileCountX * WebMercator.TileSizePx;
    public int MosaicHeightPx => TileCountY * WebMercator.TileSizePx;
    public GlobalPixel MosaicOrigin => WebMercator.TileOrigin(new TileAddress(Zoom, XMin, YMin));

    public IEnumerable<TileAddress> Tiles()
    {
        for (var y = YMin; y <= YMax; y++)
            for (var x = XMin; x <= XMax; x++)
                yield return new TileAddress(Zoom, x, y);
    }
}

/// <summary>
/// Plans the tile download for a boundary: expands its extent by the margin, converts the corners to WGS84,
/// picks the smallest zoom whose native resolution meets the target, and refuses anything past the caps —
/// a run that would fetch hundreds of tiles is a mistake, never a slow success.
/// </summary>
public static class TileCoverage
{
    public const double DefaultResolutionMPerPx = 0.30;
    public const double DefaultMarginM = 30;
    /// <summary>The image covers this many times the boundary's bounding-box area unless a margin in metres is given: the plot in its surroundings.</summary>
    public const double DefaultAreaRatio = 10;
    /// <summary>
    /// Output cap 4096 px a side (a 64 MB BGRA buffer, ~1.2 km at Esri's finest 0.29 m/px): wide enough for a plot in
    /// its surroundings at full resolution — 2048 forced a zoom-out that made the imagery visibly softer than the
    /// source. Tile cap sized to the mosaic that cap can need (18 × 18 tiles plus slack).
    /// </summary>
    public const int MaxTiles = 1024;
    public const int MaxOutputPx = 4096;

    public sealed record PlanResult(TilePlan? Plan, ConversionIssue? Error);

    /// <summary>
    /// The margin (metres, same on every side) that makes the covered area <paramref name="areaRatio"/> times the
    /// box's own: the positive root of (W + 2m)(H + 2m) = r·W·H. A ratio of 1 is no margin.
    /// </summary>
    public static double MarginForAreaRatio(GridBoundingBox boxM, double areaRatio)
    {
        if (!(areaRatio >= 1) || !double.IsFinite(areaRatio)) throw new ArgumentException("Tỉ lệ diện tích phải ≥ 1.");
        var w = boxM.WidthM;
        var h = boxM.HeightM;
        if (!(w > 0) || !(h > 0)) return 0;
        var sum = w + h;
        return (-sum + Math.Sqrt(sum * sum + 4 * (areaRatio - 1) * w * h)) / 4;
    }

    /// <summary>
    /// Plans the download. With <paramref name="fitToCaps"/> the zoom is lowered until the tile and pixel caps
    /// hold — a wide view at a coarser resolution, with a RESOLUTION_REDUCED warning — instead of refusing;
    /// without it (an explicit res= / zoom=) the caps refuse with TOO_MANY_TILES.
    /// </summary>
    public static PlanResult Plan(GridBoundingBox boundaryM, double marginM, double targetResolutionMPerPx, TmParameters tm,
        IImageryProvider provider, Vn2000Wgs84Transform? transform = null, int maxTiles = MaxTiles, int maxOutputPx = MaxOutputPx, bool fitToCaps = false)
    {
        transform ??= Vn2000Wgs84Transform.Default;
        var warnings = new List<ConversionIssue>();
        if (!tm.IsValid || !double.IsFinite(tm.CentralMeridianDeg))
            return Fail("NO_CENTRAL_MERIDIAN", "Chưa xác định KINH TUYẾN TRỤC — chọn tỉnh/KTT trước khi lấy ảnh.");
        if (boundaryM.WidthM <= 0 || boundaryM.HeightM <= 0 || !double.IsFinite(boundaryM.WidthM + boundaryM.HeightM))
            return Fail("NO_BOUNDARY", "Ranh không có diện tích (bbox rỗng).");
        if (!(marginM >= 0) || !(targetResolutionMPerPx > 0))
            return Fail("INVALID_ARGUMENT", "Biên phải ≥ 0 và độ phân giải (m/px) phải > 0.");

        var extent = boundaryM.Expand(marginM);
        var corners = new[]
        {
            new PlanePoint(extent.MinE, extent.MinN), new PlanePoint(extent.MaxE, extent.MinN),
            new PlanePoint(extent.MaxE, extent.MaxN), new PlanePoint(extent.MinE, extent.MaxN),
        };
        var geo = corners.Select(c => transform.ToWgs84(c, tm)).ToList();
        if (geo.Any(g => !VietnamEnvelope.Contains(g)))
        {
            var bad = geo.First(g => !VietnamEnvelope.Contains(g));
            return Fail("OUTSIDE_VIETNAM", $"Vùng ảnh rơi ngoài Việt Nam (Lat {bad.LatDeg:F5}, Lon {bad.LonDeg:F5}) — kiểm tra KTT {Catalog.CentralMeridian.Format(tm.CentralMeridianDeg)} và đơn vị bản vẽ.");
        }
        var geoBox = new GeoBoundingBox(geo.Min(g => g.LatDeg), geo.Min(g => g.LonDeg), geo.Max(g => g.LatDeg), geo.Max(g => g.LonDeg));
        var centerLat = geoBox.Center.LatDeg;

        var zoom = 0;
        while (zoom < provider.MaxZoom && WebMercator.ResolutionMPerPx(centerLat, zoom) > targetResolutionMPerPx) zoom++;
        var resolution = WebMercator.ResolutionMPerPx(centerLat, zoom);
        if (resolution > targetResolutionMPerPx * 1.001)
            warnings.Add(new ConversionIssue(IssueSeverity.Warning, "RESOLUTION_LIMITED",
                $"Nguồn ảnh chỉ tới zoom {provider.MaxZoom} ({resolution:F2} m/px) — thô hơn {targetResolutionMPerPx:F2} m/px yêu cầu."));

        var requestedZoom = zoom;
        TileAddress topLeft, bottomRight;
        int tileCount, widthPx, heightPx;
        while (true)
        {
            topLeft = WebMercator.ToTile(new GeoPoint(geoBox.MaxLat, geoBox.MinLon), zoom);
            bottomRight = WebMercator.ToTile(new GeoPoint(geoBox.MinLat, geoBox.MaxLon), zoom);
            tileCount = (bottomRight.X - topLeft.X + 1) * (bottomRight.Y - topLeft.Y + 1);
            widthPx = (int)Math.Ceiling(extent.WidthM / resolution);
            heightPx = (int)Math.Ceiling(extent.HeightM / resolution);
            var fits = tileCount <= maxTiles && widthPx <= maxOutputPx && heightPx <= maxOutputPx;
            if (fits || !fitToCaps || zoom == 0) break;
            zoom--; // one level out: half the pixels per side, a quarter of the tiles
            resolution = WebMercator.ResolutionMPerPx(centerLat, zoom);
        }
        if (tileCount > maxTiles || widthPx > maxOutputPx || heightPx > maxOutputPx)
            return Fail("TOO_MANY_TILES",
                $"Vùng {extent.WidthM:F0} × {extent.HeightM:F0} m ở {resolution:F2} m/px cần {tileCount} tile và ảnh {widthPx} × {heightPx} px — vượt giới hạn {maxTiles} tile / {maxOutputPx} px. Tăng res= (m/px) hoặc chọn ranh nhỏ hơn.");
        if (zoom < requestedZoom)
            warnings.Add(new ConversionIssue(IssueSeverity.Warning, "RESOLUTION_REDUCED",
                $"Vùng {extent.WidthM:F0} × {extent.HeightM:F0} m quá rộng cho {WebMercator.ResolutionMPerPx(centerLat, requestedZoom):F2} m/px — hạ xuống zoom {zoom} ({resolution:F2} m/px) để nằm trong giới hạn {maxTiles} tile / {maxOutputPx} px."));

        var output = new OutputRaster(extent.MinE, extent.MaxN, resolution, widthPx, heightPx);
        return new PlanResult(new TilePlan(zoom, topLeft.X, bottomRight.X, topLeft.Y, bottomRight.Y, resolution, geoBox, output, warnings), null);

        PlanResult Fail(string code, string message) => new(null, new ConversionIssue(IssueSeverity.Error, code, message));
    }
}
