using System;

namespace HPRebar.Core.Tcvn5574.Models;

/// <summary>
/// A candidate reinforcement layout option with 1 or 2 layers evaluated against TCVN 5574:2018 spacing rules.
/// </summary>
public sealed record RebarOption
{
    /// <summary>Number of bars in the outer primary layer (Layer 1).</summary>
    public int CountLayer1 { get; init; }

    /// <summary>Bar diameter in Layer 1 (mm).</summary>
    public double DiameterLayer1 { get; init; }

    /// <summary>Number of bars in the inner secondary layer (Layer 2). 0 if single layer.</summary>
    public int CountLayer2 { get; init; }

    /// <summary>Bar diameter in Layer 2 (mm).</summary>
    public double DiameterLayer2 { get; init; }

    /// <summary>Total number of bars across all layers.</summary>
    public int TotalCount => CountLayer1 + CountLayer2;

    /// <summary>Total provided steel area As,prov (mm2).</summary>
    public double ProvidedArea { get; init; }

    /// <summary>Clear horizontal spacing between bars in Layer 1 (mm).</summary>
    public double ClearSpacingLayer1 { get; init; }

    /// <summary>Clear horizontal spacing between bars in Layer 2 (mm). 0 if single layer.</summary>
    public double ClearSpacingLayer2 { get; init; }

    /// <summary>Minimum clear spacing observed across populated layers (mm).</summary>
    public double MinClearSpacing => CountLayer2 > 0
        ? Math.Min(ClearSpacingLayer1, ClearSpacingLayer2)
        : ClearSpacingLayer1;

    /// <summary>Number of layers (1 or 2).</summary>
    public int LayerCount => CountLayer2 > 0 ? 2 : 1;

    /// <summary>Steel waste / over-design ratio percentage: (As,prov - As,req) / As,req * 100%.</summary>
    public double WasteRatioPercent { get; init; }

    /// <summary>True if clear spacing satisfies TCVN 5574:2018 Clause 10.3.1.2.</summary>
    public bool IsSpacingCompliant { get; init; }

    /// <summary>True if option meets both area requirement and spacing rules.</summary>
    public bool IsValid { get; init; }

    /// <summary>Textual representation of this rebar arrangement (e.g. "3d20" or "2d20 + 2d18 (2 lớp)").</summary>
    public string Description { get; init; } = string.Empty;
}
