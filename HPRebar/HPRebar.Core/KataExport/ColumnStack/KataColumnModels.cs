using System.Collections.Generic;
using HPRebar.Core.KataExport.Models;

namespace HPRebar.Core.KataExport.ColumnStack;

public enum KataColumnShape
{
    Rectangle,
    Circle
}

/// <summary>
/// Plan section of one storey in the frame of the stack's first storey (local X along b, local Y along h), in
/// millimetres. A circle uses the same extents: both spans equal the diameter.
/// </summary>
public sealed record KataColumnSection(KataColumnShape Shape, double MinXMm, double MaxXMm, double MinYMm, double MaxYMm)
{
    public double WidthMm => MaxXMm - MinXMm;

    public double DepthMm => MaxYMm - MinYMm;

    public double CentreXMm => (MinXMm + MaxXMm) / 2;

    public double CentreYMm => (MinYMm + MaxYMm) / 2;

    public static KataColumnSection Rectangle(double minXMm, double maxXMm, double minYMm, double maxYMm) =>
        new(KataColumnShape.Rectangle, minXMm, maxXMm, minYMm, maxYMm);

    public static KataColumnSection Circle(double centreXMm, double centreYMm, double diameterMm) =>
        new(
            KataColumnShape.Circle,
            centreXMm - diameterMm / 2,
            centreXMm + diameterMm / 2,
            centreYMm - diameterMm / 2,
            centreYMm + diameterMm / 2);
}

/// <summary>One storey of the column stack, bottom to top.</summary>
/// <param name="LevelName">Level at the storey base; Kata labels the storey with it.</param>
/// <param name="BeamDepthMm">Depth of the deepest beam framing into the storey head; 0 when none.</param>
/// <param name="TopLevelName">Level at the storey head; on the top storey it names the row Kata needs above it.</param>
public sealed record KataColumnStorey(
    string LevelName,
    double BaseElevationMm,
    double TopElevationMm,
    KataColumnSection Section,
    double BeamDepthMm,
    string? TopLevelName = null);

/// <summary>Pile cap or footing the stack starts on.</summary>
public sealed record KataPileCap(double BottomElevationMm, double HeightMm);

/// <summary>Everything read from Revit for one column stack.</summary>
/// <param name="CrossingBeamWidthMm">Widest beam at the first storey head (C1); null keeps the user's cell.</param>
/// <param name="SlabThicknessMm">Floor at the first storey head (C2); null keeps the user's cell.</param>
public sealed record KataColumnStackInput(
    object? Name,
    object? Count,
    IReadOnlyList<KataColumnStorey> Storeys,
    KataPileCap? PileCap,
    double? CrossingBeamWidthMm = null,
    double? SlabThicknessMm = null);

/// <summary>One storey row of sheet "Cot": A name, B height (m), C b (mm), D h (mm), K beam depth (mm).</summary>
/// <param name="SizeB">
/// b with Kata's sign: negative = the storey keeps the left face of the storey below, positive = centred on it;
/// the diameter for a circle.
/// </param>
/// <param name="SizeH">h with the same sign rule; 0 for a circle (Kata's circle marker).</param>
/// <param name="IsCapRow">
/// The row Kata draws only as the stub above the last storey (Kata never draws its last row as a storey).
/// </param>
public sealed record KataColumnRow(KataText Name, double HeightM, double SizeB, double SizeH, double BeamDepthMm, bool IsCapRow = false);

/// <summary>
/// Cell values for sheet "Cot". <see cref="Header"/> maps a cell address to its value; a null value keeps the
/// cell the user typed. Storeys go to rows <see cref="FirstStoreyRow"/>.. in A, B, C, D, K; bar, hoop and tie
/// cells are user input and never written.
/// </summary>
public sealed record KataColumnSheet(
    IReadOnlyDictionary<string, object?> Header,
    IReadOnlyList<KataColumnRow> Storeys,
    IReadOnlyList<string> Warnings)
{
    public const string SheetName = "Cot";

    /// <summary>Row of the pile cap ("PILECAPS" in A, its height in metres in B).</summary>
    public const int PileCapRow = 9;

    public const int FirstStoreyRow = 10;

    /// <summary>Last row of Kata's storey list (data validation of the section picker covers A10:A216).</summary>
    public const int LastStoreyRow = 216;

    /// <summary>Storeys a stack may have: one row stays free for the stub row Kata needs above the top storey.</summary>
    public static int MaxStoreys => LastStoreyRow - FirstStoreyRow;
}
