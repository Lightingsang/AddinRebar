using System;

namespace HPRebar.Core.BeamRebar.Calculators;

/// <summary>Default leg length of a main or additional bar bent down or up at a support.</summary>
public static class BeamHookLength
{
    /// <summary>30 bar diameters, never less than 200 mm.</summary>
    public static double Default(double barDiameterMm) => Math.Max(30.0 * barDiameterMm, 200.0);
}
