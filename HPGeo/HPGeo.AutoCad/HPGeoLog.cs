using System.Globalization;
using System.IO;

namespace HPGeo.AutoCad;

/// <summary>
/// The add-in's log: <c>%LocalAppData%\HPGeo\logs\hpgeo-YYYYMMDD.log</c>, one line per entry, appended with
/// the file shared so a second AutoCAD can write too. Deliberately BCL-only: a logging package in the add-in's
/// own load context would be fine, but a plain file needs no dependency at all and is what the acceptance
/// harness greps.
/// </summary>
internal static class HPGeoLog
{
    public static readonly string LogDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPGeo", "logs");

    private static readonly object Gate = new();

    public static void Information(string message) => Write("INF", message, null);
    public static void Warning(string message) => Write("WRN", message, null);
    public static void Error(string message, Exception? exception = null) => Write("ERR", message, exception);

    private static void Write(string level, string message, Exception? exception)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(LogDirectory);
                var path = Path.Combine(LogDirectory, "hpgeo-" + DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
                using var writer = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
                writer.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                writer.Write(" [");
                writer.Write(level);
                writer.Write("] ");
                writer.WriteLine(message);
                if (exception is not null) writer.WriteLine(exception.ToString());
            }
        }
        catch (Exception)
        {
            // a log that cannot be written must never take a command down
        }
    }
}
