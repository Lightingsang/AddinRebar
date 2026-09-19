using System.Net;
using System.Net.Http;

namespace HPGeo.Core.Imagery;

/// <summary>One tile that could not be fetched: address, URL and the reason (status or exception message).</summary>
public sealed record TileFailure(TileAddress Tile, Uri Url, string Reason);

public sealed record TileFetchResult(IReadOnlyDictionary<TileAddress, byte[]> Tiles, IReadOnlyList<TileFailure> Failures, int FromCache)
{
    public bool Complete => Failures.Count == 0;
}

/// <summary>Where tiles come from at run time: the in-process fetcher, or the helper process when acad.exe itself is denied the network.</summary>
public interface ITileSource
{
    Task<TileFetchResult> FetchAsync(IImageryProvider provider, IReadOnlyList<TileAddress> tiles, IProgress<int>? progress = null, CancellationToken ct = default);
}

/// <summary>
/// Downloads tiles with a per-user cache (<c>%LocalAppData%\HPGeo\tiles\&lt;provider&gt;\z\x\y.tile</c> — the bytes as
/// served, JPEG for Esri, so the decoder sniffs the format), at most four requests in flight, a 15 s timeout
/// and two retries with back-off. Only bytes that start like a JPEG or PNG are accepted or kept: a captive
/// portal's HTML page, a truncated file from a killed process, are a reported failure, never a poisoned
/// cache. Host-free (System.Net.Http + IO only): the add-in and the helper process share it.
/// </summary>
public sealed class TileFetcher : ITileSource, IDisposable
{
    public const int MaxParallel = 4;
    public const int Retries = 2;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly HttpClient _http;
    private readonly string _cacheRoot;

    public TileFetcher(HttpMessageHandler? handler = null, string? cacheRoot = null, string? userAgent = null)
    {
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _http.Timeout = Timeout;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent ?? "HPGeo (tile fetch)");
        _cacheRoot = cacheRoot ?? TileCache.DefaultRoot;
    }

    public void Dispose() => _http.Dispose();

    public async Task<TileFetchResult> FetchAsync(IImageryProvider provider, IReadOnlyList<TileAddress> tiles, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        var results = new Dictionary<TileAddress, byte[]>();
        var failures = new List<TileFailure>();
        var fromCache = 0;
        var done = 0;
        var gate = new SemaphoreSlim(MaxParallel);
        var sync = new object();

        var tasks = tiles.Select(async tile =>
        {
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var cachePath = TileCache.PathFor(_cacheRoot, provider, tile);
                var bytes = await TileCache.ReadAsync(cachePath, ct).ConfigureAwait(false);
                var cached = bytes is not null;
                string? failure = null;
                if (!cached)
                {
                    (bytes, failure) = await DownloadAsync(provider.TileUrl(tile), ct).ConfigureAwait(false);
                    if (bytes is not null) await TileCache.WriteAsync(cachePath, bytes, ct).ConfigureAwait(false);
                }
                lock (sync)
                {
                    if (bytes is not null) results[tile] = bytes;
                    else failures.Add(new TileFailure(tile, provider.TileUrl(tile), failure ?? "unknown"));
                    if (cached) fromCache++;
                    done++;
                    progress?.Report(done * 100 / Math.Max(1, tiles.Count));
                }
            }
            finally
            {
                gate.Release();
            }
        });
        await Task.WhenAll(tasks).ConfigureAwait(false);
        return new TileFetchResult(results, failures.OrderBy(f => f.Tile.Y).ThenBy(f => f.Tile.X).ToList(), fromCache);
    }

    public string CachePath(IImageryProvider provider, TileAddress tile) => TileCache.PathFor(_cacheRoot, provider, tile);

    private async Task<(byte[]? Bytes, string? Failure)> DownloadAsync(Uri url, CancellationToken ct)
    {
        string? last = null;
        for (var attempt = 0; attempt <= Retries; attempt++)
        {
            try
            {
                using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
                    if (TileCache.LooksLikeImage(bytes)) return (bytes, null);
                    last = bytes.Length == 0 ? "empty body" : $"not an image ({response.Content.Headers.ContentType?.MediaType ?? "unknown type"}, {bytes.Length} bytes) — captive portal or proxy page?";
                    break; // the server answered; asking again returns the same page
                }
                last = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Unauthorized) break; // retrying will not help
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                last = exception.GetType().Name + ": " + exception.Message;
            }
            if (attempt < Retries) await Task.Delay(TimeSpan.FromMilliseconds(500 * (attempt + 1)), ct).ConfigureAwait(false);
        }
        return (null, last);
    }
}
