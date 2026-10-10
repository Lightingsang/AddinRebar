namespace HPNavis.BIMCoordinator.SearchSets;

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
