using Microsoft.Win32;

namespace HPCivil3d.Mcp.Server.Tests;

/// <summary>
///     Finds the installed Civil 3D 2026 API assemblies for the seed compile check, the same way Directory.Build.props
///     does for the bridge: the HPCIVIL3D_C3D_DIR environment variable, then the product's registry key
///     (ACAD-9100:409 is Civil 3D's ProductID under the R25.1 release), then the default install folder. Null when the
///     product is not installed — the tests then skip with a visible reason instead of failing.
/// </summary>
public static class Civil3dApiLocator
{
    public const string SkipReason = "Civil 3D 2026 not installed (set HPCIVIL3D_C3D_DIR to its C3D folder)";

    private static readonly Lazy<string?> Folder = new(Find);

    /// <summary>The `C3D\` folder holding AeccDbMgd.dll, or null.</summary>
    public static string? CivilFolder => Folder.Value;

    /// <summary>The three Civil assemblies the bridge compiles scripts against, or null when any is missing.</summary>
    public static string[]? Assemblies()
    {
        var civil = CivilFolder;
        if (civil is null) return null;
        var aecBase = Path.Combine(Path.GetDirectoryName(civil.TrimEnd(Path.DirectorySeparatorChar))!, "ACA", "AecBaseMgd.dll");
        string[] files = [Path.Combine(civil, "AeccDbMgd.dll"), Path.Combine(civil, "AeccPressurePipesMgd.dll"), aecBase];
        return files.All(File.Exists) ? files : null;
    }

    private static string? Find()
    {
        var fromEnv = Environment.GetEnvironmentVariable("HPCIVIL3D_C3D_DIR");
        if (!string.IsNullOrWhiteSpace(fromEnv)) return Directory.Exists(fromEnv) && File.Exists(Path.Combine(fromEnv, "AeccDbMgd.dll")) ? fromEnv : null;

        if (OperatingSystem.IsWindows())
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Autodesk\AutoCAD\R25.1\ACAD-9100:409");
            if (key?.GetValue("Location") is string location)
            {
                var civil = Path.Combine(location, "C3D");
                if (File.Exists(Path.Combine(civil, "AeccDbMgd.dll"))) return civil;
            }
        }

        var fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk", "AutoCAD 2026", "C3D");
        return File.Exists(Path.Combine(fallback, "AeccDbMgd.dll")) ? fallback : null;
    }
}
