namespace HPAutoCad.McpBridge.Service;

/// <summary>
///     AutoCAD reports its internal release (`Application.Version` = 25.1 for AutoCAD 2026); the pipe name,
///     the registry and the user all speak in product years. The table covers the releases the bridge can
///     be built for; anything else falls back to the version the assembly was compiled for.
/// </summary>
public static class AutocadVersionMap
{
    /// <summary>The product year this build targets (csproj `AutocadVersion`).</summary>
    public const int BuiltFor = 2026;

    private static readonly IReadOnlyDictionary<(int Major, int Minor), int> SeriesToYear = new Dictionary<(int, int), int>
    {
        [(24, 3)] = 2024,
        [(25, 0)] = 2025,
        [(25, 1)] = 2026,
        [(26, 0)] = 2027,
    };

    /// <summary>Returns the product year for an `Application.Version`, or <see cref="BuiltFor"/> with <paramref name="known"/> = false.</summary>
    public static int YearFor(Version version, out bool known)
    {
        known = SeriesToYear.TryGetValue((version.Major, version.Minor), out var year);
        return known ? year : BuiltFor;
    }
}
