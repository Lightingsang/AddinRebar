namespace HPGeo.Core.Catalog;

/// <summary>Which administrative table a province belongs to: the 34 provinces after the 2025 mergers or the 63 before.</summary>
public enum ProvinceCatalogKind
{
    Current,
    Legacy,
}

/// <summary>
/// One province and the central meridians its cadastral records may use. A province with several
/// meridians (merged provinces, or provinces whose districts were mapped on different zones) forces the
/// user to pick one — the tool never guesses.
/// </summary>
public sealed record Province(string Name, IReadOnlyList<double> CentralMeridians, ProvinceCatalogKind Kind)
{
    public bool HasSingleMeridian => CentralMeridians.Count == 1;
}
