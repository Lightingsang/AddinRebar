using System;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>One end of a longitudinal bar: where its centreline stops and the leg bent at that point.</summary>
/// <param name="X">Station of the end of the horizontal part (mm).</param>
/// <param name="Leg">Length of the 90° leg (mm), 0 for a straight end.</param>
/// <param name="Shortfall">Anchorage length that did not fit in the beam depth (mm).</param>
public readonly record struct KataBarEnd(double X, double Leg, double Shortfall)
{
    public bool IsBent => Leg > 0.0;
}

/// <summary>
/// End anchorage of a main bar in an end support, measured from the support's inner face. A support wide
/// enough holds the bar straight; otherwise the bar runs to the far face (keeping its cover to the bar
/// centre) and bends 90° with a leg that supplies the rest, never shorter than the minimum leg and never
/// longer than the room between the two main bar layers. A leg is rounded up to the leg step when the rounded
/// leg still fits.
/// </summary>
public static class KataAnchorage
{
    /// <param name="innerFace">Station of the support face towards the span.</param>
    /// <param name="supportWidth">Width of the support along the beam (positive).</param>
    /// <param name="outward">+1 when the support lies at larger stations than the span, −1 otherwise.</param>
    /// <param name="centreCover">Distance from the concrete face to the bar centre, used at the far face.</param>
    /// <param name="required">Anchorage length (factor × d).</param>
    /// <param name="minimumLeg">Shortest bent leg.</param>
    /// <param name="legRoom">Longest leg that fits between the two main bar layers.</param>
    /// <param name="inset">Extra distance kept from the far face, for a leg moved inboard.</param>
    /// <param name="legStep">Bent legs are rounded up to a multiple of it when that still fits; 0 = no rounding.</param>
    public static KataBarEnd Solve(
        double innerFace,
        double supportWidth,
        int outward,
        double centreCover,
        double required,
        double minimumLeg,
        double legRoom,
        double inset = 0.0,
        double legStep = 0.0)
    {
        if (outward is not (1 or -1)) throw new ArgumentOutOfRangeException(nameof(outward), outward, "Use +1 or -1.");

        if (supportWidth <= 0.0) throw new ArgumentOutOfRangeException(nameof(supportWidth), supportWidth, "A free end has nothing to anchor in.");

        double available = supportWidth - centreCover - inset;
        if (available >= required)
            return new KataBarEnd(innerFace + outward * required, 0.0, 0.0);

        double leg = Math.Max(required - Math.Max(0.0, available), minimumLeg);
        double shortfall = 0.0;
        double room = Math.Max(0.0, legRoom);
        if (leg > room)
        {
            shortfall = leg - room;
            leg = room;
        }
        else if (legStep > 0.0)
        {
            double rounded = Math.Ceiling(leg / legStep - 1e-9) * legStep;
            if (rounded <= room + 1e-6) leg = rounded;
        }

        double outerFace = innerFace + outward * supportWidth;
        return new KataBarEnd(outerFace - outward * (centreCover + inset), leg, shortfall);
    }

    /// <summary>
    /// How far a bottom-bar leg moves inboard when it would sit on top of the top-bar leg in the same plane:
    /// half of both diameters plus the larger of the clear gap and the bottom bar diameter.
    /// </summary>
    public static double BottomLegInset(double topDiameter, double bottomDiameter, double minimumGap) =>
        (topDiameter + bottomDiameter) / 2.0 + Math.Max(minimumGap, bottomDiameter);

    /// <summary>The two legs overlap in height when together they are longer than the room between the layers.</summary>
    public static bool LegsOverlap(KataBarEnd top, KataBarEnd bottom, double legRoom) =>
        top.IsBent && bottom.IsBent && top.Leg + bottom.Leg > legRoom + 1e-6;
}
