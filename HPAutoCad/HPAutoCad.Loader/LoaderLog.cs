using System;
using System.IO;

namespace HPAutoCad.Loader;

/// <summary>
/// BCL-only diagnostic logger for HPAutoCad.Loader.
/// Operates before add-in ALC initialization without any third-party dependencies.
/// Uses a dual-write mechanism to ensure compatibility with both HPAutoCad and HPGeo harnesses.
/// </summary>
internal static class LoaderLog
{
    public static readonly string PrimaryLogDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPAutoCad", "logs");

    public static readonly string LegacyLogDirectory =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPGeo", "logs");

    public static string LogDirectory => PrimaryLogDirectory;

    private static readonly object Gate = new();

    public static void Write(string message, Exception? exception = null)
    {
        try
        {
            lock (Gate)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var threadId = Environment.CurrentManagedThreadId;
                var line = $"{timestamp} [{threadId}] {message}" + (exception is null ? "" : Environment.NewLine + exception);

                WriteToFile(PrimaryLogDirectory, "loader.log", line);
                WriteToFile(LegacyLogDirectory, "loader.log", line);
            }
        }
        catch
        {
            // Diagnostics must never throw or disrupt CAD execution
        }
    }

    private static void WriteToFile(string directory, string filename, string content)
    {
        try
        {
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, filename), content + Environment.NewLine);
        }
        catch
        {
            // Silent fallback for secondary write failures
        }
    }
}
