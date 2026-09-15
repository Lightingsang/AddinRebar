using System.Text.RegularExpressions;
using HPRebar.Mcp.Contracts.Messages;

namespace HPNavis.McpBridge.Service;

/// <summary>Why the bridge rolls its own undo entry back after the mandatory commit — or keeps it.</summary>
public enum UndoReason
{
    /// <summary>A successful, non-dry run in a writing mode: the edits stay.</summary>
    Keep,

    /// <summary>dryRun=true: whatever the script changed is undone right after it committed.</summary>
    DryRun,

    /// <summary>The script threw, timed out or was cancelled: nothing partial may stay.</summary>
    Failure,

    /// <summary>transaction="none" but the document moved: undone, and the run is reported as an error.</summary>
    NoneViolation,
}

/// <summary>
///     The undo rules of a run, kept free of the Navisworks API so the table can be tested without Roamer.
///     Navisworks has no in-flight rollback: the bridge always commits and then decides whether the entry on
///     top of the undo stack is its own and must go. "Own" is decided by comparing <c>NextUndo</c> before and
///     after, which is why the label is made unique against the current top first.
/// </summary>
public static class NavisUndoDecision
{
    public const string LabelPrefix = "MCP: ";

    /// <summary>Longest label core kept; the undo menu shows the text verbatim.</summary>
    public const int MaxLabelLength = 64;

    /// <summary>
    ///     <c>MCP: &lt;label&gt;</c>, with a <c>(n)</c> suffix while it equals the entry currently on top: the
    ///     registry labels every run of a tool with the tool's name, so two consecutive runs of the same tool
    ///     would otherwise be indistinguishable and a dry run would silently persist.
    /// </summary>
    public static string LabelFor(string? requestLabel, string? currentTop)
    {
        var core = string.IsNullOrWhiteSpace(requestLabel) ? "script" : Regex.Replace(requestLabel!.Trim(), @"\s+", " ");
        if (core.Length > MaxLabelLength) core = core.Substring(0, MaxLabelLength).TrimEnd() + "…";
        var label = LabelPrefix + core;
        for (var attempt = 2; label == currentTop; attempt++) label = $"{LabelPrefix}{core} ({attempt})";
        return label;
    }

    /// <summary>The entry on top after the commit is this run's: it carries our label and was not there before.</summary>
    public static bool IsOurs(bool committed, string label, string? topBefore, string? topAfter) =>
        committed && topAfter == label && topAfter != topBefore;

    public static UndoReason Classify(string mode, bool dryRun, bool failed, bool undoIsOurs, bool documentChanged)
    {
        if (failed) return UndoReason.Failure;
        if (mode == TransactionModes.None && (undoIsOurs || documentChanged)) return UndoReason.NoneViolation;
        return dryRun ? UndoReason.DryRun : UndoReason.Keep;
    }

    /// <summary>Only an entry of our own is ever rolled back — never the user's last edit.</summary>
    public static bool ShouldRollBack(UndoReason reason, bool undoIsOurs) => reason != UndoReason.Keep && undoIsOurs;

    /// <summary>The message the AI reads for a `none` violation, once the rollback attempt is known.</summary>
    public static string NoneViolationMessage(bool rolledBack) =>
        $"The script declared transaction=\"none\" but modified the document (rolled back: {(rolledBack ? "yes" : "no")}). Use transaction=\"auto\" for scripts that change anything.";

    public const string DryRunNothingToUndo = "dry run: the script produced no undoable change; nothing to roll back.";

    /// <summary>The document moved but no undo entry of ours appeared: the change persisted and a dry run cannot honour its promise.</summary>
    public const string DryRunChangePersisted = "dry run: the script changed the document without creating an undo entry, so the change persisted (this call cannot be undone by the bridge; avoid it under dryRun).";

    public const string FailureNotUndone = " Changes could not be rolled back (no undo entry of this run on top of the stack).";

    public const string HeavyNotUndoable = "heavy operations in this run are not undoable; only the review edits were rolled back.";
}
