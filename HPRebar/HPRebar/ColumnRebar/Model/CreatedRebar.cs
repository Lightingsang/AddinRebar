using System.Collections.Generic;
using Autodesk.Revit.DB.Structure;

namespace HPRebar.ColumnRebar.Model;

/// <summary>Everything one run of the creation service put into the model, for the annotation pass to tag.</summary>
public sealed record CreatedRebar
{
    public IReadOnlyList<Rebar> MainBars { get; init; } = new List<Rebar>();

    public IReadOnlyList<Rebar> Stirrups { get; init; } = new List<Rebar>();

    /// <summary>Intermediate cross-ties, horizontal and vertical legs together.</summary>
    public IReadOnlyList<Rebar> Ties { get; init; } = new List<Rebar>();

    public int Total => MainBars.Count + Stirrups.Count + Ties.Count;
}
