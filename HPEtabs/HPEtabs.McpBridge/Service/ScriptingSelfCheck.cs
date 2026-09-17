using System.Diagnostics;
using HPEtabs.McpBridge.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Serilog;

namespace HPEtabs.McpBridge.Service;

/// <summary>
///     Compiles and runs one trivial script at start-up and logs the outcome. Needs no ETABS attachment: it
///     proves that Roslyn loads, that the compiler's references carry a real <c>Assembly.Location</c> (a
///     single-file publish would leave them empty and every compile would fail), that the ETABSv1 wrapper
///     resolved from the install folder and that its types are usable from a script (an <c>eUnits</c> value is
///     named without touching a model). Also warms the compiler.
/// </summary>
public static class ScriptingSelfCheck
{
    /// <summary>Touches every global except the model (null while detached) and names one wrapper type.</summary>
    private const string Probe = """
        progress(1, 1, "self-check");
        log("units " + units.Label + " mm/unit " + units.MmPerUnit);
        var forced = eUnits.kN_mm_C;
        return "etabs wrapper " + forced + " | units " + units.Label + " | mm " + units.ToMm(1.0) + " | args " + (args.Int("x", 41) + 1);
        """;

    public static bool Run(ScriptCompiler compiler)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var roslyn = typeof(CSharpScript).Assembly;
            Log.Information("Roslyn {RoslynVersion} from {RoslynPath}", roslyn.GetName().Version, roslyn.Location);

            var globalsLocation = typeof(EtabsScriptGlobals).Assembly.Location;
            var engineLocation = typeof(ScriptArgs).Assembly.Location;
            if (string.IsNullOrEmpty(globalsLocation) || string.IsNullOrEmpty(engineLocation))
            {
                Log.Error("MCP scripting self-check FAILED: the bridge or engine assembly has no file location (single-file publish?); the script compiler cannot reference them");
                return false;
            }

            var compiled = compiler.GetOrCompile(Probe);
            if (!compiled.Succeeded)
            {
                Log.Error("MCP scripting self-check: compile failed: {Diagnostics}", string.Join("; ", compiled.Diagnostics.Select(d => d.Message)));
                return false;
            }

            var globals = new EtabsScriptGlobals(null!, null!, EtabsUnitsPolicy.Units, CancellationToken.None, _ => { }, (_, _, _) => { });
            var value = compiled.Script!.RunAsync(globals).GetAwaiter().GetResult().ReturnValue;

            Log.Information("MCP scripting self-check OK in {Elapsed} ms: {Value} (ETABSv1.dll from {Path}, {Version})",
                stopwatch.ElapsedMilliseconds, value, EtabsAssemblyResolver.ResolvedPath ?? "<not loaded yet>", EtabsAssemblyResolver.WrapperFileVersion);
            return true;
        }
        catch (Exception exception)
        {
            // A FileNotFoundException for ETABSv1 means the resolver was installed too late or found no install folder.
            Log.Error(exception, "MCP scripting self-check FAILED after {Elapsed} ms", stopwatch.ElapsedMilliseconds);
            return false;
        }
    }
}
