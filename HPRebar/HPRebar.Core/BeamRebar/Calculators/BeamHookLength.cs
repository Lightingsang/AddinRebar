using System;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>Leg length of a bar bent down or up at a support when the engineer leaves the hook length at 0.</summary>
public static class BeamHookLength
{
    /// <summary>30 bar diameters, never less than 200 mm.</summary>
    public static double Default(double barDiameterMm) => Math.Max(30.0 * barDiameterMm, 200.0);
}
