namespace HPNavis.BIMCoordinator.Validation;

/// <summary>
///     One finding. Codes: EMPTY, NOT_SAVED, SAVED_DIFFERS, WRONG_CATEGORY, WRONG_DISCIPLINE, OVERLAP,
///     DETAIL_OUTSIDE_PARENT, DETAIL_OVERLAP, NOT_COVERED_BY_DETAILS, GAP, NO_ROLE. Severity error = the set is wrong,
///     warning = needs a decision, info = expected but worth knowing.
/// </summary>
public sealed record ValidationIssue(string Severity, string Code, string? Set, string Message, int Count);

/// <summary>Per set: what its search finds now and what the saved set in the document resolves to.</summary>
public sealed record SetCheck(string Code, string Kind, string Name, string Folder, int Items, int? SavedItems, string Saved);

public sealed record SearchSetValidation(
    IReadOnlyList<SetCheck> Sets,
    IReadOnlyList<ValidationIssue> Issues,
    IReadOnlyDictionary<string, int> Gaps,
    int ElementsChecked,
    long ElapsedMs)
{
    public int Errors => Issues.Count(i => i.Severity == "error");

    public int Warnings => Issues.Count(i => i.Severity == "warning");
}
