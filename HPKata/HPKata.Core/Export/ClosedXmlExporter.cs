using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;

namespace HPKata.Core.Export
{
    /// <summary>
    /// Headless Excel spreadsheet generator creating Kata.xlsm-compatible workbooks via ClosedXML.
    /// </summary>
    public static class ClosedXmlExporter
    {
        public static void ExportScheduleToFile(RebarScheduleReport report, string filePath)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path required.", nameof(filePath));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Thong_ke");
            BuildSheet(ws, report);
            workbook.SaveAs(filePath);
        }

        public static void ExportScheduleToStream(RebarScheduleReport report, Stream stream)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Thong_ke");
            BuildSheet(ws, report);
            workbook.SaveAs(stream);
        }

        private static void BuildSheet(IXLWorksheet ws, RebarScheduleReport report)
        {
            WriteScheduleHeaders(ws);
            int lastRow = WriteScheduleRows(ws, report.Items);
            WriteDiameterSummary(ws, report.DiameterSummaries, report.Bins);
            WriteCouplerSummary(ws, report.Couplers);
            ws.Columns().AdjustToContents();
        }

        private static void WriteScheduleHeaders(IXLWorksheet ws)
        {
            ws.Cell("A1").Value = "BẢNG THỐNG KÊ CỐT THÉP";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 14;

            string[] headers =
            {
                "TÊN CẤU KIỆN", "SỐ HIỆU", "DẠNG",
                "L1", "L2", "L3", "L4", "L5", "L6",
                "Ghi chú", "Ø (mm)", "Dài ko kể nối", "CHIỀU DÀI 1 THANH",
                "SỐ C.KIỆN", "SỐ THANH MỘT CK", "SỐ THANH TOÀN BỘ",
                "TỔNG CHIỀU DÀI", "TỔNG TRỌNG LƯỢNG", "TỔNG/CẤU KIỆN"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                var cell = ws.Cell(3, col + 1);
                cell.Value = headers[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        private static int WriteScheduleRows(IXLWorksheet ws, IReadOnlyList<RebarItem> items)
        {
            int row = 4;
            foreach (var item in items)
            {
                ws.Cell(row, 1).Value = item.ComponentMark;
                ws.Cell(row, 2).Value = item.BarMark;
                ws.Cell(row, 3).Value = item.ShapeCode;

                for (int s = 0; s < 6; s++)
                {
                    double segVal = s < item.ParametersMm.Length ? item.ParametersMm[s] : 0.0;
                    ws.Cell(row, 4 + s).Value = segVal;
                }

                ws.Cell(row, 10).Value = item.Notes;
                ws.Cell(row, 11).Value = item.DiameterMm;
                ws.Cell(row, 12).Value = item.StructuralLengthMm > 0 ? item.StructuralLengthMm : item.CuttingLengthMm;
                ws.Cell(row, 13).Value = item.CuttingLengthMm;
                ws.Cell(row, 14).Value = item.MemberCount;
                ws.Cell(row, 15).Value = item.CountPerMember;
                ws.Cell(row, 16).Value = item.TotalCount;
                ws.Cell(row, 17).Value = Math.Round(item.TotalLengthM, 2);
                ws.Cell(row, 18).Value = Math.Round(item.TotalWeightKg, 2);
                ws.Cell(row, 19).Value = Math.Round(item.WeightPerMemberKg, 2);
                row++;
            }
            return row;
        }

        private static void WriteDiameterSummary(
            IXLWorksheet ws,
            IReadOnlyList<DiameterSummaryItem> summaries,
            TcvnDiameterBins bins)
        {
            ws.Cell("U1").Value = "TỔNG HỢP CỐT THÉP";
            ws.Cell("U1").Style.Font.Bold = true;

            ws.Cell("U2").Value = "Ø (mm)";
            ws.Cell("U3").Value = "Tổng chiều dài (m)";
            ws.Cell("U4").Value = "Tổng trọng lượng (kg)";

            for (int i = 0; i < summaries.Count; i++)
            {
                int col = 22 + i; // V is col 22
                var s = summaries[i];
                ws.Cell(2, col).Value = s.DiameterMm;
                ws.Cell(3, col).Value = Math.Round(s.TotalLengthM, 2);
                ws.Cell(4, col).Value = Math.Round(s.TotalWeightKg, 2);
            }

            ws.Cell("U5").Value = "Thép tròn trơn <= 10mm (kg)";
            ws.Cell("V5").Value = Math.Round(bins.WireLe10Kg, 2);

            ws.Cell("U6").Value = "Thép gờ 10 < phi <= 18mm (kg)";
            ws.Cell("V6").Value = Math.Round(bins.Deformed10To18Kg, 2);

            ws.Cell("U7").Value = "Thép gờ phi > 18mm (kg)";
            ws.Cell("V7").Value = Math.Round(bins.DeformedGt18Kg, 2);

            ws.Cell("U8").Value = "TỔNG CỘNG DỰ ÁN (kg)";
            ws.Cell("V8").Value = Math.Round(bins.TotalProjectWeightKg, 2);
            ws.Cell("U8").Style.Font.Bold = true;
            ws.Cell("V8").Style.Font.Bold = true;
        }

        private static void WriteCouplerSummary(IXLWorksheet ws, IReadOnlyList<CouplerSummaryItem> couplers)
        {
            if (couplers == null || couplers.Count == 0) return;

            ws.Cell("U10").Value = "BẢNG THỐNG KÊ COUPLER (MỐI NỐI REN)";
            ws.Cell("U10").Style.Font.Bold = true;

            string[] cHeaders = { "TÊN CẤU KIỆN", "Ø (mm)", "SỐ C.KIỆN", "MỘT CK", "TOÀN BỘ", "GHI CHÚ" };
            for (int col = 0; col < cHeaders.Length; col++)
            {
                var cell = ws.Cell(11, 21 + col);
                cell.Value = cHeaders[col];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            int cRow = 12;
            foreach (var c in couplers)
            {
                ws.Cell(cRow, 21).Value = c.ComponentMark;
                ws.Cell(cRow, 22).Value = c.DiameterMm;
                ws.Cell(cRow, 23).Value = c.MemberCount;
                ws.Cell(cRow, 24).Value = c.CouplersPerMember;
                ws.Cell(cRow, 25).Value = c.TotalCouplers;
                ws.Cell(cRow, 26).Value = c.Notes;
                cRow++;
            }
        }
    }
}
