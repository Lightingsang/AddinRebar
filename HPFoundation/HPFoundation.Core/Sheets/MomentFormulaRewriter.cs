using System.Text.RegularExpressions;

namespace HPFoundation.Core.Sheets;

/// <summary>
///     Brings the sheet's base moments to the convention of the ETABS model: My acts with Qx along the footing,
///     Mx acts with Qy across it. The result is made absolute: column moments are signed, and the sheet's eccentricity
///     and pressure formulas expect a magnitude. The rest of each formula stays as the engineer wrote it.
/// </summary>
public static partial class MomentFormulaRewriter
{
    public static string RewriteLongDirection(string formula) =>
        ReplaceFirstSum(formula, LongFormula(), "$1F$2:F$3$4");

    public static string RewriteShortDirection(string formula) =>
        ReplaceFirstSum(formula, ShortFormula(), "$1D$2:D$3$4");

    private static string ReplaceFirstSum(string formula, Regex pattern, string replacement)
    {
        if (!pattern.IsMatch(formula))
        {
            throw new ArgumentException($"Unexpected moment formula: {formula}", nameof(formula));
        }

        var swapped = pattern.Replace(formula, replacement, 1);
        return $"=ABS({swapped[1..]})";
    }

    [GeneratedRegex(@"^(=ROUND\(SUM\()D(\d+):D(\d+)(\)\+ABS\()")]
    private static partial Regex LongFormula();

    [GeneratedRegex(@"^(=ROUND\(SUM\()F(\d+):F(\d+)(\)\+SUM\(G)")]
    private static partial Regex ShortFormula();
}
