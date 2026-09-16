using System.Collections.Generic;
using System.Text.Json;

namespace HPRebar.Mcp.Contracts.Messages;

/// <summary>
///     Outcome of one `revit.execute`. Travels back to the AI verbatim as the tool's text content, so every
///     field is something the model can act on: diagnostics to fix the code, <see cref="Changed"/> to
///     confirm the effect, <see cref="TimedOut"/> / <see cref="RolledBack"/> to know nothing persisted.
/// </summary>
public sealed class ExecuteResult
{
    /// <summary>True when the script did not complete normally: guard rejection, compile error, exception or timeout.</summary>
    public bool IsError { get; set; }

    /// <summary>What the script `return`ed, already serialized by the bridge (Revit types are summarised, not dumped).</summary>
    public JsonElement? Value { get; set; }

    public string? ValueType { get; set; }

    /// <summary>Human-readable explanation when <see cref="IsError"/>; never contains machine paths or stack traces.</summary>
    public string? Message { get; set; }

    public IReadOnlyList<string> Logs { get; set; } = System.Array.Empty<string>();

    public IReadOnlyList<ScriptDiagnostic> Diagnostics { get; set; } = System.Array.Empty<ScriptDiagnostic>();

    public ChangedCounts Changed { get; set; } = new ChangedCounts(0, 0, 0);

    /// <summary>True when the transaction group was rolled back: dryRun, exception, or timeout.</summary>
    public bool RolledBack { get; set; }

    public bool TimedOut { get; set; }

    public long DurationMs { get; set; }

    /// <summary>True when <see cref="Value"/> or <see cref="Logs"/> were cut to stay under the output limit.</summary>
    public bool Truncated { get; set; }

    /// <summary>Set by the server: id of this run in the registry's history (null when history is off).</summary>
    public long? RunId { get; set; }

    /// <summary>Set by the server: a nudge when the run looks worth packaging as a tool.</summary>
    public string? Hint { get; set; }

    /// <summary>
    ///     File name (never a directory) of the model copy a bridge took before a writing run, for hosts that
    ///     have no transaction to roll back (ETABS). Null — and omitted from the JSON — for every other host
    ///     and for runs that did not write; the bridge window shows where the copies live.
    /// </summary>
    public string? Snapshot { get; set; }

    public static ExecuteResult Failure(string message, bool rolledBack = false, bool timedOut = false) => new ExecuteResult
    {
        IsError = true,
        Message = message,
        RolledBack = rolledBack,
        TimedOut = timedOut,
    };
}

/// <summary>One compiler or guard finding; line and column are 1-based to match what editors show.</summary>
public sealed record ScriptDiagnostic(int Line, int Column, string Id, string Message);

/// <summary>Element counts observed through DocumentChanged while the script ran.</summary>
public sealed record ChangedCounts(int Added, int Modified, int Deleted);
