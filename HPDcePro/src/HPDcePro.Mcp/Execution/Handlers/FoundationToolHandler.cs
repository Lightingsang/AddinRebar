using System;
using System.Collections.Generic;
using System.Text.Json;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Foundation;
using HPDcePro.Core.Calculations.Pile;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Execution.Handlers
{
    public sealed class FoundationToolHandler
    {
        public DceProToolResult Execute(JsonElement? args)
        {
            string op = JsonArgHelper.GetString(args, "operation", "pile_capacity").ToLowerInvariant();
            return op switch
            {
                "pile_capacity" => CalculatePileCapacity(args),
                "pile_cap" => CalculatePileCap(args),
                "pad_footing" => CalculatePadFooting(args),
                _ => DceProToolResult.Fail($"Unsupported operation '{op}' for dce_design_foundation_pile.")
            };
        }

        private static (ConcreteProperties Concrete, RebarProperties Steel) ResolveMaterials(JsonElement? args)
        {
            string cName = JsonArgHelper.GetString(args, "concreteGrade", "B25");
            string sName = JsonArgHelper.GetString(args, "steelGrade", "CB400-V");

            if (!MaterialLibrary.TryGetConcrete(cName, out var concrete) || concrete == null)
            {
                concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B25);
            }
            if (!MaterialLibrary.TryGetRebar(sName, out var steel) || steel == null)
            {
                steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            }
            return (concrete, steel);
        }

        private static DceProToolResult CalculatePileCapacity(JsonElement? args)
        {
            double diaM = JsonArgHelper.GetDouble(args, "diameterM", 0.4);
            double lenM = JsonArgHelper.GetDouble(args, "lengthM", 20.0);
            string installStr = JsonArgHelper.GetString(args, "installationType", "DrivenPile");
            var installType = installStr.Equals("BoredPile", StringComparison.OrdinalIgnoreCase)
                ? PileInstallationType.BoredPile
                : PileInstallationType.DrivenPile;

            double tipN60 = JsonArgHelper.GetDouble(args, "tipSoilN60", 30.0);

            var pile = new PileGeometry
            {
                IsCircular = true,
                DiameterM = diaM,
                TopDepthM = 0.0,
                TipDepthM = lenM
            };

            var layers = new List<SoilLayer>
            {
                new SoilLayer("Lớp 1 - Sét dẻo mềm", SoilConsistency.Cohesive, 0.0, 5.0, 6.0),
                new SoilLayer("Lớp 2 - Cát pha dẻo", SoilConsistency.Cohesionless, 5.0, 14.0, 15.0),
                new SoilLayer("Lớp 3 - Cát hạt thô chặt", SoilConsistency.Cohesionless, 14.0, lenM + 5.0, tipN60)
            };

            var result = PileCapacityCalculator.Calculate(
                pile, layers, installType, tipN60, SoilConsistency.Cohesionless, 1.4, 1.15);

            return DceProToolResult.Success(result);
        }

        private static DceProToolResult CalculatePileCap(JsonElement? args)
        {
            var (concrete, steel) = ResolveMaterials(args);
            string mark = JsonArgHelper.GetString(args, "elementMark", "DC1");
            double widthB = JsonArgHelper.GetDouble(args, "widthBMm", 1800.0);
            double lengthL = JsonArgHelper.GetDouble(args, "lengthLMm", 2400.0);
            double thickH = JsonArgHelper.GetDouble(args, "thicknessHMm", 900.0);
            double colB = JsonArgHelper.GetDouble(args, "columnWidthMm", 400.0);
            double colH = JsonArgHelper.GetDouble(args, "columnHeightMm", 400.0);
            double n = JsonArgHelper.GetDouble(args, "axialNKn", 2000.0);
            double mx = JsonArgHelper.GetDouble(args, "momentMxKnm", 120.0);
            double my = JsonArgHelper.GetDouble(args, "momentMyKnm", 90.0);
            double rc = JsonArgHelper.GetDouble(args, "pileCapacityRcKn", 650.0);

            var piles = new List<PileCoordinateItem>
            {
                new PileCoordinateItem("P1", -500.0, -700.0),
                new PileCoordinateItem("P2", 500.0, -700.0),
                new PileCoordinateItem("P3", 500.0, 700.0),
                new PileCoordinateItem("P4", -500.0, 700.0)
            };

            var input = new PileCapInput(mark, widthB, lengthL, thickH, colB, colH, concrete, steel, piles, n, mx, my, rc);
            var result = PileCapCalculator.Calculate(input);
            return DceProToolResult.Success(result);
        }

        private static DceProToolResult CalculatePadFooting(JsonElement? args)
        {
            var (concrete, steel) = ResolveMaterials(args);
            string mark = JsonArgHelper.GetString(args, "elementMark", "M1");
            double widthB = JsonArgHelper.GetDouble(args, "widthBMm", 1800.0);
            double lengthL = JsonArgHelper.GetDouble(args, "lengthLMm", 2000.0);
            double thickH = JsonArgHelper.GetDouble(args, "thicknessHMm", 500.0);
            double colB = JsonArgHelper.GetDouble(args, "columnWidthMm", 400.0);
            double colH = JsonArgHelper.GetDouble(args, "columnHeightMm", 400.0);
            double depthM = JsonArgHelper.GetDouble(args, "foundationDepthM", 1.5);
            double n = JsonArgHelper.GetDouble(args, "axialNKn", 600.0);
            double mx = JsonArgHelper.GetDouble(args, "momentMxKnm", 45.0);
            double my = JsonArgHelper.GetDouble(args, "momentMyKnm", 30.0);
            double rtc = JsonArgHelper.GetDouble(args, "allowableSoilPressureKpa", 200.0);

            var input = new PadFootingInput(mark, widthB, lengthL, thickH, colB, colH, depthM, concrete, steel, n, mx, my, rtc);
            var result = PadFootingCalculator.Calculate(input);
            return DceProToolResult.Success(result);
        }
    }
}
