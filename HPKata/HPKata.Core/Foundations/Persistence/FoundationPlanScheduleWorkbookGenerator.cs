using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using HPKata.Core.Foundations.Calculators;
using HPKata.Core.Foundations.Models;

namespace HPKata.Core.Foundations.Persistence
{
    /// <summary>
    /// Generates ClosedXML Excel workbooks for foundation plan schedules matching sheet 'Mong' of Kata.xlsm.
    /// </summary>
    public static class FoundationPlanScheduleWorkbookGenerator
    {
        public static readonly string[] ColumnHeaders =
        {
            "STT", "Tên Móng", "Loại Móng", "Số Cấu Kiện",
            "Kích Thước L (mm)", "Kích Thước B (mm)", "Chiều Cao H (mm)",
            "Số Cọc", "Quy Cách Cọc", "Thép Đáy X", "Thép Đáy Y",
            "Thép Lớp Trên", "Thép Đai Thành", "Thép Chờ Cột",
            "Bê Tông (m3)", "Bê Tông Lót (m3)", "Ván Khuôn (m2)",
            "Tổng Thép (kg)", "Ghi Chú"
        };

        public static void GenerateWorkbookFile(List<FoundationRebarLayout> layouts, string filePath)
        {
            if (layouts == null) throw new ArgumentNullException(nameof(layouts));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Path required.", nameof(filePath));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Mong");
            PopulateWorksheet(ws, layouts);
            workbook.SaveAs(filePath);
        }

        public static void GenerateWorkbookStream(List<FoundationRebarLayout> layouts, Stream stream)
        {
            if (layouts == null) throw new ArgumentNullException(nameof(layouts));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Mong");
            PopulateWorksheet(ws, layouts);
            workbook.SaveAs(stream);
        }

        private static void PopulateWorksheet(IXLWorksheet ws, List<FoundationRebarLayout> layouts)
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
                var row = ws.Row(rowIdx);
                row.Cell(2).Value = stt++;
                row.Cell(3).Value = layout.FoundationMark;

                if (layout.IsPileCap && layout.PileCap != null)
                {
                    PopulatePileCapRow(row, layout.PileCap);
                }
                else if (layout.Footing != null)
                {
                    PopulateFootingRow(row, layout.Footing);
                }

                rowIdx++;
            }

            ws.Columns(2, ColumnHeaders.Length + 1).AdjustToContents();
        }

        private static void PopulatePileCapRow(IXLRow row, PileCapModel cap)
        {
            double vol = FootingGeometryCalculator.CalculatePileCapVolume(cap);
            double fw = FootingGeometryCalculator.CalculatePileCapFormwork(cap);
            double leanVol = ((cap.LengthMm + 200.0) * (cap.WidthMm + 200.0) * 100.0) / 1e9 * cap.MemberCount;
            var items = FoundationScheduleAggregator.AggregatePileCap(cap);
            double totalWeight = 0.0;
            foreach (var it in items) totalWeight += it.TotalWeightKg;

            row.Cell(4).Value = cap.IsStepped ? "Đài Giật Cấp" : "Đài Cọc";
            row.Cell(5).Value = cap.MemberCount;
            row.Cell(6).Value = cap.LengthMm;
            row.Cell(7).Value = cap.WidthMm;
            row.Cell(8).Value = cap.TotalHeightMm;
            row.Cell(9).Value = cap.Piles.Count;
            row.Cell(10).Value = $"D{cap.PileDiameterMm:F0}";
            row.Cell(11).Value = $"D{cap.BottomMeshXDiaMm}a{cap.BottomMeshXSpacingMm:F0}";
            row.Cell(12).Value = $"D{cap.BottomMeshYDiaMm}a{cap.BottomMeshYSpacingMm:F0}";
            row.Cell(13).Value = cap.HasTopMesh ? $"D{cap.TopMeshXDiaMm}a{cap.TopMeshXSpacingMm:F0}" : "-";
            row.Cell(14).Value = cap.HasSkinTies ? $"D{cap.SkinTieDiaMm}a{cap.SkinTieSpacingMm:F0}" : "-";
            row.Cell(15).Value = $"{cap.StarterDowelCount}D{cap.StarterDowelDiaMm}";
            row.Cell(16).Value = Math.Round(vol, 2);
            row.Cell(17).Value = Math.Round(leanVol, 2);
            row.Cell(18).Value = Math.Round(fw, 2);
            row.Cell(19).Value = Math.Round(totalWeight, 1);
            row.Cell(20).Value = cap.ConcreteGrade;
        }

        private static void PopulateFootingRow(IXLRow row, IsolatedFootingModel f)
        {
            double vol = FootingGeometryCalculator.CalculateFootingVolume(f);
            double fw = FootingGeometryCalculator.CalculateFootingFormwork(f);
            double leanVol = FootingGeometryCalculator.CalculateLeanConcreteVolume(f);
            var items = FoundationScheduleAggregator.AggregateFooting(f);
            double totalWeight = 0.0;
            foreach (var it in items) totalWeight += it.TotalWeightKg;

            row.Cell(4).Value = "Móng Đơn";
            row.Cell(5).Value = f.MemberCount;
            row.Cell(6).Value = f.LengthMm;
            row.Cell(7).Value = f.WidthMm;
            row.Cell(8).Value = f.TotalHeightMm;
            row.Cell(9).Value = 0;
            row.Cell(10).Value = "-";
            row.Cell(11).Value = $"D{f.BottomMeshXDiaMm}a{f.BottomMeshXSpacingMm:F0}";
            row.Cell(12).Value = $"D{f.BottomMeshYDiaMm}a{f.BottomMeshYSpacingMm:F0}";
            row.Cell(13).Value = "-";
            row.Cell(14).Value = "-";
            row.Cell(15).Value = $"{f.StarterDowelCount}D{f.StarterDowelDiaMm}";
            row.Cell(16).Value = Math.Round(vol, 2);
            row.Cell(17).Value = Math.Round(leanVol, 2);
            row.Cell(18).Value = Math.Round(fw, 2);
            row.Cell(19).Value = Math.Round(totalWeight, 1);
            row.Cell(20).Value = f.ConcreteGrade;
        }
    }
}
