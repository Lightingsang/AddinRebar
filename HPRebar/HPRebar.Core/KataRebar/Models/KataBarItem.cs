using System;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// A single item or group of reinforcing bars (e.g., 2f18, 3f20, 2d8).
/// </summary>
public sealed record KataBarItem(
    int Count = 0,
    double Diameter = 0.0,
    int Layer = 1,
    double Offset = 0.0,
    string RawNotation = "")
{
    public static readonly KataBarItem Empty = new(0, 0.0);

    /// <summary>Total cross-sectional steel area in mm².</summary>
    public double TotalAreaMm2 => Count > 0 && Diameter > 0.0
        ? Count * Math.PI * (Diameter / 2.0) * (Diameter / 2.0)
        : 0.0;

    /// <summary>True if this item represents no bars.</summary>
    public bool IsEmpty => Count <= 0 || Diameter <= 0.0;

    /// <summary>The same bars: count and diameter, whatever the notation, layer or offset.</summary>
    public bool SameBars(KataBarItem other) =>
        other is not null && (IsEmpty ? other.IsEmpty : !other.IsEmpty && Count == other.Count && Math.Abs(Diameter - other.Diameter) < 0.5);

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(RawNotation))
            return RawNotation;

        return IsEmpty ? "-" : $"{Count}f{Diameter:0}";
    }
}
