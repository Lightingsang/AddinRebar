using System;
using System.Text.Json;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Secondary;
using HPDcePro.Core.Calculations.Slab;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Execution.Handlers
{
    public sealed class SlabSecondaryToolHandler
    {
        public DceProToolResult Execute(JsonElement? args)
        {
            string op = JsonArgHelper.GetString(args, "operation", "two_way_slab").ToLowerInvariant();
            return op switch
            {
                "two_way_slab" => CalculateTwoWaySlab(args),
                "staircase" => CalculateStaircase(args),
                "lintel" => CalculateLintel(args),
                _ => DceProToolResult.Fail($"Unsupported operation '{op}' for dce_design_slab_stair_lintel.")
            };
        }

        private static (ConcreteProperties Concrete, RebarProperties Steel) ResolveMaterials(JsonElement? args)
        {
            string cName = JsonArgHelper.GetString(args, "concreteGrade", "B25");
            string sName = JsonArgHelper.GetString(args, "steelGrade", "CB300-V");

            if (!MaterialLibrary.TryGetConcrete(cName, out var concrete) || concrete == null)
            {
                concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            }
            if (!MaterialLibrary.TryGetRebar(sName, out var steel) || steel == null)
            {
                steel = MaterialLibrary.GetRebar(RebarGrade.CB300V);
            }
            return (concrete, steel);
        }

        private static DceProToolResult CalculateTwoWaySlab(JsonElement? args)
        {
            var (concrete, steel) = ResolveMaterials(args);
            string name = JsonArgHelper.GetString(args, "mark", "S1");
            double lx = JsonArgHelper.GetDouble(args, "shortSpanLxMm", 4000.0);
            double ly = JsonArgHelper.GetDouble(args, "longSpanLyMm", 5000.0);
            double h = JsonArgHelper.GetDouble(args, "slabThicknessHMm", 120.0);
            double g = JsonArgHelper.GetDouble(args, "deadLoadGKnM2", 3.5);
            double p = JsonArgHelper.GetDouble(args, "liveLoadPKnM2", 2.4);

            var input = new TwoWaySlabInput(name, lx, ly, h, g, p, SlabBoundaryCondition.FourEdgesFixed, concrete, steel);
            var result = TwoWaySlabCalculator.Calculate(input);
            return DceProToolResult.Success(result);
        }

        private static DceProToolResult CalculateStaircase(JsonElement? args)
        {
            var (concrete, steel) = ResolveMaterials(args);
            string mark = JsonArgHelper.GetString(args, "mark", "THANG-1");
            string typeStr = JsonArgHelper.GetString(args, "type", "WaistSlab");
            var type = typeStr.Equals("FoldedPlateZigZag", StringComparison.OrdinalIgnoreCase)
                ? StaircaseType.FoldedPlateZigZag
                : StaircaseType.WaistSlab;

            double flightL = JsonArgHelper.GetDouble(args, "flightSpanLMm", 3000.0);
            double totalH = JsonArgHelper.GetDouble(args, "totalRiseHMm", 1800.0);
            double waistH = JsonArgHelper.GetDouble(args, "slabThicknessHMm", 120.0);
            int steps = JsonArgHelper.GetInt(args, "numberOfSteps", 10);
            double widthB = JsonArgHelper.GetDouble(args, "stairWidthBMm", 1200.0);

            var input = new StaircaseInput(mark, type, flightL, totalH, waistH, steps, widthB, concrete, steel);
            var result = StaircaseCalculator.Calculate(input);
            return DceProToolResult.Success(result);
        }

        private static DceProToolResult CalculateLintel(JsonElement? args)
        {
            var (concrete, steel) = ResolveMaterials(args);
            string mark = JsonArgHelper.GetString(args, "mark", "LT-1");
            double l0 = JsonArgHelper.GetDouble(args, "clearSpanL0Mm", 1200.0);
            double bearing = JsonArgHelper.GetDouble(args, "bearingLengthMm", 250.0);
            double wallB = JsonArgHelper.GetDouble(args, "wallThicknessBMm", 220.0);
            double depthH = JsonArgHelper.GetDouble(args, "lintelDepthHMm", 200.0);
            double wallH = JsonArgHelper.GetDouble(args, "wallHeightAboveMm", 1500.0);

            var input = new LintelInput(mark, l0, bearing, wallB, depthH, wallH, concrete, steel);
            var result = LintelCalculator.Calculate(input);
            return DceProToolResult.Success(result);
        }
    }
}
