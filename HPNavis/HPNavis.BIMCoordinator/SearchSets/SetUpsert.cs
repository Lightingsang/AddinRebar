namespace HPNavis.BIMCoordinator.SearchSets;

/// <summary>What to do with a set saved at an earlier registry path.</summary>
public enum Relocation
{
    /// <summary>Nothing saved at an earlier path.</summary>
    None,

    /// <summary>Saved only at an earlier path: move (and rename) it to the current one.</summary>
    Move,

    /// <summary>Saved at both: the current one wins and the old one is left for the user (reported as an orphan).</summary>
    KeepOld,
}

public enum UpsertAction
{
    Create,
    Unchanged,
    Conflict,
    Update,
}

/// <summary>
///     The one rule for writing a registry set into a document: absent → create, same search → leave it, different search →
///     conflict (nothing written) unless the caller was approved to update. Kept apart from the Navisworks adapter so the
///     "never overwrite without approval, never delete" promise is tested offline.
/// </summary>
public static class SetUpsert
{
    public static UpsertAction Decide(bool exists, bool sameSearch, bool allowUpdate) =>
        !exists ? UpsertAction.Create
        : sameSearch ? UpsertAction.Unchanged
        : allowUpdate ? UpsertAction.Update
        : UpsertAction.Conflict;

    /// <summary>A set found at an earlier path is moved only when its current path is still free — never merged, never deleted.</summary>
    public static Relocation Relocate(bool atCurrentPath, bool atPreviousPath) =>
        !atPreviousPath ? Relocation.None : atCurrentPath ? Relocation.KeepOld : Relocation.Move;

    /// <summary>Action text of one set: the move (if any) joined to the upsert, e.g. <c>moved</c>, <c>moved+updated</c>, <c>move+conflict</c>.</summary>
    public static string Describe(string? move, string upsert) =>
        move is null ? upsert : upsert == "unchanged" ? move : $"{move}+{upsert}";

    /// <summary>True when the saved set differs and was (or would be) left as it is.</summary>
    public static bool IsConflict(string action) => action == "conflict" || action.EndsWith("+conflict", StringComparison.Ordinal);

    /// <summary>
    ///     An update replaces saved sets, so it is approved set by set: allowUpdate needs an explicit code list, never
    ///     "everything in the registry".
    /// </summary>
    public static void RequireCodesForUpdate(bool allowUpdate, IReadOnlyCollection<string>? codes)
    {
        if (allowUpdate && codes is not { Count: > 0 })
            throw new ArgumentException("allowUpdate replaces saved sets: name the approved sets in codes (e.g. [\"A2\"])");
    }

    /// <summary>Paths of sets under the top folder that no registry entry owns (renamed or moved entries, hand-made sets) — reported, never deleted.</summary>
    public static IReadOnlyList<string> Orphans(IEnumerable<string> savedPaths, IEnumerable<string> registryPaths)
    {
        var owned = new HashSet<string>(registryPaths, StringComparer.Ordinal);
        return savedPaths.Where(path => !owned.Contains(path)).ToList();
    }
}
