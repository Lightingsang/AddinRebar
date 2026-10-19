namespace HPGrasshopper.McpBridge.Coordinator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPGrasshopper.McpBridge.Support;
using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;

/// <summary>
///     Provides assembly references and available imports needed by the Roslyn script compiler.
/// </summary>
internal static class GrasshopperCompilerReferences
{
    public static Assembly[] All
    {
        get
        {
            var list = new List<Assembly>
            {
                typeof(object).Assembly,
                typeof(Enumerable).Assembly,
                typeof(List<>).Assembly,
                typeof(ScriptArgs).Assembly,
                typeof(System.Text.Json.JsonElement).Assembly,
                typeof(GrasshopperScriptGlobals).Assembly
            };

            TryAdd(list, RevitDocAssembly);
            TryAdd(list, RevitUiAssembly);
            TryAdd(list, RhinoCommonAssembly);
            TryAdd(list, GrasshopperAssembly);
            TryAdd(list, GhIoAssembly);
            TryAdd(list, RhinoInsideRevitAssembly);

            return list.ToArray();
        }
    }

    public static string ComputeFingerprint()
    {
        var refs = All;
        return string.Join(";", refs.Select(a => $"{a.GetName().Name}:{a.GetName().Version}").OrderBy(x => x));
    }

    public static string[] ImportsFor(Assembly[] references)
    {
        var names = references.Select(a => a.GetName().Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return HostScriptContracts.GrasshopperImports
            .Where(ns => !ns.StartsWith("Rhino", StringComparison.Ordinal) || names.Contains("RhinoCommon"))
            .Where(ns => !ns.StartsWith("Grasshopper", StringComparison.Ordinal) || names.Contains("Grasshopper"))
            .Where(ns => !ns.StartsWith("GH_IO", StringComparison.Ordinal) || names.Contains("GH_IO"))
            .Where(ns => !ns.StartsWith("RhinoInside.Revit", StringComparison.Ordinal) || names.Contains("RhinoInside.Revit"))
            .Where(ns => ns != "Autodesk.Revit.DB" || names.Contains("RevitAPI"))
            .Where(ns => ns != "Autodesk.Revit.UI" || names.Contains("RevitAPIUI"))
            .ToArray();
    }

    private static void TryAdd(List<Assembly> list, Func<Assembly?> getter)
    {
        try
        {
            var asm = getter();
            if (asm != null && !list.Contains(asm))
            {
                list.Add(asm);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "GrasshopperCompilerReferences: Assembly omitted in current environment");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? RevitDocAssembly() =>
        ResolveAssembly("RevitAPI") ?? TryDirect(() => typeof(Document).Assembly);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? RevitUiAssembly() =>
        ResolveAssembly("RevitAPIUI") ?? TryDirect(() => typeof(UIApplication).Assembly);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? RhinoCommonAssembly() => ResolveAssembly("RhinoCommon");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? GrasshopperAssembly() => ResolveAssembly("Grasshopper");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? GhIoAssembly() => ResolveAssembly("GH_IO");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? RhinoInsideRevitAssembly() => ResolveAssembly("RhinoInside.Revit");

    private static Assembly? TryDirect(Func<Assembly> loader)
    {
        try { return loader(); }
        catch (Exception ex)
        {
            Log.Debug(ex, "TryDirect assembly load failed");
            return null;
        }
    }

    private static Assembly? ResolveAssembly(string name)
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
        if (loaded != null) return loaded;

        try { return Assembly.Load(name); }
        catch (Exception ex)
        {
            Log.Debug(ex, "Assembly {Name} not loaded by name in current load context", name);
        }

        // Only probe disk when NOT running inside Revit process (e.g. standalone test runner).
        // When running in Revit, McNeel's RhinoInside.Revit must be the sole loader of Rhino assemblies.
        bool isInsideRevit = string.Equals(System.Diagnostics.Process.GetCurrentProcess().ProcessName, "Revit", StringComparison.OrdinalIgnoreCase);
        if (!isInsideRevit)
        {
            try
            {
                var probingDirs = GetProbingDirectories();
                foreach (var dir in probingDirs)
                {
                    var path = Path.Combine(dir, name + ".dll");
                    if (File.Exists(path)) return Assembly.LoadFrom(path);
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Assembly {Name} resolution from probing directories failed", name);
            }
        }

        return null;
    }

    private static string[] GetProbingDirectories()
    {
        var dirs = new List<string>();
        var executing = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrEmpty(executing)) dirs.Add(executing);

        var envRevit = Environment.GetEnvironmentVariable("HPREBAR_REVIT_DIR");
        if (!string.IsNullOrWhiteSpace(envRevit) && Directory.Exists(envRevit)) dirs.Add(envRevit);

        var envRhino = Environment.GetEnvironmentVariable("HPGRASSHOPPER_RHINO_DIR");
        if (!string.IsNullOrWhiteSpace(envRhino) && Directory.Exists(envRhino))
        {
            dirs.Add(envRhino);
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
