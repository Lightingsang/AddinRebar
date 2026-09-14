using System.Reflection;
using System.Runtime.Loader;

namespace HPAutoCad.McpBridge.Loader;

/// <summary>
///     The bridge's own load context. AutoCAD loads every plugin into the default context, where the
///     shared framework's System.Collections.Immutable 8.0 and AutoCAD's own Microsoft.CodeAnalysis 4.10
///     would win over the Roslyn 5.9 the bridge ships; resolving from the bridge's deps.json here gives
///     it private copies. Anything the resolver does not know — the .NET runtime, WPF, the AutoCAD API
///     (ExcludeAssets=runtime keeps them out of the deps.json) — falls through to the default context so
///     the bridge and AutoCAD share one identity for those types.
/// </summary>
internal sealed class BridgeLoadContext : AssemblyLoadContext
{
    private static readonly string[] HostAssemblyPrefixes = ["Ac", "Ad", "Autodesk."];

    private readonly AssemblyDependencyResolver _resolver;

    public BridgeLoadContext(string mainAssemblyPath) : base(name: "HPAutoCad.McpBridge", isCollectible: false)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name ?? string.Empty;

        // Never a second copy of the AutoCAD API, whatever the deps.json says: the bridge must see the
        // Document/Database instances AutoCAD hands it, not types from a duplicate assembly.
        if (HostAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))) return null;

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path is null) return null;

        LoaderLog.Write($"load {name} {assemblyName.Version} <- {Path.GetFileName(path)}");
        return LoadFromAssemblyPath(path);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
    }
}
