using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Columns.Models;

namespace HPKata.Core.Columns.Persistence
{
    /// <summary>
    /// Generates ClosedXML Excel workbooks for multi-story column schedules matching sheet 'Cot' of Kata.xlsm.
    /// </summary>
    public static class ColumnPlanScheduleWorkbookGenerator
    {
        public static readonly string[] ColumnHeaders =
        {
            "Story", "ColumnMark", "Width_b", "Depth_h",
            "CornerBars", "SideBars_b", "SideBars_h",
            "TieDense_a1", "TieSparse_a2", "TieJoint", "TotalWeightKg"
        };

        public static void GenerateWorkbookFile(List<ColumnRebarLayout> layouts, string filePath)
        {
            if (layouts == null) throw new ArgumentNullException(nameof(layouts));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path required.", nameof(filePath));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Cot");
            PopulateWorksheet(ws, layouts);
            workbook.SaveAs(filePath);
        }

        public static void GenerateWorkbookStream(List<ColumnRebarLayout> layouts, Stream stream)
        {
            if (layouts == null) throw new ArgumentNullException(nameof(layouts));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Cot");
            PopulateWorksheet(ws, layouts);
            workbook.SaveAs(stream);
        }

        private static void PopulateWorksheet(IXLWorksheet ws, List<ColumnRebarLayout> layouts)
        {
            for (int c = 0; c < ColumnHeaders.Length; c++)
            {
                var cell = ws.Cell(2, c + 2);
                cell.Value = ColumnHeaders[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int rowIdx = 3;
            foreach (var layout in layouts)
            {
                var scheduleItems = ColumnScheduleAggregator.AggregateSchedule(layout);
                double totalWeight = 0.0;
                foreach (var it in scheduleItems) totalWeight += it.TotalWeightKg;

                foreach (var story in layout.Stories)
                {
                    WriteStoryRow(ws, rowIdx++, layout.ColumnMark, story, totalWeight);
                }
            }

            ws.Columns(2, ColumnHeaders.Length + 1).AdjustToContents();
        }

        private static void WriteStoryRow(
            IXLWorksheet ws, int row, string colMark, ColumnStorySegment story, double totalWeight)
        {
            var sec = story.Section;
            var tie = story.TieDistribution;

            ws.Cell(row, 2).Value = story.StoryName;
            ws.Cell(row, 3).Value = colMark;
            ws.Cell(row, 4).Value = story.WidthMm;
            ws.Cell(row, 5).Value = story.DepthMm;
            ws.Cell(row, 6).Value = $"4d{sec.CornerDiameterMm}";
            ws.Cell(row, 7).Value = $"{sec.SideCountB * 2}d{sec.SideDiameterB}";
            ws.Cell(row, 8).Value = $"{sec.SideCountH * 2}d{sec.SideDiameterH}";
            ws.Cell(row, 9).Value = $"d{tie.TieDiameterMm}a{tie.DenseSpacingMm}";
            ws.Cell(row, 10).Value = $"d{tie.TieDiameterMm}a{tie.SparseSpacingMm}";
            ws.Cell(row, 11).Value = $"d{tie.TieDiameterMm}a{tie.JointSpacingMm}";
            ws.Cell(row, 12).Value = Math.Round(totalWeight, 2);
        }
    }
}
