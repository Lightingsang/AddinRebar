using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPRebar.McpBridge.Core.Scripting;
using RevitApplication = Autodesk.Revit.ApplicationServices.Application;

namespace HPRebar.McpBridge.Model;

/// <summary>
///     What a script sees as its globals. The public fields are the script's API, so their names are
///     deliberately lowercase (`doc`, `uidoc`, …) — they read like the Revit samples the model learned from,
///     and every tool description promises exactly these names.
/// </summary>
public sealed class ScriptGlobals
{
    // ReSharper disable InconsistentNaming — names are the script-facing contract, see summary.
    public readonly Document doc;
    public readonly UIDocument uidoc;
    public readonly RevitApplication app;
    public readonly UIApplication uiapp;

    /// <summary>Cooperative cancellation: timeout or cancel_execution. Long loops should check it.</summary>
    public readonly CancellationToken ct;

    /// <summary>Appends a line to the result's `logs` (capped by settings).</summary>
    public readonly Action<string> log;

    /// <summary>progress(current, total, message) — forwarded to the AI client as a progress notification.</summary>
    public readonly Action<int, int, string> progress;

    /// <summary>Data sent beside the code (`args.Double("spacing")`); <see cref="ScriptArgs.Empty"/> when none.</summary>
    public readonly ScriptArgs args;
    // ReSharper restore InconsistentNaming

    public ScriptGlobals(Document doc, UIDocument uidoc, RevitApplication app, UIApplication uiapp,
        CancellationToken ct, Action<string> log, Action<int, int, string> progress, ScriptArgs? args = null)
    {
        this.args = args ?? ScriptArgs.Empty;
        this.doc = doc;
        this.uidoc = uidoc;
        this.app = app;
        this.uiapp = uiapp;
        this.ct = ct;
        this.log = log;
        this.progress = progress;
    }
}
