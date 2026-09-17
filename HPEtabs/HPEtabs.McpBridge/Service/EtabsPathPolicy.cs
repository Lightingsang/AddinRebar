using System.IO;
using System.Text.Json;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     Where a script may point a file-taking OAPI member. Two passes: the static one on the pipe thread refuses
///     what is wrong whatever the model is (UNC shares, the bridge's and the server's own folders, the ETABS
///     install), the run-time one on the worker adds "under the model's folder or under %LocalAppData%\HPEtabs"
///     once the model path is known, and screens the `args` values the script reads for paths. A relative path
///     is refused too: ETABS would resolve it against a working directory nobody chose.
/// </summary>
public static class EtabsPathPolicy
{
    private static readonly string[] ForbiddenFragments =
    [
        @"HPEtabs\McpBridge", "HPEtabs/McpBridge", @"HPEtabs\McpServer", "HPEtabs/McpServer", @"\Computers and Structures\", "/Computers and Structures/",
    ];

    /// <summary>The folder every writing script may use besides the model's own: `%LocalAppData%\HPEtabs\`.</summary>
    public static string LocalRoot { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPEtabs");

    /// <summary>Refusal reason for a path value known before the run, or null when nothing static is wrong with it.</summary>
    public static string? StaticRefusal(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "empty path";
        if (value.StartsWith(@"\\", StringComparison.Ordinal) || value.StartsWith("//", StringComparison.Ordinal)) return "UNC paths are refused; use a local folder";
        foreach (var fragment in ForbiddenFragments)
        {
            if (value.Contains(fragment, StringComparison.OrdinalIgnoreCase)) return "the bridge, server and ETABS install folders are off limits";
        }
        return null;
    }

    /// <summary>
    ///     Refusal reason at run time: the static rules (on the raw value and again on the normalised one, so `\.\`,
    ///     doubled separators and `..` cannot spell a forbidden folder), then "absolute, under the model folder or the
    ///     bridge's local root — but never inside the bridge's own `McpBridge` folder, where the snapshots live".
    /// </summary>
    public static string? RuntimeRefusal(string value, string? modelDirectory, string? localRoot = null)
    {
        if (StaticRefusal(value) is { } reason) return reason;
        if (!Path.IsPathRooted(value) || value.Length < 3 || value[1] != ':') return "path must be absolute (a drive letter), under the model folder or %LocalAppData%\\HPEtabs";
        if (value.IndexOf(':', 2) >= 0) return "alternate data streams are refused";

        string full;
        try { full = Path.GetFullPath(value); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException) { return "path is not valid"; }

        if (full.StartsWith(@"\\", StringComparison.Ordinal)) return "UNC paths are refused; use a local folder";
        if (StaticRefusal(full) is { } normalisedReason) return normalisedReason;

        var root = localRoot ?? LocalRoot;
        if (IsUnder(full, Path.Combine(root, "McpBridge"))) return "the bridge's own folder (settings, audit, snapshots) is off limits";
        if (IsUnder(full, root)) return null;
        if (string.IsNullOrEmpty(modelDirectory) || Path.GetPathRoot(modelDirectory) == Path.GetFullPath(modelDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar)
            return "path must be under the model folder or %LocalAppData%\\HPEtabs (a model at a drive root has no folder of its own)";
        return IsUnder(full, modelDirectory) ? null : "path must be under the model folder or %LocalAppData%\\HPEtabs";
    }

    /// <summary>True when <paramref name="value"/> could be a file system path: a backslash, a drive letter or a leading slash.</summary>
    public static bool LooksLikePath(string value) =>
        value.Contains('\\') || value.StartsWith('/') || (value.Length >= 2 && char.IsLetter(value[0]) && value[1] == ':');

    /// <summary>Every string value in the request's `args`, with the key path that reached it, for run-time screening.</summary>
    public static IEnumerable<(string key, string value)> StringValues(JsonElement? args)
    {
        if (args is null) yield break;
        foreach (var pair in Walk(args.Value, "")) yield return pair;
    }

    private static IEnumerable<(string key, string value)> Walk(JsonElement element, string prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                yield return (prefix, element.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                foreach (var pair in Walk(property.Value, prefix.Length == 0 ? property.Name : prefix + "." + property.Name))
                    yield return pair;
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var pair in Walk(item, $"{prefix}[{index}]")) yield return pair;
                    index++;
                }
                break;
        }
    }

    private static bool IsUnder(string fullPath, string? directory)
    {
        if (string.IsNullOrEmpty(directory)) return false;
        var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }
}
