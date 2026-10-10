using Autodesk.Navisworks.Api;
using HPNavis.BIMCoordinator.Rules;
using HPNavis.BIMCoordinator.SearchSets;
using HPNavis.BIMCoordinator.Validation;

namespace HPNavis.BIMCoordinator;

/// <summary>The search-set half of the facade: registry, sync (preview / apply), validation and the inventory used as a backup.</summary>
public static partial class CoordinatorTools
{
    private const int MaxIssuesListed = 60;
    private const int MaxGapsListed = 60;
    private const int MaxInventoryItems = 400;
    private const int MaxOrphansListed = 30;
    private const int MaxInventoryConditionChars = 40_000;

    /// <summary>
    ///     Preview (default) or apply the registry. Apply creates what is missing; a saved set whose search differs is a
    ///     <c>conflict</c> left untouched unless <paramref name="allowUpdate" /> (the approval step) replaces it.
    /// </summary>
    public static object SyncSearchSets(Document doc, bool apply, bool allowUpdate, IReadOnlyCollection<string>? codes, bool includeExtras, double mmPerUnit, CancellationToken ct)
    {
        RequirePositive(mmPerUnit);
        SetUpsert.RequireCodesForUpdate(allowUpdate, codes);
        var catalog = BaseSetCatalog.Default;
        var plans = SearchSetPlan.All(catalog, ClashMatrix.Default, codes, includeExtras);
        var outcomes = apply ? NavisSearchSetCompiler.Apply(doc, plans, mmPerUnit, allowUpdate, ct) : NavisSearchSetCompiler.Preview(doc, plans, mmPerUnit, ct);
        var empty = outcomes.Where(o => o.Items == 0).Select(o => o.Code).ToList();
        var conflicts = outcomes.Where(o => o.Action == "conflict").Select(o => o.Code).ToList();
        var warnings = new List<string>();
        warnings.AddRange(empty.Select(code => $"{code} finds no element in this model"));
        warnings.AddRange(conflicts.Select(code => $"{code}: the saved set differs from the registry and was left as it is; re-run with allowUpdate=true and codes=[\"{code}\"] once approved"));
        var registryPaths = SearchSetPlan.All(catalog, ClashMatrix.Default, includeExtras: true).Select(p => $"{p.Folder}/{p.DisplayName}");
        var orphans = SetUpsert.Orphans(NavisSearchSetCompiler.SetPathsUnder(doc, new[] { catalog.Folder }), registryPaths);
        warnings.AddRange(orphans.Take(MaxOrphansListed).Select(path => $"{path}: under {catalog.Folder} but owned by no registry entry (renamed/moved entry or hand-made set) — left as it is"));
        return new
        {
            success = conflicts.Count == 0,
            applied = apply,
            summary = $"{outcomes.Count} set(s): " + string.Join(", ", outcomes.GroupBy(o => o.Action).Select(g => $"{g.Count()} {g.Key}")) +
                      (empty.Count > 0 ? $"; EMPTY: {string.Join(", ", empty)}" : ""),
            sets = outcomes,
            orphanCount = orphans.Count,
            warnings,
        };
    }

    /// <summary>Read-only acceptance check (<paramref name="scope" /> <c>base</c> or <c>extras</c>), see <see cref="NavisSearchSetValidator" />.</summary>
    public static object ValidateSearchSets(Document doc, string scope, double mmPerUnit, CancellationToken ct)
    {
        RequirePositive(mmPerUnit);
        var result = NavisSearchSetValidator.Validate(doc, BaseSetCatalog.Default, ClashMatrix.Default, mmPerUnit, scope, ct);
        var ordered = result.Issues.OrderBy(i => i.Severity == "error" ? 0 : i.Severity == "warning" ? 1 : 2).ToList();
        return new
        {
            success = result.Errors == 0,
            scope,
            summary = $"{result.Sets.Count} set(s), {result.Errors} error(s), {result.Warnings} warning(s), {result.ElapsedMs} ms" +
                      (result.ElementsChecked > 0 ? $"; {result.ElementsChecked} Revit elements checked for gaps" : ""),
            sets = result.Sets,
            issues = ordered.Take(MaxIssuesListed).ToList(),
            issueCount = ordered.Count,
            gaps = result.Gaps.Take(MaxGapsListed).ToDictionary(p => p.Key, p => p.Value),
            gapGroups = result.Gaps.Count,
        };
    }

    /// <summary>
    ///     Inventory of the saved selection/search sets and folders: path, kind, condition count and, on request, the
    ///     conditions as text within a character budget (the bridge answer is capped at 64 KB; the registry alone has ~1 200
    ///     conditions). A record of what existed before an apply — the full restore point is the NWD file itself.
    /// </summary>
    public static object ListSelectionSets(Document doc, string? folder = null, bool withConditions = false)
    {
        var conditionBudget = MaxInventoryConditionChars;
        var conditionsCut = false;
        List<string>? Conditions(Search search)
        {
            if (!withConditions) return null;
            var texts = search.SearchConditions.Select(c => c.ToString()).ToList();
            var size = texts.Sum(t => t.Length);
            if (size > conditionBudget) { conditionsCut = true; return null; }
            conditionBudget -= size;
            return texts;
        }

        var path = (folder ?? "").Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        var start = NavisSearchSetCompiler.FindFolder(doc, path) ?? throw new ArgumentException($"folder '{folder}' is not in the Sets tree");
        var items = new List<object>();
        void Walk(GroupItem folder, string path)
        {
            foreach (var child in folder.Children)
            {
                if (items.Count >= MaxInventoryItems) return;
                var here = path.Length == 0 ? child.DisplayName : $"{path}/{child.DisplayName}";
                if (child is SelectionSet set)
                {
                    items.Add(new
                    {
                        path = here,
                        kind = set.HasSearch ? "search" : set.HasExplicitModelItems ? "explicit" : "empty",
                        guid = set.Guid,
                        pruneBelowMatch = set.HasSearch ? set.Search.PruneBelowMatch : (bool?)null,
                        conditionCount = set.HasSearch ? set.Search.SearchConditions.Count : (int?)null,
                        conditions = set.HasSearch ? Conditions(set.Search) : null,
                        explicitItems = set.HasExplicitModelItems ? set.ExplicitModelItems.Count : (int?)null,
                    });
                }
                else if (child is GroupItem group)
                {
                    items.Add(new { path = here, kind = "folder", guid = group.Guid });
                    Walk(group, here);
                }
            }
        }

        Walk(start, string.Join("/", path));
        return new
        {
            success = true,
            document = doc.FileName,
            summary = $"{items.Count} folder(s)/set(s)" + (items.Count >= MaxInventoryItems ? " (truncated)" : "") +
                      (conditionsCut ? $"; conditions omitted for sets past the {MaxInventoryConditionChars}-character budget — ask for one folder" : ""),
            items,
        };
    }
}
