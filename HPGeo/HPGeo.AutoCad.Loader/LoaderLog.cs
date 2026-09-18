using System.IO;

namespace HPGeo.AutoCad.Loader;

/// <summary>
/// The loader's own trace: <c>%LocalAppData%\HPGeo\logs\loader.log</c>. BCL-only, because it exists to record
/// what happened before (or instead of) the add-in coming up.
/// </summary>
internal static class LoaderLog
{
    public static readonly string LogDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPGeo", "logs");

    private static readonly object Gate = new();

    public static void Write(string message, System.Exception? exception = null)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(LogDirectory);
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}" + (exception is null ? "" : Environment.NewLine + exception);
                File.AppendAllText(Path.Combine(LogDirectory, "loader.log"), line + Environment.NewLine);
            }
        }
        catch (System.Exception)
        {
            // nothing left to report to
        }
    }
}
