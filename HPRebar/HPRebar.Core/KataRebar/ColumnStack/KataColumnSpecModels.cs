using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.ColumnStack;

/// <summary>Hoop notation of sheet "Cot" ("d10a100/200/50"): Ø, dense and middle spacing, spacing inside the beam.</summary>
public sealed record KataColumnHoopSpec(double DiameterMm, double DenseSpacingMm, double MiddleSpacingMm, double BeamSpacingMm);

/// <summary>Tie notation ("d8a400"): Ø and spacing along the storey.</summary>
public sealed record KataColumnTieSpec(double DiameterMm, double SpacingMm);

/// <summary>One storey row of sheet "Cot" (rows 10…), as Kata reads it.</summary>
/// <param name="SizeB">b with Kata's sign (negative = keeps the left face of the storey below); the diameter when round.</param>
/// <param name="BarsAlongB">E: layer-1 bars on each b face, corners included (all bars on the circle with F when round).</param>
/// <param name="InnerBarsAlongB">F: layer-2 bars on the row parallel to each b face (0 = none).</param>
/// <param name="BarsAlongH">G: layer-1 bars on each h face, corners included.</param>
/// <param name="InnerBarsAlongH">H: layer-2 bars on the column parallel to each h face (0 = none).</param>
/// <param name="OuterDiameterMm">Ø of layer 1 (I "a/b": a).</param>
/// <param name="InnerDiameterMm">Ø of layer 2 (I "a/b": b; a when I holds one value).</param>
/// <param name="LapStartMm">J: where the lap of the bars coming from below starts, above the storey base; null when empty.</param>
public sealed record KataColumnStoreyRow(
    int Row,
    string Name,
    double HeightMm,
    double SizeB,
    double SizeH,
    int BarsAlongB,
    int InnerBarsAlongB,
    int BarsAlongH,
    int InnerBarsAlongH,
    double OuterDiameterMm,
    double InnerDiameterMm,
    double? LapStartMm,
    double BeamDepthMm)
{
    public bool IsRound => SizeH == 0;

    public double WidthMm => System.Math.Abs(SizeB);

    public double DepthMm => IsRound ? WidthMm : System.Math.Abs(SizeH);
}

/// <summary>
/// Sheet "Cot" as the rebar plan needs it. The last storey row is the stub Kata draws only as the lap above the top
/// storey.
/// </summary>
/// <param name="CoverMm">F4 first number: concrete cover to the hoop.</param>
/// <param name="StarterLiftMm">F4 second number: starter bar foot above the pile-cap bottom.</param>
/// <param name="BendFactor">F5: starter foot (and top anchorage) length in bar diameters.</param>
/// <param name="AutoTies">I3 "Bố trí đai tự động": ties placed by Kata's rule with <see cref="Ties"/>; rows M–V ignored.</param>
/// <param name="HoopsThroughBeam">K3 "Bố trí đai xuyên dầm": the top hoop zone runs through the beam depth.</param>
/// <param name="DenseAtLaps">H3 "Bố trí đai dày tại vị trí nối thép".</param>
/// <param name="TightInnerLayer">G3 "Thép lớp 2 ghép sát thép lớp 1".</param>
public sealed record KataColumnSpec(
    string Name,
    int Count,
    double PileCapBottomMm,
    double PileCapHeightMm,
    double CoverMm,
    double StarterLiftMm,
    double BendFactor,
    KataColumnHoopSpec Hoops,
    KataColumnTieSpec Ties,
    bool AutoTies,
    bool HoopsThroughBeam,
    bool DenseAtLaps,
    bool TightInnerLayer,
    IReadOnlyList<KataColumnStoreyRow> Rows);

/// <summary>What sheet "Cot" holds, or why the plan cannot be built from it (one message per wrong cell).</summary>
public sealed record KataColumnSpecReading(KataColumnSpec? Spec, IReadOnlyList<string> Errors);
