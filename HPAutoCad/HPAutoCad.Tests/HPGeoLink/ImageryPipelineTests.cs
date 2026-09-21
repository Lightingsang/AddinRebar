using System.Net;
using System.Net.Http;
using HPAutoCad.HPGeoLink.Imagery;
using HPAutoCad.HPGeoLink.Support;
using HPAutoCad.Core.HPGeoLink.Imagery;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Projection;
using Xunit;

namespace HPAutoCad.Tests.HPGeoLink;

/// <summary>
/// The fetch → stitch → warp chain over a stub tile server (no network), plus the one live spike that talks
/// to the real provider when HPGEO_LIVE_TILES=1 (writes output/spike/, logs the gate line).
/// </summary>
public sealed class ImageryPipelineTests
{
    private static readonly TmParameters Hcm = TmParameters.Tm3(105.75);
    private static readonly IImageryProvider Esri = ImageryProviders.Default;
    private static readonly PlanePoint[] Ring =
    {
        new(600125.887, 1231608.428), new(600124.894, 1231587.765), new(600130.542, 1231543.79), new(600138.537, 1231528.803),
        new(600154.577, 1231483.188), new(600157.772, 1231465.515), new(600167.147, 1231439.669), new(600185.982, 1231422.961),
        new(600172.355, 1231418.421), new(600131.909, 1231412.989), new(600135.362, 1231392), new(600138.509, 1231379.42),
        new(600102.308, 1231385.196),
    };

    /// <summary>Serves a PNG per tile whose pixels encode the global pixel position: R = x mod 256, G = x / 256 mod 256, B = y mod 256.</summary>
    private sealed class StubTileHandler : HttpMessageHandler
    {
        public int Requests;
        public HashSet<(int X, int Y)> Missing { get; } = new();
        public HttpStatusCode MissingStatus { get; set; } = HttpStatusCode.NotFound;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Requests);
            var parts = request.RequestUri!.AbsolutePath.Split('/');
            var z = int.Parse(parts[^3]); var y = int.Parse(parts[^2]); var x = int.Parse(parts[^1]);
            if (Missing.Contains((x, y))) return Task.FromResult(new HttpResponseMessage(MissingStatus));
            var tile = new RasterBuffer(256, 256);
            for (var py = 0; py < 256; py++)
                for (var px = 0; px < 256; px++)
                {
                    var gx = x * 256 + px;
                    var gy = y * 256 + py;
                    tile.Set(px, py, (byte)(gy % 256), (byte)((gx / 256) % 256), (byte)(gx % 256));
                }
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(TileStitcher.EncodePng(tile)) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            return Task.FromResult(response);
        }
    }

    private static string TempCache() => Path.Combine(Path.GetTempPath(), "hpgeo-tiles-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Tiles_are_fetched_in_parallel_cached_and_stitched_at_the_right_offsets()
    {
        var plan = TileCoverage.Plan(GridBoundingBox.Of(Ring), 30, 0.3, Hcm, Esri).Plan!;
        var handler = new StubTileHandler();
        var cache = TempCache();
        try
        {
            var fetcher = new TileFetcher(handler, cache);
            var tiles = plan.Tiles().ToList();
            var progress = new List<int>();
            var first = await fetcher.FetchAsync(Esri, tiles, new Progress<int>(progress.Add), TestContext.Current.CancellationToken);
            Assert.True(first.Complete);
            Assert.Equal(tiles.Count, first.Tiles.Count);
            Assert.Equal(0, first.FromCache);
            Assert.Equal(tiles.Count, handler.Requests);

            var second = await fetcher.FetchAsync(Esri, tiles, null, TestContext.Current.CancellationToken);
            Assert.Equal(tiles.Count, second.FromCache);
            Assert.Equal(tiles.Count, handler.Requests); // nothing re-downloaded
            Assert.True(TileCache.SizeBytes(cache) > 0);

            var mosaic = TileStitcher.Stitch(plan, first.Tiles);
            Assert.Equal(plan.MosaicWidthPx, mosaic.Width);
            // Pixel (300, 70) of the mosaic is global pixel (origin + 300, origin + 70).
            var origin = plan.MosaicOrigin;
            var p = mosaic.Get(300, 70);
            var gx = (int)origin.X + 300;
            var gy = (int)origin.Y + 70;
            Assert.Equal(gx % 256, p.R);
            Assert.Equal((gx / 256) % 256, p.G);
            Assert.Equal(gy % 256, p.B);
            Assert.Equal(255, p.A);
        }
        finally
        {
            if (Directory.Exists(cache)) Directory.Delete(cache, true);
        }
    }

    [Fact]
    public async Task A_missing_tile_is_reported_and_403_is_not_retried()
    {
        var plan = TileCoverage.Plan(GridBoundingBox.Of(Ring), 30, 0.3, Hcm, Esri).Plan!;
        var handler = new StubTileHandler { MissingStatus = HttpStatusCode.Forbidden };
        handler.Missing.Add((plan.XMin, plan.YMin));
        var cache = TempCache();
        try
        {
            var result = await new TileFetcher(handler, cache).FetchAsync(Esri, plan.Tiles().ToList(), null, TestContext.Current.CancellationToken);
            Assert.False(result.Complete);
            var failure = Assert.Single(result.Failures);
            Assert.Equal(new TileAddress(plan.Zoom, plan.XMin, plan.YMin), failure.Tile);
            Assert.Contains("403", failure.Reason);
            Assert.Contains("/World_Imagery/MapServer/tile/", failure.Url.ToString());
            Assert.Equal(plan.TileCount, handler.Requests); // one request for the 403 tile, no retries
        }
        finally
        {
            if (Directory.Exists(cache)) Directory.Delete(cache, true);
        }
    }

    [Fact]
    public async Task Warped_output_puts_each_output_pixel_at_the_control_grid_source_within_one_pixel()
    {
        var plan = TileCoverage.Plan(GridBoundingBox.Of(Ring), 30, 0.3, Hcm, Esri).Plan!;
        var cache = TempCache();
        try
        {
            var fetched = await new TileFetcher(new StubTileHandler(), cache).FetchAsync(Esri, plan.Tiles().ToList(), null, TestContext.Current.CancellationToken);
            var mosaic = TileStitcher.Stitch(plan, fetched.Tiles);
            var grid = plan.Output.BuildControlGrid(plan, Hcm, Vn2000Wgs84Transform.Default);
            var warped = RasterWarper.Warp(mosaic, grid, plan.Output);
            Assert.Equal(plan.Output.WidthPx, warped.Width);
            Assert.Equal(plan.Output.HeightPx, warped.Height);

            foreach (var (col, row) in new[] { (0, 0), (plan.Output.WidthPx / 2, plan.Output.HeightPx / 3), (plan.Output.WidthPx - 1, plan.Output.HeightPx - 1), (17, plan.Output.HeightPx - 5) })
            {
                var expected = WebMercator.ToPixel(Vn2000Wgs84Transform.Default.ToWgs84(plan.Output.PixelCenter(col, row), Hcm), plan.Zoom);
                // The stub encodes the *global* pixel: x in R + 256·G (mod 65536), y in B (mod 256).
                var ex = expected.X % 65536;
                var ey = expected.Y % 256;
                var p = warped.Get(col, row);
                Assert.Equal(255, p.A);
                var decodedX = p.R + 256 * p.G;
                var decodedY = p.B;
                // The stub encodes the integer pixel; the warp samples bilinearly at the fractional centre → ≤ 1 px (wrap-safe).
                var dx = Math.Abs(decodedX - ex);
                Assert.True(dx <= 1.0 || Math.Abs(dx - 65536) <= 1.0, $"({col},{row}): x {decodedX} vs {ex:F2}");
                var dy = Math.Abs(decodedY - ey);
                Assert.True(dy <= 1.0 || Math.Abs(dy - 256) <= 1.0, $"({col},{row}): y {decodedY} vs {ey:F2}");
            }
        }
        finally
        {
            if (Directory.Exists(cache)) Directory.Delete(cache, true);
        }
    }

    /// <summary>A captive portal: HTTP 200 with an HTML page for every tile.</summary>
    private sealed class PortalHandler : HttpMessageHandler
    {
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Interlocked.Increment(ref Requests);
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html><body>Sign in to the network</body></html>") };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
            return Task.FromResult(response);
        }
    }

    /// <summary>A black hole: the request never completes until the token cancels it.</summary>
    private sealed class HangingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, ct);
            throw new InvalidOperationException("unreachable");
        }
    }

    [Fact]
    public async Task A_portal_page_is_a_reported_failure_never_cached_and_not_retried()
    {
        var plan = TileCoverage.Plan(GridBoundingBox.Of(Ring), 30, 0.3, Hcm, Esri).Plan!;
        var handler = new PortalHandler();
        var cache = TempCache();
        try
        {
            var result = await new TileFetcher(handler, cache).FetchAsync(Esri, plan.Tiles().ToList(), null, TestContext.Current.CancellationToken);
            Assert.Equal(plan.TileCount, result.Failures.Count);
            Assert.Contains("not an image", result.Failures[0].Reason);
            Assert.Contains("text/html", result.Failures[0].Reason);
            Assert.Equal(plan.TileCount, handler.Requests); // one request per tile, no retry on a served page
            Assert.Equal(0, TileCache.SizeBytes(cache));
        }
        finally
        {
            if (Directory.Exists(cache)) Directory.Delete(cache, true);
        }
    }

    [Fact]
    public async Task A_poisoned_or_truncated_cache_file_is_evicted_and_fetched_again()
    {
        var plan = TileCoverage.Plan(GridBoundingBox.Of(Ring), 30, 0.3, Hcm, Esri).Plan!;
        var tile = plan.Tiles().First();
        var handler = new StubTileHandler();
        var cache = TempCache();
        try
        {
            var fetcher = new TileFetcher(handler, cache);
            var poisoned = fetcher.CachePath(Esri, tile);
            Directory.CreateDirectory(Path.GetDirectoryName(poisoned)!);
            await File.WriteAllTextAsync(poisoned, "<html>portal</html>", TestContext.Current.CancellationToken);

            var result = await fetcher.FetchAsync(Esri, new[] { tile }, null, TestContext.Current.CancellationToken);
            Assert.True(result.Complete);
            Assert.Equal(0, result.FromCache);
            Assert.Equal(1, handler.Requests);
            Assert.True(TileCache.LooksLikeImage(await File.ReadAllBytesAsync(poisoned, TestContext.Current.CancellationToken)), "the cache now holds the real tile");
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(poisoned)!, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(cache)) Directory.Delete(cache, true);
        }
    }

    [Fact]
    public async Task Cancelling_the_token_ends_a_hanging_download_as_a_cancellation()
    {
        var plan = TileCoverage.Plan(GridBoundingBox.Of(Ring), 30, 0.3, Hcm, Esri).Plan!;
        var cache = TempCache();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(300));
            var started = DateTime.UtcNow;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new TileFetcher(new HangingHandler(), cache).FetchAsync(Esri, plan.Tiles().ToList(), null, cts.Token));
            Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(10), "cancellation must not wait for the 15 s HTTP timeout");
        }
        finally
        {
            if (Directory.Exists(cache)) Directory.Delete(cache, true);
        }
    }

    [Theory]
    [InlineData(1, 60)]
    [InlineData(15, 60)]
    [InlineData(50, 150)]
    [InlineData(400, 300)]
    public void The_download_deadline_scales_with_the_tile_count_between_one_and_five_minutes(int tiles, int expectedSeconds) =>
        Assert.Equal(expectedSeconds, ImageryPipeline.DeadlineFor(tiles).TotalSeconds);

    /// <summary>
    /// Fills the user's default tile cache with the acceptance plan (the 13-point ring, margin 30 m, res 0.5 → z19,
    /// 15 tiles) so that tools/acceptance.ps1 can exercise the insert chain even where acad.exe itself is denied
    /// the network (a firewall rule on this machine blocks it outbound). Same fetcher, same cache layout.
    /// </summary>
    [Fact]
    public async Task Live_prefetch_fills_the_default_cache_for_the_acceptance_ring()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("HPGEO_LIVE_TILES") == "1", "set HPGEO_LIVE_TILES=1 to hit the real provider");
        var plan = TileCoverage.Plan(GridBoundingBox.Of(Ring), 30, 0.5, Hcm, Esri).Plan!;
        using var fetcher = new TileFetcher();
        var result = await fetcher.FetchAsync(Esri, plan.Tiles().ToList(), null, TestContext.Current.CancellationToken);
        var line = $"prefetch: {result.Tiles.Count}/{plan.TileCount} tiles z{plan.Zoom} ({result.FromCache} already cached) -> {TileCache.DefaultRoot}";
        HPGeoLog.Information(line);
        Assert.True(result.Complete, string.Join("; ", result.Failures.Select(f => $"{f.Url} → {f.Reason}")));
    }

    [Fact]
    public async Task Live_spike_fetches_four_real_tiles_and_stitches_512x512()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("HPGEO_LIVE_TILES") == "1", "set HPGEO_LIVE_TILES=1 to hit the real provider");
        var center = Vn2000Wgs84Transform.Default.ToWgs84(new PlanePoint(600125.887, 1231608.428), Hcm);
        var t = WebMercator.ToTile(center, 19);
        var plan = new TilePlan(19, t.X, t.X + 1, t.Y, t.Y + 1, WebMercator.ResolutionMPerPx(center.LatDeg, 19), new GeoBoundingBox(0, 0, 0, 0),
            new OutputRaster(0, 0, 1, 1, 1), Array.Empty<HPAutoCad.Core.HPGeoLink.Conversion.ConversionIssue>());
        var result = await new TileFetcher().FetchAsync(Esri, plan.Tiles().ToList(), null, TestContext.Current.CancellationToken);
        Assert.True(result.Complete, string.Join("; ", result.Failures.Select(f => $"{f.Url} → {f.Reason}")));
        var mosaic = TileStitcher.Stitch(plan, result.Tiles);
        var outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "output", "spike"));
        var path = Path.Combine(outDir, "spike-4tiles-z19.png");
        TileStitcher.SavePng(mosaic, path);
        var line = $"tiles {result.Tiles.Count}/4 fetched (z19) stitched {mosaic.Width}x{mosaic.Height} -> {path}";
        HPGeoLog.Information("spike: " + line);
        Assert.Equal((512, 512), (mosaic.Width, mosaic.Height));
        Assert.True(new FileInfo(path).Length > 10_000, "a real satellite mosaic is far larger than 10 KB");
        Assert.NotEqual(0, mosaic.Get(256, 256).A);
    }
}
