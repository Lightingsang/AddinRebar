namespace HPDynamo.McpBridge.Coordinator;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Dynamo.Graph.Nodes;
using Dynamo.Models;
using Dynamo.ViewModels;
using HPDynamo.McpBridge.Support;
using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Scripting;
using Serilog;

/// <summary>
///     Provides assembly references and available imports needed by the Roslyn script compiler.
/// </summary>
internal static class DynamoCompilerReferences
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
                typeof(DynamoScriptGlobals).Assembly
            };

            TryAdd(list, DynamoModelAssembly);
            TryAdd(list, DynamoViewModelAssembly);
            TryAdd(list, NodeModelAssembly);
            TryAdd(list, RevitDocAssembly);
            TryAdd(list, RevitUiAssembly);
            TryAdd(list, ProtoCoreAssembly);
            TryAdd(list, DesignScriptBuiltinAssembly);
            TryAdd(list, DynamoApplicationsAssembly);
            TryAdd(list, RevitNodesAssembly);
            TryAdd(list, ProtoGeometryAssembly);
            TryAdd(list, DynamoServicesAssembly);
            TryAdd(list, DynamoRevitDSAssembly);
            TryAdd(list, RevitServicesAssembly);

            return list.ToArray();
        }
    }

    public static string[] ImportsFor(Assembly[] references)
    {
        var names = references.Select(a => a.GetName().Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return HostScriptContracts.DynamoImports
            .Where(ns => ns != "Autodesk.DesignScript.Geometry" || names.Contains("ProtoGeometry"))
            .Where(ns => !ns.StartsWith("Dynamo.", StringComparison.Ordinal) || names.Contains("DynamoCore") || names.Contains("DynamoServices") || names.Contains("DynamoCoreWpf") || names.Contains("DynamoApplications"))
            .Where(ns => ns != "Dynamo.ViewModels" || names.Contains("DynamoCoreWpf"))
            .Where(ns => ns != "Dynamo.Applications" || names.Contains("DynamoApplications"))
            .Where(ns => ns != "Revit.Elements" || names.Contains("RevitNodes"))
            .Where(ns => ns != "ProtoCore.Mirror" || names.Contains("ProtoCore"))
            .Where(ns => ns != "DesignScript.Builtin" || names.Contains("DesignScriptBuiltin"))
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
            // Host assembly not available in offline test runner environment
            Log.Debug(ex, "DynamoCompilerReferences: Assembly omitted in current environment");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? RevitDocAssembly() =>
        ResolveAssembly("RevitAPI") ?? TryDirect(() => typeof(Document).Assembly);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? RevitUiAssembly() =>
        ResolveAssembly("RevitAPIUI") ?? TryDirect(() => typeof(UIApplication).Assembly);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? DynamoModelAssembly() =>
        ResolveAssembly("DynamoCore") ?? TryDirect(() => typeof(DynamoModel).Assembly);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? DynamoViewModelAssembly() =>
        ResolveAssembly("DynamoCoreWpf") ?? TryDirect(() => typeof(DynamoViewModel).Assembly);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? NodeModelAssembly() =>
        ResolveAssembly("DynamoCore") ?? TryDirect(() => typeof(NodeModel).Assembly);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? ProtoCoreAssembly() =>
        ResolveAssembly("ProtoCore") ?? TryDirect(() => typeof(ProtoCore.Mirror.MirrorData).Assembly);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? DesignScriptBuiltinAssembly() => ResolveAssembly("DesignScriptBuiltin");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? DynamoApplicationsAssembly() =>
        ResolveAssembly("DynamoApplications") ?? TryDirect(() => typeof(Dynamo.Applications.DynamoRevit).Assembly);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? RevitNodesAssembly() =>
        ResolveAssembly("RevitNodes") ?? TryDirect(() => typeof(Revit.Elements.Element).Assembly);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? RevitServicesAssembly() =>
        ResolveAssembly("RevitServices") ?? TryDirect(() => typeof(RevitServices.Persistence.DocumentManager).Assembly);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? ProtoGeometryAssembly() => ResolveAssembly("ProtoGeometry");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? DynamoServicesAssembly() =>
        ResolveAssembly("DynamoServices") ?? TryDirect(() => typeof(DynamoServices.RegisterForTraceAttribute).Assembly);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Assembly? DynamoRevitDSAssembly() => ResolveAssembly("DynamoRevitDS");

    private static Assembly? TryDirect(Func<Assembly> loader)
    {
        try { return loader(); }
        catch (Exception ex)
        {
            // Direct type resolution failed in offline context
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
            // Assembly not in standard load context
            Log.Debug(ex, "Assembly {Name} not loaded by name in current load context", name);
        }

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
            // File resolution from probing directories failed
            Log.Debug(ex, "Assembly {Name} resolution from probing directories failed", name);
        }

        return null;
    }

    private static string[] GetProbingDirectories()
    {
        var dirs = new List<string>();
        var executing = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrEmpty(executing)) dirs.Add(executing);

        try
        {
            var protoCore = typeof(ProtoCore.Mirror.MirrorData).Assembly.Location;
            var dir = Path.GetDirectoryName(protoCore);
            if (!string.IsNullOrEmpty(dir)) dirs.Add(dir);
        }
        catch { /* Assembly location unavailable in dynamic context */ }

        var envRevit = Environment.GetEnvironmentVariable("HPREBAR_REVIT_DIR");
        if (!string.IsNullOrWhiteSpace(envRevit) && Directory.Exists(envRevit)) dirs.Add(envRevit);

        var envDynamo = Environment.GetEnvironmentVariable("HPDYNAMO_DYNAMO_DIR");
        if (!string.IsNullOrWhiteSpace(envDynamo) && Directory.Exists(envDynamo))
        {
            dirs.Add(envDynamo);
            dirs.Add(Path.Combine(envDynamo, "Revit"));
        }

        var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var revitBase = Path.Combine(progFiles, "Autodesk", "Revit 2026");
        dirs.Add(revitBase);
        dirs.Add(Path.Combine(revitBase, "AddIns", "DynamoForRevit"));
        dirs.Add(Path.Combine(revitBase, "AddIns", "DynamoForRevit", "Revit"));

        return dirs.Where(Directory.Exists).Distinct().ToArray();
    }
}
