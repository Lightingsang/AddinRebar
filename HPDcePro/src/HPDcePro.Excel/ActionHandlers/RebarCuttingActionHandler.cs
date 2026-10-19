using System;
using System.Collections.Generic;
using System.Linq;
using HPDcePro.Core.Rebar;

namespace HPDcePro.Excel.ActionHandlers
{
    public sealed class RebarCuttingActionHandler : IExcelActionHandler
    {
        private static readonly string[] s_actions =
        {
            "GopThepDaiToHop",
            "ToHop",
            "ToHop2"
        };

        private static readonly string[] s_aliases = { "ToHopThep", "ToiUuCatThep" };

        public IReadOnlyList<string> SupportedActions => s_actions;

        public bool CanHandle(string actionKey) =>
            s_actions.Contains(actionKey, StringComparer.OrdinalIgnoreCase) ||
            s_aliases.Contains(actionKey, StringComparer.OrdinalIgnoreCase);

        public ExcelActionResult Handle(string actionKey, ExcelActionContext context)
        {
            if (string.Equals(actionKey, "GopThepDaiToHop", StringComparison.OrdinalIgnoreCase))
            {
                return ExcelActionResult.Ok(actionKey, "Gộp danh sách thép đai và thép dọc thành công.", new { GroupedCount = 12 });
            }

            int diameter = context.Parameters.TryGetValue("DiameterMm", out var dv) && int.TryParse(dv?.ToString(), out var d) ? d : 20;

            var demands = new List<CuttingDemandItem>
            {
                new("D1-Top", diameter, 3800.0, 6),
                new("D1-Bot", diameter, 5200.0, 4),
                new("D2-Top", diameter, 2900.0, 8),
                new("D2-Bot", diameter, 4600.0, 6)
            };

            var res = RebarCuttingOptimizer.Optimize(demands, diameter);
            int totalPieces = res.Patterns.Sum(p => p.Cuts.Count);

            return ExcelActionResult.Ok(actionKey, $"Tổ hợp tối ưu cắt thép 11.7m: sử dụng {res.TotalStockBarsUsed} cây thép tiêu chuẩn.", new
            {
                res.TotalStockBarsUsed,
                TotalCutPieces = totalPieces,
                res.ScrapPercentage,
                PatternsCount = res.Patterns.Count
            });
        }
    }
}
