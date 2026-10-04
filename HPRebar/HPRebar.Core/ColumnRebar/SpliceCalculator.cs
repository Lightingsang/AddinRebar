using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>
///     Works out where each main bar has to arrive on the section above.
///
///     Bars that carry straight on through the beam get redistributed: only the bars actually making the
///     transition are counted, and they are respread across the upper face. That is what lets a wide column
///     splice into a narrow one without bars colliding. Bars that stop under the beam keep their grid slot.
/// </summary>
public static class SpliceCalculator
{
    /// <summary>
    ///     Plan position each bar must reach at the top of its segment, aligned index-for-index with
    ///     <paramref name="bars"/>. With no section above, every bar simply stays where it is.
    /// </summary>
    public static IReadOnlyList<PlanPoint> ComputeUpperPositions(
        ColumnSection? upper,
        BarLayoutSpec spec,
        double lowerStirrupDiameter,
        double upperStirrupDiameter,
        IReadOnlyList<BarPosition> bars,
        IReadOnlyList<SpliceSpec> splices)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        if (bars is null) throw new ArgumentNullException(nameof(bars));
        if (splices is null) throw new ArgumentNullException(nameof(splices));

        if (splices.Count != bars.Count)
        {
            throw new ArgumentException("One splice spec is required per bar.", nameof(splices));
        }

        if (upper is null)
        {
            return bars.Select(bar => new PlanPoint(bar.X0, bar.Y0)).ToList();
        }

        var transitions = splices.Select(IsTransition).ToList();
        var barNumbers = bars.Select(bar => bar.BarNumber).ToList();

        var defaultLowerX = DefaultUpperPositions.X(upper, spec, lowerStirrupDiameter, spec.BarDiameter);
        var defaultLowerY = DefaultUpperPositions.Y(upper, spec, lowerStirrupDiameter, spec.BarDiameter);
        var defaultUpperX = DefaultUpperPositions.X(upper, spec, upperStirrupDiameter, spec.BarDiameter);
        var defaultUpperY = DefaultUpperPositions.Y(upper, spec, upperStirrupDiameter, spec.BarDiameter);

        var result = new List<PlanPoint>(bars.Count);

        for (var i = 0; i < bars.Count; i++)
        {
            if (!transitions[i])
            {
                var index = bars[i].BarNumber - 1;
                result.Add(new PlanPoint(defaultLowerX[index], defaultLowerY[index]));
                continue;
            }

            result.Add(upper.Shape == SectionShape.Rectangle
                ? RectangleTransition(upper, spec, upperStirrupDiameter, bars[i], barNumbers, transitions, defaultUpperX)
                : CircularTransition(upper, spec, upperStirrupDiameter, bars[i], barNumbers, transitions, defaultUpperX, defaultUpperY));
        }

        return result;
    }

    /// <summary>A bar bends across into the column above only when top dowels are on and set to type 0.</summary>
    public static bool IsTransition(SpliceSpec splice) =>
        splice.IsTopDowels && splice.TopStyle == TopDowelStyle.BendIntoColumnAbove;

    private static PlanPoint RectangleTransition(
        ColumnSection upper,
        BarLayoutSpec spec,
        double ds,
        BarPosition bar,
        IReadOnlyList<int> barNumbers,
        IReadOnlyList<bool> transitions,
        IReadOnlyList<double> defaultX)
    {
        var d = spec.BarDiameter;
        var inset = spec.Cover + ds + d / 2;
        var side = BarSideClassifier.SideOf(bar.BarNumber, spec.Nx, spec.Ny);

        var group = new List<int>();

        for (var i = 0; i < barNumbers.Count; i++)
        {
            if (transitions[i] && BarSideClassifier.SideOf(barNumbers[i], spec.Nx, spec.Ny) == side)
            {
                group.Add(barNumbers[i]);
            }
        }

        group.Sort();

        var index = group.IndexOf(bar.BarNumber);
        var count = group.Count;

        switch (side)
        {
            case BarSide.South:
            {
                var y = upper.SouthPosition + inset;

                if (count == 1) return new PlanPoint(defaultX[bar.BarNumber - 1], y);

                var delta = (upper.B - 2 * spec.Cover - 2 * ds - d) / (count - 1);
                var nudge = index == count - 1 ? -d : d;

                return new PlanPoint(upper.WestPosition + inset + nudge + index * delta, y);
            }

            case BarSide.East:
            {
                // Faces running north-south share their corners with the south and north faces, so the
                // bars are spread over count + 1 gaps to stay clear of them.
                var delta = (upper.H - 2 * spec.Cover - 2 * ds - d) / (count + 1);

                return new PlanPoint(
                    upper.EastPosition - inset,
                    upper.SouthPosition + inset + (index + 1) * delta - d);
            }

            case BarSide.North:
            {
                var y = upper.NorthPosition - inset;

                if (count == 1) return new PlanPoint(defaultX[bar.BarNumber - 1], y);

                var delta = (upper.B - 2 * spec.Cover - 2 * ds - d) / (count - 1);
                var nudge = index == count - 1 ? d : -d;

                return new PlanPoint(upper.EastPosition - inset + nudge - index * delta, y);
            }

            default:
            {
                var delta = (upper.H - 2 * spec.Cover - 2 * ds - d) / (count + 1);

                return new PlanPoint(
                    upper.WestPosition + inset,
                    upper.NorthPosition - inset - (index + 1) * delta - d);
            }
        }
    }

    private static PlanPoint CircularTransition(
        ColumnSection upper,
        BarLayoutSpec spec,
        double ds,
        BarPosition bar,
        IReadOnlyList<int> barNumbers,
        IReadOnlyList<bool> transitions,
        IReadOnlyList<double> defaultX,
        IReadOnlyList<double> defaultY)
    {
        var index0 = bar.BarNumber - 1;

        // With four bars there is nothing to respread, and the bar sitting exactly on a quadrant boundary
        // keeps its slot in every case.
        if (spec.Nd == 4 || bar.BarNumber % (spec.Nd / 4) == 1)
        {
            return new PlanPoint(defaultX[index0], defaultY[index0]);
        }

        var perQuadrant = spec.Nd / 4;
        var quadrant = BarSideClassifier.QuadrantOf(bar.BarNumber, spec.Nd);
        var lowerExclusive = quadrant * perQuadrant + 1;
        var upperExclusive = (quadrant + 1) * perQuadrant + 1;

        var group = new List<int>();

        for (var i = 0; i < barNumbers.Count; i++)
        {
            var number = barNumbers[i];
            var insideQuadrant = number > lowerExclusive && (quadrant == 3 || number < upperExclusive);

            if (transitions[i] && insideQuadrant)
            {
                group.Add(number);
            }
        }

        group.Sort();

        var index = group.IndexOf(bar.BarNumber);
        var count = group.Count;

        if (count == 0)
        {
            return new PlanPoint(defaultX[index0], defaultY[index0]);
        }

        var d = spec.BarDiameter;
        var radius = upper.D * 0.5 - spec.Cover - ds - d * 0.5;

        // One bar diameter of clearance from the quadrant boundary, then an even spread over the quarter turn.
        var seed = 2 * d / (radius * 2);
        var angle = seed + (index + 1) * (Math.PI / 2) / (count + 1) + quadrant * Math.PI / 2;

        return new PlanPoint(
            upper.CenterX + radius * Math.Round(Math.Cos(angle), 9),
            upper.CenterY + radius * Math.Round(Math.Sin(angle), 9));
    }
}
