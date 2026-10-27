using System.Diagnostics;
using Autodesk.Revit.UI;
using HPRebar.McpBridge.Core.Scripting;
using HPRebar.McpBridge.Model;
using Serilog;

namespace HPRebar.McpBridge.Service;

/// <summary>
///     Compiles and runs one trivial script with real globals and logs the outcome. It proves
///     the two things that cannot be unit-tested outside Revit — Roslyn loads inside the add-in's load
///     context, and the globals type binds to the same instance the script was compiled against — and it
///     warms the compiler off the UI thread so startup never pays the Roslyn JIT/compile cost.
/// </summary>
public static class ScriptingSelfCheck
{
    private const string Probe = "return \"revit \" + app.VersionNumber + \", doc: \" + (doc == null ? \"none\" : doc.Title);";

    public static async Task RunAsync(ScriptCompiler compiler, Func<Func<UIApplication, object?>, Task<object>> runOnRevitThread)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var compiled = await Task.Run(() => compiler.GetOrCompile(Probe)).ConfigureAwait(false);
            if (!compiled.Succeeded)
            {
                Log.Error(
                    "MCP scripting self-check: compile failed: {Diagnostics}",
                    string.Join("; ", compiled.Diagnostics.Select(d => d.Message)));
                return;
            }

            var value = await runOnRevitThread(uiapp => RunProbe(compiled.Script!, uiapp)).ConfigureAwait(false);

            Log.Information("MCP scripting self-check OK in {Elapsed} ms: {Value}", stopwatch.ElapsedMilliseconds, value);
        }
        catch (Exception exception)
        {
            // An InvalidCastException here means the globals type was loaded twice (load-context split).
            Log.Error(exception, "MCP scripting self-check FAILED after {Elapsed} ms", stopwatch.ElapsedMilliseconds);
        }
    }

    private static object? RunProbe(Microsoft.CodeAnalysis.Scripting.Script<object> script, UIApplication uiapp)
    {
        var uidoc = uiapp.ActiveUIDocument;
        var globals = new ScriptGlobals(
            uidoc?.Document!,
            uidoc!,
            uiapp.Application,
            uiapp,
            CancellationToken.None,
            _ => { },
            (_, _, _) => { });

        return script.RunAsync(globals).GetAwaiter().GetResult().ReturnValue;
    }
}
