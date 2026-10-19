using System;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.QuantitySurveying.Models;

namespace HPKata.Core.QuantitySurveying.Persistence
{
    /// <summary>
    /// Headless Excel generator creating Kata.xlsm-compatible workbooks
    /// containing Kata_QS and Thong_ke_QS sheets via ClosedXML.
    /// </summary>
    public static class QsWorkbookGenerator
    {
        public static void ExportToFile(QsTakeoffSummary summary, string filePath)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path required.", nameof(filePath));

            using var workbook = new XLWorkbook();
            BuildWorkbook(workbook, summary);
            workbook.SaveAs(filePath);
        }

        public static void ExportToStream(QsTakeoffSummary summary, Stream stream)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using var workbook = new XLWorkbook();
            BuildWorkbook(workbook, summary);
            workbook.SaveAs(stream);
        }

        private static void BuildWorkbook(IXLWorkbook workbook, QsTakeoffSummary summary)
        {
            var wsQs = workbook.Worksheets.Add("Kata_QS");
            BuildKataQsSheet(wsQs, summary);

            var wsSummary = workbook.Worksheets.Add("Thong_ke_QS");
            BuildSummarySheet(wsSummary, summary);
        }

        private static void BuildKataQsSheet(IXLWorksheet ws, QsTakeoffSummary summary)
        {
            WriteKataQsHeaders(ws);
            int row = 3;

            foreach (var item in summary.Items)
            {
                ws.Cell(row, 1).Value = item.DrawingName;
                ws.Cell(row, 2).Value = item.ItemMark;
                ws.Cell(row, 3).Value = item.Count;
                ws.Cell(row, 4).Value = Math.Round(item.AreaM2, 3);
                ws.Cell(row, 5).Value = Math.Round(item.LengthM, 3);
                ws.Cell(row, 6).Value = Math.Round(item.HeightM, 3);
                ws.Cell(row, 7).FormulaA1 = $"=C{row}*D{row}";
                ws.Cell(row, 8).FormulaA1 = $"=C{row}*E{row}";
                ws.Cell(row, 9).Value = Math.Round(item.TotalVolumeM3, 3);
                ws.Cell(row, 10).Value = Math.Round(item.FormworkAreaM2, 3);
                row++;
            }

            int totalRow = row;
            ws.Cell(totalRow, 2).Value = "TỔNG CỘNG";
            ws.Cell(totalRow, 2).Style.Font.Bold = true;
            if (row > 3)
            {
                ws.Cell(totalRow, 9).FormulaA1 = $"=SUM(I3:I{row - 1})";
                ws.Cell(totalRow, 10).FormulaA1 = $"=SUM(J3:J{row - 1})";
            }
            ws.Range(totalRow, 2, totalRow, 10).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
        }

        private static void WriteKataQsHeaders(IXLWorksheet ws)
        {
            string[] headers =
            {
                "Tên bản vẽ", "Hạng mục", "Số lượng", "Diện tích (m2) -A-",
                "Chiều dài (m) -L-", "Chiều cao (m) -H-", "Tổng diện tích (m2)",
                "Tổng chiều dài (m)", "Tổng thể tích (m3)", "Diện tích xung quanh (m2)"
            };

            for (int col = 0; col < headers.Length; col++)
            {
                ws.Cell(1, col + 1).Value = headers[col];
                ws.Cell(1, col + 1).Style.Font.Bold = true;
            }

            string[] subHeaders =
            {
                "(0)", "(1)", "(2)", "(3)", "(4)", "(5)",
                "(6) = (3)x(2)", "(7) = (4)x(2)", "(8)", "(9)"
            };

            for (int col = 0; col < subHeaders.Length; col++)
            {
                ws.Cell(2, col + 1).Value = subHeaders[col];
                ws.Cell(2, col + 1).Style.Font.Italic = true;
            }
        }

        private static void BuildSummarySheet(IXLWorksheet ws, QsTakeoffSummary summary)
        {
            ws.Cell(1, 1).Value = "BẢNG TỔNG HỢP KHỐI LƯỢNG BÊ TÔNG & CỐP PHA";
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 13;

            ws.Cell(2, 1).Value = "Hạng mục cấu kiện";
            ws.Cell(2, 2).Value = "Thể tích bê tông (m3)";
            ws.Cell(2, 3).Value = "Diện tích cốp pha (m2)";
            ws.Range(2, 1, 2, 3).Style.Font.Bold = true;

            var categories = new (string Name, double Vol, double Fw)[]
            {
                ("Móng (Footings)", summary.FootingVolumeM3, summary.FootingFormworkM2),
                ("Cột (Columns)", summary.ColumnVolumeM3, summary.ColumnFormworkM2),
                ("Vách (Walls)", summary.WallVolumeM3, summary.WallFormworkM2),
                ("Dầm (Beams)", summary.BeamVolumeM3, summary.BeamFormworkM2),
                ("Sàn (Slabs)", summary.SlabVolumeM3, summary.SlabFormworkM2),
                ("Cầu thang (Stairs)", summary.StairVolumeM3, summary.StairFormworkM2)
            };

            for (int i = 0; i < categories.Length; i++)
            {
                int r = 3 + i;
                ws.Cell(r, 1).Value = categories[i].Name;
                ws.Cell(r, 2).Value = Math.Round(categories[i].Vol, 3);
                ws.Cell(r, 3).Value = Math.Round(categories[i].Fw, 3);
            }

            int grandRow = 3 + categories.Length;
            ws.Cell(grandRow, 1).Value = "TỔNG CỘNG TOÀN DỰ ÁN";
            ws.Cell(grandRow, 2).FormulaA1 = $"=SUM(B3:B{grandRow - 1})";
            ws.Cell(grandRow, 3).FormulaA1 = $"=SUM(C3:C{grandRow - 1})";
            ws.Range(grandRow, 1, grandRow, 3).Style.Font.Bold = true;
            ws.Columns().AdjustToContents();
        }
    }
}
