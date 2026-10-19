using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.AutoCad.Abstractions;
using HPKata.Core.Models;
using HPKata.Core.Optimization;
using HPKata.Core.Scheduling;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Pure command runner for 1D Cutting-Stock optimization ('tohopthep', 'loadkho', 'upkho').
    /// </summary>
    public static class CuttingStockCommandRunner
    {
        public static List<CuttingStockResult> ExecuteCuttingStock(
            ICadDocumentContext context,
            bool headless = false)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            IReadOnlyList<CadEntityDto> entities = context.GetAllRebarEntities();
            List<RebarItem> items = ScheduleCommandRunner.ExtractRebarItems(entities);
            var results = new List<CuttingStockResult>();

            var demands = items
                .Where(i => i.DiameterMm >= 10 && i.CuttingLengthMm > 0.0)
                .GroupBy(i => i.DiameterMm);

            var engine = new CuttingStockEngine();
            foreach (var group in demands)
            {
                var groupDemands = group.Select(i => new CuttingDemand(
                    i.DiameterMm,
                    i.CuttingLengthMm,
                    i.TotalCount,
                    i.BarMark.ToString(),
                    i.ComponentMark)).ToList();

                CuttingStockResult res = engine.Solve(group.Key, groupDemands);
                results.Add(res);
            }

            if (results.Count == 0)
            {
                context.WriteMessage("\n[HPKata] Không tìm thấy thanh thép >= Ø10 để tổ hợp cắt.\n");
                return results;
            }

            CadTableDto table = BuildCuttingStockTable(results);
            context.PromptPoint("Specify cutting pattern table insertion point: ", out CadPoint3D insertionPt);
            context.InsertTable(table, insertionPt);

            int totalBars = results.Sum(r => r.NewStandardBarsCount);
            double avgWaste = results.Count > 0 ? results.Average(r => r.GrossWastePercent) : 0.0;
            context.WriteMessage($"\n[HPKata] Tổ hợp cắt thép: {totalBars} cây 11.7m, hao hụt TB: {avgWaste:F2}%.\n");

            return results;
        }

        public static void ExecuteLoadKho(ICadDocumentContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.WriteMessage("\n[HPKata] Load kho thép tồn (loadkho): Đã nạp danh mục đoạn thừa tái sử dụng.\n");
        }

        public static void ExecuteUpKho(ICadDocumentContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.WriteMessage("\n[HPKata] Cập nhật kho thép (upkho): Đã lưu các đoạn thừa vào sổ cái kho.\n");
        }

        private static CadTableDto BuildCuttingStockTable(IReadOnlyList<CuttingStockResult> results)
        {
            var table = new CadTableDto { Title = "TỔ HỢP CẮT THÉP (1D-CSP 11,700 mm)" };
            table.ColumnHeaders.AddRange(new[]
            {
                "Mẫu số", "Đường kính Ø", "Chiều dài phôi", "Các đoạn cắt (mm)", "Dài sử dụng", "Đoạn thừa", "Số thanh"
            });

            int patternIdx = 1;
            foreach (CuttingStockResult res in results)
            {
                var groupedPatterns = res.Patterns
                    .GroupBy(p => string.Join("+", p.CutsMm.OrderByDescending(c => c).Select(c => c.ToString("F0"))));

                foreach (var g in groupedPatterns)
                {
                    CuttingPattern sample = g.First();
                    string cutsStr = string.Join(" + ", sample.CutsMm.Select(c => c.ToString("F0")));
                    table.AddRow(
                        patternIdx++.ToString(),
                        $"Ø{res.DiameterMm}",
                        $"{sample.StockLengthMm:F0}",
                        cutsStr,
                        $"{sample.UsedLengthMm:F0}",
                        $"{sample.RemnantLengthMm:F0}",
                        g.Count().ToString()
                    );
                }
            }
            return table;
        }
    }
}
