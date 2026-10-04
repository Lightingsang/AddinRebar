namespace HPRebar.Core.ColumnRebar.Models;

/// <summary>Intermediate cross-tie settings, horizontal and vertical legs kept separate. Millimetres.</summary>
public sealed record AdditionalTieSpec
{
    public bool AddH { get; init; }

    /// <summary>
    ///     <see cref="CrossTieKind.ClosedTie"/> places one closed inner tie, <see cref="AH"/> wide, centred across the
    ///     section width; a cross-tie kind places <see cref="NH"/> of them, evenly spaced across the width.
    /// </summary>
    public CrossTieKind KindH { get; init; }

    public int NH { get; init; } = 1;

    public double AH { get; init; }

    public bool AddV { get; init; }

    /// <summary>
    ///     <see cref="CrossTieKind.ClosedTie"/> places one closed inner tie, <see cref="AV"/> deep, centred across the
    ///     section depth; a cross-tie kind places <see cref="NV"/> of them, evenly spaced across the depth.
    /// </summary>
    public CrossTieKind KindV { get; init; }

    public int NV { get; init; } = 1;

    public double AV { get; init; }
}
