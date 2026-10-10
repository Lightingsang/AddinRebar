using System.Text.RegularExpressions;

namespace HPNavis.BIMCoordinator.SearchSets;

/// <summary>
///     The role code of an ISO 19650 container name: <c>Project-Originator-Volume-Level-Type-Role-Number</c>, e.g.
///     <c>THCSLT-HPC-TT-ZZ-M3-EP-0001.nwc</c> → <c>EP</c>. Read from the right (role, then a four-digit number) because
///     volume codes such as <c>LH_HB_HC</c> vary in length. A Revit local copy adds a user suffix after the number
///     (<c>…-AA-0001_sangtq6ANLE.rvt</c>), which is allowed; a name without that tail has no role.
/// </summary>
public static class IsoFileName
{
    private static readonly Regex RoleBeforeNumber = new(@"-(?<role>[A-Z]{1,2})-(?<number>\d{4})(_[^.\\]*)?(\.[A-Za-z0-9]+)?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static string? Role(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;
        var name = fileName!.Replace('/', '\\');
        name = name.Substring(name.LastIndexOf('\\') + 1).Trim();
        var match = RoleBeforeNumber.Match(name);
        // upper-cased: the search wildcard ignores case too, so both readings of a name agree
        return match.Success ? match.Groups["role"].Value.ToUpperInvariant() : null;
    }
}
