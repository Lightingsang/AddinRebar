using System;
using System.Collections.Generic;
using HPKata.AutoCad.Abstractions;
using HPKata.Core.Calculators;
using HPKata.Core.Models;
using HPKata.Core.Scheduling;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Pure command runner for rebar schedule aggregation ('tkt' and 'suatk').
    /// Takes ICadDocumentContext for 100% headless testability.
    /// </summary>
    public static class ScheduleCommandRunner
    {
        public static CadTableDto ExecuteSchedule(
            ICadDocumentContext context,
            bool interactive = false,
            bool applyBendDeduction = false)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            IReadOnlyList<CadEntityDto> entities = context.GetSelectedRebarEntities();
            if (entities == null || entities.Count == 0)
            {
                entities = context.GetAllRebarEntities();
            }

            var items = ExtractRebarItems(entities);
            RebarScheduleReport report = RebarScheduleAggregator.GenerateReport(items, applyBendDeduction);

            CadTableDto table = BuildScheduleTable(report);
            context.PromptPoint("Specify table insertion point: ", out CadPoint3D insertionPt);
            context.InsertTable(table, insertionPt);
            context.WriteMessage($"\n[HPKata] Rebar schedule generated: {report.Items.Count} items, {report.Bins.TotalProjectWeightKg:F1} kg.\n");

            return table;
        }

        public static List<RebarItem> ExtractRebarItems(IEnumerable<CadEntityDto> entities)
        {
            var items = new List<RebarItem>();
            if (entities == null) return items;

            foreach (CadEntityDto dto in entities)
            {
                if (dto.XDataRecord == null || dto.XDataRecord.Phi <= 0) continue;

                int count = ParseCount(dto.XDataRecord.SoLuong);
                int members = Math.Max(1, (int)dto.XDataRecord.SoLuongCauKien);
                double length = dto.TotalLengthMm > 0.0 ? dto.TotalLengthMm : SumSegmentLengths(dto.XDataRecord.ChieuDaiCacDoanMm);

                items.Add(new RebarItem
                {
                    ShapeCode = dto.XDataRecord.Dang ?? "00",
                    BarMark = dto.XDataRecord.STT.ToString(),
                    DiameterMm = dto.XDataRecord.Phi,
                    Spacing = dto.XDataRecord.KhoangCach ?? string.Empty,
                    CountPerMember = count,
                    MemberCount = members,
                    CuttingLengthMm = length,
                    UnitMassKgPerM = RebarCalculator.GetUnitMass(dto.XDataRecord.Phi),
                    ComponentMark = dto.XDataRecord.TenCauKien ?? string.Empty,
                    Notes = dto.XDataRecord.ChuThich ?? string.Empty,
                    ParametersMm = dto.XDataRecord.ChieuDaiCacDoanMm ?? Array.Empty<double>()
                });
            }
            return items;
        }

        private static CadTableDto BuildScheduleTable(RebarScheduleReport report)
        {
            var table = new CadTableDto { Title = "BẢNG THỐNG KÊ CỐT THÉP" };
            table.ColumnHeaders.AddRange(new[]
            {
                "STT", "CẤU KIỆN", "SỐ HIỆU", "HÌNH DẠNG", "ĐK Ø", "DÀI 1 THANH", "SỐ CK", "SL 1 CK", "TỔNG SL", "TỔNG DÀI (m)", "TRỌNG LƯỢNG (kg)"
            });

            int rowIdx = 1;
            foreach (RebarItem item in report.Items)
            {
                table.AddRow(
                    rowIdx++.ToString(),
                    item.ComponentMark,
                    item.BarMark.ToString(),
                    item.ShapeCode,
                    $"Ø{item.DiameterMm}",
                    item.CuttingLengthMm.ToString("F0"),
                    item.MemberCount.ToString(),
                    item.CountPerMember.ToString(),
                    item.TotalCount.ToString(),
                    item.TotalLengthM.ToString("F2"),
                    item.TotalWeightKg.ToString("F1")
                );
            }
            return table;
        }

        private static int ParseCount(string? countStr)
        {
            if (string.IsNullOrWhiteSpace(countStr)) return 1;
            return int.TryParse(countStr.Trim(), out int val) && val > 0 ? val : 1;
        }

        private static int ExtractSpacing(string? spacingStr)
        {
            if (string.IsNullOrWhiteSpace(spacingStr)) return 0;
            string clean = spacingStr.Trim().TrimStart('@', 'a', 'A');
            return int.TryParse(clean, out int val) ? val : 0;
        }

        private static double SumSegmentLengths(IReadOnlyList<double>? lengths)
        {
            if (lengths == null || lengths.Count == 0) return 0.0;
            double sum = 0.0;
            for (int i = 0; i < lengths.Count; i++) sum += lengths[i];
            return sum;
        }
    }
}
