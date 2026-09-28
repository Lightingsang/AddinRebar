using System;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Maps a layout's local X (0 at the outer face of the sheet's first support) onto the stations of a drawing
/// of the same run. The drawing lists the supports in its own order; when that order runs against the sheet
/// the layout was planned from, X counts back from the drawing's far end.
/// </summary>
public sealed record KataStationMap(double Origin, int Direction)
{
    public static readonly KataStationMap Identity = new(0.0, 1);

    public double ToStation(double localX) => Origin + Direction * localX;

    /// <param name="firstStart">Station where the drawing's first support starts.</param>
    /// <param name="lastEnd">Station where the drawing's last support ends.</param>
    /// <param name="sameOrder">True when the drawing lists the supports in the order of the planned sheet.</param>
    public static KataStationMap For(double firstStart, double lastEnd, bool sameOrder) =>
        sameOrder ? new KataStationMap(firstStart, 1) : new KataStationMap(lastEnd, -1);

    public (double Start, double End) ToStations(double localStart, double localEnd)
    {
        double a = ToStation(localStart), b = ToStation(localEnd);
        return (Math.Min(a, b), Math.Max(a, b));
    }
}
