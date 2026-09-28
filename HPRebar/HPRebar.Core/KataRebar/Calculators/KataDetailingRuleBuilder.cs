using System;
using System.Collections.Generic;
using HPRebar.Core.KataRebar.Models;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// Turns cell J9 and the anchorage cells into placement rules.
/// J9 "a/b": a is the distance from the concrete face to the centre of the main bars, b the clear cover of
/// the stirrups. J9 "a" alone: the stirrup wraps the main bars, so b = a − d/2 − d_stirrup. J9 empty: b is
/// 25 mm and the main bars sit against the stirrup. A main bar can never sit closer to the face than the
/// stirrup allows; such an "a" is raised and reported.
/// </summary>
public static class KataDetailingRuleBuilder
{
    public const double DefaultStirrupCover = 25.0;
    public const double DefaultStirrupDiameter = 10.0;
    public const double ThinStirrupCover = 15.0;
    public const double DefaultTopAnchorageFactor = 40.0;
    public const double DefaultBottomAnchorageFactor = 30.0;

    public static KataDetailingRules Build(KataBeamRebarSpec spec)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));

        var warnings = new List<string>();
        var errors = new List<string>();

        double ds = spec.GlobalStirrup.Diameter > 0.0 ? spec.GlobalStirrup.Diameter : DefaultStirrupDiameter;
        if (spec.GlobalStirrup.Diameter <= 0.0)
            warnings.Add($"G6 không có đường kính đai: không vẽ đai; thép chủ vẫn chừa chỗ cho đai Ø{ds:0}.");
        double dTop = spec.TopContinuous.IsEmpty ? 0.0 : spec.TopContinuous.Diameter;
        double dBot = spec.BottomContinuous.IsEmpty ? 0.0 : spec.BottomContinuous.Diameter;
        double a = spec.CoverMain;
        double b = spec.CoverStirrup;

        if (b <= 0.0 && a > 0.0)
        {
            double wrapped = a - Math.Max(dTop, dBot) / 2.0 - ds;
            b = wrapped;
            if (wrapped <= 0.0)
                errors.Add($"J9 = {a:0}: không còn chỗ cho đai Ø{ds:0} và thép chủ Ø{Math.Max(dTop, dBot):0} (lớp bảo vệ đai {wrapped:0.#} mm).");
            else if (wrapped < ThinStirrupCover)
                warnings.Add($"J9 = {a:0} cho lớp bảo vệ đai {wrapped:0.#} mm (< {ThinStirrupCover:0} mm).");
        }
        else if (b <= 0.0)
        {
            b = DefaultStirrupCover;
        }

        double topDepth = BarCentreDepth(a, b, ds, dTop, "trên", warnings);
        double botDepth = BarCentreDepth(a, b, ds, dBot, "dưới", warnings);

        double depth = spec.Height;
        if (depth > 0.0 && topDepth + botDepth >= depth)
            errors.Add($"Dầm cao {depth:0} mm không đủ chỗ cho hai lớp thép chủ cách mép {topDepth:0} và {botDepth:0} mm.");

        return new KataDetailingRules
        {
            TopBarCentreDepth = topDepth,
            BottomBarCentreDepth = botDepth,
            StirrupCover = b,
            StirrupDiameter = ds,
            TopAnchorageFactor = spec.TensionLapMultiplier > 0.0 ? spec.TensionLapMultiplier : DefaultTopAnchorageFactor,
            BottomAnchorageFactor = spec.CompressionLapMultiplier > 0.0 ? spec.CompressionLapMultiplier : DefaultBottomAnchorageFactor,
            Warnings = warnings,
            Errors = errors
        };
    }

    /// <summary>
    /// Centre depth of a main bar layer: "a" when J9 gives one and the stirrup leaves room for it, otherwise
    /// the bar rests on the stirrup's inner face.
    /// </summary>
    private static double BarCentreDepth(double a, double b, double ds, double d, string layer, List<string> warnings)
    {
        double resting = b + ds + d / 2.0;
        if (a <= 0.0) return resting;

        if (a + 1e-6 < resting)
        {
            warnings.Add($"J9: tâm thép chủ {layer} cách mép {a:0} mm sẽ cắt vào đai; dùng {resting:0.#} mm (đai {b:0.#} + Ø{ds:0} + Ø{d:0}/2).");
            return resting;
        }

        return a;
    }
}
