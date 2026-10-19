using System;
using HPKata.Core.Excel.Abstractions;
using HPKata.Core.Excel.Data;

namespace HPKata.Core.Excel.ReadersWriters
{
    /// <summary>
    /// Writes structural beam detailing parameters into sheet 'Dam' of Kata.xlsm.
    /// </summary>
    public static class KataBeamSheetWriter
    {
        public const string SheetName = "Dam";

        public static bool Write(IKataExcelSession session, in KataBeamSheetData data)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            session.EnsureSheet(SheetName);

            WriteMetadata(session, data);
            WriteAlternatingColumns(session, data);
            session.Save();
            return true;
        }

        private static void WriteMetadata(IKataExcelSession session, in KataBeamSheetData data)
        {
            session.SetCellValue(SheetName, 1, 2, data.DrawingScale);
            session.SetCellValue(SheetName, 1, 7, data.TopCurtailmentExtensionMm);

            session.SetCellValue(SheetName, 2, 2, data.SectionScale);
            session.SetCellValue(SheetName, 2, 7, data.TensionAnchorageFactor);

            session.SetCellValue(SheetName, 3, 2, data.BeamMark);
            session.SetCellValue(SheetName, 3, 7, data.CompressionAnchorageFactor);
            session.SetCellValue(SheetName, 3, 8, data.TopCurtailmentRatio);

            session.SetCellValue(SheetName, 4, 2, data.MemberCount);
            session.SetCellValue(SheetName, 4, 7, data.SkinBarDiameterMm);

            session.SetCellValue(SheetName, 5, 2, data.DefaultHeightMm);
            session.SetCellValue(SheetName, 5, 7, data.SkinBarLayers);
            session.SetCellValue(SheetName, 5, 8, data.BotCurtailmentRatio);

            session.SetCellValue(SheetName, 6, 2, data.DefaultWidthMm);
            session.SetCellValue(SheetName, 6, 7, data.StirrupDiameter);

            session.SetCellValue(SheetName, 7, 2, data.SlabThicknessMm);
            session.SetCellValue(SheetName, 7, 7, data.StirrupNearSupport);

            session.SetCellValue(SheetName, 8, 2, data.AxisName);
            session.SetCellValue(SheetName, 8, 7, data.StirrupMidSpan);
            session.SetCellValue(SheetName, 8, 9, data.LegCount);

            session.SetCellValue(SheetName, 9, 2, data.AxisOffsetMm);
            session.SetCellValue(SheetName, 9, 7, data.StirrupCantilever);

            session.SetCellValue(SheetName, 10, 2, data.ElevationString);

            session.SetCellValue(SheetName, 11, 2, data.TopContinuousRebar);
            session.SetCellValue(SheetName, 12, 2, data.BotContinuousRebar);
        }

        private static void WriteAlternatingColumns(IKataExcelSession session, in KataBeamSheetData data)
        {
            int col = 3;
            int maxItems = Math.Max(data.Supports.Count, data.Spans.Count);

            for (int i = 0; i < maxItems; i++)
            {
                if (i < data.Supports.Count)
                {
                    WriteSupportColumn(session, col, data.Supports[i]);
                    col++;
                }

                if (i < data.Spans.Count)
                {
                    WriteSpanColumn(session, col, data.Spans[i]);
                    col++;
                }
            }
        }

        private static void WriteSupportColumn(IKataExcelSession session, int col, KataBeamSupportData s)
        {
            session.SetCellValue(SheetName, 10, col, "Cột");
            session.SetCellValue(SheetName, 11, col, s.WidthMm);
            session.SetCellValue(SheetName, 13, col, s.TopCurtailmentLayer1);
            session.SetCellValue(SheetName, 14, col, s.TopCurtailmentLayer2);
            session.SetCellValue(SheetName, 15, col, s.TopCurtailmentLayer3);
            session.SetCellValue(SheetName, 20, col, s.TransverseBeamSize);
            session.SetCellValue(SheetName, 21, col, s.TransverseBeamOffsetMm);
            session.SetCellValue(SheetName, 22, col, s.ColumnGridAxis);
            session.SetCellValue(SheetName, 23, col, s.ColumnGridOffsetMm);
        }

        private static void WriteSpanColumn(IKataExcelSession session, int col, KataBeamSpanData sp)
        {
            session.SetCellValue(SheetName, 10, col, "Nhịp");
            session.SetCellValue(SheetName, 11, col, sp.ClearLengthMm);
            session.SetCellValue(SheetName, 17, col, sp.BotCurtailmentLayer2);
            session.SetCellValue(SheetName, 18, col, sp.BotCurtailmentLayer1);
            session.SetCellValue(SheetName, 19, col, sp.TopStepDownDelta);
            session.SetCellValue(SheetName, 20, col, sp.SkinBarSpec);
            session.SetCellValue(SheetName, 21, col, sp.BottomStepDownDelta);
            session.SetCellValue(SheetName, 22, col, sp.StirrupOverride);
        }
    }
}
