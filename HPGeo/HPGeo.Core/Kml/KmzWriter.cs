using System.IO.Compression;
using System.Text;

namespace HPGeo.Core.Kml;

/// <summary>
/// A KMZ is a zip whose first entry is <c>doc.kml</c>. The marker is pure vector geometry, so no icon
/// file is packed. Google Earth accepts Deflate; the reference tool stored uncompressed only because it
/// hand-rolled the zip in the browser.
/// </summary>
public static class KmzWriter
{
    public const string KmlEntryName = "doc.kml";
    public const string MimeType = "application/vnd.google-earth.kmz";

    public static void Write(string kml, Stream destination)
    {
        using var zip = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
        var entry = zip.CreateEntry(KmlEntryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(kml);
        stream.Write(bytes, 0, bytes.Length);
    }

    public static void WriteFile(string kml, string path)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        Write(kml, file);
    }

    /// <summary>Reads <c>doc.kml</c> back out of a KMZ (used by tests and the acceptance harness).</summary>
    public static string ReadKml(string kmzPath)
    {
        using var zip = ZipFile.OpenRead(kmzPath);
        var entry = zip.GetEntry(KmlEntryName) ?? throw new InvalidDataException($"{kmzPath} has no {KmlEntryName}");
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>Strips characters Windows refuses in a file name (and device names like CON), falls back to a default.</summary>
    public static string SafeFileName(string? name, string fallback = "VN2000_Points")
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length == 0) return fallback;
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(trimmed.Select(c => invalid.Contains(c) || char.IsControl(c) ? '_' : c).ToArray()).TrimEnd('.', ' ');
        if (cleaned.Length == 0) return fallback;
        var stem = cleaned.Split('.')[0];
        return ReservedNames.Contains(stem) ? stem + "_" + cleaned[stem.Length..] : cleaned;
    }
}
