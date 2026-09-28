using System;
using System.Collections.Generic;
using System.Linq;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Bars of one additional-bar cell over a support: those reaching into the span on the left and on the right.
/// "2f20;2f16" = 2Ø20 left, 2Ø16 right; "6f20;0" = 6Ø20 left only; "2f18" = the same 2Ø18 on both sides.
/// </summary>
public sealed record KataSideBars(IReadOnlyList<KataBarItem> Left, IReadOnlyList<KataBarItem> Right, string Text = "")
{
    public static readonly KataSideBars None = new(Array.Empty<KataBarItem>(), Array.Empty<KataBarItem>());

    public bool IsEmpty => Left.Count == 0 && Right.Count == 0;

    /// <summary>Both sides hold the same bars, so one bar runs across the support.</summary>
    public bool IsSymmetric => Key(Left) == Key(Right);

    private static string Key(IEnumerable<KataBarItem> items) =>
        string.Join("+", items.GroupBy(i => i.Diameter).OrderBy(g => g.Key).Select(g => $"{g.Sum(i => i.Count)}f{g.Key}"));

    public IReadOnlyList<KataBarItem> Side(bool right) => right ? Right : Left;
}
