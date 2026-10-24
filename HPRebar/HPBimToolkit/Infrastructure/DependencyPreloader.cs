using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Serilog;

namespace HPBimToolkit.Infrastructure;

/// <summary>
///     Resolves the assemblies the toolkit can touch while Revit is loading add-ins, instead of lazily on first use.
///     Lazy resolution goes through Revit's add-in load context; done from a WebView2 message callback while Revit was
///     still opening a model, it deadlocked the UI thread (seen live: stuck in AddInLoadContext.Load → GetCodeBase).
///     Once an assembly is known to the toolkit's load context, later JIT never asks Revit again.
/// </summary>
public static class DependencyPreloader
{
    /// <summary>Revit's own assemblies are already loaded by Revit; walking into them would only pull its internals in.</summary>
    private static readonly string[] HostPrefixes = ["Autodesk.", "RevitAPI", "AdWindows", "UIFramework", "Nice3point."];

    public static void Preload(ILogger logger, params Assembly[] roots)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<Assembly>(roots);
        while (pending.TryDequeue(out var assembly))
        {
            var context = AssemblyLoadContext.GetLoadContext(assembly) ?? AssemblyLoadContext.Default;
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name is not { } name || !visited.Add(name))
                {
                    continue;
                }

                if (TryLoad(context, reference, logger) is { } loaded && !HostPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    pending.Enqueue(loaded);
                }
            }
        }

        logger.Debug("Preloaded {Count} referenced assemblies", visited.Count);
    }

    private static Assembly? TryLoad(AssemblyLoadContext context, AssemblyName reference, ILogger logger)
    {
        try
        {
            return context.LoadFromAssemblyName(reference);
        }
        catch (Exception exception) when (exception is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            // A reference only used on paths that never run (a compile-time annotation assembly, say).
            logger.Debug("Preload skipped {Assembly}: {Reason}", reference.Name, exception.Message);
            return null;
        }
    }
}
