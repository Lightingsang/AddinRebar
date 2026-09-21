using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace HPAutoCad.Loader;

/// <summary>
/// Isolated AssemblyLoadContext for the HPAutoCad (HPGeoLink) add-in.
/// Resolves dependencies (CommunityToolkit.Mvvm, WebView2, HPAutoCad.Core) from Contents\App\
/// using HPAutoCad.deps.json. Host assemblies (AutoCAD API) fall through to the Default ALC.
/// </summary>
internal sealed class AppLoadContext : AssemblyLoadContext
{
    private static readonly string[] HostAssemblyPrefixes = ["Ac", "Ad", "Autodesk."];

    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _mainAssemblyPath;
    private readonly string _appDirectory;

    public AppLoadContext(string mainAssemblyPath) : base(name: "HPAutoCad.App", isCollectible: false)
    {
        _mainAssemblyPath = mainAssemblyPath ?? throw new ArgumentNullException(nameof(mainAssemblyPath));
        _appDirectory = Path.GetDirectoryName(mainAssemblyPath) ?? string.Empty;
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name ?? string.Empty;

        // Never load a second copy of AutoCAD or Autodesk API assemblies into the custom ALC.
        // Returning null allows resolution to fall through to AssemblyLoadContext.Default.
        if (HostAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        // Resolve managed assemblies using HPAutoCad.deps.json
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path is not null && File.Exists(path))
        {
            LoaderLog.Write($"load {name} {assemblyName.Version} <- {Path.GetFileName(path)}");
            return LoadFromAssemblyPath(path);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        // 1. Resolve unmanaged native DLL (e.g. WebView2Loader.dll) via deps.json runtime targets
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (path is not null && File.Exists(path))
        {
            LoaderLog.Write($"load unmanaged {unmanagedDllName} <- {Path.GetFileName(path)}");
            return LoadUnmanagedDllFromPath(path);
        }

        // 2. Fallback probe: runtimes\win-x64\native\ under App directory
        if (!string.IsNullOrEmpty(_appDirectory))
        {
            var probePath = Path.Combine(_appDirectory, "runtimes", "win-x64", "native", 
                unmanagedDllName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? unmanagedDllName : unmanagedDllName + ".dll");
            if (File.Exists(probePath))
            {
                LoaderLog.Write($"load unmanaged (fallback) {unmanagedDllName} <- {probePath}");
                return LoadUnmanagedDllFromPath(probePath);
            }
        }

        return IntPtr.Zero;
    }
}
