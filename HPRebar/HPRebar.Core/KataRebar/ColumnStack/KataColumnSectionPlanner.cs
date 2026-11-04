using System;
using System.Collections.Generic;
using System.Linq;

namespace HPRebar.Core.KataRebar.ColumnStack;

/// <summary>A main bar in a storey section, in mm from the section's lower-left corner (centre of a round one at Ø/2).</summary>
/// <param name="Slot">Grid slot (column, row) of a rectangular section; (index, 0) on a circle.</param>
public sealed record KataColumnBar(double X, double Y, double DiameterMm, int Layer, (int Column, int Row) Slot);

public enum KataColumnTieKind
{
    /// <summary>Closed tie around two bars of one grid line pair (the inner "□" of Kata).</summary>
    Closed,

    /// <summary>Single-leg tie across the section with hooks on one slot (Kata's "C").</summary>
    Crosstie
}

/// <summary>
/// An inner tie. <see cref="AlongB"/> = its legs run across b (it restrains bars of the b faces, slots
/// <see cref="FirstSlot"/>/<see cref="SecondSlot"/> counted along b); otherwise across h.
/// </summary>
public sealed record KataColumnTie(KataColumnTieKind Kind, bool AlongB, int FirstSlot, int SecondSlot);

/// <summary>Bars and inner ties of one storey section, and the bars the sheet asked for that could not be placed.</summary>
public sealed record KataColumnSection(IReadOnlyList<KataColumnBar> Bars, IReadOnlyList<KataColumnTie> Ties, double BarInsetMm, IReadOnlyList<string> Notes);

/// <summary>
/// The section layout Kata's VC command draws (derived from its drawings, 2026-11-03): layer 1 puts E bars on each b
/// face and G on each h face on an even grid, corners shared, centres at cover + hoop Ø + bar Ø/2 from the faces;
/// layer 2 adds F bars on the second grid row from each b face and H bars on the second grid column from each h face,
/// on layer-1 slots. Each count includes the line's ends: F's two layer-1 bars, H's two layer-1 bars plus the two
/// layer-2 row slots on it when there is a row (drawn or not); the rest goes symmetrically, outside in, on the slots
/// between (17 VC drawings, E = G = 7 and E = 4 / G = 5). A line with nothing between its ends is absent, ties
/// included. A round section puts E + F bars on one circle.
/// </summary>
public static class KataColumnSectionPlanner
{
    public static KataColumnSection Plan(KataColumnStoreyRow row, KataColumnSpec spec)
    {
        double inset = spec.CoverMm + spec.Hoops.DiameterMm + row.OuterDiameterMm / 2;
        return row.IsRound ? Round(row, inset) : Rectangle(row, spec, inset);
    }

    /// <summary>
    /// The section on the real faces of a storey: bars go on the Revit column, the sheet gives counts and Ø (user
    /// decision). Coordinates stay relative to the section's lower-left corner.
    /// </summary>
    public static KataColumnSection PlanOnFaces(KataColumnStoreyRow row, KataColumnStoreyGeometry faces, KataColumnSpec spec) =>
        Plan(OnFaces(row, faces), spec);

    /// <summary>The sheet row with the real section's sizes.</summary>
    public static KataColumnStoreyRow OnFaces(KataColumnStoreyRow row, KataColumnStoreyGeometry faces) =>
        row with { SizeB = faces.MaxXMm - faces.MinXMm, SizeH = row.IsRound ? 0 : faces.MaxYMm - faces.MinYMm };

    /// <summary>Slots holding <paramref name="count"/> bars among <paramref name="slots"/>: pairs from the outside in, the centre when odd.</summary>
    public static IReadOnlyList<int> SymmetricSlots(int slots, int count)
    {
        var taken = new SortedSet<int>();
        for (int k = 0; taken.Count + 2 <= count && k < slots - 1 - k; k++)
        {
            taken.Add(k);
            taken.Add(slots - 1 - k);
        }

        if (taken.Count < count && slots % 2 == 1)
        {
            taken.Add(slots / 2);
        }

        return taken.ToList();
    }

    private static KataColumnSection Rectangle(KataColumnStoreyRow row, KataColumnSpec spec, double inset)
    {
        int columns = row.BarsAlongB, rows = row.BarsAlongH;
        double[] xs = Grid(row.WidthMm, inset, columns);
        double[] ys = Grid(row.DepthMm, inset, rows);
        var bars = new Dictionary<(int, int), KataColumnBar>();

        void Put(int c, int r, int layer, double diameter)
        {
            if (!bars.ContainsKey((c, r)))
            {
                bars[(c, r)] = new KataColumnBar(xs[c], ys[r], diameter, layer, (c, r));
            }
        }

        for (int c = 0; c < columns; c++)
        {
            Put(c, 0, 1, row.OuterDiameterMm);
            Put(c, rows - 1, 1, row.OuterDiameterMm);
        }

        for (int r = 1; r < rows - 1; r++)
        {
            Put(0, r, 1, row.OuterDiameterMm);
            Put(columns - 1, r, 1, row.OuterDiameterMm);
        }

        var notes = new List<string>();
        var rowSlots = InnerSlots(row.InnerBarsAlongB, rows, columns, ends: 1, $"F{row.Row}", "G", notes);
        // With a layer-2 row, H also counts the row's two slots on the column (drawn or not) as ends.
        var columnSlots = InnerSlots(row.InnerBarsAlongH, columns, rows, ends: rowSlots.Count > 0 ? 2 : 1, $"H{row.Row}", "E", notes);
        bool innerRows = rowSlots.Count > 0, innerColumns = columnSlots.Count > 0;
        foreach (int c in rowSlots)
        {
            Put(c, 1, 2, row.InnerDiameterMm);
            Put(c, rows - 2, 2, row.InnerDiameterMm);
        }

        foreach (int r in columnSlots)
        {
            Put(1, r, 2, row.InnerDiameterMm);
            Put(columns - 2, r, 2, row.InnerDiameterMm);
        }

        var ties = spec.AutoTies
            ? AutoTies(columns, innerColumns, alongB: true).Concat(AutoTies(rows, innerRows, alongB: false)).ToList()
            : new List<KataColumnTie>();
        return new KataColumnSection(bars.Values.OrderBy(b => b.Slot.Row).ThenBy(b => b.Slot.Column).ToList(), ties, inset, notes);
    }

    /// <summary>
    /// The slots of a layer-2 line, empty when there is none. A line needs a free grid line inside each face (four bars
    /// across the other direction). Its count includes <paramref name="ends"/> slots at each end that other lines own
    /// (the layer-1 face; for a column beside a layer-2 row, the row too): the rest goes symmetrically on the slots in
    /// between. A line with nothing left there adds no bar, and Kata VC then draws the section, ties included, as
    /// without it.
    /// </summary>
    private static IReadOnlyList<int> InnerSlots(int count, int across, int slots, int ends, string cell, string acrossCell, List<string> notes)
    {
        if (count <= 0)
        {
            return Array.Empty<int>();
        }

        if (across < 4)
        {
            notes.Add($"{cell} = {count} ignored: layer 2 needs at least 4 bars in {acrossCell}.");
            return Array.Empty<int>();
        }

        int inner = slots - 2 * ends, wanted = count - 2 * ends;
        if (inner <= 0 || wanted <= 0)
        {
            string owners = ends == 1 ? "the layer-1 bars" : "the layer-1 bars and the layer-2 row";
            notes.Add($"{cell} = {count}: its ends are {owners}, no layer-2 bar added (as Kata VC).");
            return Array.Empty<int>();
        }

        var taken = SymmetricSlots(inner, Math.Min(wanted, inner)).Select(s => s + ends).ToList();
        if (taken.Count < wanted)
        {
            notes.Add($"{cell} = {count}: {taken.Count} bars placed between the ends (an odd count has no centre slot among {inner}, or more bars than slots).");
        }

        return taken;
    }

    /// <summary>
    /// Ties Kata places by itself: slots k and N−1−k are restrained for k = 2, 4 … (1, 3 … when that direction has a
    /// layer-2 line) while they are distinct, a closed tie per pair (two pairs: one tie per side), a crosstie on the
    /// centre slot when k meets it.
    /// </summary>
    private static IEnumerable<KataColumnTie> AutoTies(int slots, bool innerLine, bool alongB)
    {
        var pairs = new List<(int, int)>();
        int k = innerLine ? 1 : 2;
        for (; k < slots - 1 - k; k += 2)
        {
            pairs.Add((k, slots - 1 - k));
        }

        if (pairs.Count == 2)
        {
            yield return new KataColumnTie(KataColumnTieKind.Closed, alongB, pairs[0].Item1, pairs[1].Item1);
            yield return new KataColumnTie(KataColumnTieKind.Closed, alongB, pairs[1].Item2, pairs[0].Item2);
        }
        else
        {
            foreach (var (first, second) in pairs)
            {
                yield return new KataColumnTie(KataColumnTieKind.Closed, alongB, first, second);
            }
        }

        if (k == slots - 1 - k)
        {
            yield return new KataColumnTie(KataColumnTieKind.Crosstie, alongB, k, k);
        }
    }

    private static KataColumnSection Round(KataColumnStoreyRow row, double inset)
    {
        int count = row.BarsAlongB + row.InnerBarsAlongB;
        double centre = row.WidthMm / 2, radius = centre - inset;
        var bars = Enumerable.Range(0, count)
            .Select(i =>
            {
                double angle = Math.PI / 2 - 2 * Math.PI * i / count;
                return new KataColumnBar(centre + radius * Math.Cos(angle), centre + radius * Math.Sin(angle), row.OuterDiameterMm, 1, (i, 0));
            })
            .ToList();
        var notes = row.InnerDiameterMm != row.OuterDiameterMm
            ? new[] { $"I{row.Row}: a round section puts every bar at Ø{row.OuterDiameterMm}; Ø{row.InnerDiameterMm} is not used." }
            : Array.Empty<string>();
        return new KataColumnSection(bars, Array.Empty<KataColumnTie>(), inset, notes);
    }

    private static double[] Grid(double size, double inset, int count) =>
        Enumerable.Range(0, count).Select(i => inset + i * (size - 2 * inset) / (count - 1)).ToArray();
}
