using System.Diagnostics;
using System.Reflection;
using HPNavis.McpBridge.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Serilog;

namespace HPNavis.McpBridge.Service;

/// <summary>
///     Compiles and runs one trivial script at start-up and logs the outcome. It proves what cannot be
///     unit-tested outside Roamer.exe: Roslyn 5.9 and its System.* companions bound to the copies beside
///     the plugin (the assembly resolver did its job — another plugin's resolver could have answered
///     first), the globals type is the one the script was compiled against, and the Navisworks API
///     assemblies resolved to the ones Roamer already loaded. Also warms the compiler.
/// </summary>
public static class ScriptingSelfCheck
{
    /// <summary>
    ///     Needs no document: the plugin loads before any file is open. Touches every global the scripts get
    ///     (units conversion, progress, log, args) and builds a Search with a SearchCondition so the default
    ///     imports and the Navisworks API reference are proven, not assumed.
    /// </summary>
    private const string Probe = """
        var search = new Search();
        search.SearchConditions.Add(SearchCondition.HasPropertyByDisplayName("Item", "Name").DisplayStringContains("wall"));
        progress(1, 1, "self-check");
        log("units " + units.Label + " mm/unit " + units.MmPerUnit);
        return "navisworks " + app.Version + " | year " + app.Year + " | units " + units.Label + " | conditions " + search.SearchConditions.Count + " | mm " + units.ToMm(1.0) + " | args " + (args.Int("x", 41) + 1);
        """;

    /// <summary>Assemblies that must come from the plugin folder; a foreign copy means the resolver lost the race.</summary>
    private static readonly string[] MustBeOurs =
    [
        "Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp", "Microsoft.CodeAnalysis.Scripting", "Microsoft.CodeAnalysis.CSharp.Scripting",
        "System.Collections.Immutable", "System.Reflection.Metadata", "HPRebar.McpBridge.Core", "HPRebar.Mcp.Contracts",
    ];

    public static bool Run(ScriptCompiler compiler, NavisApp app, string pluginFolder)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var roslyn = typeof(CSharpScript).Assembly;
            var immutable = typeof(System.Collections.Immutable.ImmutableArray).Assembly;
            Log.Information("Roslyn {RoslynVersion} from {RoslynPath}; System.Collections.Immutable {ImmutableVersion} from {ImmutablePath}",
                roslyn.GetName().Version, roslyn.Location, immutable.GetName().Version, immutable.Location);

            var compiled = compiler.GetOrCompile(Probe);
            if (!compiled.Succeeded)
            {
                Log.Error("MCP scripting self-check: compile failed: {Diagnostics}", string.Join("; ", compiled.Diagnostics.Select(d => d.Message)));
                return false;
            }

            var globals = new NavisScriptGlobals(null!, app, ScriptUnits.Millimeters, CancellationToken.None, _ => { }, (_, _, _) => { });
            var value = compiled.Script!.RunAsync(globals).GetAwaiter().GetResult().ReturnValue;

            var foreign = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => MustBeOurs.Contains(a.GetName().Name, StringComparer.OrdinalIgnoreCase) && !PluginAssemblyResolver.IsInFolder(a, pluginFolder))
                .Select(a => $"{a.GetName().Name} {a.GetName().Version} from {SafeLocation(a)}")
                .ToArray();

            foreach (var line in PluginAssemblyResolver.ResolvedNames) Log.Debug("MCP bridge resolved {Resolution}", line);

            if (foreign.Length > 0)
            {
                Log.Error("MCP scripting self-check FAILED: {Count} engine assemblies were loaded from outside the plugin folder: {Foreign}", foreign.Length, string.Join("; ", foreign));
                return false;
            }

            Log.Information("MCP scripting self-check OK in {Elapsed} ms ({Resolved} assemblies resolved by the plugin): {Value}",
                stopwatch.ElapsedMilliseconds, PluginAssemblyResolver.ResolvedNames.Count, value);
            return true;
        }
        catch (Exception exception)
        {
            // An InvalidCastException here means the globals type was loaded twice; a FileLoadException means
            // Roslyn or its dependencies resolved to the wrong copy or not at all.
            Log.Error(exception, "MCP scripting self-check FAILED after {Elapsed} ms", stopwatch.ElapsedMilliseconds);
            return false;
        }
    }

    private static string SafeLocation(Assembly assembly)
    {
        try { return assembly.Location; }
        catch { return "<dynamic>"; }
    }
}
