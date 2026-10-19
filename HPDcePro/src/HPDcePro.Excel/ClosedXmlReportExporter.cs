using System;
using System.IO;
using ClosedXML.Excel;

namespace HPDcePro.Excel
{
    public sealed class BeamDesignReportData
    {
        public string BeamMark { get; set; } = "D1";
        public string StoryName { get; set; } = "TẦNG 2";
        public double WidthMm { get; set; } = 220.0;
        public double HeightMm { get; set; } = 400.0;
        public double SpanMm { get; set; } = 5000.0;
        public double CoverMm { get; set; } = 40.0;
        public string ConcreteGrade { get; set; } = "B25";
        public string SteelGrade { get; set; } = "CB400-V";
        public double DesignMomentKnm { get; set; } = 80.0;
        public double DesignShearKn { get; set; } = 60.0;
        public double AsRequiredMm2 { get; set; } = 612.0;
        public double AsProvidedMm2 { get; set; } = 763.0;
        public double ReinforcementRatioPercent { get; set; } = 0.96;
        public double MomentCapacityKnm { get; set; } = 98.5;
        public double ShearCapacityKn { get; set; } = 78.2;
        public bool IsFlexureCompliant { get; set; } = true;
        public bool IsShearCompliant { get; set; } = true;
        public string OverallStatus => (IsFlexureCompliant && IsShearCompliant) ? "PASS" : "FAIL";
    }

    /// <summary>
    /// Headless beam design calculation report exporter (.xlsx) per TCVN 5574:2018.
    /// </summary>
    public static class ClosedXmlReportExporter
    {
        public static void ExportToFile(BeamDesignReportData data, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));
            using var workbook = CreateWorkbook(data);
            workbook.SaveAs(filePath);
        }

        public static void ExportToStream(BeamDesignReportData data, Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using var workbook = CreateWorkbook(data);
            workbook.SaveAs(stream);
        }

        public static XLWorkbook CreateWorkbook(BeamDesignReportData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("ThuyetMinhDam");

            WriteTitle(ws);
            WriteGeometrySection(ws, data);
            WriteInternalForcesSection(ws, data);
            WriteFlexuralCheckSection(ws, data);
            WriteShearCheckSection(ws, data);
            WriteConclusionSection(ws, data);

            ws.Columns().AdjustToContents();
            return workbook;
        }

        private static void WriteTitle(IXLWorksheet ws)
        {
            ws.Cell(1, 1).Value = "THUYẾT MINH TÍNH TOÁN KẾT CẤU DẦM BTCT";
            ws.Range(1, 1, 1, 5).Merge();
            ws.Cell(1, 1).Style.Font.Bold = true;
            ws.Cell(1, 1).Style.Font.FontSize = 16;
            ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(2, 1).Value = "Tiêu chuẩn áp dụng: TCVN 5574:2018 - Thiết kế kết cấu bê tông và bê tông cốt thép";
            ws.Range(2, 1, 2, 5).Merge();
            ws.Cell(2, 1).Style.Font.Italic = true;
            ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void WriteGeometrySection(IXLWorksheet ws, BeamDesignReportData data)
        {
            ws.Cell(4, 1).Value = "1. THÔNG SỐ HÌNH HỌC VÀ VẬT LIỆU";
            ws.Cell(4, 1).Style.Font.Bold = true;

            ws.Cell(5, 1).Value = "Cấu kiện / Tầng:";
            ws.Cell(5, 2).Value = $"{data.BeamMark} ({data.StoryName})";
            ws.Cell(6, 1).Value = "Tiết diện b x h (mm):";
            ws.Cell(6, 2).Value = $"{data.WidthMm} x {data.HeightMm}";
            ws.Cell(7, 1).Value = "Nhịp dầm L (mm):";
            ws.Cell(7, 2).Value = data.SpanMm;
            ws.Cell(8, 1).Value = "Vật liệu:";
            ws.Cell(8, 2).Value = $"Bê tông {data.ConcreteGrade}, Cốt thép {data.SteelGrade}";
        }

        private static void WriteInternalForcesSection(IXLWorksheet ws, BeamDesignReportData data)
        {
            ws.Cell(10, 1).Value = "2. NỘI LỰC TÍNH TOÁN (ULS)";
            ws.Cell(10, 1).Style.Font.Bold = true;

            ws.Cell(11, 1).Value = "Momen uốn thiết kế M_Ed (kNm):";
            ws.Cell(11, 2).Value = data.DesignMomentKnm;
            ws.Cell(12, 1).Value = "Lực cắt thiết kế V_Ed (kN):";
            ws.Cell(12, 2).Value = data.DesignShearKn;
        }

        private static void WriteFlexuralCheckSection(IXLWorksheet ws, BeamDesignReportData data)
        {
            ws.Cell(14, 1).Value = "3. KIỂM TRA KHẢ NĂNG CHỊU UỐN";
            ws.Cell(14, 1).Style.Font.Bold = true;

            ws.Cell(15, 1).Value = "Diện tích thép yêu cầu As,req (mm²):";
            ws.Cell(15, 2).Value = data.AsRequiredMm2;
            ws.Cell(16, 1).Value = "Diện tích thép bố trí As,prov (mm²):";
            ws.Cell(16, 2).Value = data.AsProvidedMm2;
            ws.Cell(17, 1).Value = "Hàm lượng cốt thép mu (%):";
            ws.Cell(17, 2).Value = $"{data.ReinforcementRatioPercent:F2}%";
            ws.Cell(18, 1).Value = "Khả năng chịu momen M_Rd (kNm):";
            ws.Cell(18, 2).Value = data.MomentCapacityKnm;
            ws.Cell(19, 1).Value = "Điều kiện uốn (M_Rd >= M_Ed):";
            ws.Cell(19, 2).Value = data.IsFlexureCompliant ? "ĐẠT (PASS)" : "KHÔNG ĐẠT (FAIL)";
            ws.Cell(19, 2).Style.Font.Bold = true;
        }

        private static void WriteShearCheckSection(IXLWorksheet ws, BeamDesignReportData data)
        {
            ws.Cell(21, 1).Value = "4. KIỂM TRA KHẢ NĂNG CHỊU CẮT";
            ws.Cell(21, 1).Style.Font.Bold = true;

            ws.Cell(22, 1).Value = "Khả năng chịu cắt tổng Vu (kN):";
            ws.Cell(22, 2).Value = data.ShearCapacityKn;
            ws.Cell(23, 1).Value = "Điều kiện cắt (V_Rd >= V_Ed):";
            ws.Cell(23, 2).Value = data.IsShearCompliant ? "ĐẠT (PASS)" : "KHÔNG ĐẠT (FAIL)";
            ws.Cell(23, 2).Style.Font.Bold = true;
        }

        private static void WriteConclusionSection(IXLWorksheet ws, BeamDesignReportData data)
        {
            ws.Cell(25, 1).Value = "5. KẾT LUẬN CHUNG";
            ws.Cell(25, 1).Style.Font.Bold = true;

            ws.Cell(26, 1).Value = "Trạng thái cấu kiện:";
            ws.Cell(26, 2).Value = data.OverallStatus == "PASS"
                ? "THỎA MÃN ĐIỀU KIỆN CHỊU LỰC TCVN 5574:2018"
                : "KHÔNG THỎA MÃN - CẦN ĐIỀU CHỈNH TIẾT DIỆN HOẶC BỐ TRÍ THÉP";
            ws.Cell(26, 2).Style.Font.Bold = true;
        }
    }
}
