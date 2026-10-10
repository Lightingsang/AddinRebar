using System.Text.RegularExpressions;

namespace HPNavis.BIMCoordinator.Rules;

/// <summary>
///     Clash test names: <c>HP|P{priority}|LOD{lod}|{disciplinePair}|{left}-{right}</c>, e.g.
///     <c>HP|P2|LOD350|ARC-ARC|A1-A1</c>. The name is the test's identity when the matrix is applied again
///     (upsert by name), so it must be stable and parseable back to its rule and LOD.
/// </summary>
public static class ClashTestNaming
{
    private static readonly Regex Pattern = new(@"^HP\|P(?<p>[1-3])\|LOD(?<lod>\d{3})\|(?<pair>[A-Z]{3}-[A-Z]{3})\|(?<l>[ASM][1-9])-(?<r>[ASM][1-9])$", RegexOptions.CultureInvariant);

    public static string NameFor(ClashRule rule, string lod) =>
        $"HP|P{rule.Priority}|LOD{lod}|{rule.DisciplinePair}|{rule.Left}-{rule.Right}";

    /// <summary>Rule id and LOD encoded in a test name, or null when the test is not one of ours.</summary>
    public static (string RuleId, string Lod)? Parse(string? name)
    {
        var match = Pattern.Match(name ?? "");
        if (!match.Success) return null;
        return ($"HP_{match.Groups["l"].Value}_{match.Groups["r"].Value}", match.Groups["lod"].Value);
    }
}
