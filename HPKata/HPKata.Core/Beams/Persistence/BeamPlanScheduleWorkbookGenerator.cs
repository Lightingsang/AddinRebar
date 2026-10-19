using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Beams.Models;

namespace HPKata.Core.Beams.Persistence
{
    /// <summary>
    /// Generates ClosedXML Excel workbooks for beam framing plans matching the 44-column
    /// schema of Kata.xlsm sheet "ThepDamAllMBKC" (Columns B through AR).
    /// </summary>
    public static class BeamPlanScheduleWorkbookGenerator
    {
        public static readonly string[] ColumnHeaders =
        {
            "Name", "Span", "Width", "Height",
            "T1.0", "T1.1", "T1.2", "T1.3",
            "T2.0", "T2.1", "T2.2", "T2.3",
            "T3.0", "T3.1", "T3.2", "T3.3",
            "B1.0", "B1.1", "B1.2", "B1.3",
            "B2.0", "B2.1", "B2.2", "B2.3",
            "B3.0", "B3.1", "B3.2", "B3.3",
            "DD", "S1", "S2", "S3",
            "DDE", "S1E", "S2E", "S3E",
            "SE", "SEC", "SEU",
            "GIA", "DGIA",
            "KE", "lap_xoan"
        };

        public static void GenerateWorkbookFile(List<BeamRebarLayout> layouts, string filePath)
        {
            if (layouts == null) throw new ArgumentNullException(nameof(layouts));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path required.", nameof(filePath));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("ThepDamAllMBKC");
            PopulateWorksheet(ws, layouts);
            workbook.SaveAs(filePath);
        }

        public static void GenerateWorkbookStream(List<BeamRebarLayout> layouts, Stream stream)
        {
            if (layouts == null) throw new ArgumentNullException(nameof(layouts));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("ThepDamAllMBKC");
            PopulateWorksheet(ws, layouts);
            workbook.SaveAs(stream);
        }

        private static void PopulateWorksheet(IXLWorksheet ws, List<BeamRebarLayout> layouts)
        {
            // Header Row at Row 2, starting Column B (col index 2)
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
                for (int s = 0; s < layout.Spans.Count; s++)
                {
                    WriteSpanRow(ws, rowIdx++, layout, s);
                }
            }

            ws.Columns(2, ColumnHeaders.Length + 1).AdjustToContents();
        }

        private static void WriteSpanRow(IXLWorksheet ws, int row, BeamRebarLayout layout, int spanIndex)
        {
            var span = layout.Spans[spanIndex];
            layout.StirrupZones.TryGetValue(spanIndex, out var stirrup);

            int col = 2; // Column B
            ws.Cell(row, col++).Value = layout.BeamName;
            ws.Cell(row, col++).Value = spanIndex + 1;
            ws.Cell(row, col++).Value = span.WidthMm;
            ws.Cell(row, col++).Value = span.HeightMm;

            WriteLongitudinalRebarCells(ws, row, ref col, layout);
            WriteStirrupAndDetailCells(ws, row, ref col, layout, stirrup);
        }

        private static void WriteLongitudinalRebarCells(
            IXLWorksheet ws, int row, ref int col, BeamRebarLayout layout)
        {
            string topMain = layout.TopContinuousBars.Count > 0
                ? $"{layout.TopContinuousBars[0].BarCount}Ø{layout.TopContinuousBars[0].DiameterMm}" : "";
            string botMain = layout.BottomContinuousBars.Count > 0
                ? $"{layout.BottomContinuousBars[0].BarCount}Ø{layout.BottomContinuousBars[0].DiameterMm}" : "";

            for (int i = 0; i < 4; i++) ws.Cell(row, col++).Value = topMain;
            for (int i = 0; i < 8; i++) ws.Cell(row, col++).Value = "";
            for (int i = 0; i < 4; i++) ws.Cell(row, col++).Value = botMain;
            for (int i = 0; i < 8; i++) ws.Cell(row, col++).Value = "";
        }

        private static void WriteStirrupAndDetailCells(
            IXLWorksheet ws, int row, ref int col, BeamRebarLayout layout, StirrupZoneDistributionModel? stirrup)
        {
            ws.Cell(row, col++).Value = stirrup?.StirrupDiameterMm ?? 8;
            ws.Cell(row, col++).Value = stirrup?.LeftDenseSpacingMm ?? 100.0;
            ws.Cell(row, col++).Value = stirrup?.MidspanSparseSpacingMm ?? 200.0;
            ws.Cell(row, col++).Value = stirrup?.RightDenseSpacingMm ?? 100.0;

            var hanger = layout.Intersections.Count > 0 ? layout.Intersections[0] : null;
            ws.Cell(row, col++).Value = hanger?.HangerStirrupDiameterMm ?? 0;
            ws.Cell(row, col++).Value = hanger?.HangerStirrupSpacingMm ?? 0;
            ws.Cell(row, col++).Value = 0;
            ws.Cell(row, col++).Value = 0;

            ws.Cell(row, col++).Value = stirrup?.TotalLegCount ?? 2;
            ws.Cell(row, col++).Value = stirrup?.CTieCount ?? 0;
            ws.Cell(row, col++).Value = stirrup?.UCapCount ?? 0;

            int skinCount = layout.SkinBars.Count * 2;
            int skinDia = layout.SkinBars.Count > 0 ? layout.SkinBars[0].DiameterMm : 0;
            ws.Cell(row, col++).Value = skinCount;
            ws.Cell(row, col++).Value = skinDia;
            ws.Cell(row, col++).Value = 0;
            ws.Cell(row, col++).Value = 0;
        }
    }
}
