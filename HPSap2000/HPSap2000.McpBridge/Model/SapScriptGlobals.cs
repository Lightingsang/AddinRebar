using SAP2000v1;
using HPRebar.McpBridge.Core.Scripting;

namespace HPSap2000.McpBridge.Model;

/// <summary>
///     What a SAP2000 script sees as its globals. The public fields are the script's API, so their names are
///     deliberately lowercase (`sapModel`, `sap`, `units`, …) — the tool description promises exactly
///     these names. There is no `helper`: attaching to SAP2000 is the bridge's job and the guard refuses the
///     Helper class; there is no transaction global either, SAP2000 has none.
/// </summary>
public sealed class SapScriptGlobals
{
    // ReSharper disable InconsistentNaming — names are the script-facing contract, see summary.
    /// <summary>The attached model (<c>cOAPI.SapModel</c>); every OAPI member hangs off it.</summary>
    public readonly cSapModel sapModel;

    /// <summary>The application root the bridge is attached to; only its read-only members pass the guard.</summary>
    public readonly cOAPI sap;

    /// <summary>The unit system the bridge forced for this run (kN, m, °C): lengths m, forces kN, moments kN·m.</summary>
    public readonly ScriptUnits units;

    /// <summary>Cooperative cancellation: timeout or cancel_execution. Long loops should check it; a running OAPI call cannot be interrupted.</summary>
    public readonly CancellationToken ct;

    /// <summary>Appends a line to the result's `logs` (capped by settings).</summary>
    public readonly Action<string> log;

    /// <summary>progress(current, total, message) — forwarded to the AI client as a progress notification.</summary>
    public readonly Action<int, int, string> progress;

    /// <summary>Data sent beside the code (`args.Str("frame")`); <see cref="ScriptArgs.Empty"/> when none.</summary>
    public readonly ScriptArgs args;
    // ReSharper restore InconsistentNaming

    public SapScriptGlobals(cSapModel sapModel, cOAPI sap, ScriptUnits units, CancellationToken ct, Action<string> log,
        Action<int, int, string> progress, ScriptArgs? args = null)
    {
        this.sapModel = sapModel;
        this.sap = sap;
        this.units = units;
        this.ct = ct;
        this.log = log;
        this.progress = progress;
        this.args = args ?? ScriptArgs.Empty;
    }
}
