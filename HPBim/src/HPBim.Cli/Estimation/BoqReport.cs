namespace HPBim.Cli.Estimation;

using System.Collections.Generic;

/// <summary>
/// Takeoff summary line item for a pipe run.
/// </summary>
public readonly record struct PipeTakeoffItem(
    string SystemType,
    double DiameterMm,
    string Material,
    double LengthM);

/// <summary>
/// Comprehensive BOQ (Bill of Quantities) report for concrete, formwork, rebar, and MEP.
/// </summary>
public sealed record BoqReport(
    double TotalConcreteM3,
    double TotalFormworkM2,
    double TotalRebarKg,
    double SmallRebarKg,
    double LargeRebarKg,
    IReadOnlyDictionary<string, double> ConcreteByComponent,
    IReadOnlyDictionary<string, double> FormworkByComponent,
    IReadOnlyDictionary<string, double> RebarByZone,
    IReadOnlyDictionary<string, int> FixturesByCategory,
    IReadOnlyList<PipeTakeoffItem> Pipes);
