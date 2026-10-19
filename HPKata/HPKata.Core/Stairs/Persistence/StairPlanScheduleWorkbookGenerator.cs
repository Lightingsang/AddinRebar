using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Stairs.Calculators;
using HPKata.Core.Stairs.Models;

namespace HPKata.Core.Stairs.Persistence
{
    /// <summary>
    /// Generates ClosedXML Excel workbooks for staircase and lintel schedules matching sheet 'Thang' of Kata.xlsm.
    /// </summary>
    public static class StairPlanScheduleWorkbookGenerator
    {
        public static readonly string[] ColumnHeaders =
        {
            "STT", "Tên Cấu Kiện", "Loại", "Số Cấu Kiện", "Số Bậc / Nhịp",
            "Bề Rộng (mm)", "Chiều Dày/Cao (mm)", "Thép Nhịp / Chủ", "Thép Gối / Giá",
            "Thép Mũi / Đai", "Bê Tông (m3)", "Ván Khuôn (m2)", "Tổng Thép (kg)", "Ghi Chú"
        };

        public static void GenerateWorkbookFile(List<StairReinforcementModel> models, string filePath)
        {
            if (models == null) throw new ArgumentNullException(nameof(models));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path required.", nameof(filePath));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Thang");
            PopulateWorksheet(ws, models);
            workbook.SaveAs(filePath);
        }

        public static void GenerateWorkbookStream(List<StairReinforcementModel> models, Stream stream)
        {
            if (models == null) throw new ArgumentNullException(nameof(models));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Thang");
            PopulateWorksheet(ws, models);
            workbook.SaveAs(stream);
        }

        private static void PopulateWorksheet(IXLWorksheet ws, List<StairReinforcementModel> models)
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

            foreach (var m in models)
            {
                if (m.Flight == null) continue;
                var f = m.Flight;
                var row = ws.Row(rowIdx);

                double vol = StairGeometryCalculator.CalculateConcreteVolume(f);
                double fw = StairGeometryCalculator.CalculateFormworkArea(f);
                var items = StairScheduleAggregator.AggregateFlight(f);
                double totalWeight = 0.0;
                foreach (var it in items) totalWeight += it.TotalWeightKg;

                row.Cell(2).Value = stt++;
                row.Cell(3).Value = f.FlightMark;
                row.Cell(4).Value = "Vế Thang";
                row.Cell(5).Value = f.MemberCount;
                row.Cell(6).Value = $"{f.RiserCount} bậc";
                row.Cell(7).Value = f.FlightWidthMm;
                row.Cell(8).Value = f.ThroatThicknessMm;
                row.Cell(9).Value = $"D{f.BottomTensionDiaMm}a{f.BottomTensionSpacingMm:F0}";
                row.Cell(10).Value = $"D{f.TopNegativeDiaMm}a{f.TopNegativeSpacingMm:F0}";
                row.Cell(11).Value = $"D{f.StepNoseDiaMm} mũi bậc";
                row.Cell(12).Value = Math.Round(vol, 2);
                row.Cell(13).Value = Math.Round(fw, 2);
                row.Cell(14).Value = Math.Round(totalWeight, 1);
                row.Cell(15).Value = f.ConcreteGrade;

                rowIdx++;
            }

            ws.Columns(2, ColumnHeaders.Length + 1).AdjustToContents();
        }
    }
}
