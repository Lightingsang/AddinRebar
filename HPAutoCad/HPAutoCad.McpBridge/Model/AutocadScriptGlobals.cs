using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPRebar.McpBridge.Core.Scripting;

namespace HPAutoCad.McpBridge.Model;

/// <summary>
///     What a script sees as its globals. The public fields are the script's API, so their names are
///     deliberately lowercase (`doc`, `db`, `ed`, `tr`, …) — they read like the AutoCAD samples the model
///     learned from, and the tool description promises exactly these names. `tr` is the outermost
///     transaction the bridge opened around the run; the script reads and writes through it and never
///     commits or aborts it (the guard rejects that). `app` is the document collection because
///     `Application` itself is a static class.
/// </summary>
public sealed class AutocadScriptGlobals
{
    // ReSharper disable InconsistentNaming — names are the script-facing contract, see summary.
    public readonly Document doc;
    public readonly Database db;
    public readonly Editor ed;
    public readonly DocumentCollection app;
    public readonly Transaction tr;

    /// <summary>mm ↔ drawing units, from the drawing's INSUNITS.</summary>
    public readonly ScriptUnits units;

    /// <summary>Cooperative cancellation: timeout or cancel_execution. Long loops should check it.</summary>
    public readonly CancellationToken ct;

    /// <summary>Appends a line to the result's `logs` (capped by settings).</summary>
    public readonly Action<string> log;

    /// <summary>progress(current, total, message) — forwarded to the AI client as a progress notification.</summary>
    public readonly Action<int, int, string> progress;

    /// <summary>Data sent beside the code (`args.Double("radiusMm")`); <see cref="ScriptArgs.Empty"/> when none.</summary>
    public readonly ScriptArgs args;
    // ReSharper restore InconsistentNaming

    public AutocadScriptGlobals(Document doc, Database db, Editor ed, DocumentCollection app, Transaction tr, ScriptUnits units,
        CancellationToken ct, Action<string> log, Action<int, int, string> progress, ScriptArgs? args = null)
    {
        this.doc = doc;
        this.db = db;
        this.ed = ed;
        this.app = app;
        this.tr = tr;
        this.units = units;
        this.ct = ct;
        this.log = log;
        this.progress = progress;
        this.args = args ?? ScriptArgs.Empty;
    }
}
