using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPDcePro.Core.Rebar;

namespace HPDcePro.Excel
{
    /// <summary>
    /// Headless Excel rebar schedule exporter (.xlsx) powered by ClosedXML.
    /// Eliminates background Excel COM hangs and produces standard TCVN 1651:2018 schedules.
    /// </summary>
    public static class ClosedXmlScheduleExporter
    {
        public static void ExportToFile(IEnumerable<ScheduleItem> items, string filePath, string title = "BẢNG THỐNG KÊ CỐT THÉP")
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using var workbook = CreateWorkbook(items, title);
            workbook.SaveAs(filePath);
        }

        public static void ExportToStream(IEnumerable<ScheduleItem> items, Stream stream, string title = "BẢNG THỐNG KÊ CỐT THÉP")
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using var workbook = CreateWorkbook(items, title);
            workbook.SaveAs(stream);
        }

        public static XLWorkbook CreateWorkbook(IEnumerable<ScheduleItem> items, string title)
        {
            var report = RebarScheduleAggregator.Aggregate(items);
            var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("ThongKeThep");

            WriteTitleBlock(ws, title);
            WriteHeaderRow(ws);
            int lastRow = WriteDataRows(ws, report.Items);
            WriteSummarySection(ws, report, lastRow);

            ws.Columns().AdjustToContents();
            return workbook;
        }

        private static void WriteTitleBlock(IXLWorksheet ws, string title)
        {
            ws.Cell(1, 1).Value = title;
            ws.Range(1, 1, 1, 11).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void WriteHeaderRow(IXLWorksheet ws)
        {
            string[] headers =
            {
                "STT", "Cấu kiện", "Số hiệu", "ĐK (mm)", "Dài L (mm)",
                "SL/CK", "Số CK", "Tổng SL", "Tổng dài (m)", "KLR (kg/m)", "Tổng KL (kg)"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(3, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(230, 240, 250);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }

        private static int WriteDataRows(IXLWorksheet ws, IReadOnlyList<ScheduleItem> items)
        {
            int row = 4;
            for (int i = 0; i < items.Count; i++, row++)
            {
                var item = items[i];
                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 2).Value = item.ElementMark;
                ws.Cell(row, 3).Value = item.BarMark;
                ws.Cell(row, 4).Value = item.DiameterMm;
                ws.Cell(row, 5).Value = item.LengthMm;
                ws.Cell(row, 6).Value = item.QuantityPerElement;
                ws.Cell(row, 7).Value = item.ElementCount;
                ws.Cell(row, 8).Value = item.TotalQuantity;
                ws.Cell(row, 9).Value = Math.Round(item.TotalLengthM, 2);
                ws.Cell(row, 10).Value = Math.Round(item.UnitMassKgPerM, 3);
                ws.Cell(row, 11).Value = Math.Round(item.TotalWeightKg, 2);

                ws.Range(row, 1, row, 11).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, 11).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }
            return row;
        }

        private static void WriteSummarySection(IXLWorksheet ws, ScheduleSummaryReport report, int startRow)
        {
            int row = startRow;
            ws.Cell(row, 1).Value = "TỔNG CỘNG";
            ws.Range(row, 1, row, 10).Merge();
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(row, 11).Value = Math.Round(report.TotalWeightKg, 2);
            ws.Cell(row, 11).Style.Font.Bold = true;
            ws.Range(row, 1, row, 11).Style.Fill.BackgroundColor = XLColor.FromArgb(245, 245, 245);
            ws.Range(row, 1, row, 11).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            row += 2;
            ws.Cell(row, 1).Value = "TỔNG HỢP THEO ĐƯỜNG KÍNH";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            foreach (var diam in report.DiameterSummaries)
            {
                ws.Cell(row, 2).Value = $"Thép phi {diam.DiameterMm}:";
                ws.Cell(row, 4).Value = $"{Math.Round(diam.TotalWeightKg, 1)} kg";
                row++;
            }
        }
    }
}
