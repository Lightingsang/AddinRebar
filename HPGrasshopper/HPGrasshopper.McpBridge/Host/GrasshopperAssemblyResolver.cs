namespace HPGrasshopper.McpBridge.Host;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Serilog;

/// <summary>
///     Proactively resolves Rhino, Grasshopper, RhinoInside.Revit, and bridge assemblies for Revit 2026.
/// </summary>
public static class GrasshopperAssemblyResolver
{
    private static int _installed;
    private static readonly List<string> ResolvedAssemblies = new();
    private static readonly HashSet<string> HandledAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "RevitAPI", "RevitAPIUI", "AdWindows",
        "HPRebar.Mcp.Contracts", "HPRebar.McpBridge.Core", "HPGrasshopper.McpBridge",
        "Serilog", "Serilog.Sinks.File", "CommunityToolkit.Mvvm"
    };

    private static readonly HashSet<string> RhinoAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "RhinoCommon", "Grasshopper", "GH_IO", "RhinoInside.Revit"
    };

    private static bool IsInsideRevit =>
        string.Equals(System.Diagnostics.Process.GetCurrentProcess().ProcessName, "Revit", StringComparison.OrdinalIgnoreCase);

    private static bool ShouldHandleAssembly(string name)
    {
        if (HandledAssemblyNames.Contains(name)) return true;
        if (!IsInsideRevit && RhinoAssemblyNames.Contains(name)) return true;
        return false;
    }

    private static readonly HashSet<string> RepackedAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "MaterialDesignThemes.Wpf", "MaterialDesignColors", "Microsoft.Xaml.Behaviors"
    };

    private static readonly string[] ProbingDirectories = InitializeProbingDirectories();

    public static IReadOnlyList<string> Resolved
    {
        get
        {
            lock (ResolvedAssemblies)
            {
                return [.. ResolvedAssemblies];
            }
        }
    }

    public static void Install()
    {
        if (Interlocked.Exchange(ref _installed, 1) == 1) return;

        var currentAlc = AssemblyLoadContext.GetLoadContext(typeof(GrasshopperAssemblyResolver).Assembly);
        if (currentAlc != null && currentAlc != AssemblyLoadContext.Default)
        {
            currentAlc.Resolving += OnAlcResolving;
        }
        else
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }
    }

    public static void Uninstall()
    {
        if (Interlocked.Exchange(ref _installed, 0) == 0) return;

        var currentAlc = AssemblyLoadContext.GetLoadContext(typeof(GrasshopperAssemblyResolver).Assembly);
        if (currentAlc != null && currentAlc != AssemblyLoadContext.Default)
        {
            currentAlc.Resolving -= OnAlcResolving;
        }
        else
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
        }
    }

    private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        try
        {
            var requestedName = new AssemblyName(args.Name).Name;
            if (string.IsNullOrWhiteSpace(requestedName)) return null;

            if (RepackedAssemblyNames.Contains(requestedName))
                return typeof(GrasshopperAssemblyResolver).Assembly;

            if (!ShouldHandleAssembly(requestedName))
                return null;

            var candidate = FindCandidatePath(requestedName);
            if (candidate == null) return null;

            var loaded = Assembly.LoadFrom(candidate);
            RecordResolved(requestedName, candidate);
            SafeLogDebug("GrasshopperAssemblyResolver resolved {Name} from {Path}", requestedName, candidate);
            return loaded;
        }
        catch (Exception ex)
        {
            SafeLogWarning(ex, "GrasshopperAssemblyResolver failed resolving {Name}", args.Name);
            return null;
        }
    }

    private static Assembly? OnAlcResolving(AssemblyLoadContext alc, AssemblyName assemblyName)
    {
        var requestedName = assemblyName.Name;
        if (string.IsNullOrWhiteSpace(requestedName)) return null;

        if (RepackedAssemblyNames.Contains(requestedName))
            return typeof(GrasshopperAssemblyResolver).Assembly;

        if (!ShouldHandleAssembly(requestedName))
            return null;

        var candidate = FindCandidatePath(requestedName);
        if (candidate == null) return null;

        try
        {
            var loaded = alc.LoadFromAssemblyPath(candidate);
            RecordResolved(requestedName, candidate);
            SafeLogDebug("GrasshopperAssemblyResolver resolved {Name} into ALC {Alc}", requestedName, alc.Name ?? "Default");
            return loaded;
        }
        catch (Exception ex)
        {
            SafeLogWarning(ex, "GrasshopperAssemblyResolver failed resolving {Name} into ALC", requestedName);
            return null;
        }
    }

    private static string? FindCandidatePath(string requestedName)
    {
        foreach (var directory in ProbingDirectories)
        {
            if (!Directory.Exists(directory)) continue;
            var candidate = Path.Combine(directory, requestedName + ".dll");
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    private static void RecordResolved(string name, string path)
    {
        lock (ResolvedAssemblies)
        {
            ResolvedAssemblies.Add($"{name} -> {path}");
        }
    }

    private static void SafeLogDebug(string message, params object?[] propertyValues)
    {
        try { Log.Debug(message, propertyValues); }
        catch (Exception ex) { Log.Verbose(ex, "SafeLogDebug suppressed error"); }
    }

    private static void SafeLogWarning(Exception ex, string message, params object?[] propertyValues)
    {
        try { Log.Warning(ex, message, propertyValues); }
        catch (Exception logEx) { Log.Verbose(logEx, "SafeLogWarning suppressed error"); }
    }

    private static string[] InitializeProbingDirectories()
    {
        var dirs = new List<string>();

        var bridgeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrWhiteSpace(bridgeFolder) && Directory.Exists(bridgeFolder))
            dirs.Add(bridgeFolder);

        var envRhino = Environment.GetEnvironmentVariable("HPGRASSHOPPER_RHINO_DIR");
        if (!string.IsNullOrWhiteSpace(envRhino) && Directory.Exists(envRhino))
        {
            dirs.Add(Path.Combine(envRhino, "System", "netcore"));
            dirs.Add(Path.Combine(envRhino, "Plug-ins", "Grasshopper"));
        }

        var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        dirs.Add(Path.Combine(progFiles, "Autodesk", "Revit 2026"));
        dirs.Add(Path.Combine(progFiles, "Rhino 8", "System", "netcore"));
        dirs.Add(Path.Combine(progFiles, "Rhino 8", "Plug-ins", "Grasshopper"));

        var commonData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        dirs.Add(Path.Combine(commonData, "Autodesk", "Revit", "Addins", "2026", "RhinoInside.Revit", "R8"));

        return dirs.Where(Directory.Exists).Distinct().ToArray();
    }
}
