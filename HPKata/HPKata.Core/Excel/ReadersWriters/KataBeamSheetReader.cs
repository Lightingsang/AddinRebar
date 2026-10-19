using System;
using System.Globalization;
using HPKata.Core.Excel.Abstractions;
using HPKata.Core.Excel.Data;

namespace HPKata.Core.Excel.ReadersWriters
{
    /// <summary>
    /// Reads structural beam detailing parameters from sheet 'Dam' in Kata.xlsm.
    /// </summary>
    public static class KataBeamSheetReader
    {
        public const string SheetName = "Dam";

        public static bool Read(IKataExcelSession session, out KataBeamSheetData data)
        {
            data = new KataBeamSheetData();
            if (!session.SheetExists(SheetName)) return false;

            ReadMetadata(session, data);
            ReadAlternatingColumns(session, data);
            return true;
        }

        private static void ReadMetadata(IKataExcelSession session, KataBeamSheetData data)
        {
            data.DrawingScale = session.GetCellValue(SheetName, 1, 2) ?? "1:50";
            data.TopCurtailmentExtensionMm = session.GetCellDouble(SheetName, 1, 7, 500.0);
            data.SameScaleFlag = session.GetCellInt(SheetName, 1, 9, 1);

            data.SectionScale = session.GetCellValue(SheetName, 2, 2) ?? "1:25";
            data.TensionAnchorageFactor = session.GetCellDouble(SheetName, 2, 7, 40.0);

            data.BeamMark = session.GetCellValue(SheetName, 3, 2) ?? "B01";
            data.CompressionAnchorageFactor = session.GetCellDouble(SheetName, 3, 7, 30.0);
            data.TopCurtailmentRatio = session.GetCellDouble(SheetName, 3, 8, 0.2);

            data.MemberCount = session.GetCellInt(SheetName, 4, 2, 1);
            data.SkinBarDiameterMm = session.GetCellInt(SheetName, 4, 7, 12);

            data.DefaultHeightMm = session.GetCellDouble(SheetName, 5, 2, 600.0);
            data.SkinBarLayers = session.GetCellInt(SheetName, 5, 7, 1);
            data.BotCurtailmentRatio = session.GetCellDouble(SheetName, 5, 8, 0.25);

            data.DefaultWidthMm = session.GetCellDouble(SheetName, 6, 2, 300.0);
            data.StirrupDiameterMm = session.GetCellInt(SheetName, 6, 7, 8);
            data.StirrupDiameter = data.StirrupDiameterMm;

            data.SlabThicknessMm = session.GetCellDouble(SheetName, 7, 2, 150.0);
            data.StirrupNearSupport = session.GetCellValue(SheetName, 7, 7) ?? "a100";
            data.TieSpacingSpec = session.GetCellValue(SheetName, 7, 10) ?? "a500";

            data.AxisName = session.GetCellValue(SheetName, 8, 2) ?? string.Empty;
            data.StirrupMidSpan = session.GetCellValue(SheetName, 8, 7) ?? "a200";
            data.LegCount = session.GetCellInt(SheetName, 8, 9, 2);

            data.AxisOffsetMm = session.GetCellDouble(SheetName, 9, 2, 0.0);
            data.StirrupCantilever = session.GetCellValue(SheetName, 9, 7) ?? "150";
            data.CoverSpec = session.GetCellValue(SheetName, 9, 10) ?? "50/25";
            ParseCoverSpec(data.CoverSpec, out double centroidCover, out double clearCover);
            data.MainBarCentroidCoverMm = centroidCover;
            data.ConcreteCoverMm = clearCover;

            data.ElevationString = session.GetCellValue(SheetName, 10, 2) ?? "+3.300";

            data.TopContinuousRebar = session.GetCellValue(SheetName, 11, 2) ?? "2f20";
            data.BotContinuousRebar = session.GetCellValue(SheetName, 12, 2) ?? "2f20";

            Mappers.KataBeamDataMapper.ParseBarSpec(data.TopContinuousRebar, out int topCnt, out int topDia);
            data.TopContinuousCount = topCnt;
            data.TopContinuousDiameter = topDia;

            Mappers.KataBeamDataMapper.ParseBarSpec(data.BotContinuousRebar, out int botCnt, out int botDia);
            data.BotContinuousCount = botCnt;
            data.BotContinuousDiameter = botDia;

            data.HasSideBars = data.SkinBarLayers > 0 && data.SkinBarDiameterMm > 0;
            data.SideBarCount = data.SkinBarLayers * 2;
            data.SideBarDiameter = data.SkinBarDiameterMm;
            data.DenseSpacing = ParseSpacingMm(data.StirrupNearSupport, 150.0);
            data.SparseSpacing = ParseSpacingMm(data.StirrupMidSpan, 200.0);
        }

        private static void ParseCoverSpec(string spec, out double centroidCover, out double clearCover)
        {
            centroidCover = 50.0;
            clearCover = 25.0;
            if (string.IsNullOrWhiteSpace(spec)) return;

            string[] parts = spec.Split('/');
            if (parts.Length >= 1 && double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double c0) && c0 > 0)
            {
                centroidCover = c0;
            }
            if (parts.Length >= 2 && double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double c1) && c1 > 0)
            {
                clearCover = c1;
            }
        }

        public static double ParseSpacingMm(string spec, double fallback)
        {
            if (string.IsNullOrWhiteSpace(spec)) return fallback;
            string digits = string.Empty;
            foreach (char ch in spec)
            {
                if (char.IsDigit(ch) || ch == '.') digits += ch;
            }
            return double.TryParse(digits, NumberStyles.Any, CultureInfo.InvariantCulture, out double val) && val > 0
                ? val
                : fallback;
        }

        private static void ReadAlternatingColumns(IKataExcelSession session, KataBeamSheetData data)
        {
            int col = 3;
            int supportIdx = 0;
            int spanIdx = 0;
            double curWidth = data.DefaultWidthMm;
            double curBottomStepUp = 0.0;
            double curTopStepDown = 0.0;

            while (col <= 80)
            {
                string colType = (session.GetCellValue(SheetName, 10, col) ?? string.Empty).Trim();
                string? row11Val = session.GetCellValue(SheetName, 11, col);
                if (string.IsNullOrWhiteSpace(row11Val) && string.IsNullOrWhiteSpace(colType))
                {
                    break;
                }

                bool isOddSupportCol = (col % 2) == 1;
                if (colType.IndexOf("Cột", StringComparison.OrdinalIgnoreCase) >= 0
                    || (colType.Length == 0 && isOddSupportCol))
                {
                    if (col > 3 && string.IsNullOrWhiteSpace(row11Val)) break;
                    var support = ParseSupportColumn(session, col, supportIdx++);
                    data.Supports.Add(support);
                }
                else if (colType.IndexOf("Nhịp", StringComparison.OrdinalIgnoreCase) >= 0
                    || (colType.Length == 0 && !isOddSupportCol))
                {
                    if (col > 4 && string.IsNullOrWhiteSpace(row11Val)) break;
                    UpdateSpanSectionState(session, col, ref curWidth, ref curTopStepDown, ref curBottomStepUp,
                        out string topBarOverride, out string botBarOverride);
                    double spanH = Math.Max(200.0, data.DefaultHeightMm - curBottomStepUp + curTopStepDown);
                    var span = ParseSpanColumn(
                        session, col, spanIdx++, curWidth, spanH,
                        curTopStepDown, curBottomStepUp, topBarOverride, botBarOverride);
                    data.Spans.Add(span);
                }

                col++;
            }

            LinkAdjacentSupportWidths(data);
        }

        private static void UpdateSpanSectionState(
            IKataExcelSession session, int col, ref double curWidth, ref double curTopStepDown, ref double curBottomStepUp,
            out string topBarOverride, out string botBarOverride)
        {
            topBarOverride = string.Empty;
            botBarOverride = string.Empty;

            string row19 = (session.GetCellValue(SheetName, 19, col) ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(row19))
            {
                if (TryParseLeadingNumber(row19, out double topDelta))
                {
                    curTopStepDown = topDelta;
                }
                else if (ContainsBarToken(row19))
                {
                    topBarOverride = row19;
                }
            }

            string row20 = (session.GetCellValue(SheetName, 20, col) ?? string.Empty).Trim();
            if (IsPureNumber(row20, out double widthOverride) && widthOverride >= 100.0)
            {
                curWidth = widthOverride;
            }

            string row21 = (session.GetCellValue(SheetName, 21, col) ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(row21))
            {
                if (IsPureNumber(row21, out double botStepUp))
                {
                    curBottomStepUp = botStepUp;
                }
                else if (ContainsBarToken(row21))
                {
                    botBarOverride = row21;
                }
            }
        }

        private static bool ContainsBarToken(string text)
        {
            return text.IndexOf('f') >= 0 || text.IndexOf('F') >= 0
                || text.IndexOf('d') >= 0 || text.IndexOf('D') >= 0
                || text.IndexOf('Ø') >= 0 || text.IndexOf('T') >= 0;
        }

        private static bool IsPureNumber(string text, out double value)
        {
            value = 0.0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (ContainsBarToken(text)) return false;
            return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseLeadingNumber(string text, out double value)
        {
            value = 0.0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string part = text.Split(';')[0].Trim();
            return IsPureNumber(part, out value);
        }

        private static void LinkAdjacentSupportWidths(KataBeamSheetData data)
        {
            for (int i = 0; i < data.Spans.Count; i++)
            {
                if (i < data.Supports.Count)
                {
                    data.Spans[i].LeftSupportWidthMm = data.Supports[i].WidthMm;
                    data.Spans[i].IsCantileverLeft = data.Supports[i].WidthMm <= 0.0 && i == 0;
                }
                if (i + 1 < data.Supports.Count)
                {
                    data.Spans[i].RightSupportWidthMm = data.Supports[i + 1].WidthMm;
                    data.Spans[i].IsCantileverRight = data.Supports[i + 1].WidthMm <= 0.0 && i == data.Spans.Count - 1;
                }
            }
        }

        private static KataBeamSupportData ParseSupportColumn(IKataExcelSession session, int col, int index)
        {
            return new KataBeamSupportData
            {
                Index = index,
                Name = $"Gối {index + 1}",
                WidthMm = session.GetCellDouble(SheetName, 11, col, 400.0),
                TopCurtailmentLayer1 = session.GetCellValue(SheetName, 13, col) ?? string.Empty,
                TopCurtailmentLayer2 = session.GetCellValue(SheetName, 14, col) ?? string.Empty,
                TopCurtailmentLayer3 = session.GetCellValue(SheetName, 15, col) ?? string.Empty,
                TransverseBeamSize = session.GetCellValue(SheetName, 20, col) ?? string.Empty,
                TransverseBeamOffsetMm = session.GetCellDouble(SheetName, 21, col, 0.0),
                ColumnGridAxis = session.GetCellValue(SheetName, 22, col) ?? string.Empty,
                ColumnGridOffsetMm = session.GetCellDouble(SheetName, 23, col, 0.0)
            };
        }

        private static KataBeamSpanData ParseSpanColumn(
            IKataExcelSession session, int col, int index, double widthMm, double heightMm,
            double topDeltaMm, double botStepUpMm, string topBarOverride, string botBarOverride)
        {
            return new KataBeamSpanData
            {
                SpanIndex = index,
                SpanName = $"Nhịp {index + 1}",
                ClearLengthMm = session.GetCellDouble(SheetName, 11, col, 5000.0),
                SlabThicknessOverrideMm = session.GetCellDouble(SheetName, 12, col, 0.0),
                WidthMm = widthMm,
                HeightMm = heightMm,
                TopElevationDeltaMm = topDeltaMm,
                BottomStepUpMm = botStepUpMm,
                BotCurtailmentLayer3 = session.GetCellValue(SheetName, 16, col) ?? string.Empty,
                BotCurtailmentLayer2 = session.GetCellValue(SheetName, 17, col) ?? string.Empty,
                BotCurtailmentLayer1 = session.GetCellValue(SheetName, 18, col) ?? string.Empty,
                TopStepDownDelta = session.GetCellValue(SheetName, 19, col) ?? string.Empty,
                SkinBarSpec = session.GetCellValue(SheetName, 20, col) ?? string.Empty,
                BottomStepDownDelta = session.GetCellValue(SheetName, 21, col) ?? string.Empty,
                TopMainRebarOverride = topBarOverride,
                BotMainRebarOverride = botBarOverride,
                StirrupOverride = session.GetCellValue(SheetName, 22, col) ?? string.Empty
            };
        }
    }
}
