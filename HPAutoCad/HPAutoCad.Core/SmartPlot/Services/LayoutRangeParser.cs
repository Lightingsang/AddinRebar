namespace HPAutoCad.Core.SmartPlot.Services;

/// <summary>
/// Robust parser for sheet and layout range expressions ("All", "*", "1-5", "1,3,5", "1-3,5,8-10").
/// Guarantees zero unhandled exceptions on malformed or garbage inputs.
/// </summary>
public static class LayoutRangeParser
{
    private const int HardCap = 10000;

    /// <summary>
    /// Parses a range text into a sorted, distinct list of 1-based indices.
    /// </summary>
    /// <param name="rangeText">The input range string, e.g. "All", "*", "1-5", "1,3,5", "1-3,5,8-10".</param>
    /// <param name="maxCount">Maximum available count to bound "All" or open ranges.</param>
    /// <returns>A sorted list of valid 1-based indices.</returns>
    public static IReadOnlyList<int> Parse(string? rangeText, int maxCount = int.MaxValue)
    {
        if (maxCount <= 0) return Array.Empty<int>();

        var text = rangeText?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return maxCount < int.MaxValue
                ? Enumerable.Range(1, Math.Min(maxCount, HardCap)).ToList()
                : Array.Empty<int>();
        }

        if (text.Equals("all", StringComparison.OrdinalIgnoreCase) || text == "*")
        {
            return maxCount < int.MaxValue
                ? Enumerable.Range(1, Math.Min(maxCount, HardCap)).ToList()
                : Array.Empty<int>();
        }

        var result = new HashSet<int>();
        var tokens = text.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            var trimmed = token.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (trimmed.Contains('-'))
            {
                var parts = trimmed.Split('-');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0].Trim(), out var start) &&
                    int.TryParse(parts[1].Trim(), out var end))
                {
                    if (start <= 0 && end <= 0) continue;

                    var min = Math.Max(1, Math.Min(start, end));
                    var max = Math.Min(Math.Max(start, end), Math.Min(maxCount, HardCap));

                    for (var i = min; i <= max; i++)
                    {
                        result.Add(i);
                    }
                }
            }
            else if (int.TryParse(trimmed, out var single) && single >= 1 && single <= maxCount)
            {
                result.Add(single);
            }
        }

        return result.OrderBy(x => x).ToList();
    }
}
