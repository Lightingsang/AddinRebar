using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Win32;

namespace HPTekla.McpBridge.Tests;

/// <summary>
///     The bridge under test references Tekla.Structures.* without copying them (TeklaStructures.exe supplies them).
///     Should a test reach a method whose JIT needs one of them, this resolver hands out the installed copy
///     from C:\Program Files\Tekla Structures\2025.0\bin\ (same lookup order as Directory.Build.props).
/// </summary>
internal static class TeklaApiProbe
{
    private static string? _installDir;

    [ModuleInitializer]
    internal static void Install() => AppDomain.CurrentDomain.AssemblyResolve += OnResolve;

    public static string? InstallDir => _installDir ??= Locate();

    private static Assembly? OnResolve(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        if (name is null || !name.StartsWith("Tekla.Structures", StringComparison.Ordinal)) return null;

        var dir = InstallDir;
        if (dir is null) return null;

        var candidate = Path.Combine(dir, name + ".dll");
        return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
    }

    private static string? Locate()
    {
        var fromEnv = Environment.GetEnvironmentVariable("HPTEKLA_TEKLA_DIR");
        if (!string.IsNullOrEmpty(fromEnv) && Directory.Exists(fromEnv)) return fromEnv;

        try
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\Trimble\Tekla Structures\2025.0\setup");
            if (key?.GetValue("MainDir") is string mainDir)
            {
                var binDir = Path.Combine(mainDir, "2025.0", "bin");
                if (Directory.Exists(binDir)) return binDir;
            }
        }
        catch
        {
            // registry unavailable: fall through
        }

        var programs = Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var fallback = Path.Combine(programs, "Tekla Structures", "2025.0", "bin");
        return Directory.Exists(fallback) ? fallback : null;
    }
}
