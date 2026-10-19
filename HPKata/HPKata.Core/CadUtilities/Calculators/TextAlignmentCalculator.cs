using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.Core.CadUtilities.Models;

namespace HPKata.Core.CadUtilities.Calculators
{
    public enum TextHorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    public enum TextVerticalAlignment
    {
        Top,
        Middle,
        Bottom
    }

    /// <summary>
    /// Computes aligned insertion coordinates and equal distributions for multiple text/annotation items.
    /// </summary>
    public static class TextAlignmentCalculator
    {
        public static List<Point2D> AlignHorizontal(
            IEnumerable<Point2D> points,
            TextHorizontalAlignment mode,
            double? referenceX = null)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            var list = points.ToList();
            if (list.Count == 0) return list;

            double targetX = referenceX ?? mode switch
            {
                TextHorizontalAlignment.Left => list.Min(p => p.X),
                TextHorizontalAlignment.Center => (list.Min(p => p.X) + list.Max(p => p.X)) * 0.5,
                TextHorizontalAlignment.Right => list.Max(p => p.X),
                _ => list[0].X
            };

            return list.Select(p => new Point2D(targetX, p.Y)).ToList();
        }

        public static List<Point2D> AlignVertical(
            IEnumerable<Point2D> points,
            TextVerticalAlignment mode,
            double? referenceY = null)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            var list = points.ToList();
            if (list.Count == 0) return list;

            double targetY = referenceY ?? mode switch
            {
                TextVerticalAlignment.Top => list.Max(p => p.Y),
                TextVerticalAlignment.Middle => (list.Min(p => p.Y) + list.Max(p => p.Y)) * 0.5,
                TextVerticalAlignment.Bottom => list.Min(p => p.Y),
                _ => list[0].Y
            };

            return list.Select(p => new Point2D(p.X, targetY)).ToList();
        }

        public static List<Point2D> DistributeVertically(IEnumerable<Point2D> points)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            var sorted = points.OrderByDescending(p => p.Y).ToList();
            if (sorted.Count <= 2) return sorted;

            double topY = sorted[0].Y;
            double bottomY = sorted[sorted.Count - 1].Y;
            double step = (topY - bottomY) / (sorted.Count - 1);

            var result = new List<Point2D>();
            for (int i = 0; i < sorted.Count; i++)
            {
                result.Add(new Point2D(sorted[i].X, topY - i * step));
            }

            return result;
        }
    }
}
