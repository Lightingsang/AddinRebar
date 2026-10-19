using System;
using System.IO;
using ClosedXML.Excel;
using HPDcePro.Core.Calculations.Takeoff;

namespace HPDcePro.Excel
{
    /// <summary>
    /// Headless ClosedXML multi-sheet BOQ calculation workbook exporter.
    /// Creates sheets: TongHop, BeTong, VanKhuon, CotThep, ChiTietTheoTang.
    /// </summary>
    public static class ClosedXmlBoqExporter
    {
        public static void ExportToFile(QuantityTakeoffResult boq, string filePath, string projectName = "Dự án mẫu")
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            using var workbook = CreateWorkbook(boq, projectName);
            workbook.SaveAs(filePath);
        }

        public static void ExportToStream(QuantityTakeoffResult boq, Stream stream, string projectName = "Dự án mẫu")
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using var workbook = CreateWorkbook(boq, projectName);
            workbook.SaveAs(stream);
        }

        public static XLWorkbook CreateWorkbook(QuantityTakeoffResult boq, string projectName = "Dự án mẫu")
        {
            if (boq == null) throw new ArgumentNullException(nameof(boq));

            var workbook = new XLWorkbook();
            BuildTongHopSheet(workbook.Worksheets.Add("TongHop"), boq, projectName);
            BuildBeTongSheet(workbook.Worksheets.Add("BeTong"), boq);
            BuildVanKhuonSheet(workbook.Worksheets.Add("VanKhuon"), boq);
            BuildCotThepSheet(workbook.Worksheets.Add("CotThep"), boq);
            BuildChiTietTheoTangSheet(workbook.Worksheets.Add("ChiTietTheoTang"), boq);

            return workbook;
        }

        private static void BuildTongHopSheet(IXLWorksheet ws, QuantityTakeoffResult boq, string project)
        {
            ws.Cell(1, 1).Value = "TỔNG HỢP KHỐI LƯỢNG DỰ TOÁN KẾT CẤU (BOQ)";
            ws.Range(1, 1, 1, 5).Merge().Style.Font.SetBold().Font.SetFontSize(16);
            ws.Cell(2, 1).Value = $"Dự án: {project} | Ngày xuất: {DateTime.Now:dd/MM/yyyy}";
            ws.Range(2, 1, 2, 5).Merge().Style.Font.SetItalic();

            string[] headers = { "STT", "Hạng mục vật tư", "Đơn vị", "Khối lượng", "Ghi chú" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(4, c + 1).Value = headers[c];
            ws.Range(4, 1, 4, 5).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);

            ws.Cell(5, 1).Value = 1; ws.Cell(5, 2).Value = "Bê tông kết cấu các loại"; ws.Cell(5, 3).Value = "m3"; ws.Cell(5, 4).Value = boq.TotalConcreteVolumeM3;
            ws.Cell(6, 1).Value = 2; ws.Cell(6, 2).Value = "Ván khuôn bề mặt tiếp xúc"; ws.Cell(6, 3).Value = "m2"; ws.Cell(6, 4).Value = boq.TotalFormworkAreaM2;
            ws.Cell(7, 1).Value = 3; ws.Cell(7, 2).Value = "Cốt thép d <= 10mm"; ws.Cell(7, 3).Value = "kg"; ws.Cell(7, 4).Value = boq.TotalRebarKgLe10;
            ws.Cell(8, 1).Value = 4; ws.Cell(8, 2).Value = "Cốt thép 10mm < d <= 18mm"; ws.Cell(8, 3).Value = "kg"; ws.Cell(8, 4).Value = boq.TotalRebarKgLe18;
            ws.Cell(9, 1).Value = 5; ws.Cell(9, 2).Value = "Cốt thép d > 18mm"; ws.Cell(9, 3).Value = "kg"; ws.Cell(9, 4).Value = boq.TotalRebarKgGt18;
            ws.Cell(10, 1).Value = 6; ws.Cell(10, 2).Value = "Tổng trọng lượng cốt thép"; ws.Cell(10, 3).Value = "kg"; ws.Cell(10, 4).FormulaA1 = "=SUM(D7:D9)";
            ws.Cell(11, 1).Value = 7; ws.Cell(11, 2).Value = "Tổng trọng lượng cốt thép"; ws.Cell(11, 3).Value = "Tấn"; ws.Cell(11, 4).FormulaA1 = "=D10/1000";

            ws.Range(5, 4, 11, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Range(10, 1, 11, 5).Style.Font.SetBold();
            ws.Columns().AdjustToContents();
        }

        private static void BuildBeTongSheet(IXLWorksheet ws, QuantityTakeoffResult boq)
        {
            ws.Cell(1, 1).Value = "CHI TIẾT KHỐI LƯỢNG BÊ TÔNG THEO CẤU KIỆN";
            ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.SetFontSize(14);

            string[] headers = { "STT", "Tầng", "Cấu kiện", "Ký hiệu", "Số lượng", "Kích thước (b x h x L mm)", "Thể tích (m3)" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(3, c + 1).Value = headers[c];
            ws.Range(3, 1, 3, 7).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(220, 230, 242));

            int row = 4;
            int stt = 1;
            foreach (var el in boq.Elements)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = el.StoryName;
                ws.Cell(row, 3).Value = el.ElementType.ToString();
                ws.Cell(row, 4).Value = el.Mark;
                ws.Cell(row, 5).Value = el.Count;
                ws.Cell(row, 6).Value = $"{el.WidthMm:F0}x{el.HeightMm:F0}x{el.LengthMm:F0}";
                ws.Cell(row, 7).Value = el.ConcreteVolumeM3;
                row++;
            }

            ws.Cell(row, 3).Value = "TỔNG CỘNG";
            ws.Cell(row, 7).FormulaA1 = row > 4 ? $"=SUM(G4:G{row - 1})" : "0";
            ws.Range(row, 1, row, 7).Style.Font.SetBold();
            ws.Range(4, 7, row, 7).Style.NumberFormat.Format = "#,##0.000";
            ws.Columns().AdjustToContents();
        }

        private static void BuildVanKhuonSheet(IXLWorksheet ws, QuantityTakeoffResult boq)
        {
            ws.Cell(1, 1).Value = "CHI TIẾT KHỐI LƯỢNG VÁN KHUÔN THEO CẤU KIỆN";
            ws.Range(1, 1, 1, 7).Merge().Style.Font.SetBold().Font.SetFontSize(14);

            string[] headers = { "STT", "Tầng", "Cấu kiện", "Ký hiệu", "Số lượng", "Kích thước (b x h x L mm)", "Diện tích ván khuôn (m2)" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(3, c + 1).Value = headers[c];
            ws.Range(3, 1, 3, 7).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(235, 241, 222));

            int row = 4;
            int stt = 1;
            foreach (var el in boq.Elements)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = el.StoryName;
                ws.Cell(row, 3).Value = el.ElementType.ToString();
                ws.Cell(row, 4).Value = el.Mark;
                ws.Cell(row, 5).Value = el.Count;
                ws.Cell(row, 6).Value = $"{el.WidthMm:F0}x{el.HeightMm:F0}x{el.LengthMm:F0}";
                ws.Cell(row, 7).Value = el.FormworkAreaM2;
                row++;
            }

            ws.Cell(row, 3).Value = "TỔNG CỘNG";
            ws.Cell(row, 7).FormulaA1 = row > 4 ? $"=SUM(G4:G{row - 1})" : "0";
            ws.Range(row, 1, row, 7).Style.Font.SetBold();
            ws.Range(4, 7, row, 7).Style.NumberFormat.Format = "#,##0.00";
            ws.Columns().AdjustToContents();
        }

        private static void BuildCotThepSheet(IXLWorksheet ws, QuantityTakeoffResult boq)
        {
            ws.Cell(1, 1).Value = "BẢNG THỐNG KÊ VÀ TỔNG HỢP CỐT THÉP THEO ĐƯỜNG KÍNH";
            ws.Range(1, 1, 1, 8).Merge().Style.Font.SetBold().Font.SetFontSize(14);

            string[] headers = { "STT", "Tầng", "Cấu kiện", "Tên thép", "Đường kính d (mm)", "Tổng dài (m)", "Phân nhóm", "Trọng lượng (kg)" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(3, c + 1).Value = headers[c];
            ws.Range(3, 1, 3, 8).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(253, 233, 217));

            int row = 4;
            int stt = 1;
            foreach (var rb in boq.Rebars)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = rb.StoryName;
                ws.Cell(row, 3).Value = rb.ElementType.ToString();
                ws.Cell(row, 4).Value = rb.Mark;
                ws.Cell(row, 5).Value = rb.DiameterMm;
                ws.Cell(row, 6).Value = rb.TotalLengthM;
                ws.Cell(row, 7).Value = rb.Category.ToString();
                ws.Cell(row, 8).Value = rb.WeightKg;
                row++;
            }

            ws.Cell(row, 4).Value = "TỔNG CỘNG (KG)";
            ws.Cell(row, 8).FormulaA1 = row > 4 ? $"=SUM(H4:H{row - 1})" : "0";
            ws.Range(row, 1, row, 8).Style.Font.SetBold();
            ws.Range(4, 6, row, 6).Style.NumberFormat.Format = "#,##0.0";
            ws.Range(4, 8, row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Columns().AdjustToContents();
        }

        private static void BuildChiTietTheoTangSheet(IXLWorksheet ws, QuantityTakeoffResult boq)
        {
            ws.Cell(1, 1).Value = "TỔNG HỢP TIÊN LƯỢNG KHỐI LƯỢNG CHI TIẾT THEO TẦNG";
            ws.Range(1, 1, 1, 9).Merge().Style.Font.SetBold().Font.SetFontSize(14);

            string[] headers = { "STT", "Tên tầng", "Bê tông (m3)", "Ván khuôn (m2)", "Thép d<=10 (kg)", "Thép d<=18 (kg)", "Thép d>18 (kg)", "Tổng thép (kg)", "Tổng thép (Tấn)" };
            for (int c = 0; c < headers.Length; c++)
                ws.Cell(3, c + 1).Value = headers[c];
            ws.Range(3, 1, 3, 9).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromArgb(216, 216, 216));

            int row = 4;
            int stt = 1;
            foreach (var s in boq.StorySummaries)
            {
                ws.Cell(row, 1).Value = stt++;
                ws.Cell(row, 2).Value = s.StoryName;
                ws.Cell(row, 3).Value = s.ConcreteVolumeM3;
                ws.Cell(row, 4).Value = s.FormworkAreaM2;
                ws.Cell(row, 5).Value = s.RebarKgLe10;
                ws.Cell(row, 6).Value = s.RebarKgLe18;
                ws.Cell(row, 7).Value = s.RebarKgGt18;
                ws.Cell(row, 8).FormulaA1 = $"=SUM(E{row}:G{row})";
                ws.Cell(row, 9).FormulaA1 = $"=H{row}/1000";
                row++;
            }

            ws.Cell(row, 2).Value = "TỔNG CỘNG TOÀN NHÀ";
            ws.Cell(row, 3).FormulaA1 = row > 4 ? $"=SUM(C4:C{row - 1})" : "0";
            ws.Cell(row, 4).FormulaA1 = row > 4 ? $"=SUM(D4:D{row - 1})" : "0";
            ws.Cell(row, 5).FormulaA1 = row > 4 ? $"=SUM(E4:E{row - 1})" : "0";
            ws.Cell(row, 6).FormulaA1 = row > 4 ? $"=SUM(F4:F{row - 1})" : "0";
            ws.Cell(row, 7).FormulaA1 = row > 4 ? $"=SUM(G4:G{row - 1})" : "0";
            ws.Cell(row, 8).FormulaA1 = row > 4 ? $"=SUM(H4:H{row - 1})" : "0";
            ws.Cell(row, 9).FormulaA1 = row > 4 ? $"=SUM(I4:I{row - 1})" : "0";

            ws.Range(row, 1, row, 9).Style.Font.SetBold();
            ws.Range(4, 3, row, 9).Style.NumberFormat.Format = "#,##0.00";
            ws.Columns().AdjustToContents();
        }
    }
}
