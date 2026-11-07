namespace HPFoundation.Core.Modeling;

/// <summary>Beam forces at one output station, in metric tons and metres, with ETABS signs.</summary>
public sealed record StationForce(double XM, double ShearT, double MomentTm);

/// <summary>What an analysis gave: forces along the beam and the soil reaction to check equilibrium with.</summary>
public sealed record AnalysisResult(IReadOnlyList<StationForce> Stations, double TotalReactionT);

/// <summary>Checks results before anyone relies on them.</summary>
public static class EquilibriumCheck
{
    private const double ToleranceT = 0.01;

    public static double AppliedLoadT(ModelSpec spec) =>
        spec.Nodes.Where(node => node.Load is not null).Sum(node => node.Load!.AxialT);

    public static bool Holds(ModelSpec spec, AnalysisResult result) =>
        Math.Abs(AppliedLoadT(spec) - result.TotalReactionT) <= ToleranceT;
}
