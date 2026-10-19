namespace HPDynamo.McpBridge.Entry;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Serilog;

/// <summary>
///     Proactively resolves Autodesk Dynamo and bridge assemblies for Revit 2026.
///     Catches requests for DynamoCore, DynamoServices, Serilog, CommunityToolkit,
///     and resolves them against bridge and DynamoForRevit install directories.
/// </summary>
public static class DynamoAssemblyResolver
{
    private static int _installed;
    private static readonly List<string> ResolvedAssemblies = new();
    private static readonly HashSet<string> HandledAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "DynamoCore", "DynamoCoreWpf", "DynamoServices", "DynamoApplications",
        "DesignScriptBuiltin", "DSCoreNodes", "ProtoCore", "ProtoGeometry",
        "DynamoRevitVersionSelector", "DynamoRevitDS", "RevitNodes", "RevitServices",
        "DSRevitNodesUI", "RevitAPI", "RevitAPIUI", "AdWindows",
        "HPRebar.Mcp.Contracts", "HPRebar.McpBridge.Core", "HPDynamo.McpBridge",
        "Serilog", "Serilog.Sinks.File", "CommunityToolkit.Mvvm"
    };

    private static readonly HashSet<string> DynamoSharedAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "DynamoCore", "DynamoCoreWpf", "DynamoServices", "DynamoApplications",
        "DesignScriptBuiltin", "DSCoreNodes", "ProtoCore", "ProtoGeometry",
        "DynamoRevitVersionSelector", "DynamoRevitDS", "RevitNodes", "RevitServices",
        "DSRevitNodesUI", "RevitAPI", "RevitAPIUI", "AdWindows"
    };

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

        var currentAlc = AssemblyLoadContext.GetLoadContext(typeof(DynamoAssemblyResolver).Assembly);
        if (currentAlc != null && currentAlc != AssemblyLoadContext.Default)
        {
            currentAlc.Resolving += OnAlcResolving;
        }
        else
        {
            // Only hook AppDomain in standalone test runners or environments where no isolated ALC exists
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }
    }

    public static void Uninstall()
    {
        if (Interlocked.Exchange(ref _installed, 0) == 0) return;

        var currentAlc = AssemblyLoadContext.GetLoadContext(typeof(DynamoAssemblyResolver).Assembly);
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
            {
                return typeof(DynamoAssemblyResolver).Assembly;
            }

            if (!HandledAssemblyNames.Contains(requestedName))
            {
                return null;
            }

            var candidate = FindCandidatePath(requestedName);
            if (candidate == null) return null;

            var loaded = Assembly.LoadFrom(candidate);
            RecordResolved(requestedName, candidate);
            SafeLogDebug("DynamoAssemblyResolver resolved {Name} from {Path}", requestedName, candidate);
            return loaded;
        }
        catch (Exception ex)
        {
            SafeLogWarning(ex, "DynamoAssemblyResolver failed resolving {Name}", args.Name);
            return null;
        }
    }

    private static Assembly? OnAlcResolving(AssemblyLoadContext alc, AssemblyName assemblyName)
    {
        var requestedName = assemblyName.Name;
        if (string.IsNullOrWhiteSpace(requestedName)) return null;

        if (RepackedAssemblyNames.Contains(requestedName))
        {
            return typeof(DynamoAssemblyResolver).Assembly;
        }

        if (!HandledAssemblyNames.Contains(requestedName))
        {
            return null;
        }

        var candidate = FindCandidatePath(requestedName);
        if (candidate == null) return null;

        try
        {
            var targetAlc = DynamoSharedAssemblyNames.Contains(requestedName)
                ? AssemblyLoadContext.Default
                : alc;

            var loaded = targetAlc.LoadFromAssemblyPath(candidate);
            RecordResolved(requestedName, candidate);
            SafeLogDebug("DynamoAssemblyResolver resolved {Name} from {Path} into {Alc}", requestedName, candidate, targetAlc.Name ?? "Default");
            return loaded;
        }
        catch (Exception ex)
        {
            SafeLogWarning(ex, "DynamoAssemblyResolver failed resolving {Name} from {Path}", requestedName, candidate);
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
        try
        {
            Log.Debug(message, propertyValues);
        }
        catch
        {
            // Suppress logging errors if logger is uninitialized during resolution
        }
    }

    private static void SafeLogWarning(Exception ex, string message, params object?[] propertyValues)
    {
        try
        {
            Log.Warning(ex, message, propertyValues);
        }
        catch
        {
            // Suppress logging errors if logger is uninitialized during resolution
        }
    }

    private static string[] InitializeProbingDirectories()
    {
        var dirs = new List<string>();

        var bridgeFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (!string.IsNullOrWhiteSpace(bridgeFolder) && Directory.Exists(bridgeFolder))
        {
            dirs.Add(bridgeFolder);
        }

        var envDir = Environment.GetEnvironmentVariable("HPDYNAMO_DYNAMO_DIR");
        if (!string.IsNullOrWhiteSpace(envDir) && Directory.Exists(envDir))
        {
            dirs.Add(envDir);
            dirs.Add(Path.Combine(envDir, "Revit"));
        }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var revitBase = Path.Combine(programFiles, "Autodesk", "Revit 2026");
        dirs.Add(revitBase);

        var dynamoBase = Path.Combine(revitBase, "AddIns", "DynamoForRevit");
        dirs.Add(dynamoBase);
        dirs.Add(Path.Combine(dynamoBase, "Revit"));

        return dirs.ToArray();
    }
}
