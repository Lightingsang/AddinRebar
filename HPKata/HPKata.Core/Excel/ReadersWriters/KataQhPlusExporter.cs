using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Configuration;
using HPKata.Core.Excel.Data;
using HPKata.Core.Excel.Session;

namespace HPKata.Core.Excel.ReadersWriters
{
    /// <summary>
    /// Exports rebar fabrication schedule in Iso_QHPlus.xls schema for automated bar cutting/bending plants.
    /// </summary>
    public static class KataQhPlusExporter
    {
        public const string SheetName = "Output_QHP";

        public static bool Export(string? workbookPath, IEnumerable<KataRebarScheduleItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            string targetPath = string.IsNullOrWhiteSpace(workbookPath)
                ? (KataInstallEnvironment.Current.IsoQhPlusPath ?? string.Empty)
                : workbookPath!;

            if (KataExcelSessionProvider.IsDesktopExcelOpen)
            {
                using var session = KataExcelSessionProvider.OpenSession(targetPath, preferCom: true);
                return WriteQhPlusRows(session, items);
            }

            return ExportViaClosedXml(targetPath, items);
        }


        private static bool WriteQhPlusRows(Abstractions.IKataExcelSession session, IEnumerable<KataRebarScheduleItem> items)
        {
            session.EnsureSheet(SheetName);
            int row = 5;
            foreach (var item in items)
            {
                WriteRow(session, row++, item);
            }
            session.Save();
            return true;
        }

        private static void WriteRow(Abstractions.IKataExcelSession session, int row, KataRebarScheduleItem item)
        {
            session.SetCellValue(SheetName, row, 1, item.ComponentMark);
            session.SetCellValue(SheetName, row, 2, item.SubLayer);
            session.SetCellValue(SheetName, row, 3, item.BarMark);
            session.SetCellValue(SheetName, row, 4, item.ShapeCode);

            for (int i = 0; i < 6; i++)
            {
                double seg = (item.SegmentsMm != null && i < item.SegmentsMm.Length) ? item.SegmentsMm[i] : 0.0;
                session.SetCellValue(SheetName, row, 5 + i, seg > 0.0 ? (object)seg : string.Empty);
            }

            session.SetCellValue(SheetName, row, 14, item.DiameterMm);
            session.SetCellValue(SheetName, row, 15, item.CountPerMember);
            session.SetCellValue(SheetName, row, 16, item.MemberCount);
            session.SetCellValue(SheetName, row, 17, item.TotalCount);
            session.SetCellValue(SheetName, row, 18, item.CuttingLengthMm);
            session.SetCellValue(SheetName, row, 19, Math.Round(item.TotalLengthM, 2));
            session.SetCellValue(SheetName, row, 20, Math.Round(item.UnitMassKgPerM, 3));
            session.SetCellValue(SheetName, row, 21, Math.Round(item.TotalWeightKg, 2));
        }

        private static bool ExportViaClosedXml(string targetPath, IEnumerable<KataRebarScheduleItem> items)
        {
            string exportPath = targetPath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase)
                ? Path.ChangeExtension(targetPath, ".xlsx")
                : targetPath;

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(SheetName);

            ws.Cell(4, 1).Value = "Cấu kiện";
            ws.Cell(4, 2).Value = "Cấu kiện phụ";
            ws.Cell(4, 3).Value = "Số hiệu";
            ws.Cell(4, 4).Value = "Hình dạng";
            ws.Cell(4, 14).Value = "Đường kính";
            ws.Cell(4, 15).Value = "SL/CK";
            ws.Cell(4, 16).Value = "Số CK";
            ws.Cell(4, 17).Value = "Tổng SL";
            ws.Cell(4, 18).Value = "Chiều dài (mm)";
            ws.Cell(4, 19).Value = "Tổng dài (m)";
            ws.Cell(4, 20).Value = "Trọng lượng (kg/m)";
            ws.Cell(4, 21).Value = "Tổng TL (kg)";

            int row = 5;
            foreach (var item in items)
            {
                ws.Cell(row, 1).Value = item.ComponentMark;
                ws.Cell(row, 2).Value = item.SubLayer;
                ws.Cell(row, 3).Value = item.BarMark;
                ws.Cell(row, 4).Value = item.ShapeCode;
                ws.Cell(row, 14).Value = item.DiameterMm;
                ws.Cell(row, 15).Value = item.CountPerMember;
                ws.Cell(row, 16).Value = item.MemberCount;
                ws.Cell(row, 17).Value = item.TotalCount;
                ws.Cell(row, 18).Value = item.CuttingLengthMm;
                ws.Cell(row, 19).Value = Math.Round(item.TotalLengthM, 2);
                ws.Cell(row, 20).Value = Math.Round(item.UnitMassKgPerM, 3);
                ws.Cell(row, 21).Value = Math.Round(item.TotalWeightKg, 2);
                row++;
            }

            string? dir = Path.GetDirectoryName(exportPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            workbook.SaveAs(exportPath);
            return true;
        }
    }
}
