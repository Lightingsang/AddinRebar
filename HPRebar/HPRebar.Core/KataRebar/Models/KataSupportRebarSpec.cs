using System;
using System.Collections.Generic;

namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// Reinforcement and geometric specification at a support/column location (odd columns in sheet Dam).
/// </summary>
public sealed record KataSupportRebarSpec
{
    /// <summary>0-based index of this support along the beam run.</summary>
    public int SupportIndex { get; init; }

    /// <summary>Column/support width along beam axis in mm (sheet Dam row 11). 0 indicates cantilever tip or free end.</summary>
    public double ColumnWidth { get; init; }

    /// <summary>Optional section description if bearing on another beam (e.g. "300x500").</summary>
    public string SupportSection { get; init; } = "";

    /// <summary>True if this support represents a cantilever tip or zero-width end joint.</summary>
    public bool IsCantilever => ColumnWidth <= 0.0;

    /// <summary>Name of grid line intersecting this support (sheet Dam row 22, e.g. "1", "A").</summary>
    public string GridName { get; init; } = "";

    /// <summary>Offset from support centerline to grid line in mm (sheet Dam row 23).</summary>
    public double GridOffset { get; init; }

    /// <summary>Width of column directly above this floor level in mm (sheet Dam row 19).</summary>
    public double UpperColumnWidth { get; init; }

    /// <summary>Offset of column directly above this floor level in mm (sheet Dam row 19).</summary>
    public double UpperColumnOffset { get; init; }

    /// <summary>Width of secondary intersecting beam bearing at this support in mm (sheet Dam row 20).</summary>
    public double CrossingBeamWidth { get; init; }

    /// <summary>Offset of secondary intersecting beam in mm (sheet Dam row 21).</summary>
    public double CrossingBeamOffset { get; init; }

    /// <summary>Top extra negative reinforcement bars - Layer 1 (sheet Dam row 13).</summary>
    public IReadOnlyList<KataBarItem> TopExtraLayer1 { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Top extra negative reinforcement bars - Layer 2 (sheet Dam row 14).</summary>
    public IReadOnlyList<KataBarItem> TopExtraLayer2 { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Top extra negative reinforcement bars - Layer 3 (sheet Dam row 15).</summary>
    public IReadOnlyList<KataBarItem> TopExtraLayer3 { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Top extra negative reinforcement bars - Layer 4 (sheet Dam row 16).</summary>
    public IReadOnlyList<KataBarItem> TopExtraLayer4 { get; init; } = Array.Empty<KataBarItem>();

    /// <summary>Convenience accessor for all 4 top extra bar layers.</summary>
    public IReadOnlyList<IReadOnlyList<KataBarItem>> AllTopExtraLayers =>
        new[] { TopExtraLayer1, TopExtraLayer2, TopExtraLayer3, TopExtraLayer4 };
}
