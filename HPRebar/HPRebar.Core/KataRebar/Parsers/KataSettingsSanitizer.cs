using System;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Parsers;

/// <summary>
/// Every value out of range (negative, NaN, infinite, a fraction past half the span, a notation that does not read)
/// replaced by the default, as the dialog would refuse it. Applied to a hand-edited file, to what the dialog saves and
/// to what the rules use.
/// </summary>
public static class KataSettingsSanitizer
{
    public static KataSettingsFile File(KataSettingsFile f)
    {
        if (f is null) throw new ArgumentNullException(nameof(f));
        return new KataSettingsFile(Drawing(f.Drawing), Pending(f.Pending), Shop(f.Shop));
    }

    public static KataSettings Drawing(KataSettings s)
    {
        if (s is null) throw new ArgumentNullException(nameof(s));
        var d = KataSettings.Default;
        static bool HalfSpan(double v) => v >= 0.0 && v <= 0.5;
        static bool Angle(int a) => a is 90 or 135 or 180;
        return s with
        {
            ClosedStirrupHookAngle = Angle(s.ClosedStirrupHookAngle) ? s.ClosedStirrupHookAngle : d.ClosedStirrupHookAngle,
            ClosedStirrupHookFactor = Positive(s.ClosedStirrupHookFactor) ? s.ClosedStirrupHookFactor : d.ClosedStirrupHookFactor,
            CrossTieHookAngle = Angle(s.CrossTieHookAngle) ? s.CrossTieHookAngle : d.CrossTieHookAngle,
            CrossTieHookFactor = Positive(s.CrossTieHookFactor) ? s.CrossTieHookFactor : d.CrossTieHookFactor,
            RoundCutExtraMm = NonNegative(s.RoundCutExtraMm) ? s.RoundCutExtraMm : d.RoundCutExtraMm,
            SideBarAnchorageFactor = Positive(s.SideBarAnchorageFactor) ? s.SideBarAnchorageFactor : d.SideBarAnchorageFactor,
            LayerTieMinBarCount = s.LayerTieMinBarCount >= 2 ? s.LayerTieMinBarCount : d.LayerTieMinBarCount,
            CrankMinDiameter = NonNegative(s.CrankMinDiameter) ? s.CrankMinDiameter : d.CrankMinDiameter,
            CrankSlope = Positive(s.CrankSlope) ? s.CrankSlope : d.CrankSlope,
            JointBeam = Joint(s.JointBeam),
            JointColumn = Joint(s.JointColumn),
            CurtailedExtensionMm = NonNegative(s.CurtailedExtensionMm) ? s.CurtailedExtensionMm : d.CurtailedExtensionMm,
            DenseZoneHeightFactor = NonNegative(s.DenseZoneHeightFactor) ? s.DenseZoneHeightFactor : d.DenseZoneHeightFactor,
            EndZoneFraction = HalfSpan(s.EndZoneFraction) ? s.EndZoneFraction : d.EndZoneFraction,
            BottomExtraCutFraction = HalfSpan(s.BottomExtraCutFraction) && s.BottomExtraCutFraction < 0.5 ? s.BottomExtraCutFraction : d.BottomExtraCutFraction,
            MinimumLegFactor = NonNegative(s.MinimumLegFactor) ? s.MinimumLegFactor : d.MinimumLegFactor,
            LayerClearGap = NonNegative(s.LayerClearGap) ? s.LayerClearGap : d.LayerClearGap,
            RoundLegMm = NonNegative(s.RoundLegMm) ? s.RoundLegMm : d.RoundLegMm,
            SideBarRequiredHeight = NonNegative(s.SideBarRequiredHeight) ? s.SideBarRequiredHeight : d.SideBarRequiredHeight
        };
    }

    public static KataJointRebarSettings Joint(KataJointRebarSettings? j)
    {
        var d = KataJointRebarSettings.Default;
        if (j is null) return d;
        return j with
        {
            Stirrups = KataJointNotation.TryParseStirrups(j.Stirrups, out _, out _, out _) ? j.Stirrups.Trim() : d.Stirrups,
            StirrupCountSpec = string.IsNullOrWhiteSpace(j.StirrupCountSpec) ? d.StirrupCountSpec : j.StirrupCountSpec.Trim(),
            Hanger = KataJointNotation.TryParseBars(j.Hanger, out _, out _) ? j.Hanger.Trim() : d.Hanger,
            HangerTopLengthMm = NonNegative(j.HangerTopLengthMm) ? j.HangerTopLengthMm : d.HangerTopLengthMm,
            HangerAngleDegrees = j.HangerAngleDegrees is 45 or 60 ? j.HangerAngleDegrees : d.HangerAngleDegrees
        };
    }

    public static KataBeamOptions Pending(KataBeamOptions? p)
    {
        var d = KataBeamOptions.Default;
        if (p is null) return d;
        return p with { AlwaysBendFactor = NonNegative(p.AlwaysBendFactor) ? p.AlwaysBendFactor : d.AlwaysBendFactor };
    }

    public static KataShopSettings Shop(KataShopSettings? s)
    {
        var d = KataShopSettings.Default;
        if (s is null) return d;
        static double Keep(double v, double fallback) => NonNegative(v) ? v : fallback;
        static double Fraction(double v, double fallback) => v >= 0.0 && v <= 0.5 ? v : fallback;
        return s with
        {
            CouplerMinDiameter = Keep(s.CouplerMinDiameter, d.CouplerMinDiameter),
            MaxBarLengthMm = Positive(s.MaxBarLengthMm) ? s.MaxBarLengthMm : d.MaxBarLengthMm,
            MinLengthForCuttingMm = Keep(s.MinLengthForCuttingMm, d.MinLengthForCuttingMm),
            MinBarLengthFactor = Keep(s.MinBarLengthFactor, d.MinBarLengthFactor),
            RoundLapMm = Keep(s.RoundLapMm, d.RoundLapMm),
            TopLapZoneFraction = Fraction(s.TopLapZoneFraction, d.TopLapZoneFraction),
            BottomLapZoneFraction = Fraction(s.BottomLapZoneFraction, d.BottomLapZoneFraction),
            CompressionLapFactor = Keep(s.CompressionLapFactor, d.CompressionLapFactor),
            TensionLapFactor = Keep(s.TensionLapFactor, d.TensionLapFactor),
            CrankToLapEdgeMm = Keep(s.CrankToLapEdgeMm, d.CrankToLapEdgeMm),
            MinLapGapMm = Keep(s.MinLapGapMm, d.MinLapGapMm),
            CuttingToleranceMm = Keep(s.CuttingToleranceMm, d.CuttingToleranceMm),
            UnitMassTable = KataShopTables.NormalizeMass(s.UnitMassTable),
            LapTable = KataShopTables.NormalizeLaps(s.LapTable)
        };
    }

    private static bool Positive(double v) => v > 0.0 && !double.IsInfinity(v);

    private static bool NonNegative(double v) => v >= 0.0 && !double.IsInfinity(v);
}
