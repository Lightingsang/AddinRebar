using System.Diagnostics;
using HPSap2000.McpBridge.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Serilog;

namespace HPSap2000.McpBridge.Service;

/// <summary>
///     Compiles and runs one trivial script at start-up and logs the outcome. Needs no SAP2000 attachment.
/// </summary>
public static class ScriptingSelfCheck
{
    private const string Probe = """
        progress(1, 1, "self-check");
        log("units " + units.Label + " mm/unit " + units.MmPerUnit);
        var forced = eUnits.kN_m_C;
        return "sap2000 wrapper " + forced + " | units " + units.Label + " | mm " + units.ToMm(1.0) + " | args " + (args.Int("x", 41) + 1);
        """;

    public static bool Run(ScriptCompiler compiler)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var roslyn = typeof(CSharpScript).Assembly;
            Log.Information("Roslyn {RoslynVersion} from {RoslynPath}", roslyn.GetName().Version, roslyn.Location);

            var globalsLocation = typeof(SapScriptGlobals).Assembly.Location;
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

            var globals = new SapScriptGlobals(null!, null!, SapUnitsPolicy.Units, CancellationToken.None, _ => { }, (_, _, _) => { });
            var value = compiled.Script!.RunAsync(globals).GetAwaiter().GetResult().ReturnValue;

            Log.Information("MCP scripting self-check OK in {Elapsed} ms: {Value} (SAP2000v1.dll from {Path}, {Version})",
                stopwatch.ElapsedMilliseconds, value, SapAssemblyResolver.ResolvedPath ?? "<not loaded yet>", SapAssemblyResolver.WrapperFileVersion);
            return true;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "MCP scripting self-check FAILED after {Elapsed} ms", stopwatch.ElapsedMilliseconds);
            return false;
        }
    }
}
