using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Kata draws a top or bottom bar half a bar and half a stirrup nearer its face than the bar's centre lies: a
/// layer-1 bar sits on the stirrup's centre line (T2-DY7: Ø18 bars in Ø8 stirrups at 25 cover drawn 29 mm inside
/// the face, layer 2 at 72), side bars where they are.
/// </summary>
public static class KataDrawingLevels
{
    /// <summary>How far up (positive) Kata draws <paramref name="bar"/> from its centre line.</summary>
    public static double Shift(KataRebarCurve bar, double stirrupDiameter)
    {
        double half = (bar.Diameter + stirrupDiameter) / 2.0;
        return bar.Role switch
        {
            KataBarRole.MainTop or KataBarRole.ExtraTop => half,
            KataBarRole.MainBottom or KataBarRole.ExtraBottom => -half,
            _ => 0.0
        };
    }

    /// <summary>The height Kata draws <paramref name="bar"/> at where its centre lies at <paramref name="z"/>.</summary>
    public static double Drawn(KataRebarCurve bar, double z, double stirrupDiameter) => z + Shift(bar, stirrupDiameter);
}
