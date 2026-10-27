using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataRebar.Models;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.KataExport.Settings;

/// <summary>
/// Settings file ↔ settings page. Reading shows the default for a saved choice outside its list and says so (the bars
/// keep the saved value until the user accepts); writing starts from the defaults, as Kata's dialog does, and keeps only
/// the table rows the user filled.
/// </summary>
public static class KataSettingsFormMapper
{
    public static KataSettingsSnapshot Snapshot(KataSettingsFile file)
    {
        var replaced = new List<string>();
        var form = new KataSettingsForm(Detail(file, replaced), Special(file.Shop), Joints(file.Drawing, replaced));
        string? notice = replaced.Count == 0
            ? null
            : $"File thiết lập có {string.Join(", ", replaced)} ngoài danh sách: cửa sổ hiện mặc định; thép vẫn vẽ theo giá trị cũ tới khi bấm Chấp nhận.";
        return new KataSettingsSnapshot(form, KataSettingsFormChoices.Kata, notice);
    }

    /// <summary>The file a valid form stands for.</summary>
    /// <exception cref="ArgumentException">The form holds a value <see cref="KataSettingsRules.Validate" /> refuses.</exception>
    public static KataSettingsFile ToFile(KataSettingsForm form)
    {
        if (form is null) throw new ArgumentNullException(nameof(form));
        var errors = KataSettingsRules.Validate(form);
        if (errors.Count > 0) throw new ArgumentException($"{errors[0].Field}: {errors[0].Message}", nameof(form));

        var d = form.Detail;
        var drawing = KataSettings.Default with
        {
            ClosedStirrupHookAngle = d.ClosedStirrupHookAngle!.Value,
            ClosedStirrupHookFactor = d.ClosedStirrupHookFactor!.Value,
            CrossTieHookAngle = d.CrossTieHookAngle!.Value,
            CrossTieHookFactor = d.CrossTieHookFactor!.Value,
            RoundCutExtraMm = d.RoundCutExtraMm!.Value,
            SideBarAnchorageFactor = d.SideBarAnchorageFactor!.Value,
            LayerTieMinBarCount = d.LayerTieMinBarCount!.Value,
            CrankMinDiameter = d.CrankMinDiameter!.Value,
            CrankSlope = d.CrankSlope!.Value,
            CurtailedExtensionMm = d.CurtailedExtensionMm!.Value,
            DenseZoneHeightFactor = d.DenseZoneHeightFactor!.Value,
            EndZoneFraction = d.EndZoneFraction!.Value,
            BottomExtraCutFraction = d.BottomExtraCutFraction!.Value,
            MinimumLegFactor = d.MinimumLegFactor!.Value,
            LayerClearGap = d.LayerClearGap!.Value,
            RoundLegMm = d.RoundLegMm!.Value,
            SideBarRequiredHeight = d.SideBarRequiredHeight!.Value,
            ShowBarEndMarks = d.ShowBarEndMarks,
            CreateKataDrawings = d.CreateKataDrawings,
            JointBeam = Joint(form.Joints.Beam),
            JointColumn = Joint(form.Joints.Column),
        };

        var pending = new KataBeamOptions
        {
            BottomLayerNoBend = d.BottomLayerNoBend,
            TopNotAnchoredIntoColumnBelow = d.TopNotAnchoredIntoColumnBelow,
            AlwaysBend = d.AlwaysBend,
            AlwaysBendFactor = d.AlwaysBendFactor!.Value,
            CutContinuousBarsAtSpanEnds = d.CutContinuousBarsAtSpanEnds,
            DifferentNumbersForIdenticalBars = d.DifferentNumbersForIdenticalBars,
        };

        var s = form.Special;
        var shop = KataShopSettings.Default with
        {
            CouplerMinDiameter = d.CouplerMinDiameter!.Value,
            MaxBarLengthMm = d.MaxBarLengthMm!.Value,
            MinLengthForCuttingMm = d.MinLengthForCuttingMm!.Value,
            MinBarLengthFactor = d.MinBarLengthFactor!.Value,
            RoundLapMm = d.RoundLapMm!.Value,
            TopLapAtMidspan = d.TopLapAtMidspan,
            TopLapAtSupport = d.TopLapAtSupport,
            BottomLapAtSupport = d.BottomLapAtSupport,
            BottomLapAtMidspan = d.BottomLapAtMidspan,
            TopLapZoneFraction = d.TopLapZoneFraction!.Value,
            TopLapZoneFromCentre = d.TopLapZoneFromCentre,
            BottomLapZoneFraction = d.BottomLapZoneFraction!.Value,
            BottomLapZoneFromCentre = d.BottomLapZoneFromCentre,
            PreferFewerLaps = d.PreferFewerLaps,
            UseUnitMassTable = s.UseUnitMassTable,
            UnitMassTable = KataShopTables.WriteMass(s.MassRows.Where(r => !KataSettingsRules.IsBlank(r))
                .Select(r => (r.Diameter!.Value, r.KgPerM!.Value))),
            CompressionLapFactor = s.CompressionLapFactor!.Value,
            AllowTensionZoneLap = s.AllowTensionZoneLap,
            TensionLapFactor = s.TensionLapFactor!.Value,
            CrankToLapEdgeMm = s.CrankToLapEdgeMm!.Value,
            MinLapGapMm = s.MinLapGapMm!.Value,
            CuttingToleranceMm = s.CuttingToleranceMm!.Value,
            UseLapTable = s.UseLapTable,
            LapTable = KataShopTables.WriteLaps(s.LapRows.Where(r => !KataSettingsRules.IsBlank(r))
                .Select(r => new KataLapRow(r.Diameter!.Value, r.LapCompression!.Value, r.LapTension!.Value,
                    r.AnchorCompression!.Value, r.AnchorTension!.Value))),
        };

        return new KataSettingsFile(drawing, pending, shop);
    }

    private static KataDetailForm Detail(KataSettingsFile file, List<string> replaced)
    {
        var s = file.Drawing;
        var d = KataSettings.Default;
        var p = file.Pending;
        var shop = file.Shop;
        var choices = KataSettingsFormChoices.Kata;
        return new KataDetailForm
        {
            ClosedStirrupHookAngle = Pick(choices.HookAngles, s.ClosedStirrupHookAngle, d.ClosedStirrupHookAngle, "góc móc đai kín {0}°", replaced),
            ClosedStirrupHookFactor = s.ClosedStirrupHookFactor,
            CrossTieHookAngle = Pick(choices.HookAngles, s.CrossTieHookAngle, d.CrossTieHookAngle, "góc móc đai móc {0}°", replaced),
            CrossTieHookFactor = s.CrossTieHookFactor,
            RoundCutExtraMm = s.RoundCutExtraMm,
            SideBarAnchorageFactor = s.SideBarAnchorageFactor,
            LayerTieMinBarCount = s.LayerTieMinBarCount,
            CrankMinDiameter = Pick(choices.CrankMinDiameters, s.CrankMinDiameter, d.CrankMinDiameter, "Ø bẻ cổ chai {0}", replaced),
            CrankSlope = Pick(choices.CrankSlopes, s.CrankSlope, d.CrankSlope, "tỷ lệ cổ chai 1/{0}", replaced),
            CurtailedExtensionMm = s.CurtailedExtensionMm,
            DenseZoneHeightFactor = s.DenseZoneHeightFactor,
            EndZoneFraction = s.EndZoneFraction,
            BottomExtraCutFraction = s.BottomExtraCutFraction,
            MinimumLegFactor = s.MinimumLegFactor,
            LayerClearGap = s.LayerClearGap,
            RoundLegMm = s.RoundLegMm,
            SideBarRequiredHeight = s.SideBarRequiredHeight,
            ShowBarEndMarks = s.ShowBarEndMarks,
            CreateKataDrawings = s.CreateKataDrawings,

            BottomLayerNoBend = p.BottomLayerNoBend,
            TopNotAnchoredIntoColumnBelow = p.TopNotAnchoredIntoColumnBelow,
            AlwaysBend = p.AlwaysBend,
            AlwaysBendFactor = p.AlwaysBendFactor,
            CutContinuousBarsAtSpanEnds = p.CutContinuousBarsAtSpanEnds,
            DifferentNumbersForIdenticalBars = p.DifferentNumbersForIdenticalBars,

            CouplerMinDiameter = Pick(choices.CouplerMinDiameters, shop.CouplerMinDiameter, KataShopSettings.Default.CouplerMinDiameter,
                "Ø coupler {0}", replaced),
            MaxBarLengthMm = shop.MaxBarLengthMm,
            MinLengthForCuttingMm = shop.MinLengthForCuttingMm,
            MinBarLengthFactor = shop.MinBarLengthFactor,
            RoundLapMm = shop.RoundLapMm,
            TopLapAtMidspan = shop.TopLapAtMidspan,
            TopLapAtSupport = shop.TopLapAtSupport,
            BottomLapAtSupport = shop.BottomLapAtSupport,
            BottomLapAtMidspan = shop.BottomLapAtMidspan,
            TopLapZoneFraction = shop.TopLapZoneFraction,
            TopLapZoneFromCentre = shop.TopLapZoneFromCentre,
            BottomLapZoneFraction = shop.BottomLapZoneFraction,
            BottomLapZoneFromCentre = shop.BottomLapZoneFromCentre,
            PreferFewerLaps = shop.PreferFewerLaps,
        };
    }

    private static KataSpecialForm Special(KataShopSettings shop) => new()
    {
        UseUnitMassTable = shop.UseUnitMassTable,
        CompressionLapFactor = shop.CompressionLapFactor,
        AllowTensionZoneLap = shop.AllowTensionZoneLap,
        TensionLapFactor = shop.TensionLapFactor,
        CrankToLapEdgeMm = shop.CrankToLapEdgeMm,
        MinLapGapMm = shop.MinLapGapMm,
        CuttingToleranceMm = shop.CuttingToleranceMm,
        UseLapTable = shop.UseLapTable,
        MassRows = KataShopTables.ReadMass(shop.UnitMassTable).Select(r => new KataMassRowForm(r.Diameter, r.KgPerM)).ToList(),
        LapRows = KataShopTables.ReadLaps(shop.LapTable)
            .Select(r => new KataLapRowForm(r.Diameter, r.LapCompression, r.LapTension, r.AnchorCompression, r.AnchorTension)).ToList(),
    };

    private static KataJointsForm Joints(KataSettings drawing, List<string> replaced) =>
        new(Joint(drawing.JointBeam, "dầm giao", replaced), Joint(drawing.JointColumn, "cột cấy", replaced));

    private static KataJointForm Joint(KataJointRebarSettings s, string title, List<string> replaced) => new()
    {
        Stirrups = s.Stirrups,
        StirrupCountSpec = s.StirrupCountSpec,
        StirrupsThroughJoint = s.StirrupsThroughJoint,
        HangerEnabled = s.HangerEnabled,
        Hanger = s.Hanger,
        HangerTopLengthMm = s.HangerTopLengthMm,
        HangerAngleDegrees = Pick(KataSettingsFormChoices.Kata.HangerAngles, s.HangerAngleDegrees,
            KataJointRebarSettings.Default.HangerAngleDegrees, "góc vai bò " + title + " {0}°", replaced),
    };

    private static KataJointRebarSettings Joint(KataJointForm form) => new()
    {
        Stirrups = form.Stirrups.Trim(),
        StirrupCountSpec = string.IsNullOrWhiteSpace(form.StirrupCountSpec)
            ? KataJointRebarSettings.Default.StirrupCountSpec
            : form.StirrupCountSpec.Trim(),
        StirrupsThroughJoint = form.StirrupsThroughJoint,
        HangerEnabled = form.HangerEnabled,
        Hanger = form.Hanger.Trim(),
        HangerTopLengthMm = form.HangerTopLengthMm!.Value,
        HangerAngleDegrees = form.HangerAngleDegrees!.Value,
    };

    private static double Pick(IReadOnlyList<double> options, double value, double fallback, string label, List<string> replaced)
    {
        double picked = KataSettingsChoices.Pick(options, value, fallback, out bool inList);
        if (!inList) replaced.Add(string.Format(CultureInfo.InvariantCulture, label, value.ToString("0.##", CultureInfo.InvariantCulture)));
        return picked;
    }

    private static int Pick(IReadOnlyList<int> options, int value, int fallback, string label, List<string> replaced)
    {
        if (options.Contains(value)) return value;
        replaced.Add(string.Format(CultureInfo.InvariantCulture, label, value));
        return fallback;
    }
}
