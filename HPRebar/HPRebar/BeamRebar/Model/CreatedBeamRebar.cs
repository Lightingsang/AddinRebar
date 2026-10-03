using System;
using System.Collections.Generic;
using Autodesk.Revit.DB.Structure;

namespace HPRebar.BeamRebar.Model;

/// <summary>
/// Inventory of all Rebar instances instantiated in the Revit model.
/// </summary>
public sealed record CreatedBeamRebar
{
    public IReadOnlyList<Rebar> Stirrups { get; init; } = Array.Empty<Rebar>();

    public IReadOnlyList<Rebar> MainBars { get; init; } = Array.Empty<Rebar>();

    public IReadOnlyList<Rebar> AdditionalBars { get; init; } = Array.Empty<Rebar>();

    public IReadOnlyList<Rebar> SideBars { get; init; } = Array.Empty<Rebar>();

    public IReadOnlyList<Rebar> SpecialBars { get; init; } = Array.Empty<Rebar>();

    public int Total => Stirrups.Count + MainBars.Count + AdditionalBars.Count + SideBars.Count + SpecialBars.Count;
}
