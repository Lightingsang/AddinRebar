namespace HPRebar.ColumnRebar.Model;

/// <summary>
///     How the views the run creates are named: the elevations are <c>{DetailViewName}X</c> and
///     <c>{DetailViewName}Y</c>, each cross-section <c>{DetailViewName} {column number} {SectionSuffix}</c>.
/// </summary>
public sealed record ViewNaming(string DetailViewName, string SectionSuffix)
{
    public static ViewNaming Default { get; } = new("Detail", "MC");

    /// <summary>The names the user typed, trimmed; a blank entry keeps its default.</summary>
    public static ViewNaming From(string? detailViewName, string? sectionSuffix) => new(
        string.IsNullOrWhiteSpace(detailViewName) ? Default.DetailViewName : detailViewName!.Trim(),
        string.IsNullOrWhiteSpace(sectionSuffix) ? Default.SectionSuffix : sectionSuffix!.Trim());
}
