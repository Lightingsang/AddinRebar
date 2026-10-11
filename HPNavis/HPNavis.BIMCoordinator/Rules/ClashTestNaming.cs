using System.Text.RegularExpressions;

namespace HPNavis.BIMCoordinator.Rules;

/// <summary>
///     Clash test names: <c>HP|LOD{phase}|P{priority}|{disciplinePair}|{left label}-{right label}</c>, e.g.
///     <c>HP|LOD350-400|P2|ARC-ARC|A1 Furniture-A1 Furniture</c>; names without the labels (and the LOD350 / LOD400
///     forms) are still recognised. LOD first, then the sheet's priority group (Nhóm), so sorting Clash Detective
///     by name groups the tests LOD by LOD and group by group — the 2026 API cannot create Clash Detective folders
///     (every folder type is refused as "unsupported type of SavedItem"). The name is the test's identity when the
///     matrix is applied again (upsert by name), so it must be stable and parseable back to its rule and LOD; the
///     earlier <c>HP|P{priority}|LOD{lod}|…</c> form is still recognised so such a test is renamed, not orphaned.
/// </summary>
public static class ClashTestNaming
{
    // the two sides may carry their group names ("A5 Doors-S2 Structural Walls"); a name holds no "-<code>" of its own
    private static readonly Regex Pattern = new(
        @"^HP\|(?:LOD(?<lod>\d{3}(?:-\d{3})?)\|P(?<p>[1-3])|P(?<p>[1-3])\|LOD(?<lod>\d{3}))\|(?<pair>[A-Z]{3}-[A-Z]{3})\|(?<l>[ASM][1-9])(?: [^|]*?)?-(?<r>[ASM][1-9])(?: [^|]*)?$",
        RegexOptions.CultureInvariant);

    /// <summary>
    ///     Name of a rule's test, each side labelled with its group (<paramref name="groupLabel" />, e.g. the base search set
    ///     <c>A5 Doors</c>): <c>HP|LOD200|P1|ARC-STR|A5 Doors-S2 Structural Walls</c>.
    /// </summary>
    public static string NameFor(ClashRule rule, string lod, Func<string, string> groupLabel) =>
        $"HP|LOD{lod}|P{rule.Priority}|{rule.DisciplinePair}|{groupLabel(rule.Left)}-{groupLabel(rule.Right)}";

    /// <summary>Rule id and LOD encoded in a test name, or null when the test is not one of ours.</summary>
    public static (string RuleId, string Lod)? Parse(string? name)
    {
        var match = Pattern.Match(name ?? "");
        if (!match.Success) return null;
        return ($"HP_{match.Groups["l"].Value}_{match.Groups["r"].Value}", match.Groups["lod"].Value);
    }
}
