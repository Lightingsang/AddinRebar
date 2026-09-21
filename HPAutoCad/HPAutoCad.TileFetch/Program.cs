using HPAutoCad.Core.HPGeoLink.Imagery;

namespace HPAutoCad.TileFetch;

/// <summary>
/// <c>HPAutoCad.TileFetch.exe &lt;request-file&gt;</c>: downloads the tiles the request names into the cache it names,
/// printing <c>progress N</c> / <c>fail z/x/y reason</c> / <c>done ok failed cached</c> lines
/// (<see cref="TileFetchProtocol"/>). The provider id is the only thing that decides a URL. Exit 0 = every tile
/// in the cache, 2 = some failed (listed), 1 = bad request. Cancelled by killing the process; the cache stays
/// consistent because every tile is written through a temp name.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 1 || !File.Exists(args[0]))
        {
            await Console.Error.WriteLineAsync("usage: HPAutoCad.TileFetch <request-file>");
            return TileFetchProtocol.ExitUsage;
        }
        TileFetchRequest request;
        IImageryProvider provider;
        try
        {
            request = TileFetchProtocol.ReadRequest(args[0]);
            provider = ImageryProviders.Resolve(request.ProviderId);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or IOException)
        {
            await Console.Error.WriteLineAsync("bad request: " + exception.Message);
            return TileFetchProtocol.ExitUsage;
        }

        var stdout = Console.Out;
        var sync = new object();
        var progress = new SyncProgress(p => { lock (sync) { stdout.WriteLine(TileFetchProtocol.Format(new TileFetchLine.Progress(p))); stdout.Flush(); } });
        using var fetcher = new TileFetcher(cacheRoot: request.CacheRoot, userAgent: request.UserAgent);
        var result = await fetcher.FetchAsync(provider, request.Tiles, progress);
        lock (sync)
        {
            foreach (var failure in result.Failures)
                stdout.WriteLine(TileFetchProtocol.Format(new TileFetchLine.Failed(failure.Tile, failure.Reason)));
            stdout.WriteLine(TileFetchProtocol.Format(new TileFetchLine.Done(result.Tiles.Count, result.Failures.Count, result.FromCache)));
            stdout.Flush();
        }
        return result.Complete ? TileFetchProtocol.ExitOk : TileFetchProtocol.ExitPartial;
    }

    /// <summary>IProgress that reports on the calling thread (Progress&lt;T&gt; would need a synchronization context).</summary>
    private sealed class SyncProgress(Action<int> onReport) : IProgress<int>
    {
        public void Report(int value) => onReport(value);
    }
}
