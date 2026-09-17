using System.IO;
using Microsoft.Win32;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     Finds the ETABS install folder — where <c>ETABSv1.dll</c> lives — the same way the build does
///     (Directory.Build.props): an explicit environment override, then the COM registration ETABS writes for
///     its API object (there is no vendor "install path" key), then the default Program Files location. Pure
///     file-system and registry reads, no ETABS type in sight, so the server tests link this file to find the
///     wrapper for their seed compile check.
/// </summary>
public static class EtabsApiLocator
{
    public const string EnvironmentVariable = "HPETABS_ETABS_DIR";

    /// <summary>CLSID behind the ProgID <c>CSI.ETABS.API.ETABSObject</c>; its LocalServer32 value is the full path of ETABS.exe.</summary>
    public const string EtabsObjectClsid = "{e4f6d00f-51a5-4d65-a09a-ba00fcbf1f82}";

    public const string WrapperFileName = "ETABSv1.dll";

    public const int DefaultMajor = 22;

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

    /// <summary>Where each candidate came from, for the log line that explains which ETABS the bridge bound to.</summary>
    public static IEnumerable<(string source, string? directory)> Describe(int major = DefaultMajor)
    {
        yield return ("env " + EnvironmentVariable, Environment.GetEnvironmentVariable(EnvironmentVariable));
        yield return ("registry LocalServer32 of " + EtabsObjectClsid, FromComRegistration());
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
            using var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Classes\CLSID\{EtabsObjectClsid}\LocalServer32");
            var server = key?.GetValue(null) as string;
            if (string.IsNullOrWhiteSpace(server)) return null;

            // The value is the exe path; tolerate a quoted path and trailing arguments, which some registrations carry.
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
        return string.IsNullOrEmpty(programFiles) ? null : Path.Combine(programFiles, "Computers and Structures", "ETABS " + major);
    }
}
