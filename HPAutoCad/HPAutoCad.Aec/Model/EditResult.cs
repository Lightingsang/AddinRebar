using System.Text.Json.Serialization;

namespace HPAutoCad.Aec.Model;

/// <summary>What happened to one input item of a batch, in input order: the handle it produced or touched, or the error that stopped it.</summary>
public sealed record ItemOutcome(int Index, bool Ok, string? Handle, string? Type = null, IReadOnlyList<string>? Changed = null, ToolError? Error = null);

/// <summary>
///     The envelope of every AEC write tool. <c>Success</c> is true when the requested work was done for every item
///     (warnings allowed); a refused op reports <c>Success = false</c>, zero counts and the errors that refused it —
///     nothing was written. Per-item errors live on <c>items[i].error</c>; <c>errors[]</c> repeats the first
///     <see cref="MaxListedErrors"/> of them and counts the rest, so a batch of refusals stays under the result cap.
///     Serialised camelCase by the bridge.
/// </summary>
public sealed class EditResult
{
    /// <summary>Item errors repeated at the top level; beyond this, one summary error counts the remainder.</summary>
    public const int MaxListedErrors = 20;

    /// <summary>Indices listed per grouped warning before "+N more".</summary>
    private const int MaxListedIndices = 10;

    private readonly Dictionary<string, List<int>> _grouped = new(StringComparer.Ordinal);

    public bool Success { get; set; } = true;

    /// <summary>Tool-specific aggregate (what op ran, by type…) — small by construction.</summary>
    public object? Summary { get; set; }

    public int CreatedCount { get; set; }

    public int ModifiedCount { get; set; }

    public int DeletedCount { get; set; }

    /// <summary>Every entity handle the run created, modified or erased, in the order it happened.</summary>
    public List<string> AffectedHandles { get; } = [];

    /// <summary>The created handles alone — what a change-set rollback erases. Not serialised: <see cref="AffectedHandles"/> is the envelope.</summary>
    [JsonIgnore]
    public List<string> CreatedHandles { get; } = [];

    /// <summary>Per-item outcomes in input order, so <c>items[i]</c> answers for <c>request.items[i]</c>.</summary>
    public IReadOnlyList<ItemOutcome> Items { get; set; } = [];

    public List<string> Warnings { get; } = [];

    public List<ToolError> Errors { get; } = [];

    public EditResult Warn(string warning)
    {
        if (!Warnings.Contains(warning)) Warnings.Add(warning);
        return this;
    }

    /// <summary>A warning that many items may share (frozen layer, unknown tag): listed once with the item indices, never once per item.</summary>
    public void WarnItem(int index, string warning)
    {
        if (!_grouped.TryGetValue(warning, out var indices)) _grouped[warning] = indices = [];
        indices.Add(index);
    }

    public EditResult Fail(ToolError error)
    {
        Errors.Add(error);
        Success = false;
        return this;
    }

    public void Created(string handle)
    {
        CreatedCount++;
        AffectedHandles.Add(handle);
        CreatedHandles.Add(handle);
    }

    public void Modified(string handle)
    {
        ModifiedCount++;
        if (!AffectedHandles.Contains(handle)) AffectedHandles.Add(handle);
    }

    public void Deleted(string handle)
    {
        DeletedCount++;
        AffectedHandles.Add(handle);
    }

    /// <summary>
    ///     Records the per-item outcomes, flushes grouped warnings, lists the first item errors at the top level with a count of
    ///     the rest, and settles <see cref="Success"/>: true only when every item went through.
    /// </summary>
    public void Settle(IReadOnlyList<ItemOutcome> items)
    {
        Items = items;
        foreach (var (message, indices) in _grouped)
        {
            var listed = string.Join(", ", indices.Take(MaxListedIndices));
            var more = indices.Count > MaxListedIndices ? $" +{indices.Count - MaxListedIndices} more" : "";
            Warnings.Add($"{message} (items {listed}{more})");
        }

        _grouped.Clear();
        var failed = items.Where(i => i.Error is not null).ToArray();
        foreach (var item in failed.Take(MaxListedErrors)) Errors.Add(item.Error!);
        if (failed.Length > MaxListedErrors) Errors.Add(ToolError.Argument($"{failed.Length - MaxListedErrors} more item(s) failed; see items[].error."));
        Success = items.All(i => i.Ok) && Errors.Count == 0;
    }

    /// <summary>A single-item op refused before anything was written: one outcome, one error, nothing counted.</summary>
    public static EditResult Refused(ToolError error, string? handle = null, string? type = null)
    {
        var result = new EditResult();
        result.Settle([new ItemOutcome(0, false, handle, type, Error: error)]);
        result.Summary = new { refused = true, reason = error.Message };
        return result;
    }
}
