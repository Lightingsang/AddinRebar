using System;
using HPKata.Core.Excel.Abstractions;
using HPKata.Core.Excel.Data;

namespace HPKata.Core.Excel.ReadersWriters
{
    /// <summary>
    /// Reads structural column detailing parameters from sheet 'Cot' in Kata.xlsm.
    /// </summary>
    public static class KataColumnSheetReader
    {
        public const string SheetName = "Cot";

        public static bool Read(IKataExcelSession session, out KataColumnSheetData data)
        {
            data = new KataColumnSheetData();
            if (!session.SheetExists(SheetName)) return false;

            ReadMetadata(session, data);
            ReadStories(session, data);
            return true;
        }

        private static void ReadMetadata(IKataExcelSession session, KataColumnSheetData data)
        {
            data.TransverseBeamWidthMm = session.GetCellDouble(SheetName, 1, 3, 800.0);
            data.SlabThicknessMm = session.GetCellDouble(SheetName, 2, 3, 200.0);
            data.DrawingScale = session.GetCellValue(SheetName, 2, 6) ?? "1:30";

            data.ColumnMark = session.GetCellValue(SheetName, 3, 3) ?? "C1";
            data.SectionScale = session.GetCellValue(SheetName, 3, 6) ?? "1:20";

            data.MemberCount = session.GetCellInt(SheetName, 4, 3, 1);
            data.CoverSpec = session.GetCellValue(SheetName, 4, 6) ?? "30/100";
            data.DefaultOuterTie = session.GetCellValue(SheetName, 4, 8) ?? "d10a100/200";

            data.FoundationBottomElevationM = session.GetCellDouble(SheetName, 5, 3, -5.5);
            data.DowelHookFactor = session.GetCellInt(SheetName, 5, 6, 15);
            data.DefaultExtraTie = session.GetCellValue(SheetName, 5, 8) ?? "d8a400";
        }

        private static void ReadStories(IKataExcelSession session, KataColumnSheetData data)
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
                data.TieDiameter = ParseDiameterFromSpec(s0.OuterTieSpec, 8);
                data.DenseSpacing = s0.DenseSpacing;
                data.SparseSpacing = s0.SparseSpacing;
            }
        }

        private static KataColumnStoryData ParseStoryRow(
            IKataExcelSession session, int row, int index, string name)
        {
            double heightM = session.GetCellDouble(SheetName, row, 2, 3.3);
            string diaSpec = session.GetCellValue(SheetName, row, 9) ?? "20";
            int cornerDia = ParseDiameterFromSpec(diaSpec, 20);

            var story = new KataColumnStoryData
            {
                StoryIndex = index,
                StoryName = name,
                StoryHeightMm = heightM > 20.0 ? heightM : heightM * 1000.0,
                WidthMm = session.GetCellDouble(SheetName, row, 3, 400.0),
                DepthMm = session.GetCellDouble(SheetName, row, 4, 400.0),
                SideCountB = session.GetCellInt(SheetName, row, 5, 1),
                SideCountH = session.GetCellInt(SheetName, row, 7, 1),
                CornerDiameter = cornerDia,
                SideDiameterB = cornerDia,
                SideDiameterH = cornerDia,
                RebarDiameterSpec = diaSpec,
                SplicePositionMm = session.GetCellDouble(SheetName, row, 10, 0.0),
                BeamDepthMm = session.GetCellDouble(SheetName, row, 11, 600.0),
                OuterTieSpec = session.GetCellValue(SheetName, row, 12) ?? "d10a100/200",
                RectTieVerRatio = session.GetCellDouble(SheetName, row, 13, 0.0),
                RectTieVerSpec = session.GetCellValue(SheetName, row, 14) ?? string.Empty,
                RectTieVerCount = session.GetCellInt(SheetName, row, 15, 0),
                RectTieHorRatio = session.GetCellDouble(SheetName, row, 16, 0.0),
                RectTieHorSpec = session.GetCellValue(SheetName, row, 17) ?? string.Empty,
                RectTieHorCount = session.GetCellInt(SheetName, row, 18, 0),
                CrossTieVerSpec = session.GetCellValue(SheetName, row, 19) ?? string.Empty,
                CrossTieVerCount = session.GetCellInt(SheetName, row, 20, 0),
                CrossTieHorSpec = session.GetCellValue(SheetName, row, 21) ?? string.Empty,
                CrossTieHorCount = session.GetCellInt(SheetName, row, 22, 0)
            };

            ParseTieSpacings(story.OuterTieSpec, out double dense, out double sparse);
            story.DenseSpacing = dense;
            story.SparseSpacing = sparse;

            return story;
        }

        private static int ParseDiameterFromSpec(string spec, int defaultDia)
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

        private static void ParseTieSpacings(string spec, out double dense, out double sparse)
        {
            dense = 100.0;
            sparse = 200.0;
            if (string.IsNullOrWhiteSpace(spec)) return;

            int aIdx = spec.IndexOf('a');
            if (aIdx >= 0)
            {
                string rest = spec.Substring(aIdx + 1);
                string[] parts = rest.Split('/');
                if (parts.Length > 0 && double.TryParse(parts[0], out double d1)) dense = d1;
                if (parts.Length > 1 && double.TryParse(parts[1], out double d2)) sparse = d2;
                else sparse = dense * 2.0;
            }
        }
    }
}
