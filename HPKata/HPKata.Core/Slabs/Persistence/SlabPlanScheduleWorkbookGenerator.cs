using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Slabs.Models;
using HPKata.Core.Slabs.Persistence;

namespace HPKata.Core.Slabs.Persistence
{
    /// <summary>
    /// Generates ClosedXML Excel workbooks for slab schedules matching sheet 'San' of Kata.xlsm.
    /// </summary>
    public static class SlabPlanScheduleWorkbookGenerator
    {
        public static readonly string[] ColumnHeaders =
        {
            "STT", "Tên Ô Sàn", "Số Cấu Kiện", "Nhịp Lx (mm)", "Nhịp Ly (mm)",
            "Chiều Dày hs (mm)", "Lớp B1 (Đáy X)", "Lớp B2 (Đáy Y)", "Thép Mũ Gối",
            "Thép Chân Chó", "Bê Tông (m3)", "Ván Khuôn (m2)", "Tổng Thép (kg)", "Ghi Chú"
        };

        public static void GenerateWorkbookFile(List<SlabRebarLayout> layouts, string filePath)
        {
            if (layouts == null) throw new ArgumentNullException(nameof(layouts));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path required.", nameof(filePath));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("San");
            PopulateWorksheet(ws, layouts);
            workbook.SaveAs(filePath);
        }

        public static void GenerateWorkbookStream(List<SlabRebarLayout> layouts, Stream stream)
        {
            if (layouts == null) throw new ArgumentNullException(nameof(layouts));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("San");
            PopulateWorksheet(ws, layouts);
            workbook.SaveAs(stream);
        }

        private static void PopulateWorksheet(IXLWorksheet ws, List<SlabRebarLayout> layouts)
        {
            for (int c = 0; c < ColumnHeaders.Length; c++)
            {
                var cell = ws.Cell(2, c + 2);
                cell.Value = ColumnHeaders[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int rowIdx = 3;
            int stt = 1;

            foreach (var layout in layouts)
            {
                if (layout.Panel == null) continue;
                var p = layout.Panel;
                var row = ws.Row(rowIdx);

                double vol = (p.SpanXMm * p.SpanYMm * p.ThicknessMm) / 1e9 * p.MemberCount;
                double fw = (p.SpanXMm * p.SpanYMm) / 1e6 * p.MemberCount;
                var items = SlabScheduleAggregator.AggregatePanel(p);
                double totalWeight = 0.0;
                foreach (var it in items) totalWeight += it.TotalWeightKg;

                row.Cell(2).Value = stt++;
                row.Cell(3).Value = p.PanelMark;
                row.Cell(4).Value = p.MemberCount;
                row.Cell(5).Value = p.SpanXMm;
                row.Cell(6).Value = p.SpanYMm;
                row.Cell(7).Value = p.ThicknessMm;
                row.Cell(8).Value = $"D{p.BottomMeshXDiaMm}a{p.BottomMeshXSpacingMm:F0}";
                row.Cell(9).Value = $"D{p.BottomMeshYDiaMm}a{p.BottomMeshYSpacingMm:F0}";
                row.Cell(10).Value = $"D{p.TopSupportDiaMm}a{p.TopSupportSpacingMm:F0}";
                row.Cell(11).Value = "D10 (1.2/m2)";
                row.Cell(12).Value = Math.Round(vol, 2);
                row.Cell(13).Value = Math.Round(fw, 2);
                row.Cell(14).Value = Math.Round(totalWeight, 1);
                row.Cell(15).Value = p.ConcreteGrade;

                rowIdx++;
            }

            ws.Columns(2, ColumnHeaders.Length + 1).AdjustToContents();
        }
    }
}
