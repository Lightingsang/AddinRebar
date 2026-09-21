using System.Diagnostics;
using HPAutoCad.HPGeoLink.Imagery;
using HPAutoCad.HPGeoLink.Support;
using HPAutoCad.Core.HPGeoLink.Imagery;
using HPAutoCad.Core.HPGeoLink.Model;
using HPAutoCad.Core.HPGeoLink.Projection;
using Xunit;

namespace HPAutoCad.Tests.HPGeoLink;

/// <summary>
/// The helper process contract: the request file and the stdout lines round-trip through <see cref="TileFetchProtocol"/>,
/// the real exe refuses a bad request with exit 1, and (live, gated) fetches four real tiles through <see cref="HelperTileFetcher"/>.
/// </summary>
public sealed class TileFetchHelperTests
{
    private static readonly string HelperExe = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "HPAutoCad.TileFetch", "bin", "Debug", "net8.0", "HPAutoCad.TileFetch.exe"));

    [Fact]
    public void Request_file_round_trips_and_lines_parse_both_ways()
    {
        var path = Path.Combine(Path.GetTempPath(), "hpgeo-req-" + Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            var tiles = new[] { new TileAddress(19, 245823, 417490), new TileAddress(19, 245824, 417491) };
            TileFetchProtocol.WriteRequest(path, new TileFetchRequest("esri", @"C:\cache dir", "UA/1", tiles));
            var back = TileFetchProtocol.ReadRequest(path);
            Assert.Equal(("esri", @"C:\cache dir", "UA/1"), (back.ProviderId, back.CacheRoot, back.UserAgent));
            Assert.Equal(tiles, back.Tiles);
        }
        finally { File.Delete(path); }

        Assert.Equal(new TileFetchLine.Progress(40), TileFetchProtocol.Parse(TileFetchProtocol.Format(new TileFetchLine.Progress(40))));
        Assert.Equal(new TileFetchLine.Done(14, 1, 3), TileFetchProtocol.Parse("done 14 1 3"));
        var failed = Assert.IsType<TileFetchLine.Failed>(TileFetchProtocol.Parse(TileFetchProtocol.Format(new TileFetchLine.Failed(new TileAddress(19, 1, 2), "HTTP 403 Forbidden\nsecond line"))));
        Assert.Equal(new TileAddress(19, 1, 2), failed.Tile);
        Assert.Equal("HTTP 403 Forbidden second line", failed.Reason);
        Assert.Null(TileFetchProtocol.Parse("Unhandled exception"));
        Assert.Null(TileFetchProtocol.Parse(null));
        Assert.Null(TileFetchProtocol.ParseTile("19/x/2"));
    }

    [Fact]
    public void The_helper_exe_refuses_a_missing_or_malformed_request_with_exit_1()
    {
        Assert.SkipUnless(File.Exists(HelperExe), "helper not built at " + HelperExe);
        Assert.Equal(TileFetchProtocol.ExitUsage, Run("\"" + Path.Combine(Path.GetTempPath(), "does-not-exist.txt") + "\"", out _));

        var bad = Path.Combine(Path.GetTempPath(), "hpgeo-bad-" + Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            File.WriteAllText(bad, "provider=google\ncache=" + Path.GetTempPath() + "\n19/1/2\n");
            Assert.Equal(TileFetchProtocol.ExitUsage, Run("\"" + bad + "\"", out var stderr));
            Assert.Contains("google", stderr);
        }
        finally { File.Delete(bad); }
    }

    [Fact]
    public async Task Live_helper_fetches_four_real_tiles_into_a_temp_cache_through_the_host_side_fetcher()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("HPGEO_LIVE_TILES") == "1", "set HPGEO_LIVE_TILES=1 to hit the real provider");
        Assert.SkipUnless(File.Exists(HelperExe), "helper not built at " + HelperExe);
        var centre = Vn2000Wgs84Transform.Default.ToWgs84(new PlanePoint(600125.887, 1231608.428), TmParameters.Tm3(105.75));
        var t = WebMercator.ToTile(centre, 19);
        var tiles = new[] { t, new TileAddress(19, t.X + 1, t.Y), new TileAddress(19, t.X, t.Y + 1), new TileAddress(19, t.X + 1, t.Y + 1) };
        var cache = Path.Combine(Path.GetTempPath(), "hpgeo-helper-" + Guid.NewGuid().ToString("N"));
        try
        {
            var progress = new List<int>();
            var result = await new HelperTileFetcher(HelperExe, cache).FetchAsync(ImageryProviders.Default, tiles, new SyncProgress(progress.Add), TestContext.Current.CancellationToken);
            Assert.True(result.Complete, string.Join("; ", result.Failures.Select(f => f.Url + " -> " + f.Reason)));
            Assert.Equal(4, result.Tiles.Count);
            Assert.Equal(0, result.FromCache);
            Assert.Contains(100, progress);
            Assert.All(result.Tiles.Values, bytes => Assert.True(TileCache.LooksLikeImage(bytes)));
            HPGeoLog.Information($"helper spike: {result.Tiles.Count}/4 tiles z19 via {HelperExe} -> {cache}");

            var again = await new HelperTileFetcher(HelperExe, cache).FetchAsync(ImageryProviders.Default, tiles, null, TestContext.Current.CancellationToken);
            Assert.Equal(4, again.FromCache);
        }
        finally
        {
            if (Directory.Exists(cache)) Directory.Delete(cache, true);
        }
    }

    private static int Run(string args, out string stderr)
    {
        using var p = Process.Start(new ProcessStartInfo(HelperExe, args) { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true })!;
        stderr = p.StandardError.ReadToEnd();
        p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return p.ExitCode;
    }

    private sealed class SyncProgress(Action<int> onReport) : IProgress<int>
    {
        public void Report(int value) => onReport(value);
    }
}
