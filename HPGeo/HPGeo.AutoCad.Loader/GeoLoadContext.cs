using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace HPGeo.AutoCad.Loader;

/// <summary>
/// The add-in's own load context. AutoCAD loads every bundle into the default context, where another
/// bundle's CommunityToolkit.Mvvm (or any shared dependency) would collide with ours; resolving from the
/// add-in's deps.json here gives it private copies. Anything the resolver does not know — the runtime, WPF,
/// the AutoCAD API (ExcludeAssets=runtime keeps them out of the deps.json) — falls through to the default
/// context so the add-in and AutoCAD share one identity for those types.
/// </summary>
internal sealed class GeoLoadContext : AssemblyLoadContext
{
    private static readonly string[] HostAssemblyPrefixes = ["Ac", "Ad", "Autodesk."];

    private readonly AssemblyDependencyResolver _resolver;

    public GeoLoadContext(string mainAssemblyPath) : base(name: "HPGeo.AutoCad", isCollectible: false)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name ?? string.Empty;
        // Never a second copy of the AutoCAD API: the add-in must see the Document/Database instances AutoCAD hands it.
        if (HostAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))) return null;

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
