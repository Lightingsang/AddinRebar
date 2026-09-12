using System;
using System.Collections.Generic;

namespace HPRebar.Core.BeamRebar.Models;

/// <summary>
/// Detailing specification for additional reinforcement:
/// Negative moment top bars over supports and positive moment bottom bars in midspans.
/// </summary>
public sealed record BeamAdditionalBarSpec
{
    /// <summary>Per-support additional top bar configurations.</summary>
    public IReadOnlyList<SupportAdditionalTopBarConfig> SupportTopBars { get; init; } = Array.Empty<SupportAdditionalTopBarConfig>();

    /// <summary>Per-span additional bottom bar configurations.</summary>
    public IReadOnlyList<SpanAdditionalBottomBarConfig> SpanBottomBars { get; init; } = Array.Empty<SpanAdditionalBottomBarConfig>();
}

/// <summary>
/// Configuration for additional top bars centered over a specific support node.
/// </summary>
public sealed record SupportAdditionalTopBarConfig
{
    /// <summary>Index of the support node where bars are centered (0 to N).</summary>
    public int SupportIndex { get; init; }

    // --- Layer 1 ---
    /// <summary>Bar count in Layer 1 (placed at same elevation as top main bars).</summary>
    public int Layer1Count { get; init; }

    /// <summary>Bar diameter in Layer 1 (mm).</summary>
    public double Layer1Diameter { get; init; }

    /// <summary>Extension ratio into adjacent clear spans for Layer 1 (default: 1/3 = L/3).</summary>
    public double Layer1ExtensionRatio { get; init; } = 1.0 / 3.0;

    // --- Layer 2 ---
    /// <summary>Bar count in Layer 2 (placed underneath Layer 1; 0 if single layer).</summary>
    public int Layer2Count { get; init; }

    /// <summary>Bar diameter in Layer 2 (mm).</summary>
    public double Layer2Diameter { get; init; }

    /// <summary>Extension ratio for Layer 2 (default: 1/4 = L/4, cut shorter than Layer 1).</summary>
    public double Layer2ExtensionRatio { get; init; } = 1.0 / 4.0;

    /// <summary>Vertical gap DeltaZ between Layer 1 and Layer 2 (mm, default: 50 mm).</summary>
    public double LayerGap { get; init; } = 50.0;

    // --- Exterior Anchorage ---
    /// <summary>Exterior anchorage hook type if this is an exterior support (Support 0 or N).</summary>
    public EndAnchorageType ExteriorEndAnchorage { get; init; } = EndAnchorageType.Hook90Down;

    /// <summary>Exterior hook length (mm, 0 for auto).</summary>
    public double ExteriorHookLength { get; init; }

    /// <summary>Revit RebarBarType name.</summary>
    public string BarTypeName { get; init; } = string.Empty;
}

/// <summary>
/// Configuration for additional bottom bars placed in the midspan region of a span.
/// </summary>
public sealed record SpanAdditionalBottomBarConfig
{
    /// <summary>Index of the span where bars are placed (0 to N-1).</summary>
    public int SpanIndex { get; init; }

    // --- Layer 1 ---
    /// <summary>Bar count in Layer 1 (placed in line with main bottom bars).</summary>
    public int Layer1Count { get; init; }

    /// <summary>Bar diameter in Layer 1 (mm).</summary>
    public double Layer1Diameter { get; init; }

    /// <summary>Cutoff distance ratio from support inner face (default: 1/7 = L/7).</summary>
    public double CutoffRatio { get; init; } = 1.0 / 7.0;

    // --- Layer 2 ---
    /// <summary>Bar count in Layer 2 (placed above Layer 1; 0 if single layer).</summary>
    public int Layer2Count { get; init; }

    /// <summary>Bar diameter in Layer 2 (mm).</summary>
    public double Layer2Diameter { get; init; }

    /// <summary>Vertical gap DeltaZ between Layer 1 and Layer 2 (mm, default: 50 mm).</summary>
    public double LayerGap { get; init; } = 50.0;

    /// <summary>Revit RebarBarType name.</summary>
    public string BarTypeName { get; init; } = string.Empty;
}

/// <summary>
/// Dedicated specification container for support top additional bars.
/// </summary>
public sealed record BeamAdditionalTopBarSpec
{
    public IReadOnlyList<SupportAdditionalTopBarConfig> SupportTopBars { get; init; } = Array.Empty<SupportAdditionalTopBarConfig>();
    public double CutoffRatioLayer1 { get; init; } = 1.0 / 3.0;
    public double CutoffRatioLayer2 { get; init; } = 1.0 / 4.0;
    public double LayerGap { get; init; } = 50.0;
}

/// <summary>
/// Dedicated specification container for midspan bottom additional bars.
/// </summary>
public sealed record BeamAdditionalBottomBarSpec
{
    public IReadOnlyList<SpanAdditionalBottomBarConfig> SpanBottomBars { get; init; } = Array.Empty<SpanAdditionalBottomBarConfig>();
    public double CutoffRatio { get; init; } = 1.0 / 7.0;
    public double LayerGap { get; init; } = 50.0;
}
