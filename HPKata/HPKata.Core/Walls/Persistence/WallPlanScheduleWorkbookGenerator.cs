using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Walls.Models;

namespace HPKata.Core.Walls.Persistence
{
    /// <summary>
    /// Generates ClosedXML Excel workbooks for shear wall schedules matching sheet 'Vach' of Kata.xlsm.
    /// </summary>
    public static class WallPlanScheduleWorkbookGenerator
    {
        public static readonly string[] ColumnHeaders =
        {
            "Story", "WallMark", "Thickness_tw", "Length_Lw",
            "Boundary_bbz", "BoundaryVertical", "BoundaryTie",
            "WebVertical", "WebHorizontal", "WebCrossTie", "TotalWeightKg"
        };

        public static void GenerateWorkbookFile(List<ShearWallModel> walls, string filePath)
        {
            if (walls == null) throw new ArgumentNullException(nameof(walls));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path required.", nameof(filePath));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Vach");
            PopulateWorksheet(ws, walls);
            workbook.SaveAs(filePath);
        }

        public static void GenerateWorkbookStream(List<ShearWallModel> walls, Stream stream)
        {
            if (walls == null) throw new ArgumentNullException(nameof(walls));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Vach");
            PopulateWorksheet(ws, walls);
            workbook.SaveAs(stream);
        }

        private static void PopulateWorksheet(IXLWorksheet ws, List<ShearWallModel> walls)
        {
            for (int c = 0; c < ColumnHeaders.Length; c++)
            {
                var cell = ws.Cell(2, c + 2);
                cell.Value = ColumnHeaders[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int rowIdx = 3;
            foreach (var wall in walls)
            {
                var scheduleItems = WallScheduleAggregator.AggregateSchedule(wall);
                double totalWeight = 0.0;
                foreach (var it in scheduleItems) totalWeight += it.TotalWeightKg;

                foreach (var story in wall.Stories)
                {
                    WriteStoryRow(ws, rowIdx++, wall, story, totalWeight);
                }
            }

            ws.Columns(2, ColumnHeaders.Length + 1).AdjustToContents();
        }

        private static void WriteStoryRow(
            IXLWorksheet ws, int row, ShearWallModel wall, WallStorySegment story, double totalWeight)
        {
            var be = wall.BoundaryElements.Count > 0 ? wall.BoundaryElements[0] : null;
            var web = wall.WebReinforcement;

            ws.Cell(row, 2).Value = story.StoryName;
            ws.Cell(row, 3).Value = wall.WallMark;
            ws.Cell(row, 4).Value = story.ThicknessMm;
            ws.Cell(row, 5).Value = story.LengthMm;
            ws.Cell(row, 6).Value = be?.LengthBbMm ?? 0.0;
            ws.Cell(row, 7).Value = be != null ? $"{be.VerticalBarCount}d{be.VerticalBarDiameterMm}" : "-";
            ws.Cell(row, 8).Value = be != null ? $"d{be.TieDiameterMm}a{be.TieSpacingMm}" : "-";
            ws.Cell(row, 9).Value = $"d{web.VerticalBarDiameterMm}a{web.VerticalBarSpacingMm}";
            ws.Cell(row, 10).Value = $"d{web.HorizontalBarDiameterMm}a{web.HorizontalBarSpacingMm}";
            ws.Cell(row, 11).Value = $"d{web.CrossTieDiameterMm}";
            ws.Cell(row, 12).Value = Math.Round(totalWeight, 2);
        }
    }
}
