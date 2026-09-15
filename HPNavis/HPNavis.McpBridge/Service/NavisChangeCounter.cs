using Autodesk.Navisworks.Api;
using HPRebar.Mcp.Contracts.Messages;

namespace HPNavis.McpBridge.Service;

/// <summary>
///     Navisworks raises no per-object change events, so the bridge fingerprints the cheap things a review
///     script can touch before and after the run: the saved-item collections, the appended models, the
///     current selection (count plus a hash of the first guids), the clash-test count, the undo stack's
///     top entry and the document's modified flag. Coarse by design — the tool description says so — and
///     enough to enforce <c>transaction=none</c> and to report what a run added or removed.
/// </summary>
public static class NavisChangeCounter
{
    private const int SelectionSample = 32;

    public sealed record Fingerprint(int SelectionSets, int SavedViewpoints, int Models, int SelectedItems, int SelectionHash,
        int ClashTests, string? NextUndo, bool IsModified)
    {
        public bool SameAs(Fingerprint other) =>
            SelectionSets == other.SelectionSets && SavedViewpoints == other.SavedViewpoints && Models == other.Models
            && SelectedItems == other.SelectedItems && SelectionHash == other.SelectionHash && ClashTests == other.ClashTests
            && NextUndo == other.NextUndo && IsModified == other.IsModified;
    }

    public static Fingerprint Snapshot(Document doc, Func<Document, int> clashTestCount)
    {
        var selected = Safe(() => doc.CurrentSelection.SelectedItems);
        return new Fingerprint(
            Safe(() => doc.SelectionSets.Value.Count),
            Safe(() => doc.SavedViewpoints.Value.Count),
            Safe(() => doc.Models.Count),
            Safe(() => selected?.Count ?? 0),
            Safe(() => selected is null ? 0 : HashSelection(selected)),
            Safe(() => clashTestCount(doc)),
            Safe(() => doc.NextUndo),
            Safe(() => doc.IsModified));
    }

    /// <summary>Added/Deleted from the collection counts; Modified = 1 when anything else moved without a count change.</summary>
    public static ChangedCounts Delta(Fingerprint before, Fingerprint after)
    {
        var added = 0;
        var deleted = 0;
        Accumulate(before.SelectionSets, after.SelectionSets, ref added, ref deleted);
        Accumulate(before.SavedViewpoints, after.SavedViewpoints, ref added, ref deleted);
        Accumulate(before.Models, after.Models, ref added, ref deleted);
        Accumulate(before.ClashTests, after.ClashTests, ref added, ref deleted);

        var modified = added + deleted == 0 && !before.SameAs(after) ? 1 : 0;
        return new ChangedCounts(added, modified, deleted);
    }

    private static void Accumulate(int before, int after, ref int added, ref int deleted)
    {
        if (after > before) added += after - before;
        else if (after < before) deleted += before - after;
    }

    private static int HashSelection(ModelItemCollection selected)
    {
        var hash = 17;
        var taken = 0;
        foreach (var item in selected)
        {
            hash = unchecked(hash * 31 + item.InstanceGuid.GetHashCode());
            if (++taken >= SelectionSample) break;
        }

        return hash;
    }

    private static T Safe<T>(Func<T> read)
    {
        try { return read(); }
        catch { return default!; }
    }
}
