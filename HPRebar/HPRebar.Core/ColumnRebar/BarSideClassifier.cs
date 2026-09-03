using System;
using HPRebar.Core.ColumnRebar.Models;

namespace HPRebar.Core.ColumnRebar;

/// <summary>
///     Maps a one-based bar number onto the face it sits on. Numbering runs clockwise from the south-west
///     corner: the south face takes bars 1..nx, then the east face climbs, the north face runs back west and
///     the west face descends. Corners are owned by the horizontal faces.
/// </summary>
public static class BarSideClassifier
{
    public static BarSide SideOf(int barNumber, int nx, int ny)
    {
        if (nx < 2) throw new ArgumentOutOfRangeException(nameof(nx), nx, "A rectangular section needs at least two bars per face.");
        if (ny < 2) throw new ArgumentOutOfRangeException(nameof(ny), ny, "A rectangular section needs at least two bars per face.");

        var total = 2 * nx + 2 * (ny - 2);

        if (barNumber < 1 || barNumber > total)
        {
            throw new ArgumentOutOfRangeException(nameof(barNumber), barNumber, $"Bar number must be within 1..{total}.");
        }

        if (barNumber <= nx) return BarSide.South;
        if (barNumber <= nx + ny - 2) return BarSide.East;
        if (barNumber <= 2 * nx + ny - 2) return BarSide.North;

        return BarSide.West;
    }

    /// <summary>
    ///     Quadrant 0..3 of a bar on a circular section, counter-clockwise from the positive X axis.
    ///     <paramref name="nd"/> must be a positive multiple of four so the quadrants divide evenly.
    /// </summary>
    public static int QuadrantOf(int barNumber, int nd)
    {
        if (nd <= 0 || nd % 4 != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nd), nd, "A circular section needs a bar count that is a positive multiple of four.");
        }

        if (barNumber < 1 || barNumber > nd)
        {
            throw new ArgumentOutOfRangeException(nameof(barNumber), barNumber, $"Bar number must be within 1..{nd}.");
        }

        return (barNumber - 1) / (nd / 4);
    }
}
