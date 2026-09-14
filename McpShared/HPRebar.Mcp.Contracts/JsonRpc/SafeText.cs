using System.Text.RegularExpressions;

namespace HPRebar.Mcp.Contracts.JsonRpc;

/// <summary>
///     Scrubs text that is about to reach the model. Both processes apply it — the bridge before a message
///     leaves Revit, the server before it becomes tool content — so a path never survives one side forgetting.
/// </summary>
public static class SafeText
{
    private static readonly Regex WindowsPath = new Regex(@"[A-Za-z]:\\[^\s""'<>|]+", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex UncPath = new Regex(@"\\\\[^\s""'<>|]+", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string StripPaths(string text) => UncPath.Replace(WindowsPath.Replace(text, "<path>"), "<path>");
}
