using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace HPNavis.McpBridge.Tests;

/// <summary>
///     The plugin under test references Autodesk.Navisworks.Api/Clash/Timeliner without copying them (Roamer
///     supplies them). Should a test reach a method whose JIT needs one of them, this resolver hands out the
///     installed copy — same lookup order as Directory.Build.props: environment, registry, default folder.
///     Nothing is loaded eagerly, so the pure-layer tests stay independent of the install.
/// </summary>
internal static class NavisworksApiProbe
{
    private static string? _installDir;

    [ModuleInitializer]
    internal static void Install() => AppDomain.CurrentDomain.AssemblyResolve += OnResolve;

    public static string? InstallDir => _installDir ??= Locate();

    private static Assembly? OnResolve(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        if (name is null || !name.StartsWith("Autodesk.Navisworks.", StringComparison.Ordinal)) return null;

        var dir = InstallDir;
        if (dir is null) return null;

        var candidate = Path.Combine(dir, name + ".dll");
        return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
    }

    private static string? Locate()
    {
        var fromEnv = Environment.GetEnvironmentVariable("HPNAVIS_NAVISWORKS_DIR");
        if (!string.IsNullOrEmpty(fromEnv) && Directory.Exists(fromEnv)) return fromEnv;

        try
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\Autodesk\Navisworks API Runtime\23\Navisworks Manage");
            if (key?.GetValue("Path") is string path && Directory.Exists(path)) return path;
        }
        catch
        {
            // registry unavailable: fall through to the default folder
        }

        var programs = Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var fallback = Path.Combine(programs, "Autodesk", "Navisworks Manage 2026");
        return Directory.Exists(fallback) ? fallback : null;
    }
}
