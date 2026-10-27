using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HPRebar.Core.KataRebar.Parsers;

namespace HPRebar.Core.KataExport.Settings;

/// <summary>
/// What a settings form may hold, one error per wrong field so the page marks the input itself: the ranges Kata's
/// dialog takes (fractions of the span 0 … 0.5, lengths and factors not negative, 0 = off), a value from each list, the
/// joint notations, and table rows with a positive diameter each, none twice. Rows left empty are ignored.
/// </summary>
public static class KataSettingsRules
{
    private const string NotANumber = "Nhập một số.";
    private const string NotNegative = "Không âm (0 = tắt).";
    private const string Positive = "Phải dương.";
    private const string NotInList = "Chọn một giá trị trong danh sách.";

    public static IReadOnlyList<KataSettingsError> Validate(KataSettingsForm form)
    {
        if (form is null) throw new ArgumentNullException(nameof(form));
        var errors = new Errors();
        Detail(form.Detail, errors.In("detail"));
        Special(form.Special, errors.In("special"));
        Joint(form.Joints.Beam, errors.In("joints.beam"));
        Joint(form.Joints.Column, errors.In("joints.column"));
        return errors.List;
    }

    internal static bool IsBlank(KataMassRowForm row) => IsEmpty(row.Diameter) && IsEmpty(row.KgPerM);

    internal static bool IsBlank(KataLapRowForm row) => IsEmpty(row.Diameter) && IsEmpty(row.LapCompression) && IsEmpty(row.LapTension)
                                                        && IsEmpty(row.AnchorCompression) && IsEmpty(row.AnchorTension);

    private static bool IsEmpty(double? value) => value is null or 0.0;

    private static void Detail(KataDetailForm d, Scope e)
    {
        var choices = KataSettingsFormChoices.Kata;
        e.OneOf("closedStirrupHookAngle", d.ClosedStirrupHookAngle, choices.HookAngles);
        e.Range("closedStirrupHookFactor", d.ClosedStirrupHookFactor, v => v > 0.0, Positive);
        e.OneOf("crossTieHookAngle", d.CrossTieHookAngle, choices.HookAngles);
        e.Range("crossTieHookFactor", d.CrossTieHookFactor, v => v > 0.0, Positive);
        e.Range("roundCutExtraMm", d.RoundCutExtraMm, v => v >= 0.0, NotNegative);
        e.Range("sideBarAnchorageFactor", d.SideBarAnchorageFactor, v => v > 0.0, Positive);
        e.Count("layerTieMinBarCount", d.LayerTieMinBarCount, 2, "Thanh C kê từ 2 thanh trở lên.");
        e.OneOf("crankMinDiameter", d.CrankMinDiameter, choices.CrankMinDiameters);
        e.OneOf("crankSlope", d.CrankSlope, choices.CrankSlopes);
        e.Range("curtailedExtensionMm", d.CurtailedExtensionMm, v => v >= 0.0, NotNegative);
        e.Range("denseZoneHeightFactor", d.DenseZoneHeightFactor, v => v >= 0.0, NotNegative);
        e.Range("endZoneFraction", d.EndZoneFraction, v => v is >= 0.0 and <= 0.5, "Tỉ lệ vùng đai dày 0 … 0.5 (× Ln).");
        e.Range("bottomExtraCutFraction", d.BottomExtraCutFraction, v => v is >= 0.0 and < 0.5, "Tỉ lệ cắt gia cường bụng 0 … < 0.5 (× Ln).");
        e.Range("minimumLegFactor", d.MinimumLegFactor, v => v >= 0.0, NotNegative);
        e.Range("layerClearGap", d.LayerClearGap, v => v >= 0.0, NotNegative);
        e.Range("roundLegMm", d.RoundLegMm, v => v >= 0.0, NotNegative);
        e.Range("sideBarRequiredHeight", d.SideBarRequiredHeight, v => v >= 0.0, NotNegative);

        e.Range("alwaysBendFactor", d.AlwaysBendFactor, v => v >= 0.0, NotNegative);

        e.OneOf("couplerMinDiameter", d.CouplerMinDiameter, choices.CouplerMinDiameters);
        e.Range("maxBarLengthMm", d.MaxBarLengthMm, v => v > 0.0, "Chiều dài thép tối đa phải dương.");
        e.Range("minLengthForCuttingMm", d.MinLengthForCuttingMm, v => v >= 0.0, NotNegative);
        e.Range("minBarLengthFactor", d.MinBarLengthFactor, v => v >= 0.0, NotNegative);
        e.Range("roundLapMm", d.RoundLapMm, v => v >= 0.0, NotNegative);
        e.Range("topLapZoneFraction", d.TopLapZoneFraction, v => v is >= 0.0 and <= 0.5, "Vùng nối 0 … 0.5 L.");
        e.Range("bottomLapZoneFraction", d.BottomLapZoneFraction, v => v is >= 0.0 and <= 0.5, "Vùng nối 0 … 0.5 L.");
    }

    private static void Special(KataSpecialForm s, Scope e)
    {
        e.Range("compressionLapFactor", s.CompressionLapFactor, v => v >= 0.0, NotNegative);
        e.Range("tensionLapFactor", s.TensionLapFactor, v => v >= 0.0, NotNegative);
        e.Range("crankToLapEdgeMm", s.CrankToLapEdgeMm, v => v >= 0.0, NotNegative);
        e.Range("minLapGapMm", s.MinLapGapMm, v => v >= 0.0, NotNegative);
        e.Range("cuttingToleranceMm", s.CuttingToleranceMm, v => v >= 0.0, NotNegative);

        var massDiameters = new HashSet<double>();
        for (int i = 0; i < s.MassRows.Count; i++)
        {
            var row = s.MassRows[i];
            if (IsBlank(row)) continue;
            var r = e.In($"massRows[{i}]");
            Diameter(r, row.Diameter, massDiameters);
            r.Range("kgPerM", row.KgPerM, v => v >= 0.0, NotNegative);
        }

        var lapDiameters = new HashSet<double>();
        for (int i = 0; i < s.LapRows.Count; i++)
        {
            var row = s.LapRows[i];
            if (IsBlank(row)) continue;
            var r = e.In($"lapRows[{i}]");
            Diameter(r, row.Diameter, lapDiameters);
            r.Range("lapCompression", row.LapCompression, v => v >= 0.0, NotNegative);
            r.Range("lapTension", row.LapTension, v => v >= 0.0, NotNegative);
            r.Range("anchorCompression", row.AnchorCompression, v => v >= 0.0, NotNegative);
            r.Range("anchorTension", row.AnchorTension, v => v >= 0.0, NotNegative);
        }
    }

    /// <summary>A table row's diameter: positive, and not a diameter an earlier row already holds.</summary>
    private static void Diameter(Scope row, double? diameter, HashSet<double> seen)
    {
        if (!row.Range("diameter", diameter, v => v > 0.0, "Đường kính phải dương.")) return;
        if (!seen.Add(diameter!.Value))
            row.Add("diameter", string.Format(CultureInfo.InvariantCulture, "Đường kính {0:0.##} đã có ở dòng trên.", diameter.Value));
    }

    private static void Joint(KataJointForm j, Scope e)
    {
        if (!KataJointNotation.TryParseStirrups(j.Stirrups, out _, out _, out _))
            e.Add("stirrups", "Dạng số lượng f Ø a bước, ví dụ 5f8a50.");
        if (j.HangerEnabled && !KataJointNotation.TryParseBars(j.Hanger, out _, out _))
            e.Add("hanger", "Dạng số lượng f Ø, ví dụ 2f16.");
        e.Range("hangerTopLengthMm", j.HangerTopLengthMm, v => v >= 0.0, NotNegative);
        e.OneOf("hangerAngleDegrees", j.HangerAngleDegrees, KataSettingsFormChoices.Kata.HangerAngles);
    }

    private sealed class Errors
    {
        public List<KataSettingsError> List { get; } = new();

        public Scope In(string path) => new(this, path);
    }

    /// <summary>The errors of one record, named by their path from the form.</summary>
    private sealed class Scope(Errors errors, string path)
    {
        public Scope In(string child) => new(errors, path + "." + child);

        public void Add(string field, string message) => errors.List.Add(new KataSettingsError(path + "." + field, message));

        /// <returns>Whether the value is a finite number <paramref name="ok" /> accepts.</returns>
        public bool Range(string field, double? value, Func<double, bool> ok, string message)
        {
            if (value is not { } v || double.IsNaN(v) || double.IsInfinity(v))
            {
                Add(field, NotANumber);
                return false;
            }

            if (ok(v)) return true;
            Add(field, message);
            return false;
        }

        public void Count(string field, int? value, int minimum, string message)
        {
            if (value is null) Add(field, NotANumber);
            else if (value < minimum) Add(field, message);
        }

        public void OneOf(string field, int? value, IReadOnlyList<int> options)
        {
            if (value is null || !options.Contains(value.Value)) Add(field, NotInList);
        }

        public void OneOf(string field, double? value, IReadOnlyList<double> options)
        {
            if (value is null || !options.Any(o => Math.Abs(o - value.Value) < 1e-6)) Add(field, NotInList);
        }
    }
}
