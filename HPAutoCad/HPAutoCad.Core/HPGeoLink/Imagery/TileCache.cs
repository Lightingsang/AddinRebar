namespace HPAutoCad.Core.HPGeoLink.Imagery;

/// <summary>
/// The per-user tile cache on disk: <c>%LocalAppData%\HPGeo\tiles\&lt;provider&gt;\z\x\y.tile</c>. Only image bytes
/// (JPEG / PNG magic) are ever kept or trusted; writes go through a temp name so a killed process leaves no half
/// tile. <see cref="Clear"/> removes only this folder — never anything outside it.
/// </summary>
public static class TileCache
{
    public static readonly string DefaultRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPGeo", "tiles");

    public static string PathFor(string root, IImageryProvider provider, TileAddress tile) =>
        Path.Combine(root, provider.Id, tile.Zoom.ToString(), tile.X.ToString(), tile.Y + ".tile");

    /// <summary>Bytes on disk under the cache root, for HPGEOINFO and the dialog.</summary>
    public static long SizeBytes(string? root = null)
    {
        var dir = root ?? DefaultRoot;
        if (!Directory.Exists(dir)) return 0;
        return new DirectoryInfo(dir).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
    }

    /// <summary>Deletes the cache folder (the default one unless a root is given) and reports what went.</summary>
    public static (int Files, long Bytes) Clear(string? root = null)
    {
        var dir = root ?? DefaultRoot;
        if (!Directory.Exists(dir)) return (0, 0);
        var files = new DirectoryInfo(dir).EnumerateFiles("*", SearchOption.AllDirectories).ToList();
        var bytes = files.Sum(f => f.Length);
        Directory.Delete(dir, recursive: true);
        return (files.Count, bytes);
    }

    /// <summary>JPEG (FF D8 FF) or PNG (89 50 4E 47) — the only bytes a tile may be.</summary>
    public static bool LooksLikeImage(byte[] bytes) =>
        bytes.Length >= 4 && ((bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) ||
                              (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47));

    /// <summary>A cached tile that no longer looks like an image (truncated, foreign) is evicted so it is fetched again.</summary>
    public static async Task<byte[]?> ReadAsync(string cachePath, CancellationToken ct)
    {
        if (!File.Exists(cachePath)) return null;
        try
        {
            var bytes = await File.ReadAllBytesAsync(cachePath, ct).ConfigureAwait(false);
            if (LooksLikeImage(bytes)) return bytes;
            File.Delete(cachePath);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return null;
    }

    /// <summary>Written to a temp name and moved into place; the cache is a convenience, so a full disk never fails the run.</summary>
    public static async Task WriteAsync(string cachePath, byte[] bytes, CancellationToken ct)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath)!);
            var temp = cachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await File.WriteAllBytesAsync(temp, bytes, ct).ConfigureAwait(false);
            File.Move(temp, cachePath, overwrite: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
