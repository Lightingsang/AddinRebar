using HPBimToolkit.Core.Contracts;

namespace HPBimToolkit.Core.Statistics;

/// <summary>
///     Counts elements per category in one pass. The name is read only when a category id is seen for the first time,
///     so the host pays for the (localised) category name once per category instead of once per element.
/// </summary>
public sealed class CategoryTally
{
    private readonly Dictionary<long, (string Name, int Count)> _counts = new();

    public int Total { get; private set; }

    public void Add(long categoryId, Func<string> readName)
    {
        _counts[categoryId] = _counts.TryGetValue(categoryId, out var entry)
            ? (entry.Name, entry.Count + 1)
            : (readName(), 1);
        Total++;
    }

    /// <summary>Categories by descending count, then by name (ignoring case) so equal counts keep a stable order.</summary>
    public IReadOnlyList<CategoryCount> ToCounts() =>
        _counts
            .Select(pair => new CategoryCount(pair.Key, pair.Value.Name, pair.Value.Count))
            .OrderByDescending(count => count.Count)
            .ThenBy(count => count.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
