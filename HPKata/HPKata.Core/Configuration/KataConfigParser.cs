using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HPKata.Core.Configuration
{
    /// <summary>
    /// Line-by-line parser and serializer for the 86 KataPro configuration parameters.
    /// Supports lines 152–237 of Goi lenh.lsp with UTF-8 and Windows-1258 fallback decoding.
    /// </summary>
    public static partial class KataConfigParser
    {
        static KataConfigParser()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static KataConfig ParseText(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new KataConfig();
            }

            string[] rawLines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return ParseLines(rawLines);
        }

        public static KataConfig ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Goi lenh.lsp file not found.", filePath);
            }

            byte[] bytes = File.ReadAllBytes(filePath);
            string content = DecodeWithFallback(bytes);
            return ParseText(content);
        }

        public static string DecodeWithFallback(byte[] bytes)
        {
            try
            {
                var utf8Strict = new UTF8Encoding(false, true);
                string text = utf8Strict.GetString(bytes);
                if (!text.Contains("\uFFFD"))
                {
                    return text;
                }
            }
            catch (DecoderFallbackException)
            {
                // Fall back to Vietnamese Windows-1258
            }

            try
            {
                Encoding win1258 = Encoding.GetEncoding(1258);
                return win1258.GetString(bytes);
            }
            catch (ArgumentException)
            {
                // Secondary fallback to Windows-1252
                return Encoding.GetEncoding(1252).GetString(bytes);
            }
        }

        public static KataConfig ParseLines(IReadOnlyList<string> lines)
        {
            var config = new KataConfig();
            var extracted = ExtractConfigLines(lines);
            if (extracted.Count == 0)
            {
                return config;
            }

            MapLines1To30(config, extracted);
            MapLines31To60(config, extracted);
            MapLines61To86(config, extracted);
            return config;
        }

        private static List<string> ExtractConfigLines(IReadOnlyList<string> lines)
        {
            var result = new List<string>(86);
            int startIdx = lines.Count >= 237 ? 151 : 0;
            int count = Math.Min(86, lines.Count - startIdx);

            for (int i = 0; i < count; i++)
            {
                string raw = lines[startIdx + i];
                result.Add(raw.Trim().TrimStart(';').Trim());
            }

            return result;
        }

        private static void MapLines1To30(KataConfig c, List<string> l)
        {
            if (l.Count > 0) c.LayerDim = l[0];
            if (l.Count > 1) c.LayerNetThay = l[1];
            if (l.Count > 2) c.LayerNetKhung = l[2];
            if (l.Count > 3) c.LayerNetKhuat = l[3];
            if (l.Count > 4) c.LayerGrid = l[4];
            if (l.Count > 5) c.LayerThepChu = l[5];
            if (l.Count > 6) c.LayerThepDai = l[6];
            if (l.Count > 7) c.LayerThepDaiPhu = l[7];
            if (l.Count > 8) c.LayerNetManh = l[8];
            if (l.Count > 9) c.LayerText = l[9];
            if (l.Count > 10) c.LayerTextTag = l[10];
            if (l.Count > 11) c.SpacingPrefix = l[11];
            if (l.Count > 12) c.SmallBarPrefix = l[12];
            if (l.Count > 13) c.DeformedBarPrefix = l[13];
            if (l.Count > 14) c.DimStyleName = l[14];
            if (l.Count > 15) c.ConcreteCoverMm = ParseDouble(l[15], c.ConcreteCoverMm);
            if (l.Count > 16) c.DefaultHookAngle = l[16];
            if (l.Count > 17) c.DetailDrawingScale = ParseFraction(l[17], c.DetailDrawingScale);
            if (l.Count > 18) c.BaseBarDiameter = ParseInt(l[18], c.BaseBarDiameter);
            if (l.Count > 19) c.LapLengthMultiplier = ParseDouble(l[19], c.LapLengthMultiplier);
            if (l.Count > 20) c.StaggeredLapMode = ParseInt(l[20], c.StaggeredLapMode);
            if (l.Count > 21) c.DrawFilletRadius = ParseInt(l[21], c.DrawFilletRadius);
            if (l.Count > 22) c.GroupSameRebar = ParseInt(l[22], c.GroupSameRebar);
            if (l.Count > 23) c.ApplyBendDeduction = ParseInt(l[23], c.ApplyBendDeduction);
            if (l.Count > 24) c.AutoRenumberMarks = ParseInt(l[24], c.AutoRenumberMarks);
            if (l.Count > 25) c.TensionAnchorMult = ParseDouble(l[25], c.TensionAnchorMult);
            if (l.Count > 26) c.ColSpliceRatio = ParseFraction(l[26], c.ColSpliceRatio);
            if (l.Count > 27) c.DrawElevationHooks = ParseInt(l[27], c.DrawElevationHooks);
            if (l.Count > 28) c.DimStyleOverride = ParseInt(l[28], c.DimStyleOverride);
            if (l.Count > 29) c.TagStyle = ParseInt(l[29], c.TagStyle);
        }

        private static void MapLines31To60(KataConfig c, List<string> l)
        {
            if (l.Count > 30) c.ScheduleDiaPrefix = l[30];
            if (l.Count > 31) c.StandardBarLength = ParseInt(l[31], c.StandardBarLength);
            if (l.Count > 32) c.MinRemnantLength = ParseInt(l[32], c.MinRemnantLength);
            if (l.Count > 33) c.CouplerMinDia = ParseInt(l[33], c.CouplerMinDia);
            if (l.Count > 34) c.BeamTopCutoffL1 = ParseDouble(l[34], c.BeamTopCutoffL1);
            if (l.Count > 35) c.BeamCutoffDatumL1 = l[35];
            if (l.Count > 36) c.BeamBotCutoff = ParseDouble(l[36], c.BeamBotCutoff);
            if (l.Count > 37) c.DenseZoneLayout = ParseInt(l[37], c.DenseZoneLayout);
            if (l.Count > 38) c.LengthRoundingMode = ParseInt(l[38], c.LengthRoundingMode);
            if (l.Count > 39) c.VarBarNaming = l[39];
            if (l.Count > 40) c.DrawShapeInSched = ParseInt(l[40], c.DrawShapeInSched);
            if (l.Count > 41) c.AnnotationTextH = ParseDouble(l[41], c.AnnotationTextH);
            if (l.Count > 42) c.DrawCenterlines = ParseInt(l[42], c.DrawCenterlines);
            if (l.Count > 43) c.TagSeparator = l[43];
            if (l.Count > 44) c.ScheduleRowHeight = ParseDouble(l[44], c.ScheduleRowHeight);
            if (l.Count > 45) c.SchedShapeColWidth = ParseDouble(l[45], c.SchedShapeColWidth);
            if (l.Count > 46) c.ReservedField47 = l[46];
            if (l.Count > 47) c.BeamTopCutoffL2 = ParseDouble(l[47], c.BeamTopCutoffL2);
            if (l.Count > 48) c.BeamCutoffDatumL2 = l[48];
            if (l.Count > 49) c.SecondaryBarLength = ParseInt(l[49], c.SecondaryBarLength);
            if (l.Count > 50) c.LanguageUiSched = l[50];
            if (l.Count > 51) c.SchedExportFormat = ParseInt(l[51], c.SchedExportFormat);
            if (l.Count > 52) c.CouplerSpec = ParseInt(l[52], c.CouplerSpec);
            if (l.Count > 53) c.ReservedField54 = l[53];
            if (l.Count > 54) c.TableTemplate = l[54];
            if (l.Count > 55) c.ReservedField56 = l[55];
            if (l.Count > 56) c.StirrupClosureType = l[56];
            if (l.Count > 57) c.FootingHookMult = ParseDouble(l[57], c.FootingHookMult);
            if (l.Count > 58) c.SlabHookMult = ParseDouble(l[58], c.SlabHookMult);
            if (l.Count > 59) c.WallHookMult = ParseDouble(l[59], c.WallHookMult);
        }

        private static void MapLines61To86(KataConfig c, List<string> l)
        {
            if (l.Count > 60) c.ReservedField61 = l[60];
            if (l.Count > 61) c.TitleScaleMode = ParseInt(l[61], c.TitleScaleMode);
            if (l.Count > 62) c.ReservedField63 = l[62];
            if (l.Count > 63) c.AutoDimRebar = ParseInt(l[63], c.AutoDimRebar);
            if (l.Count > 64) c.LengthUnitDisplay = l[64];
            if (l.Count > 65) c.CutLengthRoundStep = ParseDouble(l[65], c.CutLengthRoundStep);
            if (l.Count > 66) c.MassUnitDisplay = ParseInt(l[66], c.MassUnitDisplay);
            if (l.Count > 67) c.TotalLengthRound = ParseInt(l[67], c.TotalLengthRound);
            if (l.Count > 68) c.DrawLeaderArrow = ParseInt(l[68], c.DrawLeaderArrow);
            if (l.Count > 69) c.LeaderArrowType = ParseInt(l[69], c.LeaderArrowType);
            if (l.Count > 70) c.SecBeamStirrupAdd1 = l[70];
            if (l.Count > 71) c.SecBeamGroup1 = ParseInt(l[71], c.SecBeamGroup1);
            if (l.Count > 72) c.SecBeamAntiCrack1 = l[72];
            if (l.Count > 73) c.SecBeamZoneLen1 = ParseDouble(l[73], c.SecBeamZoneLen1);
            if (l.Count > 74) c.SecBeamStirrupAdd2 = l[74];
            if (l.Count > 75) c.SecBeamGroup2 = ParseInt(l[75], c.SecBeamGroup2);
            if (l.Count > 76) c.SecBeamAntiCrack2 = l[76];
            if (l.Count > 77) c.SecBeamZoneLen2 = ParseDouble(l[77], c.SecBeamZoneLen2);
            if (l.Count > 78) c.TagCircleRadius = ParseDouble(l[78], c.TagCircleRadius);
            if (l.Count > 79) c.StaggeredOffsetMm = ParseDouble(l[79], c.StaggeredOffsetMm);
            if (l.Count > 80) c.CouplerSepSchedule = ParseInt(l[80], c.CouplerSepSchedule);
            if (l.Count > 81) c.CouplerEndTreat1 = l[81];
            if (l.Count > 82) c.CouplerEndTreat2 = l[82];
            if (l.Count > 83) c.PrintQrCodeOnTag = ParseInt(l[83], c.PrintQrCodeOnTag);
            if (l.Count > 84) c.RealtimeSync = ParseInt(l[84], c.RealtimeSync);
            if (l.Count > 85) c.QsFormulaMode = ParseInt(l[85], c.QsFormulaMode);
        }

        private static double ParseDouble(string str, double fallback)
        {
            return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : fallback;
        }

        private static int ParseInt(string str, int fallback)
        {
            return int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : fallback;
        }

        private static double ParseFraction(string str, double fallback)
        {
            if (string.IsNullOrWhiteSpace(str)) return fallback;
            string[] parts = str.Split('/');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double num) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double den) &&
                den != 0.0)
            {
                return num / den;
            }
            return ParseDouble(str, fallback);
        }

        private static string FormatFraction(double value)
        {
            if (Math.Abs(value - 0.04) < 1e-6) return "1/25";
            if (Math.Abs(value - (1.0 / 6.0)) < 1e-4) return "1/6";
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
