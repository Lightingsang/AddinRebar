using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Serilog;

namespace HPSap2000.McpBridge.Service;

/// <summary>
///     Loads <c>SAP2000v1.dll</c> from the SAP2000 install folder when the runtime first asks for it. The bridge
///     compiles against that file but never copies it (Private=false), so the default probing (app folder,
///     deps.json) finds nothing and raises <see cref="AssemblyLoadContext.Resolving"/>; answering it here means
///     the wrapper the scripts bind to is always the one belonging to the SAP2000 that is installed — and no CSI
///     binary is ever redistributed with the bridge.
/// </summary>
public static class SapAssemblyResolver
{
    private static string? _installDir;
    private static string? _resolvedPath;
    private static bool _installed;

    /// <summary>Folder the wrapper is loaded from; null until <see cref="Install"/> found one.</summary>
    public static string? InstallDirectory => _installDir;

    /// <summary>Full path of the wrapper once the runtime actually loaded it through this resolver.</summary>
    public static string? ResolvedPath => _resolvedPath;

    /// <summary>File version of the wrapper on disk, read without loading it.</summary>
    public static string? WrapperFileVersion =>
        _installDir is null ? null : FileVersionInfo.GetVersionInfo(Path.Combine(_installDir, SapApiLocator.WrapperFileName)).FileVersion;

    /// <summary>Returns false (and logs each candidate) when no install folder holds the wrapper; the bridge then runs without an API.</summary>
    public static bool Install(int major = SapApiLocator.DefaultMajor)
    {
        if (_installed) return _installDir is not null;
        _installed = true;

        foreach (var (source, directory) in SapApiLocator.Describe(major))
            Log.Debug("SAP2000 install folder candidate from {Source}: {Directory}", source, directory ?? "<none>");

        _installDir = SapApiLocator.Find(major);
        if (_installDir is null)
        {
            Log.Error("SAP2000v1.dll not found: set {Env} or install SAP2000 {Major}", SapApiLocator.EnvironmentVariable, major);
            return false;
        }

        AssemblyLoadContext.Default.Resolving += Resolve;
        Log.Information("SAP2000v1.dll will resolve from {Directory} (file version {Version})", _installDir, WrapperFileVersion);
        return true;
    }

    private static Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
    {
        if (!string.Equals(name.Name, "SAP2000v1", StringComparison.OrdinalIgnoreCase) || _installDir is null) return null;

        var path = Path.Combine(_installDir, SapApiLocator.WrapperFileName);
        var assembly = context.LoadFromAssemblyPath(path);
        _resolvedPath = assembly.Location;
        Log.Information("SAP2000v1.dll resolved from {Path} ({Version})", assembly.Location, WrapperFileVersion);
        return assembly;
    }
}
