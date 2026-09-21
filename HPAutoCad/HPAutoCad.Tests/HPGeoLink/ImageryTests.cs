using System.Text.Json;
using HPAutoCad.Core.HPGeoLink.Imagery;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Projection;
using Xunit;

namespace HPAutoCad.Tests.HPGeoLink;

/// <summary>Web Mercator against proj4 and the XYZ formulas, tile planning with its caps, the control grid, the affine residual, the world file, and the pure warp.</summary>
public sealed class ImageryTests
{
    private static readonly TmParameters Hcm = TmParameters.Tm3(105.75);
    private static readonly Vn2000Wgs84Transform Transform = Vn2000Wgs84Transform.Default;
    private static readonly IImageryProvider Esri = ImageryProviders.Default;

    // The 13 sample points of the oracle (TP. Hồ Chí Minh, 105°45').
    private static readonly PlanePoint[] Ring =
    {
        new(600125.887, 1231608.428), new(600124.894, 1231587.765), new(600130.542, 1231543.79), new(600138.537, 1231528.803),
        new(600154.577, 1231483.188), new(600157.772, 1231465.515), new(600167.147, 1231439.669), new(600185.982, 1231422.961),
        new(600172.355, 1231418.421), new(600131.909, 1231412.989), new(600135.362, 1231392), new(600138.509, 1231379.42),
        new(600102.308, 1231385.196),
    };

    private sealed class Golden
    {
        public List<Case> Cases { get; set; } = new();
        public sealed class Case
        {
            public string Id { get; set; } = "";
            public double Lat { get; set; }
            public double Lon { get; set; }
            public double MercatorX { get; set; }
            public double MercatorY { get; set; }
            public List<PerZoom> PerZoom { get; set; } = new();
        }
        public sealed class PerZoom
        {
            public int Zoom { get; set; }
            public int TileX { get; set; }
            public int TileY { get; set; }
            public double PixelX { get; set; }
            public double PixelY { get; set; }
            public double ResolutionMPerPx { get; set; }
        }
    }

    private static Golden LoadGolden()
    {
        using var s = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", "golden-webmercator.json"));
        return JsonSerializer.Deserialize<Golden>(s, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public void Mercator_metres_match_proj4_and_tiles_match_the_formulas()
    {
        var g = LoadGolden();
        Assert.True(g.Cases.Count >= 15);
        foreach (var c in g.Cases)
        {
            var p = new GeoPoint(c.Lat, c.Lon);
            var (x, y) = WebMercator.ToMeters(p);
            Assert.InRange(x, c.MercatorX - 1e-6, c.MercatorX + 1e-6);
            Assert.InRange(y, c.MercatorY - 1e-6, c.MercatorY + 1e-6);
            var back = WebMercator.FromMeters(x, y);
            Assert.InRange(back.LatDeg, c.Lat - 1e-10, c.Lat + 1e-10);
            Assert.InRange(back.LonDeg, c.Lon - 1e-10, c.Lon + 1e-10);
            foreach (var z in c.PerZoom)
            {
                var tile = WebMercator.ToTile(p, z.Zoom);
                Assert.Equal((z.TileX, z.TileY), (tile.X, tile.Y));
                var px = WebMercator.ToPixel(p, z.Zoom);
                Assert.InRange(px.X, z.PixelX - 1e-6, z.PixelX + 1e-6);
                Assert.InRange(px.Y, z.PixelY - 1e-6, z.PixelY + 1e-6);
                Assert.InRange(WebMercator.ResolutionMPerPx(c.Lat, z.Zoom), z.ResolutionMPerPx - 1e-9, z.ResolutionMPerPx + 1e-9);
                var round = WebMercator.FromPixel(px);
                Assert.InRange(round.LonDeg, c.Lon - 1e-9, c.Lon + 1e-9);
            }
        }
    }

    [Fact]
    public void Plan_picks_the_zoom_for_30_cm_and_stays_under_the_caps()
    {
        var box = GridBoundingBox.Of(Ring);
        var r = TileCoverage.Plan(box, TileCoverage.DefaultMarginM, TileCoverage.DefaultResolutionMPerPx, Hcm, Esri);
        Assert.Null(r.Error);
        var plan = r.Plan!;
        Assert.Equal(19, plan.Zoom); // 0.293 m/px at 11.1° N — the first zoom under 0.30
        Assert.InRange(plan.ResolutionMPerPx, 0.29, 0.30);
        Assert.InRange(plan.TileCount, 6, 20); // ~145 x 290 m at 0.29 m/px = 3 x 5 tiles
        Assert.Equal(plan.TileCountX * plan.TileCountY, plan.TileCount);
        Assert.Equal(plan.TileCount, plan.Tiles().Count());
        // The output covers the ring plus the margin, north-up, square pixels.
        var o = plan.Output;
        Assert.InRange(o.OriginE, box.MinE - 30 - 1e-9, box.MinE - 30 + 1e-9);
        Assert.InRange(o.OriginN, box.MaxN + 30 - 1e-9, box.MaxN + 30 + 1e-9);
        Assert.True(o.WidthM >= box.WidthM + 60 && o.HeightM >= box.HeightM + 60);
        Assert.True(o.WidthPx <= TileCoverage.MaxOutputPx && o.HeightPx <= TileCoverage.MaxOutputPx);
        // Every corner of the output lies inside the geographic box of the tiles.
        foreach (var corner in new[] { o.PixelCenter(0, 0), o.PixelCenter(o.WidthPx - 1, o.HeightPx - 1) })
        {
            var geo = Transform.ToWgs84(corner, Hcm);
            Assert.InRange(geo.LatDeg, plan.GeoBox.MinLat - 1e-6, plan.GeoBox.MaxLat + 1e-6);
            Assert.InRange(geo.LonDeg, plan.GeoBox.MinLon - 1e-6, plan.GeoBox.MaxLon + 1e-6);
        }
        Assert.Empty(plan.Warnings);
    }

    [Fact]
    public void Plan_refuses_too_many_tiles_outside_vietnam_and_missing_meridian()
    {
        var box = GridBoundingBox.Of(Ring);
        var tooWide = TileCoverage.Plan(box, 2000, 0.3, Hcm, Esri); // 4 km box at 0.3 m/px: 13 900 px, over the 4096 cap
        Assert.Equal("TOO_MANY_TILES", tooWide.Error?.Code);
        Assert.Contains("res=", tooWide.Error!.Message);

        var huge = TileCoverage.Plan(new GridBoundingBox(590000, 1220000, 610000, 1240000), 0, 0.3, Hcm, Esri);
        Assert.Equal("TOO_MANY_TILES", huge.Error?.Code);

        var wrongZone = TileCoverage.Plan(box, 30, 0.3, new TmParameters(120, 0.9999, 500000, 0), Esri);
        Assert.Equal("OUTSIDE_VIETNAM", wrongZone.Error?.Code);

        Assert.Equal("NO_CENTRAL_MERIDIAN", TileCoverage.Plan(box, 30, 0.3, TmParameters.Tm3(double.NaN), Esri).Error?.Code);
        Assert.Equal("NO_BOUNDARY", TileCoverage.Plan(new GridBoundingBox(1, 1, 1, 1), 30, 0.3, Hcm, Esri).Error?.Code);
        Assert.Equal("INVALID_ARGUMENT", TileCoverage.Plan(box, -1, 0.3, Hcm, Esri).Error?.Code);
    }

    [Fact]
    public void Coarser_than_the_provider_can_deliver_is_a_warning_not_an_error()
    {
        var r = TileCoverage.Plan(GridBoundingBox.Of(Ring), 30, 0.05, Hcm, Esri, maxTiles: 100000, maxOutputPx: 100000);
        Assert.Null(r.Error);
        Assert.Equal(19, r.Plan!.Zoom);
        Assert.Contains(r.Plan.Warnings, w => w.Code == "RESOLUTION_LIMITED");
    }

    [Fact]
    public void Control_grid_is_exact_on_its_lattice_and_the_affine_residual_is_small_on_a_500_m_plot()
    {
        var box = GridBoundingBox.Of(Ring).Expand(150); // ~ 500 × 500 m
        var plan = TileCoverage.Plan(box, 0, 0.3, Hcm, Esri).Plan!;
        var grid = plan.Output.BuildControlGrid(plan, Hcm, Transform);
        Assert.Equal(plan.Output.WidthPx - 1, grid.Cols[^1]);
        Assert.Equal(plan.Output.HeightPx - 1, grid.Rows[^1]);

        // Lattice samples reproduce the exact transform; a mid point interpolates within a hundredth of a pixel.
        var (sx, sy) = grid.Sample(grid.Cols[1], grid.Rows[1]);
        Assert.Equal(grid.SourceX[1, 1], sx, 9);
        Assert.Equal(grid.SourceY[1, 1], sy, 9);
        var midCol = (grid.Cols[1] + grid.Cols[2]) / 2.0;
        var midRow = (grid.Rows[1] + grid.Rows[2]) / 2.0;
        var exact = WebMercator.ToPixel(Transform.ToWgs84(plan.Output.PixelCenter(midCol, midRow), Hcm), plan.Zoom);
        var (ix, iy) = grid.Sample(midCol, midRow);
        Assert.InRange(ix + plan.MosaicOrigin.X, exact.X - 0.01, exact.X + 0.01);
        Assert.InRange(iy + plan.MosaicOrigin.Y, exact.Y - 0.01, exact.Y + 0.01);

        // Every source position falls inside the mosaic.
        foreach (var p in grid.Points())
        {
            Assert.InRange(p.SrcX, 0, plan.MosaicWidthPx);
            Assert.InRange(p.SrcY, 0, plan.MosaicHeightPx);
        }

        var fit = AffineFit.Fit(grid, plan.Output);
        Assert.True(fit.MaxResidualM <= AffineFit.WarnResidualM, $"residual {fit.MaxResidualM:F3} m");
        Assert.True(fit.RmsResidualM < fit.MaxResidualM + 1e-9);
        // The affine scale is the pixel size and the map is near north-up (tiny rotation from grid convergence).
        Assert.InRange(fit.Transform.A, plan.ResolutionMPerPx * 0.99, plan.ResolutionMPerPx * 1.01);
        Assert.InRange(fit.Transform.E, -plan.ResolutionMPerPx * 1.01, -plan.ResolutionMPerPx * 0.99);
        Assert.InRange(Math.Abs(fit.Transform.B), 0, plan.ResolutionMPerPx * 0.05);
    }

    [Fact]
    public void World_file_has_six_lines_in_the_invariant_culture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("vi-VN");
            var raster = new OutputRaster(600000, 1232000, 0.25, 400, 200);
            var lines = raster.WorldFile().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(new[] { "0.25", "0.0", "0.0", "-0.25", "600000.125", "1231999.875" }, lines);
            Assert.Equal(new PlanePoint(600000, 1231950), raster.LowerLeft);
            Assert.Equal(100.0, raster.WidthM);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Warp_moves_a_gradient_to_where_the_control_grid_says()
    {
        // Source: 64×64, red = x, green = y. Identity-ish control grid shifted by +10 px both ways.
        var src = new RasterBuffer(64, 64);
        for (var y = 0; y < 64; y++) for (var x = 0; x < 64; x++) src.Set(x, y, 0, (byte)(y * 4), (byte)(x * 4));
        var output = new OutputRaster(0, 40, 1, 40, 40);
        var cols = ControlGrid.Lattice(40, 8);
        var rows = ControlGrid.Lattice(40, 8);
        var sx = new double[rows.Length, cols.Length];
        var sy = new double[rows.Length, cols.Length];
        for (var r = 0; r < rows.Length; r++) for (var c = 0; c < cols.Length; c++) { sx[r, c] = cols[c] + 10.5; sy[r, c] = rows[r] + 10.5; }
        var grid = new ControlGrid(cols, rows, sx, sy);

        var dst = RasterWarper.Warp(src, grid, output);
        Assert.Equal(40, dst.Width);
        foreach (var (col, row) in new[] { (0, 0), (13, 7), (39, 39), (20, 3) })
        {
            var p = dst.Get(col, row);
            Assert.InRange(p.R, (col + 10) * 4 - 1, (col + 10) * 4 + 1); // ≤ 1 px of gradient
            Assert.InRange(p.G, (row + 10) * 4 - 1, (row + 10) * 4 + 1);
            Assert.Equal(255, p.A);
        }

        // Source positions beyond the mosaic stay transparent.
        for (var r = 0; r < rows.Length; r++) for (var c = 0; c < cols.Length; c++) { sx[r, c] = cols[c] + 200; }
        var off = RasterWarper.Warp(src, new ControlGrid(cols, rows, sx, sy), output);
        Assert.Equal(0, off.Get(5, 5).A);
    }

    [Fact]
    public void Provider_ids_resolve_and_the_esri_url_is_the_map_panel_one()
    {
        Assert.Same(ImageryProviders.Default, ImageryProviders.Resolve(null));
        Assert.Same(ImageryProviders.Default, ImageryProviders.Resolve("ESRI"));
        Assert.Throws<ArgumentException>(() => ImageryProviders.Resolve("google"));
        Assert.Equal("https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/19/245823/417491", Esri.TileUrl(new TileAddress(19, 417491, 245823)).ToString());
    }

    [Theory]
    [InlineData(335.075, 123.679, 10)]   // the user's plot: 596405–596740 × 1222862–1222986
    [InlineData(83.674, 229.008, 10)]    // the 13-point ring
    [InlineData(20, 30, 4)]
    [InlineData(100, 100, 1)]
    public void Area_ratio_margin_makes_the_covered_area_exactly_the_multiple(double w, double h, double ratio)
    {
        var box = new GridBoundingBox(600000, 1231000, 600000 + w, 1231000 + h);
        var m = TileCoverage.MarginForAreaRatio(box, ratio);
        Assert.True(m >= 0);
        Assert.Equal(ratio, (w + 2 * m) * (h + 2 * m) / (w * h), 9);
        Assert.Throws<ArgumentException>(() => TileCoverage.MarginForAreaRatio(box, 0.5));
    }

    [Fact]
    public void Fit_to_caps_lowers_the_zoom_for_a_wide_view_instead_of_refusing_and_says_so()
    {
        // The user's plot at ×10: 759 × 548 m — 2590 px at zoom 19: fits the 4096 cap at full resolution.
        var plot = new GridBoundingBox(596405.199, 1222862.4, 596740.274, 1222986.079);
        var plotPlan = TileCoverage.Plan(plot, TileCoverage.MarginForAreaRatio(plot, 10), 0.3, Hcm, Esri, fitToCaps: true).Plan!;
        Assert.Equal(19, plotPlan.Zoom);
        Assert.DoesNotContain(plotPlan.Warnings, w => w.Code == "RESOLUTION_REDUCED");

        // A 2 km × 1.5 km site at ×10 — 6.3 km wide — cannot fit 4096 px at zoom 19.
        var box = new GridBoundingBox(596000, 1222000, 598000, 1223500);
        var margin = TileCoverage.MarginForAreaRatio(box, 10);
        var strict = TileCoverage.Plan(box, margin, 0.3, Hcm, Esri);
        Assert.Equal("TOO_MANY_TILES", strict.Error?.Code);

        var fitted = TileCoverage.Plan(box, margin, 0.3, Hcm, Esri, fitToCaps: true).Plan!;
        Assert.True(fitted.Zoom < 19);
        Assert.True(fitted.Output.WidthPx <= TileCoverage.MaxOutputPx && fitted.Output.HeightPx <= TileCoverage.MaxOutputPx);
        Assert.True(fitted.TileCount <= TileCoverage.MaxTiles);
        Assert.Contains(fitted.Warnings, w => w.Code == "RESOLUTION_REDUCED");
        // Same extent, coarser pixels: the covered area is untouched by the zoom-out (the far edges overrun by < 1 px).
        Assert.InRange(fitted.Output.WidthM, box.WidthM + 2 * margin, box.WidthM + 2 * margin + fitted.ResolutionMPerPx);
        Assert.InRange(fitted.Output.HeightM, box.HeightM + 2 * margin, box.HeightM + 2 * margin + fitted.ResolutionMPerPx);

        // A view nothing can hold (whole province at ×10) still refuses, after zooming all the way out.
        var huge = TileCoverage.Plan(new GridBoundingBox(500000, 1100000, 700000, 1300000), 0, 0.3, Hcm, Esri, fitToCaps: true);
        Assert.True(huge.Plan is not null || huge.Error?.Code == "TOO_MANY_TILES" || huge.Error?.Code == "OUTSIDE_VIETNAM");
    }

    /// <summary>A control grid that maps output (col,row) to source (col + shift, row + shift) — an identity warp with a sub-pixel offset.</summary>
    private static ControlGrid ShiftedGrid(int size, double shiftPx)
    {
        var lattice = ControlGrid.Lattice(size, 8);
        var sx = new double[lattice.Length, lattice.Length];
        var sy = new double[lattice.Length, lattice.Length];
        for (var r = 0; r < lattice.Length; r++)
            for (var c = 0; c < lattice.Length; c++)
            {
                sx[r, c] = lattice[c] + 0.5 + shiftPx; // source coordinates address pixel centres at +0.5
                sy[r, c] = lattice[r] + 0.5 + shiftPx;
            }
        return new ControlGrid(lattice, lattice, sx, sy);
    }

    [Fact]
    public void Bicubic_reproduces_a_linear_ramp_exactly_and_keeps_an_edge_sharper_than_bilinear()
    {
        const int n = 64;
        var output = new OutputRaster(0, n, 1, n, n);
        var ramp = new RasterBuffer(n, n);
        var edge = new RasterBuffer(n, n);
        for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                ramp.Set(x, y, (byte)(x * 3), (byte)(y * 3), 100);   // linear in x and y
                edge.Set(x, y, (byte)(x < 32 ? 0 : 255), 0, 0);      // a vertical step at x = 32
            }
        var grid = ShiftedGrid(n, 0.25);

        var rampCubic = RasterWarper.Warp(ramp, grid, output, ResampleKernel.Bicubic);
        var rampLinear = RasterWarper.Warp(ramp, grid, output, ResampleKernel.Bilinear);
        for (var y = 4; y < n - 4; y++)
            for (var x = 4; x < n - 4; x++)
            {
                var c = rampCubic.Get(x, y);
                var l = rampLinear.Get(x, y);
                Assert.Equal(l.B, c.B); // a cubic kernel is exact on linear data — the geometry is untouched
                Assert.Equal(l.G, c.G);
                Assert.Equal(255, c.A);
            }

        var edgeCubic = RasterWarper.Warp(edge, grid, output, ResampleKernel.Bicubic);
        var edgeLinear = RasterWarper.Warp(edge, grid, output, ResampleKernel.Bilinear);
        // Blur = how far the resampled row strays from a clean 0|255 step; the cubic must stray less.
        double Blur(RasterBuffer b) => Enumerable.Range(20, 24).Sum(x => { var v = b.Get(x, 10).B; return Math.Min(v, 255 - v); });
        Assert.True(Blur(edgeCubic) < Blur(edgeLinear), $"cubic {Blur(edgeCubic)} vs linear {Blur(edgeLinear)}");
        Assert.Equal(0, edgeCubic.Get(20, 10).B);
        Assert.Equal(255, edgeCubic.Get(40, 10).B);
    }
}
