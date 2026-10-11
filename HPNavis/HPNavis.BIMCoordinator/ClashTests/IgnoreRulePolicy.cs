using HPNavis.BIMCoordinator.Rules;

namespace HPNavis.BIMCoordinator.ClashTests;

/// <summary>
///     The Clash Detective Rules tab ("Ignore Clashes Between") and Composite Object Clashing for every HP test, agreed
///     with the BIM lead (2026-10-11):
///     <list type="bullet">
///         <item>same layer — off: a Revit NWC layer is the level, so it would hide every clash on one level;</item>
///         <item>same group/block/cell — on: parts of one family or group (panel and mullion of one curtain wall);</item>
///         <item>same file — off: ARC-ARC, STR-STR and MEP-MEP pairs are clashes inside one model and must be found;</item>
///         <item>coincident snap points — on only inside one discipline (pipe into fitting, beam onto column, wall
///         joins); off across disciplines so a pipe touching a beam is still reported;</item>
///         <item>composite objects — on, the Navisworks default: one clash per family, not per piece.</item>
///     </list>
///     Rules are named as Navisworks shows them (English UI); a test's enabled rules are compared by those names.
/// </summary>
public static class IgnoreRulePolicy
{
    public const string SameLayer = "Items in same layer";
    public const string SameGroup = "Items in same group/block/cell";
    public const string SameFile = "Items in same file";
    public const string SnapPoints = "Items with coincident snap points";
    public const bool MergeComposites = true;

    public static readonly IReadOnlyList<string> Known = new[] { SameLayer, SameGroup, SameFile, SnapPoints };

    /// <summary>The ignore rules enabled for a matrix pair, in the Rules tab's order.</summary>
    public static IReadOnlyList<string> For(ClashRule rule) =>
        SameDiscipline(rule) ? new[] { SameGroup, SnapPoints } : new[] { SameGroup };

    /// <summary>True when both groups belong to one discipline (ARC-ARC, STR-STR, MEP-MEP).</summary>
    public static bool SameDiscipline(ClashRule rule)
    {
        var parts = rule.DisciplinePair.Split('-');
        return parts.Length == 2 && parts[0] == parts[1];
    }

    /// <summary>Same enabled rules, whatever the order.</summary>
    public static bool SameRules(IEnumerable<string> a, IEnumerable<string> b) =>
        new HashSet<string>(a, StringComparer.Ordinal).SetEquals(b);
}
