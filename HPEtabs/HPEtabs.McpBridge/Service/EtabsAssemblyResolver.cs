using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Serilog;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     Loads <c>ETABSv1.dll</c> from the ETABS install folder when the runtime first asks for it. The bridge
///     compiles against that file but never copies it (Private=false), so the default probing (app folder,
///     deps.json) finds nothing and raises <see cref="AssemblyLoadContext.Resolving"/>; answering it here means
///     the wrapper the scripts bind to is always the one belonging to the ETABS that is installed — and no CSI
///     binary is ever redistributed with the bridge. Must be installed before any method that names an
///     ETABSv1 type is JIT-compiled, which is why <c>BridgeEntry</c> installs it first and wires the rest in a
///     separate, non-inlined method.
/// </summary>
public static class EtabsAssemblyResolver
{
    private static string? _installDir;
    private static string? _resolvedPath;
    private static bool _installed;

    /// <summary>Folder the wrapper is loaded from; null until <see cref="Install"/> found one.</summary>
    public static string? InstallDirectory => _installDir;

    /// <summary>Full path of the wrapper once the runtime actually loaded it through this resolver.</summary>
    public static string? ResolvedPath => _resolvedPath;

    /// <summary>File version of the wrapper on disk (2.10.0.0 for ETABS 22.7), read without loading it.</summary>
    public static string? WrapperFileVersion =>
        _installDir is null ? null : FileVersionInfo.GetVersionInfo(Path.Combine(_installDir, EtabsApiLocator.WrapperFileName)).FileVersion;

    /// <summary>Returns false (and logs each candidate) when no install folder holds the wrapper; the bridge then runs without an API.</summary>
    public static bool Install(int major = EtabsApiLocator.DefaultMajor)
    {
        if (_installed) return _installDir is not null;
        _installed = true;

        foreach (var (source, directory) in EtabsApiLocator.Describe(major))
            Log.Debug("ETABS install folder candidate from {Source}: {Directory}", source, directory ?? "<none>");

        _installDir = EtabsApiLocator.Find(major);
        if (_installDir is null)
        {
            Log.Error("ETABSv1.dll not found: set {Env} or install ETABS {Major}", EtabsApiLocator.EnvironmentVariable, major);
            return false;
        }

        AssemblyLoadContext.Default.Resolving += Resolve;
        Log.Information("ETABSv1.dll will resolve from {Directory} (file version {Version})", _installDir, WrapperFileVersion);
        return true;
    }

    private static Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
    {
        if (!string.Equals(name.Name, "ETABSv1", StringComparison.OrdinalIgnoreCase) || _installDir is null) return null;

        var path = Path.Combine(_installDir, EtabsApiLocator.WrapperFileName);
        var assembly = context.LoadFromAssemblyPath(path);
        _resolvedPath = assembly.Location;
        Log.Information("ETABSv1.dll resolved from {Path} ({Version})", assembly.Location, WrapperFileVersion);
        return assembly;
    }
}
