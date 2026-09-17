using System.Globalization;
using System.Text.Json;

namespace HPAutoCad.Aec.ChangeSets;

/// <summary>
///     The change sets of one drawing: ids, caps and the state machine (pending → committed → rolled_back | closed; pending → discarded;
///     a commit or a rollback undone by its request steps back), kept in the bridge process between requests. Snapshots are released
///     only when the undo can no longer be wanted: the set is closed, the rollback is confirmed to have held, or the drawing goes away.
///     Pure — the drawing adapter decides what the transitions do to the database.
/// </summary>
public sealed class ChangeSetLedger(string document)
{
    /// <summary>Live (pending or committed) sets per drawing; at the cap the oldest committed set is closed to make room, a finished set beyond it is forgotten.</summary>
    public const int MaxSets = 20;

    /// <summary>Ops per set: each is a full write-tool call replayed at commit inside one run.</summary>
    public const int MaxOps = 200;

    /// <summary>Recorded arguments per op; a batch beyond this is the caller's to split.</summary>
    public const int MaxArgsBytes = 64 * 1024;

    /// <summary>Handles listed per op / per commit list in an envelope; the rest is a count.</summary>
    public const int MaxListedHandles = 100;

    public const int MaxLabelChars = 120;

    private readonly List<ChangeSet> _sets = [];
    private int _next = 1;

    /// <summary>The drawing's file name, for the AI to see which document the sets belong to; refreshed by the adapter (SaveAs).</summary>
    public string Document { get; set; } = document;

    public IReadOnlyList<ChangeSet> Sets => _sets;

    /// <summary>Starts a set; at the live cap the oldest committed set is closed first (its undo is gone — <paramref name="closed"/> names it).</summary>
    public ChangeSet Begin(string? label, DateTimeOffset now, out ChangeSet? closed)
    {
        closed = null;
        if (_sets.Count(s => s.IsLive) >= MaxSets)
        {
            closed = _sets.FirstOrDefault(s => s.State == ChangeSetState.Committed);
            if (closed is null) throw new ArgumentException($"{MaxSets} change sets are pending in this drawing; commit or discard one first (get_change_summary lists them).");
            Close(closed, now, $"closed automatically: the drawing reached {MaxSets} live change sets");
        }

        while (_sets.Count >= MaxSets && _sets.FirstOrDefault(s => !s.IsLive) is { } finished) _sets.Remove(finished);
        var trimmed = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        if (trimmed is { Length: > MaxLabelChars }) throw new ArgumentException($"label must be {MaxLabelChars} characters or fewer.");
        var set = new ChangeSet($"CS-{_next++:000}", trimmed, now);
        _sets.Add(set);
        return set;
    }

    public ChangeSet Begin(string? label, DateTimeOffset now) => Begin(label, now, out _);

    public ChangeSet Get(string? id)
    {
        var key = (id ?? "").Trim();
        if (key.Length == 0) throw new ArgumentException("changeSetId is required (begin_change_set returns one).");
        return _sets.FirstOrDefault(s => s.Id.Equals(key, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException(_sets.Count == 0 ? $"change set '{key}' is unknown in {Document}: there are none (begin_change_set starts one)." : $"change set '{key}' is unknown in {Document}; known: {string.Join(", ", _sets.Select(s => $"{s.Id} ({s.State})"))}.");
    }

    /// <summary>Records one write-tool call; the arguments are copied so the request's JSON may go away.</summary>
    public ChangeOp Record(ChangeSet set, string tool, JsonElement args, IReadOnlyList<string> handles, DateTimeOffset now)
    {
        RequireState(set, ChangeSetState.Pending, "record into");
        if (set.Ops.Count >= MaxOps) throw new ArgumentException($"change set {set.Id} holds {MaxOps} ops already; commit it and begin another.");
        var raw = args.ValueKind == JsonValueKind.Undefined ? "{}" : args.GetRawText();
        if (raw.Length > MaxArgsBytes) throw new ArgumentException($"the arguments of this call are {raw.Length.ToString("N0", CultureInfo.InvariantCulture)} characters; a recorded op holds at most {MaxArgsBytes.ToString("N0", CultureInfo.InvariantCulture)} — split the batch.");
        var op = new ChangeOp(set.Ops.Count + 1, tool, args.ValueKind == JsonValueKind.Undefined ? args : args.Clone(), now, handles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        set.Ops.Add(op);
        return op;
    }

    public void MarkCommitted(ChangeSet set, CommitRecord commit)
    {
        RequireState(set, ChangeSetState.Pending, "commit");
        set.Commit = commit;
        set.State = ChangeSetState.Committed;
        set.Note = null;
    }

    /// <summary>A committed set whose run was rolled back by its request (dryRun) is pending again: the commit never happened.</summary>
    public void RevertToPending(ChangeSet set, string note)
    {
        RequireState(set, ChangeSetState.Committed, "revert");
        set.Commit?.ReleaseSnapshots();
        set.Commit = null;
        set.State = ChangeSetState.Pending;
        set.Note = note;
    }

    /// <summary>The undo ran; the snapshots stay until the drawing confirms it held (<see cref="ConfirmRolledBack"/>) — a dryRun request rolls the undo itself back.</summary>
    public void MarkRolledBack(ChangeSet set, DateTimeOffset now, string? note)
    {
        RequireState(set, ChangeSetState.Committed, "roll back");
        set.State = ChangeSetState.RolledBack;
        set.EndedAt = now;
        set.Note = note;
    }

    /// <summary>The drawing shows the undo held: the snapshots are no longer needed.</summary>
    public void ConfirmRolledBack(ChangeSet set)
    {
        RequireState(set, ChangeSetState.RolledBack, "confirm");
        set.Commit?.ReleaseSnapshots();
    }

    /// <summary>A rolled-back set whose undo was rolled back by its request (dryRun) is committed again, its snapshots still in hand.</summary>
    public void RevertToCommitted(ChangeSet set, string note)
    {
        RequireState(set, ChangeSetState.RolledBack, "revert");
        set.State = ChangeSetState.Committed;
        set.EndedAt = null;
        set.Note = note;
    }

    /// <summary>A rollback that could not restore every handle leaves the set committed, its snapshots kept for another try.</summary>
    public void MarkRollbackIncomplete(ChangeSet set, string note)
    {
        RequireState(set, ChangeSetState.Committed, "note");
        set.Note = note;
    }

    /// <summary>Keeps the committed work and releases its undo: the set is finished.</summary>
    public void Close(ChangeSet set, DateTimeOffset now, string? note = null)
    {
        RequireState(set, ChangeSetState.Committed, "close");
        set.Commit?.ReleaseSnapshots();
        set.State = ChangeSetState.Closed;
        set.EndedAt = now;
        set.Note = note;
    }

    public void MarkDiscarded(ChangeSet set, DateTimeOffset now)
    {
        RequireState(set, ChangeSetState.Pending, "discard");
        set.State = ChangeSetState.Discarded;
        set.EndedAt = now;
    }

    /// <summary>Releases every snapshot — the drawing is going away.</summary>
    public void ReleaseAll()
    {
        foreach (var set in _sets) set.Commit?.ReleaseSnapshots();
    }

    public Dictionary<string, int> ByState() => ChangeSetState.All.ToDictionary(s => s, s => _sets.Count(x => x.State == s));

    private static void RequireState(ChangeSet set, string state, string verb)
    {
        if (set.State != state) throw new ArgumentException($"cannot {verb} change set {set.Id}: it is {set.State}, not {state}{(set.Note is null ? "" : $" ({set.Note})")}.");
    }
}
