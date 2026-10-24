using System.IO;

namespace HPBimToolkit.Infrastructure;

/// <summary>Where the toolkit keeps per-user state: everything under <c>%LocalAppData%\HPBimToolkit</c>.</summary>
public static class ToolkitPaths
{
    public static string DataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPBimToolkit");

    public static string LogFolder { get; } = Path.Combine(DataRoot, "logs");

    /// <summary>WebView2 profile; Revit's install folder is read-only, so the browser's own data cannot live beside it.</summary>
    public static string WebViewProfile { get; } = Path.Combine(DataRoot, "WebView2");
}
