using System.Collections.Generic;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.Core.Tests.KataExport;

/// <summary>
/// Beam-run inputs in millimetres along the beam axis, laid out like the plans the Dynamo tool was used on.
/// </summary>
internal static class TestKataData
{
    public static readonly KataHeader Header = new("D1", 2, 500.0, 220.0, 3600.0);

    public static KataBeamPiece Piece(double start, double end, double b = 220, double h = 500, double zOffset = 0, string key = "B") =>
        new(new Interval1D(start, end), b, h, zOffset, key);

    public static KataSupport Column(double start, double end, Interval1D? upper = null, string key = "C") =>
        new(KataSupportKind.Column, new Interval1D(start, end), key, null, upper);

    public static KataSupport Footing(double start, double end, string key = "F") =>
        new(KataSupportKind.Foundation, new Interval1D(start, end), key);

    public static KataSupport Girder(double start, double end, string section, string key = "G") =>
        new(KataSupportKind.Beam, new Interval1D(start, end), key, section);

    public static KataGridCrossing Grid(string name, double station) => new(name, station);

    /// <summary>Two spans on three 400 mm columns centred on grids 1, 2, 3 at 0 / 6000 / 12000.</summary>
    public static KataRunInput TwoSpansOnColumns(IReadOnlyList<KataGridCrossing>? grids = null) => new(
        new[] { Piece(0, 6000, key: "B1"), Piece(6000, 12000, key: "B2") },
        new[] { Column(-200, 200), Column(5800, 6200), Column(11800, 12200) },
        grids ?? new[] { Grid("1", 0), Grid("2", 6000), Grid("3", 12000) },
        Header);
}
