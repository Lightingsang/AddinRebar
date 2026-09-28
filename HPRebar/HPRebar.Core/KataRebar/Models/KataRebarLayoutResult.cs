using System;
using System.Collections.Generic;
using System.Linq;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Output of the Kata rebar calculation engine containing all generated 3D rebar curves,
/// stirrup distributions, schedules, and diagnostic validation messages.
/// </summary>
public sealed record KataRebarLayoutResult
{
    /// <summary>Beam mark/name this layout was generated for.</summary>
    public string BeamName { get; init; } = "";

    /// <summary>Continuous top longitudinal bars.</summary>
    public IReadOnlyList<KataRebarCurve> MainTopBars { get; init; } = Array.Empty<KataRebarCurve>();

    /// <summary>Continuous bottom longitudinal bars.</summary>
    public IReadOnlyList<KataRebarCurve> MainBottomBars { get; init; } = Array.Empty<KataRebarCurve>();

    /// <summary>Top extra negative reinforcement bars over supports.</summary>
    public IReadOnlyList<KataRebarCurve> ExtraTopBars { get; init; } = Array.Empty<KataRebarCurve>();

    /// <summary>Bottom extra positive reinforcement bars at midspans.</summary>
    public IReadOnlyList<KataRebarCurve> ExtraBottomBars { get; init; } = Array.Empty<KataRebarCurve>();

    /// <summary>The longitudinal bars Kata Rebar draws in Revit: main, additional, and side bars.</summary>
    public IEnumerable<KataRebarCurve> LongitudinalBars =>
        MainTopBars.Concat(MainBottomBars).Concat(ExtraTopBars).Concat(ExtraBottomBars).Concat(SideBars);

    /// <summary>Longitudinal web skin / side bars and cross-ties.</summary>
    public IReadOnlyList<KataRebarCurve> SideBars { get; init; } = Array.Empty<KataRebarCurve>();

    /// <summary>Flat bars repeated along the beam: C ties of the side bars, inner U / C stirrups.</summary>
    public IReadOnlyList<KataBarSet> BarSets { get; init; } = Array.Empty<KataBarSet>();

    /// <summary>Structured stirrup distribution zones (for Revit distribution & spacing sets).</summary>
    public IReadOnlyList<KataStirrupZoneResult> StirrupZones { get; init; } = Array.Empty<KataStirrupZoneResult>();

    /// <summary>Individual 3D stirrup curves (for test verification and 3D preview canvas).</summary>
    public IReadOnlyList<KataRebarCurve> IndividualStirrups { get; init; } = Array.Empty<KataRebarCurve>();

    /// <summary>Informative warnings and geometry discrepancy notes.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>Layouts that cannot be built (e.g. more layers than the beam depth holds); nothing is drawn.</summary>
    public IReadOnlyList<string> Blocking { get; init; } = Array.Empty<string>();

    /// <summary>True if calculation completed without fatal errors.</summary>
    public bool IsValid => Warnings.Count == 0;

    /// <summary>Estimated total steel weight in kilograms.</summary>
    public double TotalSteelWeightKg { get; init; }

    /// <summary>Total count of individual reinforcing bar items.</summary>
    public int TotalBarCount =>
        MainTopBars.Count +
        MainBottomBars.Count +
        ExtraTopBars.Count +
        ExtraBottomBars.Count +
        SideBars.Count +
        BarSets.Sum(s => s.Count) +
        IndividualStirrups.Count;
}
