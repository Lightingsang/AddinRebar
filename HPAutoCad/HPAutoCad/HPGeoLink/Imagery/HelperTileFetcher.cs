using System.Diagnostics;
using System.IO;
using HPAutoCad.Core.HPGeoLink.Imagery;

namespace HPAutoCad.HPGeoLink.Imagery;

/// <summary>
/// Tiles through <c>HPAutoCad.TileFetch.exe</c> (shipped beside the add-in under <c>TileFetch\</c>): the request goes
/// to a temp file, the helper fills the shared cache and reports on stdout, then the tiles are read back from
/// the cache here. Used first whenever the exe exists — acad.exe itself may be denied the network by a firewall
/// rule — with the in-process <see cref="TileFetcher"/> as the fallback when the helper cannot start. Cancelling
/// the token kills the helper; a tile written by then stays (temp-name writes), the rest is simply absent.
/// </summary>
internal sealed class HelperTileFetcher : ITileSource
{
    public const string HelperFolder = "TileFetch";
    public const string HelperExe = "HPAutoCad.TileFetch.exe";

    private readonly string _exePath;
    private readonly string _cacheRoot;
    private readonly string _userAgent;

    public HelperTileFetcher(string exePath, string? cacheRoot = null, string? userAgent = null)
    {
        _exePath = exePath;
        _cacheRoot = cacheRoot ?? TileCache.DefaultRoot;
        _userAgent = userAgent ?? $"HPAutoCad/{Entry.Version} (tile fetch helper)";
    }

    /// <summary>The helper beside the add-in DLL, or null when it was not deployed.</summary>
    public static string? DefaultExePath()
    {
        try
        {
            var dir = Path.GetDirectoryName(typeof(HelperTileFetcher).Assembly.Location);
            if (dir is null) return null;
            var path = Path.Combine(dir, HelperFolder, HelperExe);
            if (File.Exists(path)) return path;
            var fallback = Path.Combine(dir, HelperFolder, "HPGeo.TileFetch.exe");
            return File.Exists(fallback) ? fallback : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<TileFetchResult> FetchAsync(IImageryProvider provider, IReadOnlyList<TileAddress> tiles, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        var requestPath = Path.Combine(Path.GetTempPath(), "hpgeo-tiles-" + Guid.NewGuid().ToString("N") + ".txt");
        TileFetchProtocol.WriteRequest(requestPath, new TileFetchRequest(provider.Id, _cacheRoot, _userAgent, tiles));
        var failures = new Dictionary<TileAddress, string>();
        var stderr = new List<string>();
        var cached = 0;
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(_exePath, "\"" + requestPath + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(_exePath)!,
                },
                EnableRaisingEvents = true,
            };
            process.OutputDataReceived += (_, e) =>
            {
                switch (TileFetchProtocol.Parse(e.Data))
                {
                    case TileFetchLine.Progress p: progress?.Report(p.Percent); break;
                    case TileFetchLine.Failed f: lock (failures) failures[f.Tile] = f.Reason; break;
                    case TileFetchLine.Done d: Interlocked.Exchange(ref cached, d.Cached); break;
                }
            };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (stderr) stderr.Add(e.Data); };
            if (!process.Start()) throw new InvalidOperationException("helper did not start");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            using (ct.Register(() => TryKill(process)))
            {
                await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            }
            ct.ThrowIfCancellationRequested();
            if (process.ExitCode == TileFetchProtocol.ExitUsage)
                throw new InvalidOperationException("helper refused the request: " + string.Join(" | ", stderr));
        }
        finally
        {
            try { File.Delete(requestPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }

        // What the helper wrote is what the cache holds now; anything missing is a failure it named (or one it could not).
        var results = new Dictionary<TileAddress, byte[]>();
        var failed = new List<TileFailure>();
        foreach (var tile in tiles)
        {
            var bytes = await TileCache.ReadAsync(TileCache.PathFor(_cacheRoot, provider, tile), ct).ConfigureAwait(false);
            if (bytes is not null) results[tile] = bytes;
            else failed.Add(new TileFailure(tile, provider.TileUrl(tile), failures.TryGetValue(tile, out var reason) ? reason : "helper wrote nothing for this tile" + (stderr.Count > 0 ? " (" + stderr[0] + ")" : "")));
        }
        return new TileFetchResult(results, failed.OrderBy(f => f.Tile.Y).ThenBy(f => f.Tile.X).ToList(), cached);
    }

    private static void TryKill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (Exception) { }
    }
}
