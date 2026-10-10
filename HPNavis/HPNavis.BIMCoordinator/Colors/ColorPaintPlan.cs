namespace HPNavis.BIMCoordinator.Colors;

/// <summary>What one colour set holds and whether it paints.</summary>
public sealed record ColorSetPaint(string Code, string Name, IReadOnlyList<int>? Rgb, int Found);

/// <summary>
///     The painting rules, kept pure so they are tested offline. Sets paint in sheet order and a later set wins where two
///     share an element — Navisworks keeps the last permanent colour, so the painter never has to work out owners up front
///     (enumerating a saved set's elements costs ~1.3 ms each, painting a whole collection milliseconds). Painting a node
///     colours the geometry below it, so a nested element (a valve inside an equipment package) can be reached by one set
///     through its parent and by a later set directly. <c>&lt;Default&gt;</c> sets never paint.
/// </summary>
public static class ColorPaintPlan
{
    /// <summary>The painting entries (RGB, not pending) in sheet order.</summary>
    public static IReadOnlyList<ColorSetEntry> PaintOrder(IEnumerable<ColorSetEntry> entries) =>
        entries.Where(e => e.Paints).OrderBy(e => e.Order).ToList();

    /// <summary>
    ///     Expected RGB of one geometry node: that of the last set (in paint order) holding the node or any of its ancestors,
    ///     or null when no set reaches it. <paramref name="ancestorsAndSelf" /> is the node and the nodes above it.
    /// </summary>
    public static IReadOnlyList<int>? ExpectedRgb<T>(IEnumerable<T> ancestorsAndSelf, IReadOnlyList<(IReadOnlyList<int> Rgb, Func<T, bool> Contains)> paintOrder)
    {
        var path = ancestorsAndSelf.ToList();
        for (var i = paintOrder.Count - 1; i >= 0; i--)
            if (path.Any(paintOrder[i].Contains)) return paintOrder[i].Rgb;
        return null;
    }

    /// <summary>True when a stored colour (channels 0..1) is the set's RGB, within half a step of 1/255.</summary>
    public static bool Matches(IReadOnlyList<int> rgb, double red, double green, double blue)
    {
        const double halfStep = 0.5 / 255;
        return Math.Abs(red - rgb[0] / 255.0) <= halfStep && Math.Abs(green - rgb[1] / 255.0) <= halfStep && Math.Abs(blue - rgb[2] / 255.0) <= halfStep;
    }
}
