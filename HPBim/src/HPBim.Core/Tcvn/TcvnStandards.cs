namespace HPBim.Core.Tcvn;

/// <summary>
/// Standard dimensions, grades, and detailing constants per TCVN 5574:2018.
/// </summary>
public static class TcvnStandards
{
    // Standard wall thicknesses (mm)
    public const double WallExteriorThicknessMm = 220.0;
    public const double WallInteriorThicknessMm = 110.0;

    // Standard column sections (mm)
    public const double ColumnTypicalWidthMm = 220.0;
    public const double ColumnTypicalDepthMm = 220.0;
    public const double ColumnMidWidthMm = 220.0;
    public const double ColumnMidDepthMm = 300.0;

    // Standard beam sections (mm)
    public const double BeamMainWidthMm = 200.0;
    public const double BeamMainHeightMm = 400.0;
    public const double BeamSubWidthMm = 200.0;
    public const double BeamSubHeightMm = 300.0;
    public const double TieBeamWidthMm = 250.0;
    public const double TieBeamHeightMm = 400.0;

    // Standard slab thicknesses (mm)
    public const double SlabThicknessMm = 100.0;
    public const double StairSlabThicknessMm = 120.0;

    // Standard pile size (mm)
    public const double PileSizeMm = 250.0;

    // Detailing multipliers per TCVN 5574:2018
    public const double AnchorageMultiplier = 30.0;           // 30D
    public const double Hook90Multiplier = 12.0;              // 12D
    public const double LapTensionMultiplier = 40.0;          // 40D
    public const double LapCompressionMultiplier = 30.0;      // 30D
    public const double MaxSplicePercentageAtSection = 50.0;  // 50% staggered
    public const double StirrupHookMultiplier = 10.0;         // 10D
    public const double StirrupHookMinExtensionMm = 75.0;     // >= 75mm
    public const double CommercialBarMaxLengthMm = 11700.0;   // 11.7m

    // Benchmark rebar ratios
    public const double ResidentialSuperstructureMinKgPerM2 = 20.0;
    public const double ResidentialSuperstructureMaxKgPerM2 = 45.0;
    public const double FoundationMinKgPerM3 = 50.0;
    public const double FoundationMaxKgPerM3 = 100.0;
}

public enum ConcreteGrade
{
    B15, // M200
    B20, // M250 (standard residential beams/columns)
    B25, // M350
    B30  // M400
}

public enum SteelGrade
{
    Cb240T, // Plain round bars (D <= 10mm, stirrups)
    Cb300V, // Ribbed longitudinal bars (D >= 10mm)
    Cb400V  // High strength ribbed bars
}

public readonly record struct ConcreteProperties(
    ConcreteGrade Grade,
    string RussianMark,
    double DesignCompressiveStrengthRbMpa,
    double DesignTensileStrengthRbtMpa,
    double ElasticModulusEbGpa);

public readonly record struct SteelProperties(
    SteelGrade Grade,
    double DesignYieldStrengthRsMpa,
    double DesignStirrupStrengthRswMpa,
    bool IsRibbed);
