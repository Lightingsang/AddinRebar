using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
// civil-only: begin
using Autodesk.Civil.ApplicationServices;
// civil-only: end
using HPRebar.McpBridge.Core.Scripting;

namespace HPCivil3d.McpBridge.Model;

/// <summary>
///     What a script sees as its globals. The public fields are the script's API, so their names are
///     deliberately lowercase (`doc`, `db`, `ed`, `tr`, …) — they read like the AutoCAD samples the model
///     learned from, and the tool description promises exactly these names. `tr` is the outermost
///     transaction the bridge opened around the run; the script reads and writes through it and never
///     commits or aborts it (the guard rejects that). `app` is the document collection because
///     `Application` itself is a static class.
// civil-only: begin
///     `civil` is the active <see cref="CivilDocument"/> — the root of every Civil query (alignments, surfaces,
///     corridors, pipe networks, COGO points, settings, styles); every drawing opened in Civil 3D has one, null
///     is only the defensive path. `units` follows the Civil drawing unit (Meters or Feet), not INSUNITS.
// civil-only: end
/// </summary>
public sealed class Civil3dScriptGlobals
{
    // ReSharper disable InconsistentNaming — names are the script-facing contract, see summary.
    public readonly Document doc;
    public readonly Database db;
    public readonly Editor ed;
    public readonly DocumentCollection app;
    public readonly Transaction tr;

    // civil-only: begin
    /// <summary>The active Civil document (every drawing opened in Civil 3D has one; null only when the API refuses).</summary>
    public readonly CivilDocument? civil;
    // civil-only: end

    /// <summary>mm ↔ the Civil drawing unit (Meters or Feet from the drawing settings); INSUNITS only when Civil has no unit.</summary>
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

    public Civil3dScriptGlobals(Document doc, Database db, Editor ed, DocumentCollection app, Transaction tr, CivilDocument? civil, ScriptUnits units,
        CancellationToken ct, Action<string> log, Action<int, int, string> progress, ScriptArgs? args = null)
    {
        this.doc = doc;
        this.db = db;
        this.ed = ed;
        this.app = app;
        this.tr = tr;
        // civil-only: begin
        this.civil = civil;
        // civil-only: end
        this.units = units;
        this.ct = ct;
        this.log = log;
        this.progress = progress;
        this.args = args ?? ScriptArgs.Empty;
    }
}
