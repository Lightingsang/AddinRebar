using System;
using HPKata.Core.Excel.Abstractions;
using HPKata.Core.Excel.Data;

namespace HPKata.Core.Excel.ReadersWriters
{
    /// <summary>
    /// Writes structural shear wall detailing parameters into sheet 'Vach' of Kata.xlsm.
    /// </summary>
    public static class KataWallSheetWriter
    {
        public const string SheetName = "Vach";

        public static bool Write(IKataExcelSession session, in KataWallSheetData data)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            session.EnsureSheet(SheetName);

            WriteMetadata(session, data);
            WriteStories(session, data);
            session.Save();
            return true;
        }

        private static void WriteMetadata(IKataExcelSession session, in KataWallSheetData data)
        {
            session.SetCellValue(SheetName, 1, 3, data.TransverseBeamWidthMm);
            session.SetCellValue(SheetName, 1, 6, data.BoundaryZoneWidthMm > 0 ? data.BoundaryZoneWidthMm : data.BoundaryLengthMm);

            session.SetCellValue(SheetName, 2, 3, data.SlabThicknessMm);
            session.SetCellValue(SheetName, 2, 6, data.DrawingScale);

            session.SetCellValue(SheetName, 3, 3, data.WallMark);
            session.SetCellValue(SheetName, 3, 6, data.SectionScale);

            session.SetCellValue(SheetName, 4, 3, data.MemberCount);
            session.SetCellValue(SheetName, 4, 6, data.CoverSpec);
            session.SetCellValue(SheetName, 4, 8, data.DefaultOuterTie);

            session.SetCellValue(SheetName, 5, 3, data.FoundationBottomElevationM);
            session.SetCellValue(SheetName, 5, 6, data.DowelHookFactor);
            session.SetCellValue(SheetName, 5, 8, data.DefaultExtraTie);
        }

        private static void WriteStories(IKataExcelSession session, in KataWallSheetData data)
        {
            int row = 10;
            if (data.Stories.Count > 0)
            {
                foreach (var s in data.Stories)
                {
                    WriteStoryRow(session, row, s);
                    row++;
                }
            }
            else
            {
                // Generate at least one story row from root parameters
                var synthetic = new KataWallStoryData
                {
                    StoryName = "Tầng 1",
                    StoryHeightM = data.StoryHeightMm / 1000.0,
                    LengthLwMm = data.LengthMm,
                    ThicknessTwMm = data.ThicknessMm,
                    BoundaryBarsCount = data.BoundaryBarCount,
                    VerticalBarDiameterSpec = data.BoundaryBarDiameter.ToString()
                };
                WriteStoryRow(session, row, synthetic);
            }
        }

        private static void WriteStoryRow(IKataExcelSession session, int row, KataWallStoryData s)
        {
            session.SetCellValue(SheetName, row, 1, s.StoryName);
            session.SetCellValue(SheetName, row, 2, s.StoryHeightM);
            session.SetCellValue(SheetName, row, 3, s.LengthLwMm);
            session.SetCellValue(SheetName, row, 4, s.ThicknessTwMm);
            session.SetCellValue(SheetName, row, 5, s.BoundaryBarsCount);
            session.SetCellValue(SheetName, row, 6, s.WebBarsCount);
            session.SetCellValue(SheetName, row, 7, s.TransverseBarsLayer1);
            session.SetCellValue(SheetName, row, 8, s.TransverseBarsLayer2);
            session.SetCellValue(SheetName, row, 9, s.VerticalBarDiameterSpec);
            session.SetCellValue(SheetName, row, 10, s.SplicePositionMm);
            session.SetCellValue(SheetName, row, 11, s.CouplingBeamDepthMm);
            session.SetCellValue(SheetName, row, 12, s.OuterTieSpec);
            session.SetCellValue(SheetName, row, 13, s.BoundaryTieSpec);
            session.SetCellValue(SheetName, row, 14, s.CrossTieVerSpec);
            session.SetCellValue(SheetName, row, 15, s.CrossTieVerCount);
            session.SetCellValue(SheetName, row, 16, s.CrossTieHorSpec);
            session.SetCellValue(SheetName, row, 17, s.CrossTieHorCount);
        }
    }
}
