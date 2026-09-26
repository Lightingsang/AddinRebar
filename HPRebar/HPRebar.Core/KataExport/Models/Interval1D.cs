using System;

namespace HPRebar.Core.KataExport.Models;

/// <summary>
/// A closed range of stations along the beam axis, in millimetres, with <see cref="Start"/> ≤ <see cref="End"/>.
/// </summary>
public readonly record struct Interval1D
{
    public Interval1D(double start, double end)
    {
        Start = Math.Min(start, end);
        End = Math.Max(start, end);
    }

    public double Start { get; }

    public double End { get; }

    public double Length => End - Start;

    public double Mid => (Start + End) / 2.0;

    public bool Contains(double station, double tolerance = KataTolerance.StationMm) =>
        station >= Start - tolerance && station <= End + tolerance;

    public bool Overlaps(Interval1D other, double tolerance = KataTolerance.StationMm) =>
        other.Start <= End + tolerance && other.End >= Start - tolerance;

    public Interval1D Union(Interval1D other) =>
        new(Math.Min(Start, other.Start), Math.Max(End, other.End));
}
