namespace HPRebar.Core.ColumnRebar;

/// <summary>Lap length for a spliced bar.</summary>
public static class DefaultOverlap
{
    /// <summary>
    ///     At 50% splitting, alternate bars get a double lap so that no two neighbours stop at the same level.
    ///     Any other split percentage gives every bar the same single lap.
    /// </summary>
    public static double LapLength(int barNumber, double barDiameter, double splitOverlap, double overlapFactor)
    {
        if (!Tolerance.AreEqual(splitOverlap, 50d))
        {
            return overlapFactor * barDiameter;
        }

        return barNumber % 2 == 0
            ? overlapFactor * barDiameter
            : overlapFactor * 2 * barDiameter;
    }
}
