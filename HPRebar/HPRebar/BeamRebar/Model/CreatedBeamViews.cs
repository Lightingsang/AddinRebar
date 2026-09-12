using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace HPRebar.BeamRebar.Model;

/// <summary>
/// Detail elevation and section views generated for the continuous beam run.
/// </summary>
public sealed record CreatedBeamViews
{
    /// <summary>Overall longitudinal elevation detail section view.</summary>
    public ViewSection? DetailView { get; init; }

    /// <summary>Alias for DetailView.</summary>
    public ViewSection? ElevationDetail => DetailView;

    /// <summary>Transverse cross-section views (e.g. 2 to 3 cuts per span).</summary>
    public IReadOnlyList<ViewSection> SectionViews { get; init; } = Array.Empty<ViewSection>();

    public int Total => (DetailView is null ? 0 : 1) + SectionViews.Count;
}
