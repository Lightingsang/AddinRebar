using System.Globalization;
using System.Text;

namespace HPGeo.Core.Imagery;

/// <summary>A request handed to the helper process: which provider, which cache, which tiles. Never a URL.</summary>
public sealed record TileFetchRequest(string ProviderId, string CacheRoot, string UserAgent, IReadOnlyList<TileAddress> Tiles);

/// <summary>One line the helper prints: progress, a failed tile, or the final tally.</summary>
public abstract record TileFetchLine
{
    public sealed record Progress(int Percent) : TileFetchLine;
    public sealed record Failed(TileAddress Tile, string Reason) : TileFetchLine;
    public sealed record Done(int Ok, int FailedCount, int Cached) : TileFetchLine;
}

/// <summary>
/// The wire between the add-in and <c>HPGeo.TileFetch.exe</c>: a small ASCII request file (the helper builds the
/// URLs itself from the provider id, so the add-in can never make it fetch an arbitrary address) and line-based
/// stdout the add-in reads while the helper runs. Shared by both sides so a change breaks one build, not a session.
/// </summary>
public static class TileFetchProtocol
{
    public const int ExitOk = 0;
    public const int ExitUsage = 1;
    public const int ExitPartial = 2;

    public static void WriteRequest(string path, TileFetchRequest request)
    {
        var sb = new StringBuilder();
        sb.Append("provider=").Append(request.ProviderId).Append('\n');
        sb.Append("cache=").Append(request.CacheRoot).Append('\n');
        sb.Append("ua=").Append(request.UserAgent).Append('\n');
        foreach (var t in request.Tiles) sb.Append(FormatTile(t)).Append('\n');
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    public static TileFetchRequest ReadRequest(string path)
    {
        string? provider = null, cache = null, ua = null;
        var tiles = new List<TileAddress>();
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("provider=", StringComparison.Ordinal)) provider = line[9..];
            else if (line.StartsWith("cache=", StringComparison.Ordinal)) cache = line[6..];
            else if (line.StartsWith("ua=", StringComparison.Ordinal)) ua = line[3..];
            else tiles.Add(ParseTile(line) ?? throw new FormatException($"Bad tile line: '{line}'"));
        }
        if (string.IsNullOrWhiteSpace(provider)) throw new FormatException("Request has no provider= line.");
        if (string.IsNullOrWhiteSpace(cache)) throw new FormatException("Request has no cache= line.");
        return new TileFetchRequest(provider, cache, string.IsNullOrWhiteSpace(ua) ? "HPGeo.TileFetch" : ua, tiles);
    }

    public static string FormatTile(TileAddress t) => string.Create(CultureInfo.InvariantCulture, $"{t.Zoom}/{t.X}/{t.Y}");

    public static TileAddress? ParseTile(string text)
    {
        var parts = text.Split('/');
        if (parts.Length != 3) return null;
        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var z) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y)) return null;
        return new TileAddress(z, x, y);
    }

    public static string Format(TileFetchLine line) => line switch
    {
        TileFetchLine.Progress p => $"progress {p.Percent}",
        TileFetchLine.Failed f => $"fail {FormatTile(f.Tile)} {f.Reason.Replace('\r', ' ').Replace('\n', ' ')}",
        TileFetchLine.Done d => $"done {d.Ok} {d.FailedCount} {d.Cached}",
        _ => throw new ArgumentOutOfRangeException(nameof(line)),
    };

    /// <summary>Null for anything that is not one of ours (the helper may inherit noise on stdout).</summary>
    public static TileFetchLine? Parse(string? text)
    {
        if (text is null) return null;
        var parts = text.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        switch (parts[0])
        {
            case "progress":
                return int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var pct) ? new TileFetchLine.Progress(pct) : null;
            case "fail":
                return ParseTile(parts[1]) is { } tile ? new TileFetchLine.Failed(tile, parts.Length > 2 ? parts[2] : "unknown") : null;
            case "done":
                var nums = parts.Length > 2 ? (parts[1] + " " + parts[2]).Split(' ', StringSplitOptions.RemoveEmptyEntries) : Array.Empty<string>();
                return nums.Length == 3 && int.TryParse(nums[0], out var ok) && int.TryParse(nums[1], out var failed) && int.TryParse(nums[2], out var cached)
                    ? new TileFetchLine.Done(ok, failed, cached) : null;
            default:
                return null;
        }
    }
}
