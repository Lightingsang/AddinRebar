using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Serilog;

namespace HPBimToolkit.Infrastructure;

/// <summary>
///     Does the toolkit's assembly bindings while Revit is still loading add-ins one by one, instead of lazily on first use.
///     A lazy binding goes through Revit's <c>AddInLoadContext.Load</c>, which walks every loaded assembly; when another
///     add-in is loading an assembly on another thread at that moment, the walk blocks and the UI thread deadlocks (seen
///     live twice: the first message handler's JIT asking for System.Text.Json while another add-in's EPPlus.Interfaces
///     was being loaded). Loading references by name is not enough — the JIT binds through each module's own type
///     references — so every type reference of the assemblies in the toolkit folder is resolved here, and the toolkit's
///     own code is compiled. Still lazy: reflection that names types by string. Best effort: a failure is logged, never
///     thrown, so the add-in always starts. Removable once Revit's add-in loader no longer walks loaded assemblies.
/// </summary>
internal static class DependencyPreloader
{
    /// <summary>Revit's own assemblies are already loaded by Revit; walking into them would only pull its internals in.</summary>
    private static readonly string[] HostPrefixes = ["Autodesk.", "RevitAPI", "AdWindows", "UIFramework"];

    private const int TypeRefTable = 0x01000000;

    private const BindingFlags AllDeclared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    /// <param name="roots">The toolkit's own assemblies: their code is also JIT-compiled.</param>
    public static void Preload(ILogger logger, params Assembly[] roots)
    {
        if (roots.Length == 0)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var folderAssemblies = LoadReferences(roots, logger, out var referenceCount);
            var typeRefs = folderAssemblies.Sum(assembly => ResolveTypeReferences(assembly, logger));
            var methods = roots.Sum(assembly => Compile(assembly, logger));
            logger.Debug("Preloaded {References} references, resolved {TypeRefs} type references of {Assemblies} toolkit-folder assemblies, compiled {Methods} methods in {Elapsed} ms",
                referenceCount, typeRefs, folderAssemblies.Count, methods, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            // Preloading only moves work earlier; whatever it could not do will bind lazily as before.
            logger.Warning(exception, "Preload stopped after {Elapsed} ms", stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>Loads every reference reachable from the roots; returns the assemblies that live in the toolkit's folder.</summary>
    private static List<Assembly> LoadReferences(Assembly[] roots, ILogger logger, out int referenceCount)
    {
        var folder = Path.GetDirectoryName(roots[0].Location);
        var inFolder = new List<Assembly>(roots);
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

                if (TryLoad(context, reference, logger) is not { } loaded || IsHost(name))
                {
                    continue;
                }

                pending.Enqueue(loaded);
                if (!loaded.IsDynamic && string.Equals(Path.GetDirectoryName(loaded.Location), folder, StringComparison.OrdinalIgnoreCase))
                {
                    inFolder.Add(loaded);
                }
            }
        }

        referenceCount = visited.Count;
        return inFolder.Distinct().ToList();
    }

    private static bool IsHost(string name) => HostPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));

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

    /// <summary>
    ///     Resolves every row of the module's TypeRef table: this binds each referenced assembly and fills the module's
    ///     type-reference map the JIT reads, generic code included, without compiling anything.
    /// </summary>
    private static int ResolveTypeReferences(Assembly assembly, ILogger logger)
    {
        var resolved = 0;
        foreach (var module in assembly.GetModules())
        {
            for (var row = 1; ; row++)
            {
                try
                {
                    module.ResolveType(TypeRefTable | row);
                    resolved++;
                }
                catch (ArgumentOutOfRangeException)
                {
                    break; // past the last row of the table
                }
                catch (Exception exception)
                {
                    // A reference to an optional assembly that is absent: nothing calls it, nothing to bind.
                    logger.Debug("Type reference {Row} of {Assembly} not resolved: {Reason}", row, assembly.GetName().Name, exception.Message);
                }
            }
        }

        return resolved;
    }

    /// <summary>JIT-compiles every non-generic method body of one assembly (closures and state machines included).</summary>
    private static int Compile(Assembly assembly, ILogger logger)
    {
        var compiled = 0;
        foreach (var type in LoadableTypes(assembly).Where(type => !type.ContainsGenericParameters))
        {
            foreach (var method in type.GetMethods(AllDeclared).Cast<MethodBase>().Concat(type.GetConstructors(AllDeclared)))
            {
                try
                {
                    if (method.IsAbstract || method.ContainsGenericParameters || method.GetMethodBody() is null)
                    {
                        continue;
                    }

                    RuntimeHelpers.PrepareMethod(method.MethodHandle);
                    compiled++;
                }
                catch (Exception exception)
                {
                    // Compiling ahead is an optimisation of timing only; a method that cannot be compiled now is
                    // compiled (or fails) when it first runs, exactly as without the warm-up.
                    logger.Debug("Warm-up skipped {Type}.{Method}: {Reason}", type.FullName, method.Name, exception.Message);
                }
            }
        }

        return compiled;
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
