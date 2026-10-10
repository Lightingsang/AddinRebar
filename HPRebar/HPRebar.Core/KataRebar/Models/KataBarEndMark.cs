namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Kata's cut mark at the end of a bar on the beam's long section ("móc cắt kết thúc thép"): a short slanted stroke
/// from the bar end back along the bar, turned toward the inside. Coordinates are the layout's local elevation
/// (x along the beam, z up from the top, mm); the direction is a unit vector, the length depends on the view scale.
/// </summary>
public sealed record KataBarEndMark(double X, double Z, double DirectionX, double DirectionZ)
{
    /// <summary>Printed length of the stroke (mm on paper): 80 mm in the model at 1:25.</summary>
    public const double PaperLengthMm = 3.2;

    /// <summary>Angle between the stroke and the bar (°).</summary>
    public const double AngleDegrees = 30.0;

    /// <summary>The far end of the stroke for a view of scale 1:<paramref name="viewScale"/>.</summary>
    public (double X, double Z) Tip(double viewScale) =>
        (X + DirectionX * PaperLengthMm * viewScale, Z + DirectionZ * PaperLengthMm * viewScale);
}
