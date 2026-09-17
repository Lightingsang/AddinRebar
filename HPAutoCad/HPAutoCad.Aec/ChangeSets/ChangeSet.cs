using System.Text.Json;

namespace HPAutoCad.Aec.ChangeSets;

/// <summary>
///     The life of a change set: recorded (<c>pending</c>), replayed (<c>committed</c>, undo available), undone (<c>rolled_back</c>),
///     kept with its undo released (<c>closed</c>), or thrown away unrun (<c>discarded</c>).
/// </summary>
public static class ChangeSetState
{
    public const string Pending = "pending";
    public const string Committed = "committed";
    public const string RolledBack = "rolled_back";
    public const string Closed = "closed";
    public const string Discarded = "discarded";

    public static readonly IReadOnlyList<string> All = [Pending, Committed, RolledBack, Closed, Discarded];
}

/// <summary>One recorded write-tool call: the tool, its arguments as sent, and the handles it named (checked when recorded).</summary>
public sealed record ChangeOp(int Index, string Tool, JsonElement Args, DateTimeOffset RecordedAt, IReadOnlyList<string> Handles)
{
    /// <summary>The keys whose array length says how big the call is.</summary>
    private static readonly string[] CountedKeys = ["items", "handles", "issues", "rules", "names"];

    /// <summary>The raw arguments, cut to <paramref name="maxChars"/> so a page of ops stays under the result cap.</summary>
    public string ArgsText(int maxChars)
    {
        var text = Args.ValueKind == JsonValueKind.Undefined ? "{}" : Args.GetRawText();
        if (text.Length <= maxChars) return text;
        var cut = maxChars > 0 && char.IsHighSurrogate(text[maxChars - 1]) ? maxChars - 1 : maxChars;
        return text[..cut] + "…";
    }

    /// <summary>A one-line reading of the arguments: the op key, item / handle counts — what the AI needs to recognise the call.</summary>
    public string Summary
    {
        get
        {
            if (Args.ValueKind != JsonValueKind.Object) return "";
            var parts = new List<string>();
            if (Args.TryGetProperty("op", out var op) && op.ValueKind == JsonValueKind.String) parts.Add($"op {op.GetString()}");
            foreach (var key in CountedKeys)
                if (Args.TryGetProperty(key, out var list) && list.ValueKind == JsonValueKind.Array && list.GetArrayLength() > 0) parts.Add($"{key} {list.GetArrayLength()}");
            if (Args.TryGetProperty("apply", out var apply) && apply.ValueKind == JsonValueKind.False) parts.Add("apply false");
            return string.Join(" · ", parts);
        }
    }

    public Dictionary<string, object?> Describe(int maxArgsChars) => new()
    {
        ["index"] = Index,
        ["tool"] = Tool,
        ["summary"] = Summary,
        ["handles"] = Handles.Take(ChangeSetLedger.MaxListedHandles).ToArray(),
        ["handleCount"] = Handles.Count,
        ["recordedAt"] = RecordedAt,
        ["args"] = ArgsText(maxArgsChars),
    };
}

/// <summary>What one op did when the set was committed.</summary>
public sealed record OpOutcome(int Index, string Tool, bool Ok, int Created, int Modified, int Deleted, string? Error);

/// <summary>
///     What a commit left behind — enough to undo it: the handles it created (erase), modified (restore from the snapshots) and
///     erased (un-erase), the layers and block definitions it added (kept, reported), and the snapshot bag the drawing adapter owns.
/// </summary>
public sealed class CommitRecord(DateTimeOffset committedAt, IReadOnlyList<OpOutcome> ops, IReadOnlyList<string> created, IReadOnlyList<string> modified, IReadOnlyList<string> deleted,
    IReadOnlyList<string> layersCreated, IReadOnlyList<string> blocksCreated, IDisposable? snapshots, bool snapshotsComplete)
{
    public DateTimeOffset CommittedAt { get; } = committedAt;
    public IReadOnlyList<OpOutcome> Ops { get; } = ops;
    public IReadOnlyList<string> CreatedHandles { get; } = created;
    public IReadOnlyList<string> ModifiedHandles { get; } = modified;
    public IReadOnlyList<string> DeletedHandles { get; } = deleted;
    public IReadOnlyList<string> LayersCreated { get; } = layersCreated;
    public IReadOnlyList<string> BlocksCreated { get; } = blocksCreated;
    /// <summary>The original state of every modified or erased entity, owned by the drawing adapter; null once released.</summary>
    public IDisposable? Snapshots { get; private set; } = snapshots;
    /// <summary>False when the snapshot cap was hit or a clone failed: a rollback then erases what was created but cannot restore every modification.</summary>
    public bool SnapshotsComplete { get; } = snapshotsComplete;

    public bool Touched => CreatedHandles.Count + ModifiedHandles.Count + DeletedHandles.Count > 0;

    public void ReleaseSnapshots()
    {
        Snapshots?.Dispose();
        Snapshots = null;
    }
}

/// <summary>A logical change set: ops recorded instead of applied, replayed in one run, undone from what the commit remembered.</summary>
public sealed class ChangeSet(string id, string? label, DateTimeOffset createdAt)
{
    public string Id { get; } = id;
    public string? Label { get; } = label;
    public string State { get; internal set; } = ChangeSetState.Pending;
    public DateTimeOffset CreatedAt { get; } = createdAt;
    public DateTimeOffset? EndedAt { get; internal set; }
    public List<ChangeOp> Ops { get; } = [];
    public CommitRecord? Commit { get; internal set; }
    /// <summary>What last happened to the set outside the normal path (a commit or rollback undone by its request, a rollback that could not restore everything).</summary>
    public string? Note { get; internal set; }

    /// <summary>Pending or committed: still doing something (the undo of a committed set stays available until it is closed).</summary>
    public bool IsLive => State is ChangeSetState.Pending or ChangeSetState.Committed;

    public Dictionary<string, object?> Describe()
    {
        var d = new Dictionary<string, object?>
        {
            ["changeSetId"] = Id,
            ["label"] = Label,
            ["state"] = State,
            ["ops"] = Ops.Count,
            ["byTool"] = Ops.GroupBy(o => o.Tool).ToDictionary(g => g.Key, g => g.Count()),
            ["createdAt"] = CreatedAt,
            ["endedAt"] = EndedAt,
            ["note"] = Note,
        };
        if (Commit is { } c)
            d["commit"] = new
            {
                committedAt = c.CommittedAt, created = c.CreatedHandles.Count, modified = c.ModifiedHandles.Count, deleted = c.DeletedHandles.Count,
                layersCreated = c.LayersCreated, blocksCreated = c.BlocksCreated, snapshotsComplete = c.SnapshotsComplete, undoAvailable = State == ChangeSetState.Committed && (c.Snapshots is not null || c.ModifiedHandles.Count + c.DeletedHandles.Count == 0),
                failedOps = c.Ops.Count(o => !o.Ok),
            };
        return d;
    }
}
