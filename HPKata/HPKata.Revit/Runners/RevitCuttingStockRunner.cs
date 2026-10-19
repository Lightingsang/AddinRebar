using HPKata.Core.Models;
using HPKata.Core.Optimization;
using HPKata.Revit.Abstractions;
using HPKata.Revit.Bridge;

namespace HPKata.Revit.Runners;

/// <summary>
/// Domain runner executing 1D cutting-stock optimization (1D-CSP) for commercial 11,700 mm bars.
/// Filters rebar demands with diameter >= 10 mm.
/// </summary>
public static class RevitCuttingStockRunner
{
    public const double DefaultStockLengthMm = 11700.0;
    public const int MinCuttingDiameterMm = 10;

    private static readonly string[] TableHeaders =
    [
        "STT",
        "Đường kính",
        "Số cây 11.7m",
        "Số đoạn cắt",
        "Hao hụt thô (%)",
        "Hao hụt tinh (%)",
        "Số phôi thừa thu hồi"
    ];

    /// <summary>
    /// Executes cutting stock optimization for all or selected rebars in the document context.
    /// </summary>
    public static IReadOnlyList<CuttingStockResult> Execute(
        IRevitDocumentContext context,
        double stockLengthMm = DefaultStockLengthMm,
        bool selectedOnly = false)
    {
        var dtos = selectedOnly && context.GetSelectedRebars().Count > 0
            ? context.GetSelectedRebars()
            : context.GetAllRebars();

        var validDemands = dtos
            .Select(RevitParameterBridge.ToRebarItem)
            .Where(i => i.DiameterMm >= MinCuttingDiameterMm && i.CuttingLengthMm > 0.0)
            .GroupBy(i => i.DiameterMm)
            .OrderBy(g => g.Key);

        var results = new List<CuttingStockResult>();
        var engine = new CuttingStockEngine { StandardStockLengthMm = stockLengthMm };

        foreach (var group in validDemands)
        {
            int dia = group.Key;
            var demands = group.Select(i => new CuttingDemand(
                dia,
                i.CuttingLengthMm,
                i.TotalCount,
                i.BarMark,
                i.ComponentMark));

            var res = engine.Solve(dia, demands);
            results.Add(res);
        }

        var table = BuildSummaryTable(results);
        context.CreateOrUpdateScheduleTable(table);

        int totalBars = results.Sum(r => r.NewStandardBarsCount);
        context.WriteNotification(
            $"Đã tối ưu tổ hợp cắt thép cho {results.Count} nhóm đường kính. Tổng số cây 11.7m: {totalBars}.");

        return results;
    }

    private static RevitScheduleTableDto BuildSummaryTable(IReadOnlyList<CuttingStockResult> results)
    {
        var rows = new List<IReadOnlyList<string>>(results.Count);
        int index = 1;
        int totalBars = 0;

        foreach (var res in results)
        {
            totalBars += res.NewStandardBarsCount;
            int totalCuts = res.Patterns.Sum(p => p.CutsMm.Count);
            int offcuts = res.NewWarehouseOffcuts.Sum(o => o.Quantity);

            rows.Add(
            [
                index.ToString(),
                $"D{res.DiameterMm}",
                res.NewStandardBarsCount.ToString(),
                totalCuts.ToString(),
                res.GrossWastePercent.ToString("F1"),
                res.NetWastePercent.ToString("F1"),
                offcuts.ToString()
            ]);
            index++;
        }

        return new RevitScheduleTableDto
        {
            Title = "BẢNG TỔ HỢP CẮT THÉP (11.7M)",
            Headers = TableHeaders,
            Rows = rows,
            TotalBarCount = totalBars
        };
    }
}
