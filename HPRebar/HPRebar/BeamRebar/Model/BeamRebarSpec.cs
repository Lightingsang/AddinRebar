using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.BeamRebar.Model;

/// <summary>
/// Complete user reinforcement specification for the continuous beam run.
/// Integrates pure domain specs with resolved Revit RebarBarType references.
/// </summary>
public sealed record BeamRebarSpec
{
    /// <summary>Stirrup distribution specification (uniform or 3-zone, spacing, bar count, cover).</summary>
    public BeamStirrupSpec Stirrups { get; init; } = new();

    /// <summary>Main continuous top and bottom longitudinal reinforcement specification.</summary>
    public BeamMainBarSpec MainBars { get; init; } = new();

    /// <summary>Additional negative moment (top) and positive moment (bottom) reinforcement.</summary>
    public BeamAdditionalBarSpec AdditionalBars { get; init; } = new();

    /// <summary>Side skin reinforcement and anti-buckling cross-ties for deep beams.</summary>
    public BeamSideBarSpec SideBars { get; init; } = new();

    /// <summary>Special hanging stirrups and diagonal ties at secondary beam joints.</summary>
    public BeamSpecialBarSpec SpecialBars { get; init; } = new();

    /// <summary>Rebar bar type for main longitudinal bars.</summary>
    public RebarTypeInfo? MainBarType { get; init; }

    /// <summary>Rebar bar type for stirrups.</summary>
    public RebarTypeInfo? StirrupBarType { get; init; }

    /// <summary>Rebar bar type for top additional bars.</summary>
    public RebarTypeInfo? AddTopBarType { get; init; }

    /// <summary>Rebar bar type for bottom additional bars.</summary>
    public RebarTypeInfo? AddBottomBarType { get; init; }

    /// <summary>Rebar bar type for side skin bars.</summary>
    public RebarTypeInfo? SideBarType { get; init; }

    /// <summary>Rebar bar type for cross-ties.</summary>
    public RebarTypeInfo? TieBarType { get; init; }

    /// <summary>Value written to the Revit rebar 'Partition' parameter for schedule grouping.</summary>
    public string PartitionName { get; init; } = "Beam";

    /// <summary>Convenience access to additional top bar configs.</summary>
    public IReadOnlyList<SupportAdditionalTopBarConfig> AdditionalTopBars => AdditionalBars.SupportTopBars;

    /// <summary>Convenience access to additional bottom bar configs.</summary>
    public IReadOnlyList<SpanAdditionalBottomBarConfig> AdditionalBottomBars => AdditionalBars.SpanBottomBars;
}
