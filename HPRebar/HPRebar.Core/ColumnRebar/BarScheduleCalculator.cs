using System;
using System.Collections.Generic;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>
///     Collapses a set of main bars into schedule rows, one row per distinct bar.
/// </summary>
public static class BarScheduleCalculator
{
    /// <summary>
    ///     Schedule rows in the order the bars were first met. <paramref name="identicalColumns"/> multiplies
    ///     every row so one schedule can stand for a group of identical columns.
    /// </summary>
    public static IReadOnlyList<BarScheduleItem> Group(
        IReadOnlyList<BarPolyline> bars,
        int identicalColumns = 1,
        string name = "L1")
    {
        if (bars is null) throw new ArgumentNullException(nameof(bars));

        if (identicalColumns < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(identicalColumns), identicalColumns, "At least one column is required.");
        }

        var remaining = new List<BarPolyline>(bars);
        var rows = new List<BarScheduleItem>();

        while (remaining.Count > 0)
        {
            var seed = remaining[0];
            var matches = new List<BarPolyline>();

            foreach (var candidate in remaining)
            {
                if (AreSameBar(seed, candidate))
                {
                    matches.Add(candidate);
                }
            }

            rows.Add(BarShapeClassifier.Classify(matches[0], matches.Count * identicalColumns, name));

            foreach (var matched in matches)
            {
                remaining.Remove(matched);
            }
        }

        return rows;
    }

    /// <summary>
    ///     Whether two bars can share a schedule row: same product, same splice arrangement, same number of
    ///     legs and the same cut length. Note the top anchorage length is deliberately not compared — bars
    ///     staggered to a double lap already differ in total length, which rule catches them.
    /// </summary>
    public static bool AreSameBar(BarPolyline first, BarPolyline second)
    {
        if (first is null) throw new ArgumentNullException(nameof(first));
        if (second is null) throw new ArgumentNullException(nameof(second));

        if (!string.Equals(first.BarTypeName, second.BarTypeName, StringComparison.Ordinal)) return false;
        if (!Tolerance.AreEqual(first.Diameter, second.Diameter)) return false;

        var a = first.Splice;
        var b = second.Splice;

        if (a.IsTopDowels != b.IsTopDowels) return false;
        if (a.TopDowelsType != b.TopDowelsType) return false;
        if (a.TopDowelsType != 0 && !Tolerance.AreEqual(a.LaTop, b.LaTop)) return false;

        if (a.IsBottomDowels != b.IsBottomDowels) return false;
        if (a.BottomDowelsType != b.BottomDowelsType) return false;

        if (a.BottomDowelsType == 0)
        {
            if (!Tolerance.AreEqual(a.LcBottom, b.LcBottom)) return false;
        }
        else
        {
            if (!Tolerance.AreEqual(a.LaBottom, b.LaBottom)) return false;
            if (!Tolerance.AreEqual(a.LbBottom, b.LbBottom)) return false;
        }

        if (first.Points.Count != second.Points.Count) return false;

        return Tolerance.AreEqual(
            BarPolylineBuilder.Length(first.Points),
            BarPolylineBuilder.Length(second.Points));
    }
}
