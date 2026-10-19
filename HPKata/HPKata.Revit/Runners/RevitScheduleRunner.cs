using HPKata.Core.Models;
using HPKata.Core.Scheduling;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Bridge;

namespace HPKata.Revit.Runners;

/// <summary>
/// Domain runner generating rebar schedule tables and diameter summaries from a Revit document.
/// </summary>
public static class RevitScheduleRunner
{
    private static readonly string[] TableHeaders =
    [
        "STT",
        "Cấu kiện",
        "Số hiệu",
        "Hình dạng",
        "Đường kính (mm)",
        "Số lượng",
        "Chiều dài 1 thanh (mm)",
        "Tổng chiều dài (m)",
        "Trọng lượng (kg)"
    ];

    /// <summary>
    /// Executes rebar schedule aggregation on the document context and creates a schedule table.
    /// </summary>
    public static RevitScheduleTableDto Execute(
        IRevitDocumentContext context,
        bool selectedOnly = false,
        bool applyBendDeduction = false)
    {
        var dtos = selectedOnly && context.GetSelectedRebars().Count > 0
            ? context.GetSelectedRebars()
            : context.GetAllRebars();

        if (dtos.Count == 0)
        {
            context.WriteNotification("Không tìm thấy cốt thép trong mô hình/lựa chọn.");
            return new RevitScheduleTableDto();
        }

        var items = new List<RebarItem>(dtos.Count);
        foreach (var dto in dtos)
        {
            items.Add(RevitParameterBridge.ToRebarItem(dto));
        }

        var report = RebarScheduleAggregator.GenerateReport(items, applyBendDeduction);
        var table = BuildTableDto(report);

        context.CreateOrUpdateScheduleTable(table);
        context.WriteNotification(
            $"Đã lập bảng thống kê {table.TotalBarCount} thanh thép. Tổng trọng lượng: {table.TotalWeightKg:F2} kg.");

        return table;
    }

    private static RevitScheduleTableDto BuildTableDto(RebarScheduleReport report)
    {
        var rows = new List<IReadOnlyList<string>>(report.Items.Count);
        int index = 1;
        double totalWeight = 0.0;
        int totalCount = 0;

        foreach (var item in report.Items)
        {
            totalWeight += item.TotalWeightKg;
            totalCount += item.TotalCount;

            rows.Add(
            [
                index.ToString(),
                item.ComponentMark,
                item.BarMark,
                item.ShapeCode,
                item.DiameterMm.ToString(),
                item.TotalCount.ToString(),
                item.CuttingLengthMm.ToString("F0"),
                item.TotalLengthM.ToString("F2"),
                item.TotalWeightKg.ToString("F2")
            ]);
            index++;
        }

        var weightByDia = report.DiameterSummaries.ToDictionary(
            s => $"D{s.DiameterMm}",
            s => s.TotalWeightKg);

        return new RevitScheduleTableDto
        {
            Title = "BẢNG THỐNG KÊ CỐT THÉP",
            Headers = TableHeaders,
            Rows = rows,
            TotalWeightKg = Math.Round(totalWeight, 2),
            TotalBarCount = totalCount,
            WeightByDiameter = weightByDia
        };
    }
}
