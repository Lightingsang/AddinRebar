namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>
///     How the views a Column Rebar run creates are named: the two elevations are <c>{DetailViewName}X</c> and
///     <c>{DetailViewName}Y</c>, each cross-section <c>{DetailViewName} {column number} {SectionSuffix}</c>.
/// </summary>
public sealed record ViewNaming(string DetailViewName, string SectionSuffix)
{
    /// <summary>Characters Revit refuses in a view name.</summary>
    public const string ForbiddenCharacters = "\\:{}[]|;<>?`~";

    public static ViewNaming Default { get; } = new("Detail", "MC");

    /// <summary>The names the user typed, trimmed; a blank entry keeps its default.</summary>
    public static ViewNaming From(string? detailViewName, string? sectionSuffix) => new(
        string.IsNullOrWhiteSpace(detailViewName) ? Default.DetailViewName : detailViewName!.Trim(),
        string.IsNullOrWhiteSpace(sectionSuffix) ? Default.SectionSuffix : sectionSuffix!.Trim());

    public (string X, string Y) ElevationNames() => (DetailViewName + "X", DetailViewName + "Y");

    public string SectionName(int columnNumber) => $"{DetailViewName} {columnNumber} {SectionSuffix}";

    /// <summary>The first character of either name that Revit would refuse, when there is one.</summary>
    public bool TryFindForbiddenCharacter(out char character)
    {
        foreach (var candidate in DetailViewName + SectionSuffix)
        {
            if (ForbiddenCharacters.IndexOf(candidate) < 0) continue;

            character = candidate;
            return true;
        }

        character = default;
        return false;
    }
}
