using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

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

    /// <summary>Least distance of an end-column bar from the column's outer face (Kata B01-B03: 50 with a = 30 or 50).</summary>
    public const double MinColumnEndCover = 50.0;

    public static KataDetailingRules Build(KataBeamRebarSpec spec, KataSettings? settings = null)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        settings = KataSettingsJson.Sanitize(settings ?? KataSettings.Default);

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
            // A single number is the main bars' centre: the stirrup wraps them, its outer face that much further out.
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

        // Across the section the main bars always rest on the stirrup, as Kata draws them; a is where they stop.
        double topDepth = b + ds + dTop / 2.0;
        double botDepth = b + ds + dBot / 2.0;

        foreach (var span in spec.Spans.Where(s => spec.Height > 0.0 && s.Depth <= 0.0 && s.SoffitDrop != 0.0))
            errors.Add($"Hàng 21 nhịp {span.SpanIndex + 1} '{span.SoffitDrop:0}': B5 − bậc đáy ≤ 0 — không còn chiều cao dầm.");

        // The shallowest span decides whether both main layers fit.
        double depth = spec.Spans.Count > 0 ? Enumerable.Range(0, spec.Spans.Count).Min(spec.DepthOf) : spec.Height;
        if (depth > 0.0 && topDepth + botDepth >= depth)
            errors.Add($"Dầm cao {depth:0} mm không đủ chỗ cho hai lớp thép chủ cách mép {topDepth:0} và {botDepth:0} mm.");

        double stagger = spec.CurtailedExtension > 0.0 ? spec.CurtailedExtension : settings.CurtailedExtensionMm;
        if (spec.CurtailedExtension <= 0.0 && !string.IsNullOrWhiteSpace(spec.CurtailedExtensionText))
            warnings.Add($"G1 '{spec.CurtailedExtensionText.Trim()}' không phải một số dương: cắt lệch thép gia cường gối dùng thiết lập {stagger:0} mm.");

        // A J7 outside the range a C tie is ever spaced at is a typo ("a5" would put thousands of ties in a beam).
        double? j7 = spec.GlobalStirrup.TieSpacing;
        bool sensible = j7 is >= KataDetailingRules.MinTieSpacing and <= KataDetailingRules.MaxTieSpacing;
        double tieSpacing = sensible ? j7!.Value : KataDetailingRules.DefaultTieSpacing;
        string text = spec.GlobalStirrup.TieSpacingText.Trim();
        string? tieNote = sensible || spec.GlobalStirrup.TieSpacingMode == KataTieSpacingMode.LikeHoops
            ? null
            : j7 is not null
                ? $"J7 '{(text.Length > 0 ? text : $"a{j7:0}")}' ngoài {KataDetailingRules.MinTieSpacing:0}–{KataDetailingRules.MaxTieSpacing:0} mm: móc C rải a{tieSpacing:0}."
                : text.Length == 0
                    ? $"J7 trống: móc C rải a{tieSpacing:0}."
                    : $"J7 '{text}' không phải một bước (ví dụ a500): móc C rải a{tieSpacing:0}.";

        return new KataDetailingRules
        {
            TopBarCentreDepth = topDepth,
            BottomBarCentreDepth = botDepth,
            TopEndCover = a > 0.0 ? a : topDepth,
            BottomEndCover = a > 0.0 ? a : botDepth,
            ColumnEndCover = Math.Max(MinColumnEndCover, a > 0.0 ? a : topDepth),
            StirrupCover = b,
            StirrupDiameter = ds,
            TopAnchorageFactor = spec.TensionLapMultiplier > 0.0 ? spec.TensionLapMultiplier : DefaultTopAnchorageFactor,
            BottomAnchorageFactor = spec.CompressionLapMultiplier > 0.0 ? spec.CompressionLapMultiplier : DefaultBottomAnchorageFactor,
            // The settings are sanitised above: every value is in range.
            SideBarAnchorageFactor = settings.SideBarAnchorageFactor,
            RoundCutExtraMm = settings.RoundCutExtraMm,
            TieSpacing = tieSpacing,
            TieSpacingMode = spec.GlobalStirrup.TieSpacingMode,
            TieSpacingNote = tieNote,
            LayerTieMinBarCount = settings.LayerTieMinBarCount,
            CrankMinDiameter = settings.CrankMinDiameter,
            CurtailedExtension = stagger,
            DenseZoneHeightFactor = settings.DenseZoneHeightFactor,
            EndZoneFraction = settings.EndZoneFraction,
            BottomExtraCutFraction = settings.BottomExtraCutFraction,
            MinimumLegFactor = settings.MinimumLegFactor,
            LayerClearGap = settings.LayerClearGap,
            RoundLegMm = settings.RoundLegMm,
            SideBarRequiredHeight = settings.SideBarRequiredHeight,
            ClosedStirrupHookAngle = settings.ClosedStirrupHookAngle,
            ClosedStirrupHookFactor = settings.ClosedStirrupHookFactor,
            CrossTieHookAngle = settings.CrossTieHookAngle,
            CrossTieHookFactor = settings.CrossTieHookFactor,
            Warnings = warnings,
            Errors = errors
        };
    }

    /// <summary>
    /// Centre depth of a main bar layer: "a" when J9 gives one and the stirrup leaves room for it, otherwise
    /// the bar rests on the stirrup's inner face.
    /// </summary>
}
