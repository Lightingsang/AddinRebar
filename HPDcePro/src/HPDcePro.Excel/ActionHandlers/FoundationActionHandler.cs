using System;
using System.Collections.Generic;
using System.Linq;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Foundation;

namespace HPDcePro.Excel.ActionHandlers
{
    public sealed class FoundationActionHandler : IExcelActionHandler
    {
        private static readonly string[] s_actions =
        {
            "Bas_Show",
            "Eta_Show",
            "Fou_Show"
        };

        private static readonly string[] s_aliases = { "TinhMong", "TinhDaiCoc", "TinhMongDon" };

        public IReadOnlyList<string> SupportedActions => s_actions;

        public bool CanHandle(string actionKey) =>
            s_actions.Contains(actionKey, StringComparer.OrdinalIgnoreCase) ||
            s_aliases.Contains(actionKey, StringComparer.OrdinalIgnoreCase);

        public ExcelActionResult Handle(string actionKey, ExcelActionContext context)
        {
            if (string.Equals(actionKey, "Fou_Show", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "TinhMong", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "TinhDaiCoc", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "TinhMongDon", StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteFoundationDesign(actionKey, context);
            }

            return ExcelActionResult.Ok(actionKey, $"Thực hiện thao tác móng '{actionKey}' thành công.");
        }

        private static ExcelActionResult ExecuteFoundationDesign(string actionKey, ExcelActionContext context)
        {
            string type = context.Parameters.TryGetValue("FoundationType", out var tv) ? tv?.ToString() ?? "PileCap" : "PileCap";

            if (type.Equals("PadFooting", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(actionKey, "TinhMongDon", StringComparison.OrdinalIgnoreCase))
            {
                return ExecutePadFootingDesign(actionKey);
            }

            return ExecutePileCapDesign(actionKey);
        }

        private static ExcelActionResult ExecutePadFootingDesign(string actionKey)
        {
            var input = new PadFootingInput(
                "MD1", 2200.0, 2500.0, 800.0,
                400.0, 400.0, 1.8,
                MaterialLibrary.GetConcrete(ConcreteGrade.B25),
                MaterialLibrary.GetRebar(RebarGrade.CB400V),
                950.0, 60.0, 40.0, 220.0);

            var res = PadFootingCalculator.Calculate(input);
            return ExcelActionResult.Ok(actionKey, "Tính toán móng đơn hoàn tất.", new
            {
                Type = "PadFooting",
                MaxSoilPressureKpa = res.BasePressureMaxKpa,
                CapacityRatio = res.SoilCapacityRatio,
                IsSoilPressureSatisfied = res.SoilCapacityRatio <= 1.0,
                IsPunchingSatisfied = res.IsPunchingSafe
            });
        }

        private static ExcelActionResult ExecutePileCapDesign(string actionKey)
        {
            var piles = new List<PileCoordinateItem>
            {
                new("P1", -600.0, -600.0),
                new("P2", 600.0, -600.0),
                new("P3", -600.0, 600.0),
                new("P4", 600.0, 600.0)
            };

            var capInput = new PileCapInput(
                "DC1", 2400.0, 2400.0, 1000.0,
                400.0, 400.0,
                MaterialLibrary.GetConcrete(ConcreteGrade.B25),
                MaterialLibrary.GetRebar(RebarGrade.CB400V),
                piles, 2400.0, 120.0, 80.0, 600.0);

            var capRes = PileCapCalculator.Calculate(capInput);
            return ExcelActionResult.Ok(actionKey, "Tính toán đài cọc hoàn tất.", new
            {
                Type = "PileCap",
                MaxPileLoadKn = capRes.MaxReactionKn,
                CapacityRatio = capRes.PileCapacityRatio,
                IsPunchingSatisfied = capRes.ColumnPunchingRatio <= 1.0,
                IsOneWayShearSatisfied = capRes.OneWayShearRatio <= 1.0
            });
        }
    }
}
