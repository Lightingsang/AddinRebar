using System.Collections.Generic;

namespace HPRebar.Core.KataExport.Settings;

// The Kata settings as the settings page edits them: one record per tab, laid out as Kata's "Cài đặt thông số Kata".
// Numbers are nullable because an emptied input reaches the host as null and must come back as a field error, not as a
// JSON failure; KataSettingsRules says what is wrong and KataSettingsFormMapper turns a valid form into the file.

/// <summary>The three tabs of the settings page.</summary>
public sealed record KataSettingsForm(KataDetailForm Detail, KataSpecialForm Special, KataJointsForm Joints);

/// <summary>Tab "Detail thép": what the bars are drawn with, the beam options kept for later and the shop settings.</summary>
public sealed record KataDetailForm
{
    // Drawn with.
    public int? ClosedStirrupHookAngle { get; init; }
    public double? ClosedStirrupHookFactor { get; init; }
    public int? CrossTieHookAngle { get; init; }
    public double? CrossTieHookFactor { get; init; }
    public double? RoundCutExtraMm { get; init; }
    public double? SideBarAnchorageFactor { get; init; }
    public int? LayerTieMinBarCount { get; init; }
    public double? CrankMinDiameter { get; init; }
    public double? CrankSlope { get; init; }
    public double? CurtailedExtensionMm { get; init; }
    public double? DenseZoneHeightFactor { get; init; }
    public double? EndZoneFraction { get; init; }
    public double? BottomExtraCutFraction { get; init; }
    public double? MinimumLegFactor { get; init; }
    public double? LayerClearGap { get; init; }
    public double? RoundLegMm { get; init; }
    public double? SideBarRequiredHeight { get; init; }
    public bool ShowBarEndMarks { get; init; }
    public bool CreateKataDrawings { get; init; }

    // Beam options kept for later ("chưa áp dụng").
    public bool BottomLayerNoBend { get; init; }
    public bool TopNotAnchoredIntoColumnBelow { get; init; }
    public bool AlwaysBend { get; init; }
    public double? AlwaysBendFactor { get; init; }
    public bool CutContinuousBarsAtSpanEnds { get; init; }
    public bool DifferentNumbersForIdenticalBars { get; init; }

    // Shop drawings.
    public double? CouplerMinDiameter { get; init; }
    public double? MaxBarLengthMm { get; init; }
    public double? MinLengthForCuttingMm { get; init; }
    public double? MinBarLengthFactor { get; init; }
    public double? RoundLapMm { get; init; }
    public bool TopLapAtMidspan { get; init; }
    public bool TopLapAtSupport { get; init; }
    public bool BottomLapAtSupport { get; init; }
    public bool BottomLapAtMidspan { get; init; }
    public double? TopLapZoneFraction { get; init; }

    /// <summary>The top lap zone is measured from the support centre ("tâm"), else from its face ("mép").</summary>
    public bool TopLapZoneFromCentre { get; init; }

    public double? BottomLapZoneFraction { get; init; }
    public bool BottomLapZoneFromCentre { get; init; }
    public bool PreferFewerLaps { get; init; }
}

/// <summary>Tab "Thông số đặc thù": laps and the two tables, all for the shop drawings.</summary>
public sealed record KataSpecialForm
{
    public bool UseUnitMassTable { get; init; }
    public double? CompressionLapFactor { get; init; }
    public bool AllowTensionZoneLap { get; init; }
    public double? TensionLapFactor { get; init; }
    public double? CrankToLapEdgeMm { get; init; }
    public double? MinLapGapMm { get; init; }
    public double? CuttingToleranceMm { get; init; }
    public bool UseLapTable { get; init; }
    public IReadOnlyList<KataMassRowForm> MassRows { get; init; } = new KataMassRowForm[0];
    public IReadOnlyList<KataLapRowForm> LapRows { get; init; } = new KataLapRowForm[0];
}

/// <summary>One row of the unit mass table: bar diameter, kg per metre. A row left empty is ignored.</summary>
public sealed record KataMassRowForm(double? Diameter, double? KgPerM);

/// <summary>One row of the lap / anchorage table, mm. A row left empty is ignored.</summary>
public sealed record KataLapRowForm(double? Diameter, double? LapCompression, double? LapTension, double? AnchorCompression, double? AnchorTension);

/// <summary>Tab "Thép mặc định": the joint stirrups and hanger bars round a crossing beam and round a stub column.</summary>
public sealed record KataJointsForm(KataJointForm Beam, KataJointForm Column);

/// <summary>One column of "Thép mặc định".</summary>
public sealed record KataJointForm
{
    public string Stirrups { get; init; } = "";
    public string StirrupCountSpec { get; init; } = "";
    public bool StirrupsThroughJoint { get; init; }
    public bool HangerEnabled { get; init; }
    public string Hanger { get; init; } = "";
    public double? HangerTopLengthMm { get; init; }
    public int? HangerAngleDegrees { get; init; }
}

/// <summary>A wrong value: <paramref name="Field" /> is the camelCase path the page edits ("detail.endZoneFraction", "special.lapRows[2].diameter").</summary>
public sealed record KataSettingsError(string Field, string Message);

/// <summary>What the settings page opens with: the form, the lists its selects offer, and why a saved value is not shown.</summary>
public sealed record KataSettingsSnapshot(KataSettingsForm Form, KataSettingsFormChoices Choices, string? Notice);
