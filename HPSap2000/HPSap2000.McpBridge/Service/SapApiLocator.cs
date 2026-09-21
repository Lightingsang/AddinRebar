using System.IO;
using Microsoft.Win32;

namespace HPSap2000.McpBridge.Service;

/// <summary>
///     Finds the SAP2000 install folder — where <c>SAP2000v1.dll</c> lives — the same way the build does
///     (Directory.Build.props): an explicit environment override, then the COM registration SAP2000 writes for
///     its API object (ProgID CSI.SAP2000.API.SapObject), then the default Program Files location. Pure
///     file-system and registry reads, no SAP2000 type in sight, so server tests can link this file to find the
///     wrapper for their seed compile check.
/// </summary>
public static class SapApiLocator
{
    public const string EnvironmentVariable = "HPSAP2000_SAP2000_DIR";

    /// <summary>CLSID behind the ProgID <c>CSI.SAP2000.API.SapObject</c>; its LocalServer32 value is the full path of SAP2000.exe.</summary>
    public const string SapObjectClsid = "{B6B21850-FB75-41DE-85EC-BC9DBEC69BD3}";

    public const string WrapperFileName = "SAP2000v1.dll";

    public const int DefaultMajor = 27;

    /// <summary>The install folder (with a trailing separator) or null when none of the three sources yields a folder holding the wrapper.</summary>
    public static string? Find(int major = DefaultMajor)
    {
        foreach (var candidate in Candidates(major))
        {
            if (candidate is null) continue;
            var dir = candidate.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (File.Exists(Path.Combine(dir, WrapperFileName))) return dir;
        }

        return null;
    }

    /// <summary>Full path of the wrapper, or null.</summary>
    public static string? WrapperPath(int major = DefaultMajor) => Find(major) is { } dir ? Path.Combine(dir, WrapperFileName) : null;

    /// <summary>Where each candidate came from, for the log line that explains which SAP2000 the bridge bound to.</summary>
    public static IEnumerable<(string source, string? directory)> Describe(int major = DefaultMajor)
    {
        yield return ("env " + EnvironmentVariable, Environment.GetEnvironmentVariable(EnvironmentVariable));
        yield return ("registry LocalServer32 of " + SapObjectClsid, FromComRegistration());
        yield return ("Program Files default", ProgramFilesDefault(major));
    }

    private static IEnumerable<string?> Candidates(int major)
    {
        yield return Environment.GetEnvironmentVariable(EnvironmentVariable);
        yield return FromComRegistration();
        yield return ProgramFilesDefault(major);
    }

    private static string? FromComRegistration()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Classes\CLSID\{SapObjectClsid}\LocalServer32");
            var server = key?.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(server)) return null;

            server = server.Trim();
            if (server.StartsWith('"'))
            {
                var end = server.IndexOf('"', 1);
                server = end > 0 ? server.Substring(1, end - 1) : server.Trim('"');
            }

            return Path.GetDirectoryName(server);
        }
        catch
        {
            return null;
        }
    }

    private static string? ProgramFilesDefault(int major)
    {
        var programFiles = Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        return string.IsNullOrEmpty(programFiles) ? null : Path.Combine(programFiles, "Computers and Structures", "SAP2000 " + major);
    }
}
