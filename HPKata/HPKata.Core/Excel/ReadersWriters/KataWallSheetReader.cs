using System;
using HPKata.Core.Excel.Abstractions;
using HPKata.Core.Excel.Data;

namespace HPKata.Core.Excel.ReadersWriters
{
    /// <summary>
    /// Reads structural shear wall detailing parameters from sheet 'Vach' in Kata.xlsm.
    /// </summary>
    public static class KataWallSheetReader
    {
        public const string SheetName = "Vach";

        public static bool Read(IKataExcelSession session, out KataWallSheetData data)
        {
            data = new KataWallSheetData();
            if (!session.SheetExists(SheetName)) return false;

            ReadMetadata(session, data);
            ReadStories(session, data);
            return true;
        }

        private static void ReadMetadata(IKataExcelSession session, KataWallSheetData data)
        {
            data.TransverseBeamWidthMm = session.GetCellDouble(SheetName, 1, 3, 800.0);
            data.BoundaryZoneWidthMm = session.GetCellDouble(SheetName, 1, 6, 600.0);
            data.BoundaryLengthMm = data.BoundaryZoneWidthMm;

            data.SlabThicknessMm = session.GetCellDouble(SheetName, 2, 3, 200.0);
            data.DrawingScale = session.GetCellValue(SheetName, 2, 6) ?? "1:30";

            data.WallMark = session.GetCellValue(SheetName, 3, 3) ?? "V1";
            data.SectionScale = session.GetCellValue(SheetName, 3, 6) ?? "1:20";

            data.MemberCount = session.GetCellInt(SheetName, 4, 3, 1);
            data.CoverSpec = session.GetCellValue(SheetName, 4, 6) ?? "30/100";
            data.DefaultOuterTie = session.GetCellValue(SheetName, 4, 8) ?? "d10a100/200";

            data.FoundationBottomElevationM = session.GetCellDouble(SheetName, 5, 3, -5.5);
            data.DowelHookFactor = session.GetCellInt(SheetName, 5, 6, 15);
            data.DefaultExtraTie = session.GetCellValue(SheetName, 5, 8) ?? "d8a400";
        }

        private static void ReadStories(IKataExcelSession session, KataWallSheetData data)
        {
            int row = 10;
            int storyIdx = 0;

            while (row <= 100)
            {
                string? storyName = session.GetCellValue(SheetName, row, 1)?.Trim();
                if (storyName == null || storyName.Length == 0) break;

                var story = ParseStoryRow(session, row, storyIdx++, storyName);
                data.Stories.Add(story);
                row++;
            }

            if (data.Stories.Count > 0)
            {
                var s0 = data.Stories[0];
                data.LengthMm = s0.LengthLwMm;
                data.ThicknessMm = s0.ThicknessTwMm;
                data.StoryHeightMm = s0.StoryHeightM * 1000.0;
                data.BoundaryBarCount = s0.BoundaryBarsCount;
                data.BoundaryBarDiameter = ParseDiameter(s0.VerticalBarDiameterSpec, 20);
                data.WebVerticalDiameter = ParseDiameter(s0.VerticalBarDiameterSpec, 12);
                data.StoryCount = data.Stories.Count;
            }
        }

        private static KataWallStoryData ParseStoryRow(
            IKataExcelSession session, int row, int index, string name)
        {
            return new KataWallStoryData
            {
                StoryIndex = index,
                StoryName = name,
                StoryHeightM = session.GetCellDouble(SheetName, row, 2, 3.3),
                LengthLwMm = session.GetCellDouble(SheetName, row, 3, 3000.0),
                ThicknessTwMm = session.GetCellDouble(SheetName, row, 4, 300.0),
                BoundaryBarsCount = session.GetCellInt(SheetName, row, 5, 6),
                WebBarsCount = session.GetCellInt(SheetName, row, 6, 10),
                TransverseBarsLayer1 = session.GetCellInt(SheetName, row, 7, 2),
                TransverseBarsLayer2 = session.GetCellInt(SheetName, row, 8, 2),
                VerticalBarDiameterSpec = session.GetCellValue(SheetName, row, 9) ?? "20",
                SplicePositionMm = session.GetCellDouble(SheetName, row, 10, 0.0),
                CouplingBeamDepthMm = session.GetCellDouble(SheetName, row, 11, 600.0),
                OuterTieSpec = session.GetCellValue(SheetName, row, 12) ?? "d10a100/200",
                BoundaryTieSpec = session.GetCellValue(SheetName, row, 13) ?? "d10a100/200",
                CrossTieVerSpec = session.GetCellValue(SheetName, row, 14) ?? "d10a400",
                CrossTieVerCount = session.GetCellValue(SheetName, row, 15) ?? "2",
                CrossTieHorSpec = session.GetCellValue(SheetName, row, 16) ?? "d10a400",
                CrossTieHorCount = session.GetCellInt(SheetName, row, 17, 2)
            };
        }

        private static int ParseDiameter(string spec, int defaultDia)
        {
            if (string.IsNullOrWhiteSpace(spec)) return defaultDia;
            string digits = string.Empty;
            foreach (char c in spec)
            {
                if (char.IsDigit(c)) digits += c;
                else if (digits.Length > 0) break;
            }
            return int.TryParse(digits, out int dia) && dia > 0 ? dia : defaultDia;
        }
    }
}
