using Autodesk.Navisworks.Api;
using HPRebar.McpBridge.Core.Scripting;

namespace HPNavis.McpBridge.Model;

/// <summary>
///     What a script sees as its globals. The public fields are the script's API, so their names are
///     deliberately lowercase (`doc`, `app`, `units`, …) — the tool description promises exactly these
///     names. There is no `tr`: the bridge owns the only transaction and the guard refuses
///     <c>BeginTransaction</c>, so a script edits `doc` directly and the bridge decides commit/undo.
/// </summary>
public sealed class NavisScriptGlobals
{
    // ReSharper disable InconsistentNaming — names are the script-facing contract, see summary.
    /// <summary>The active document (<c>Application.ActiveDocument</c>).</summary>
    public readonly Document doc;

    /// <summary>Version, documents and module facts; `Application` itself is a static class.</summary>
    public readonly NavisApp app;

    /// <summary>mm ↔ <c>Document.Units</c>: every length the API hands out is in document units.</summary>
    public readonly ScriptUnits units;

    /// <summary>Cooperative cancellation: timeout or cancel_execution. Long loops should check it.</summary>
    public readonly CancellationToken ct;

    /// <summary>Appends a line to the result's `logs` (capped by settings).</summary>
    public readonly Action<string> log;

    /// <summary>progress(current, total, message) — forwarded to the AI client as a progress notification.</summary>
    public readonly Action<int, int, string> progress;

    /// <summary>Data sent beside the code (`args.Str("name")`); <see cref="ScriptArgs.Empty"/> when none.</summary>
    public readonly ScriptArgs args;
    // ReSharper restore InconsistentNaming

    public NavisScriptGlobals(Document doc, NavisApp app, ScriptUnits units, CancellationToken ct, Action<string> log,
        Action<int, int, string> progress, ScriptArgs? args = null)
    {
        this.doc = doc;
        this.app = app;
        this.units = units;
        this.ct = ct;
        this.log = log;
        this.progress = progress;
        this.args = args ?? ScriptArgs.Empty;
    }
}
