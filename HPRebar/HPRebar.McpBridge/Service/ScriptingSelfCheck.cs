using System.Diagnostics;
using Autodesk.Revit.UI;
using HPRebar.McpBridge.Core.Scripting;
using HPRebar.McpBridge.Model;
using Serilog;

namespace HPRebar.McpBridge.Service;

/// <summary>
///     Compiles and runs one trivial script at startup, with real globals, and logs the outcome. It proves
///     the two things that cannot be unit-tested outside Revit — Roslyn loads inside the add-in's load
///     context, and the globals type binds to the same instance the script was compiled against — and it
///     warms the compiler so the first real request does not pay the JIT/compile cost.
/// </summary>
public static class ScriptingSelfCheck
{
    private const string Probe = "return \"revit \" + app.VersionNumber + \", doc: \" + (doc == null ? \"none\" : doc.Title);";

    public static void Run(ScriptCompiler compiler, UIApplication uiapp)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var compiled = compiler.GetOrCompile(Probe);
            if (!compiled.Succeeded)
            {
                Log.Error("MCP scripting self-check: compile failed: {Diagnostics}", string.Join("; ", compiled.Diagnostics.Select(d => d.Message)));
                return;
            }

            var uidoc = uiapp.ActiveUIDocument;
            var globals = new ScriptGlobals(uidoc?.Document!, uidoc!, uiapp.Application, uiapp, CancellationToken.None, _ => { }, (_, _, _) => { });
            var value = compiled.Script!.RunAsync(globals).GetAwaiter().GetResult().ReturnValue;

            Log.Information("MCP scripting self-check OK in {Elapsed} ms: {Value}", stopwatch.ElapsedMilliseconds, value);
        }
        catch (Exception exception)
        {
            // An InvalidCastException here means the globals type was loaded twice (load-context split).
            Log.Error(exception, "MCP scripting self-check FAILED after {Elapsed} ms", stopwatch.ElapsedMilliseconds);
        }
    }
}
