using System;
using System.Collections.Generic;
using System.Linq;
using HPKata.AutoCad.Abstractions;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Pure command runner for diameter summary ('ththep'), coupler summary ('tkco'), and schedule comparison ('sstk').
    /// </summary>
    public static class SummaryCommandRunner
    {
        public static CadTableDto ExecuteSummary(ICadDocumentContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            IReadOnlyList<CadEntityDto> entities = context.GetAllRebarEntities();
            List<RebarItem> items = ScheduleCommandRunner.ExtractRebarItems(entities);

            List<DiameterSummaryItem> summaries = RebarScheduleAggregator.SummarizeByDiameter(items);
            TcvnDiameterBins bins = RebarScheduleAggregator.CalculateTcvnBins(summaries);

            CadTableDto table = BuildSummaryTable(summaries, bins);
            context.PromptPoint("Specify summary table insertion point: ", out CadPoint3D insertionPt);
            context.InsertTable(table, insertionPt);
            context.WriteMessage($"\n[HPKata] Diameter summary generated: {bins.TotalProjectWeightKg:F1} kg total.\n");

            return table;
        }

        public static CadTableDto ExecuteCouplerSummary(ICadDocumentContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            IReadOnlyList<CadEntityDto> entities = context.GetAllRebarEntities();
            List<RebarItem> items = ScheduleCommandRunner.ExtractRebarItems(entities);
            List<CouplerSummaryItem> couplers = RebarScheduleAggregator.ExtractCouplers(items);

            if (couplers.Count == 0)
            {
                context.WriteMessage("\n[HPKata] Không tìm thấy cốt thép nối ren (coupler) trong bản vẽ.\n");
                return new CadTableDto { Title = "BẢNG THỐNG KÊ COUPLER (0 mối nối)" };
            }

            CadTableDto table = BuildCouplerTable(couplers);
            context.PromptPoint("Specify coupler table insertion point: ", out CadPoint3D insertionPt);
            context.InsertTable(table, insertionPt);
            context.WriteMessage($"\n[HPKata] Coupler schedule generated: {couplers.Count} coupler groups.\n");

            return table;
        }

        public static ScheduleComparisonReport ExecuteCompareSchedule(ICadDocumentContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            IReadOnlyList<CadEntityDto> selectedEntities = context.GetSelectedRebarEntities();
            IReadOnlyList<CadEntityDto> allEntities = context.GetAllRebarEntities();

            List<RebarItem> baseItems = ScheduleCommandRunner.ExtractRebarItems(allEntities);
            List<RebarItem> currentItems = (selectedEntities.Count > 0 && selectedEntities.Count != allEntities.Count)
                ? ScheduleCommandRunner.ExtractRebarItems(selectedEntities)
                : baseItems;

            ScheduleComparisonReport report = CompareRebarItems(baseItems, currentItems);

            if (report.ComparisonTable != null)
            {
                context.PromptPoint("Specify comparison table insertion point: ", out CadPoint3D pt);
                context.InsertTable(report.ComparisonTable, pt);
            }

            string diffSummary = report.HasDifferences
                ? $"{report.Differences.Count} differences found, mass diff: {report.WeightDifferenceKg:+0.0;-0.0;0.0} kg"
                : "Schedules match 100% (0 differences)";

            context.WriteMessage($"\n[HPKata] So sánh bảng thống kê (sstk): {diffSummary}.\n");
            return report;
        }

        public static ScheduleComparisonReport CompareRebarItems(
            IReadOnlyList<RebarItem> baseItems,
            IReadOnlyList<RebarItem> currentItems)
        {
            EnsureUnitMasses(baseItems);
            EnsureUnitMasses(currentItems);

            var report = new ScheduleComparisonReport
            {
                BaseItemCount = baseItems?.Count ?? 0,
                CurrentItemCount = currentItems?.Count ?? 0,
                BaseTotalWeightKg = baseItems?.Sum(i => i.TotalWeightKg) ?? 0.0,
                CurrentTotalWeightKg = currentItems?.Sum(i => i.TotalWeightKg) ?? 0.0
            };

            var baseMap = (baseItems ?? Array.Empty<RebarItem>())
                .GroupBy(i => (Comp: i.ComponentMark ?? string.Empty, Mark: i.BarMark ?? string.Empty))
                .ToDictionary(g => g.Key, g => g.First());

            var currMap = (currentItems ?? Array.Empty<RebarItem>())
                .GroupBy(i => (Comp: i.ComponentMark ?? string.Empty, Mark: i.BarMark ?? string.Empty))
                .ToDictionary(g => g.Key, g => g.First());

            DetectScheduleDifferences(baseMap, currMap, report);
            report.ComparisonTable = BuildComparisonTable(baseMap, currMap, report);
            return report;
        }

        private static void EnsureUnitMasses(IEnumerable<RebarItem>? items)
        {
            if (items == null) return;
            foreach (RebarItem item in items)
            {
                if (item.UnitMassKgPerM <= 0.0 && item.DiameterMm > 0)
                {
                    item.UnitMassKgPerM = RebarCalculator.GetUnitMass(item.DiameterMm);
                }
            }
        }

        private static void DetectScheduleDifferences(
            Dictionary<(string Comp, string Mark), RebarItem> baseMap,
            Dictionary<(string Comp, string Mark), RebarItem> currMap,
            ScheduleComparisonReport report)
        {
            foreach (var kvp in baseMap)
            {
                if (!currMap.TryGetValue(kvp.Key, out RebarItem? curr))
                {
                    report.Differences.Add($"Removed mark {kvp.Key.Mark} in '{kvp.Key.Comp}': {kvp.Value.TotalCount}xØ{kvp.Value.DiameterMm}");
                }
                else if (curr.DiameterMm != kvp.Value.DiameterMm ||
                         curr.TotalCount != kvp.Value.TotalCount ||
                         Math.Abs(curr.CuttingLengthMm - kvp.Value.CuttingLengthMm) > 1.0)
                {
                    report.Differences.Add(
                        $"Modified mark {kvp.Key.Mark} in '{kvp.Key.Comp}': " +
                        $"{kvp.Value.TotalCount}xØ{kvp.Value.DiameterMm} (L={kvp.Value.CuttingLengthMm:F0}) -> " +
                        $"{curr.TotalCount}xØ{curr.DiameterMm} (L={curr.CuttingLengthMm:F0})");
                }
            }

            foreach (var kvp in currMap)
            {
                if (!baseMap.ContainsKey(kvp.Key))
                {
                    report.Differences.Add($"Added mark {kvp.Key.Mark} in '{kvp.Key.Comp}': {kvp.Value.TotalCount}xØ{kvp.Value.DiameterMm}");
                }
            }
        }

        private static CadTableDto BuildComparisonTable(
            Dictionary<(string Comp, string Mark), RebarItem> baseMap,
            Dictionary<(string Comp, string Mark), RebarItem> currMap,
            ScheduleComparisonReport report)
        {
            var table = new CadTableDto { Title = "BẢNG ĐỐI SOÁT SO SÁNH THỐNG KÊ (SSTK)" };
            table.ColumnHeaders.AddRange(new[]
            {
                "STT", "CẤU KIỆN", "SỐ HIỆU", "Ø GỐC", "Ø MỚI", "SL GỐC", "SL MỚI", "KL GỐC (kg)", "KL MỚI (kg)", "CHÊNH LỆCH (kg)", "TRẠNG THÁI"
            });

            var allKeys = baseMap.Keys.Union(currMap.Keys).OrderBy(k => k.Comp).ThenBy(k => k.Mark);
            int idx = 1;
            foreach (var k in allKeys)
            {
                baseMap.TryGetValue(k, out RebarItem? b);
                currMap.TryGetValue(k, out RebarItem? c);

                string status = (b != null && c != null)
                    ? (b.DiameterMm == c.DiameterMm && b.TotalCount == c.TotalCount ? "Khớp 100%" : "Thay đổi")
                    : (b != null ? "Đã xóa" : "Thêm mới");

                double bWt = b?.TotalWeightKg ?? 0.0;
                double cWt = c?.TotalWeightKg ?? 0.0;
                double diffWt = cWt - bWt;

                table.AddRow(
                    idx++.ToString(),
                    k.Comp,
                    k.Mark,
                    b != null ? $"Ø{b.DiameterMm}" : "-",
                    c != null ? $"Ø{c.DiameterMm}" : "-",
                    b?.TotalCount.ToString() ?? "-",
                    c?.TotalCount.ToString() ?? "-",
                    bWt.ToString("F1"),
                    cWt.ToString("F1"),
                    diffWt.ToString("+0.0;-0.0;0.0"),
                    status
                );
            }

            table.AddRow("TỔNG", "-", "-", "-", "-", "-", "-",
                report.BaseTotalWeightKg.ToString("F1"),
                report.CurrentTotalWeightKg.ToString("F1"),
                report.WeightDifferenceKg.ToString("+0.0;-0.0;0.0"),
                report.HasDifferences ? "Có chênh lệch" : "Khớp 100%");

            return table;
        }

        private static CadTableDto BuildSummaryTable(IReadOnlyList<DiameterSummaryItem> summaries, TcvnDiameterBins bins)
        {
            var table = new CadTableDto { Title = "TỔNG HỢP CỐT THÉP" };
            table.ColumnHeaders.AddRange(new[]
            {
                "Đường kính Ø (mm)", "Tổng chiều dài (m)", "Trọng lượng đơn vị (kg/m)", "Tổng trọng lượng (kg)"
            });

            foreach (DiameterSummaryItem s in summaries)
            {
                table.AddRow(
                    $"Ø{s.DiameterMm}",
                    s.TotalLengthM.ToString("F2"),
                    s.UnitMassKgPerM.ToString("F3"),
                    s.TotalWeightKg.ToString("F1")
                );
            }

            table.AddRow("Thép tròn trơn (<= 10mm)", "-", "-", bins.WireLe10Kg.ToString("F1"));
            table.AddRow("Thép gờ (10 < Ø <= 18mm)", "-", "-", bins.Deformed10To18Kg.ToString("F1"));
            table.AddRow("Thép gờ (Ø > 18mm)", "-", "-", bins.DeformedGt18Kg.ToString("F1"));
            table.AddRow("TỔNG CỘNG TOÀN DỰ ÁN", "-", "-", bins.TotalProjectWeightKg.ToString("F1"));

            return table;
        }

        private static CadTableDto BuildCouplerTable(IReadOnlyList<CouplerSummaryItem> couplers)
        {
            var table = new CadTableDto { Title = "BẢNG THỐNG KÊ COUPLER (MỐI NỐI REN)" };
            table.ColumnHeaders.AddRange(new[]
            {
                "STT", "CẤU KIỆN", "Ø (mm)", "SỐ CK", "SL 1 CK", "TỔNG SỐ LƯỢNG", "GHI CHÚ"
            });

            int idx = 1;
            foreach (CouplerSummaryItem c in couplers)
            {
                table.AddRow(
                    idx++.ToString(),
                    c.ComponentMark,
                    $"Ø{c.DiameterMm}",
                    c.MemberCount.ToString(),
                    c.CouplersPerMember.ToString(),
                    c.TotalCouplers.ToString(),
                    c.Notes
                );
            }
            return table;
        }
    }

    public class ScheduleComparisonReport
    {
        public int BaseItemCount { get; set; }
        public int CurrentItemCount { get; set; }
        public double BaseTotalWeightKg { get; set; }
        public double CurrentTotalWeightKg { get; set; }
        public double WeightDifferenceKg => CurrentTotalWeightKg - BaseTotalWeightKg;
        public List<string> Differences { get; } = new();
        public CadTableDto? ComparisonTable { get; set; }
        public bool HasDifferences => Differences.Count > 0 || Math.Abs(WeightDifferenceKg) > 0.05;
    }
}
