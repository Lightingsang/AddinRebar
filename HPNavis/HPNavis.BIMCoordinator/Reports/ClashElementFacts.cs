using System.Text.RegularExpressions;

namespace HPNavis.BIMCoordinator.Reports;

/// <summary>
///     The pure rules behind the facts a clash description quotes: the system an element belongs to and the grid system
///     its location is read against.
/// </summary>
public static class ClashElementFacts
{
    // an ISO 19650 file name up to its last four-digit number field: THCSLT-HPC-LH_HB_HC-ZZ-M3-AA-0001(_user.rvt);
    // greedy, so a numeric volume field earlier in the name does not cut it short
    private static readonly Regex FileBase = new(@"^(.*-\d{4})", RegexOptions.Compiled);

    /// <summary>
    ///     "Sanitary / TP 76" from the values an element carries (System Classification, System Name, a distribution
    ///     system); each once, in that order; empty when the element has none.
    /// </summary>
    public static string SystemText(params string?[] parts) =>
        string.Join(" / ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    ///     The grid system of the element's own file. Navisworks names each appended file's grid system after the file
    ///     (the Revit local copy, user suffix included), so the two are matched on the ISO name up to its number; the
    ///     report's Grid Location uses the one active system instead, which names another building's grid.
    ///     Null when no system — or more than one, e.g. the same file appended twice — belongs to that file: an ambiguous
    ///     grid is never quoted.
    /// </summary>
    public static string? GridSystemFor(string? sourceFile, IEnumerable<string> systemNames)
    {
        if (string.IsNullOrWhiteSpace(sourceFile)) return null;
        var wanted = BaseOf(Path.GetFileNameWithoutExtension(sourceFile!.Trim()));
        var matches = systemNames.Where(name => string.Equals(BaseOf(name), wanted, StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    private static string BaseOf(string name)
    {
        var match = FileBase.Match(name);
        return match.Success ? match.Groups[1].Value : name;
    }
}
