using System;
using System.Text.Json;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Steel;
using HPDcePro.Core.Calculations.Wind;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Execution.Handlers
{
    public sealed class SteelPebToolHandler
    {
        public DceProToolResult Execute(JsonElement? args)
        {
            string op = JsonArgHelper.GetString(args, "operation", "portal_frame").ToLowerInvariant();
            return op switch
            {
                "portal_frame" => CalculatePortalFrame(args),
                "moment_connection" => CalculateMomentConnection(args),
                "base_plate" => CalculateBasePlate(args),
                "wind_load" => CalculateWindLoad(args),
                _ => DceProToolResult.Fail($"Unsupported operation '{op}' for dce_design_steel_peb.")
            };
        }

        private static DceProToolResult CalculatePortalFrame(JsonElement? args)
        {
            var input = new GableFrameInput
            {
                SpanMm = JsonArgHelper.GetDouble(args, "spanMm", 24000.0),
                EaveHeightMm = JsonArgHelper.GetDouble(args, "eaveHeightMm", 7000.0),
                RoofSlopePercent = JsonArgHelper.GetDouble(args, "roofSlopePercent", 10.0),
                BaySpacingMm = JsonArgHelper.GetDouble(args, "baySpacingMm", 6000.0),
                TargetPurlinSpacingMm = JsonArgHelper.GetDouble(args, "targetPurlinSpacingMm", 1350.0),
                YieldStrengthMpa = JsonArgHelper.GetDouble(args, "yieldStrengthMpa", 345.0)
            };

            var result = PreGableFrameCalculator.Calculate(input);
            return DceProToolResult.Success(result);
        }

        private static DceProToolResult CalculateMomentConnection(JsonElement? args)
        {
            string gradeStr = JsonArgHelper.GetString(args, "boltGrade", "Grade8_8");
            var grade = gradeStr.Equals("Grade10_9", StringComparison.OrdinalIgnoreCase)
                ? BoltGrade.Grade10_9
                : BoltGrade.Grade8_8;

            var input = new SteelMomentConnectionInput
            {
                BendingMomentKnm = JsonArgHelper.GetDouble(args, "bendingMomentKnm", 180.0),
                AxialForceKn = JsonArgHelper.GetDouble(args, "axialForceKn", 0.0),
                ShearForceKn = JsonArgHelper.GetDouble(args, "shearForceKn", 120.0),
                BoltDiameterMm = JsonArgHelper.GetDouble(args, "boltDiameterMm", 24.0),
                Grade = grade,
                EndPlateThicknessMm = JsonArgHelper.GetDouble(args, "endPlateThicknessMm", 25.0)
            };

            var result = SteelMomentConnectionCalculator.Calculate(input);
            return DceProToolResult.Success(result);
        }

        private static DceProToolResult CalculateBasePlate(JsonElement? args)
        {
            string cGradeStr = JsonArgHelper.GetString(args, "pedestalGrade", "B25");
            var cGrade = ConcreteGrade.B25;
            if (MaterialLibrary.TryGetConcrete(cGradeStr, out var cProps) && cProps != null)
            {
                cGrade = cProps.Grade;
            }

            var input = new AnchorBoltBasePlateInput
            {
                PlateLengthLMm = JsonArgHelper.GetDouble(args, "plateLengthMm", 600.0),
                PlateWidthBMm = JsonArgHelper.GetDouble(args, "plateWidthMm", 400.0),
                PlateThicknessMm = JsonArgHelper.GetDouble(args, "plateThicknessMm", 25.0),
                AxialForceKn = JsonArgHelper.GetDouble(args, "axialForceKn", 450.0),
                BendingMomentKnm = JsonArgHelper.GetDouble(args, "bendingMomentKnm", 95.0),
                ShearForceKn = JsonArgHelper.GetDouble(args, "shearForceKn", 65.0),
                AnchorBoltCount = JsonArgHelper.GetInt(args, "anchorBoltCount", 4),
                AnchorBoltDiameterMm = JsonArgHelper.GetDouble(args, "anchorBoltDiameterMm", 24.0),
                PedestalGrade = cGrade
            };

            var result = AnchorBoltBasePlateCalculator.Calculate(input);
            return DceProToolResult.Success(result);
        }

        private static DceProToolResult CalculateWindLoad(JsonElement? args)
        {
            string zoneStr = JsonArgHelper.GetString(args, "windZone", "ZoneII");
            var zone = zoneStr switch
            {
                "ZoneI" => WindZone.ZoneI,
                "ZoneIII" => WindZone.ZoneIII,
                "ZoneIV" => WindZone.ZoneIV,
                "ZoneV" => WindZone.ZoneV,
                _ => WindZone.ZoneII
            };

            string terrStr = JsonArgHelper.GetString(args, "terrainCategory", "B");
            var terr = terrStr.ToUpperInvariant() switch
            {
                "A" => TerrainCategory.A,
                "C" => TerrainCategory.C,
                "D" => TerrainCategory.D,
                _ => TerrainCategory.B
            };

            double heightZ = JsonArgHelper.GetDouble(args, "heightZM", 10.0);
            double cpe = JsonArgHelper.GetDouble(args, "cpe", 0.8);
            double cpi = JsonArgHelper.GetDouble(args, "cpi", -0.2);

            var result = WindLoadCalculator.CalculateByZone(zone, terr, heightZ, cpe, cpi);
            return DceProToolResult.Success(result);
        }
    }
}
