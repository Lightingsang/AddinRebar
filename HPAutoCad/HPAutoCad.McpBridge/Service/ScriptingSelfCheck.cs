using System.Diagnostics;
using System.Runtime.Loader;
using HPAutoCad.McpBridge.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Serilog;

namespace HPAutoCad.McpBridge.Service;

/// <summary>
///     Compiles and runs one trivial script at start-up and logs the outcome. It proves the three things
///     that cannot be unit-tested outside AutoCAD: Roslyn 5.9 loads into the bridge's own load context
///     (not AutoCAD's Roslyn 4.10 or the framework's Immutable 8.0), the globals type binds to the same
///     instance the script was compiled against, and the AutoCAD API assemblies resolved to the ones
///     acad.exe already loaded. It also warms the compiler so the first real request does not pay for it.
/// </summary>
public static class ScriptingSelfCheck
{
    /// <summary>Needs no document: the extension initialises before the first drawing may be ready.</summary>
    private const string Probe = "return \"autocad \" + Autodesk.AutoCAD.ApplicationServices.Core.Application.Version + \" | units \" + units.Label;";

    public static bool Run(ScriptCompiler compiler)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var roslyn = typeof(CSharpScript).Assembly;
            var immutable = typeof(System.Collections.Immutable.ImmutableArray).Assembly;
            Log.Information("Roslyn {RoslynVersion} in load context '{RoslynContext}'; System.Collections.Immutable {ImmutableVersion} in '{ImmutableContext}'",
                roslyn.GetName().Version, ContextOf(roslyn), immutable.GetName().Version, ContextOf(immutable));

            var compiled = compiler.GetOrCompile(Probe);
            if (!compiled.Succeeded)
            {
                Log.Error("MCP scripting self-check: compile failed: {Diagnostics}", string.Join("; ", compiled.Diagnostics.Select(d => d.Message)));
                return false;
            }

            var globals = new AutocadScriptGlobals(null!, null!, null!, null!, null!, ScriptUnits.Millimeters, CancellationToken.None, _ => { }, (_, _, _) => { });
            var value = compiled.Script!.RunAsync(globals).GetAwaiter().GetResult().ReturnValue;

            Log.Information("MCP scripting self-check OK in {Elapsed} ms: {Value}", stopwatch.ElapsedMilliseconds, value);
            return true;
        }
        catch (Exception exception)
        {
            // An InvalidCastException here means the globals type was loaded twice (load-context split);
            // a FileLoadException means Roslyn or its dependencies resolved to the wrong copy.
            Log.Error(exception, "MCP scripting self-check FAILED after {Elapsed} ms", stopwatch.ElapsedMilliseconds);
            return false;
        }
    }

    public static string ContextOf(System.Reflection.Assembly assembly) => AssemblyLoadContext.GetLoadContext(assembly)?.Name ?? "<none>";
}
