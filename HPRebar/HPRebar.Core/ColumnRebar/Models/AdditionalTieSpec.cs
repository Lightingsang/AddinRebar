namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>Intermediate cross-tie settings, horizontal and vertical legs kept separate. Millimetres.</summary>
public sealed record AdditionalTieSpec
{
    public bool AddH { get; init; }

    /// <summary>
    ///     0 places one closed inner tie, <see cref="AH"/> wide, centred across the section width; any other value
    ///     places <see cref="NH"/> cross-ties of that shape number, evenly spaced across the width.
    /// </summary>
    public int TypeH { get; init; }

    public int NH { get; init; } = 1;

    public double AH { get; init; }

    public bool AddV { get; init; }

    /// <summary>
    ///     0 places one closed inner tie, <see cref="AV"/> deep, centred across the section depth; any other value
    ///     places <see cref="NV"/> cross-ties of that shape number, evenly spaced across the depth.
    /// </summary>
    public int TypeV { get; init; }

    public int NV { get; init; } = 1;

    public double AV { get; init; }
}
