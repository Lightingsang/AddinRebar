using System;
using HPKata.Core.Excel.Abstractions;
using HPKata.Core.Excel.Data;

namespace HPKata.Core.Excel.ReadersWriters
{
    /// <summary>
    /// Writes structural column detailing parameters into sheet 'Cot' of Kata.xlsm.
    /// </summary>
    public static class KataColumnSheetWriter
    {
        public const string SheetName = "Cot";

        public static bool Write(IKataExcelSession session, in KataColumnSheetData data)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            session.EnsureSheet(SheetName);

            WriteMetadata(session, data);
            WriteStories(session, data);
            session.Save();
            return true;
        }

        private static void WriteMetadata(IKataExcelSession session, in KataColumnSheetData data)
        {
            session.SetCellValue(SheetName, 1, 3, data.TransverseBeamWidthMm);
            session.SetCellValue(SheetName, 2, 3, data.SlabThicknessMm);
            session.SetCellValue(SheetName, 2, 6, data.DrawingScale);

            session.SetCellValue(SheetName, 3, 3, data.ColumnMark);
            session.SetCellValue(SheetName, 3, 6, data.SectionScale);

            session.SetCellValue(SheetName, 4, 3, data.MemberCount);
            session.SetCellValue(SheetName, 4, 6, data.CoverSpec);
            session.SetCellValue(SheetName, 4, 8, data.DefaultOuterTie);

            session.SetCellValue(SheetName, 5, 3, data.FoundationBottomElevationM);
            session.SetCellValue(SheetName, 5, 6, data.DowelHookFactor);
            session.SetCellValue(SheetName, 5, 8, data.DefaultExtraTie);
        }

        private static void WriteStories(IKataExcelSession session, in KataColumnSheetData data)
        {
            int row = 10;
            foreach (var s in data.Stories)
            {
                WriteStoryRow(session, row, s);
                row++;
            }
        }

        private static void WriteStoryRow(IKataExcelSession session, int row, KataColumnStoryData s)
        {
            double heightM = s.StoryHeightMm / 1000.0;
            string diaSpec = string.IsNullOrEmpty(s.RebarDiameterSpec) ? s.CornerDiameter.ToString() : s.RebarDiameterSpec;

            session.SetCellValue(SheetName, row, 1, s.StoryName);
            session.SetCellValue(SheetName, row, 2, heightM);
            session.SetCellValue(SheetName, row, 3, s.WidthMm);
            session.SetCellValue(SheetName, row, 4, s.DepthMm);
            session.SetCellValue(SheetName, row, 5, s.SideCountB);
            session.SetCellValue(SheetName, row, 6, s.SideCountB);
            session.SetCellValue(SheetName, row, 7, s.SideCountH);
            session.SetCellValue(SheetName, row, 8, s.SideCountH);
            session.SetCellValue(SheetName, row, 9, diaSpec);
            session.SetCellValue(SheetName, row, 10, s.SplicePositionMm);
            session.SetCellValue(SheetName, row, 11, s.BeamDepthMm);
            session.SetCellValue(SheetName, row, 12, s.OuterTieSpec);
            session.SetCellValue(SheetName, row, 13, s.RectTieVerRatio);
            session.SetCellValue(SheetName, row, 14, s.RectTieVerSpec);
            session.SetCellValue(SheetName, row, 15, s.RectTieVerCount);
            session.SetCellValue(SheetName, row, 16, s.RectTieHorRatio);
            session.SetCellValue(SheetName, row, 17, s.RectTieHorSpec);
            session.SetCellValue(SheetName, row, 18, s.RectTieHorCount);
            session.SetCellValue(SheetName, row, 19, s.CrossTieVerSpec);
            session.SetCellValue(SheetName, row, 20, s.CrossTieVerCount);
            session.SetCellValue(SheetName, row, 21, s.CrossTieHorSpec);
            session.SetCellValue(SheetName, row, 22, s.CrossTieHorCount);
        }
    }
}
