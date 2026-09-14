using System.IO;
namespace HPAutoCad.McpBridge.Loader;

/// <summary>
///     The loader runs before the bridge (and its Serilog) exists, and a load-context failure is exactly
///     the moment nothing else can write a log — so it appends plain lines to its own file. Never throws:
///     a logging failure must not turn a diagnosable problem into an add-in that vanishes without a trace.
/// </summary>
internal static class LoaderLog
{
    public static string LogDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPAutoCad", "McpBridge", "logs");

    public static string FilePath { get; } = Path.Combine(LogDirectory, "loader.log");

    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(FilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}");
        }
        catch
        {
            // nowhere left to report to
        }
    }

    public static void Write(string message, Exception exception) => Write(message + Environment.NewLine + exception);
}
