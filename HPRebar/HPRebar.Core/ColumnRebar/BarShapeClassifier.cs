using System;
using System.Collections.Generic;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>
///     Reads a finished centre-line back as a bending shape plus its leg lengths, which is what a bar
///     schedule and a detail-shop drawing need. Millimetres.
/// </summary>
public static class BarShapeClassifier
{
    /// <summary>Shape and leg lengths of one bar. <paramref name="count"/> only fills in the row quantity.</summary>
    public static BarScheduleItem Classify(BarPolyline bar, int count, string name = "L1")
    {
        if (bar is null) throw new ArgumentNullException(nameof(bar));

        var points = bar.Points;
        var splice = bar.Splice;
        var total = BarPolylineBuilder.Length(points);

        var item = new BarScheduleItem
        {
            Name = name,
            Count = count,
            Diameter = bar.Diameter,
            Shape = BarShape.DS00,
            L = total
        };

        if (BarPolylineBuilder.IsStraight(points))
        {
            return item;
        }

        var bottomHooked = splice.IsBottomDowels
                           && splice.BottomDowelsType != 0
                           && !Tolerance.AreEqual(splice.LaBottom, 0d);

        var topBends = splice.IsTopDowels && splice.TopDowelsType == 0;
        var topHooked = splice.IsTopDowels && splice.TopDowelsType != 0 && !Tolerance.AreEqual(splice.LaTop, 0d);

        if (topBends && HasTransitionBend(points))
        {
            return WithTransitionBend(item, points, splice, bottomHooked);
        }

        if (topHooked)
        {
            var upperLeg = Math.Abs(splice.LaTop);

            if (!bottomHooked)
            {
                return item with
                {
                    Shape = splice.LaTop > 0 ? BarShape.DS05 : BarShape.DS02,
                    La = 0,
                    Lb = upperLeg,
                    L = total - upperLeg
                };
            }

            var lowerLeg = Math.Abs(splice.LaBottom);

            return item with
            {
                Shape = splice.LaTop > 0
                    ? splice.LaBottom > 0 ? BarShape.DS06 : BarShape.DS06A
                    : splice.LaBottom > 0 ? BarShape.DS03A : BarShape.DS03,
                La = lowerLeg,
                Lb = upperLeg,
                L = total - lowerLeg - upperLeg
            };
        }

        if (!bottomHooked)
        {
            return item;
        }

        // A bar hooked only at the bottom. The source tool flips the two shape names for the one case where
        // the top is a stopped dowel with no hook; that mapping is kept so ported drawings match the original.
        var topIsStoppedDowel = splice.IsTopDowels && splice.TopDowelsType != 0;
        var hookLength = Math.Abs(splice.LaBottom);

        var shape = topIsStoppedDowel
            ? splice.LaBottom > 0 ? BarShape.DS01 : BarShape.DS04
            : splice.LaBottom > 0 ? BarShape.DS04 : BarShape.DS01;

        return item with
        {
            Shape = shape,
            La = hookLength,
            Lb = 0,
            L = total - hookLength
        };
    }

    /// <summary>
    ///     True when the last-but-one leg actually moves in plan, i.e. the bar really does cross over into a
    ///     different position on the section above rather than running straight up.
    /// </summary>
    private static bool HasTransitionBend(IReadOnlyList<Point3> points)
    {
        if (points.Count < 3) return false;

        var a = points[points.Count - 3];
        var b = points[points.Count - 2];

        return !Tolerance.AreEqual(a.X, b.X) || !Tolerance.AreEqual(a.Y, b.Y);
    }

    private static BarScheduleItem WithTransitionBend(
        BarScheduleItem item,
        IReadOnlyList<Point3> points,
        SpliceSpec splice,
        bool bottomHooked)
    {
        var last = points.Count - 1;
        var a = points[last - 2];
        var b = points[last - 1];

        var planShift = Math.Sqrt(
            (a.X - b.X) * (a.X - b.X) +
            (a.Y - b.Y) * (a.Y - b.Y));

        var rise = Math.Abs(a.Z - b.Z);

        return item with
        {
            Shape = bottomHooked
                ? splice.LaBottom > 0 ? BarShape.DS07A : BarShape.DS07B
                : BarShape.DS07,
            SlopeX = rise,
            SlopeY = planShift,
            L = Math.Sqrt(rise * rise + planShift * planShift),
            La = points.Count >= 4 ? BarPolylineBuilder.Distance(points[last - 3], a) : 0d,
            Lb = BarPolylineBuilder.Distance(points[last], b),
            L1 = bottomHooked ? Math.Abs(splice.LaBottom) : 0d
        };
    }
}
