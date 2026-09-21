using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace HPAutoCad.HPGeoLink.Support;

/// <summary>
/// Opens a KMZ in the Google Earth installed on this PC. The desktop application is started directly when it is
/// found in one of its standard install folders (Google Earth Pro, then the older Google Earth), so the file lands
/// in the program the surveyor expects even when the .kmz association points elsewhere; only then the Windows
/// association is tried. Every outcome comes back as a one-line note for the dialog's status line and the log —
/// a missing Google Earth is a note, never an exception in the export.
/// </summary>
internal static class GoogleEarthLauncher
{
    public const string ExeName = "googleearth.exe";

    /// <summary>Where Google Earth installs itself, most likely first.</summary>
    public static IEnumerable<string> CandidatePaths()
    {
        foreach (var root in new[] { Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.LocalApplicationData })
        {
            var baseDir = Environment.GetFolderPath(root);
            if (string.IsNullOrEmpty(baseDir)) continue;
            yield return Path.Combine(baseDir, "Google", "Google Earth Pro", "client", ExeName);
            yield return Path.Combine(baseDir, "Google", "Google Earth", "client", ExeName);
        }
    }

    public static string? FindExe() => CandidatePaths().FirstOrDefault(File.Exists);

    public static string Open(string kmzPath)
    {
        if (!File.Exists(kmzPath)) return $"không mở được: không thấy file {kmzPath}";
        var exe = FindExe();
        if (exe is not null)
        {
            try
            {
                Process.Start(new ProcessStartInfo(exe, "\"" + kmzPath + "\"") { UseShellExecute = false });
                return $"đã mở trong {(exe.Contains("Earth Pro", StringComparison.OrdinalIgnoreCase) ? "Google Earth Pro" : "Google Earth")}";
            }
            catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException)
            {
                HPGeoLog.Warning($"Google Earth at {exe} did not start: {exception.Message}");
            }
        }
        try
        {
            Process.Start(new ProcessStartInfo(kmzPath) { UseShellExecute = true });
            return "đã mở bằng chương trình liên kết với .kmz";
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException)
        {
            HPGeoLog.Warning($"no application opened {kmzPath}: {exception.Message}");
            return "không tìm thấy Google Earth trên máy — cài Google Earth Pro hoặc mở file KMZ thủ công";
        }
    }
}
