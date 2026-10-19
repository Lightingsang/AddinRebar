using System;
using System.Collections.Generic;
using System.Text.Json;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Drafting;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Execution.Handlers
{
    public sealed class BeamToolHandler
    {
        public DceProToolResult Execute(JsonElement? args)
        {
            string op = JsonArgHelper.GetString(args, "operation", "single_span").ToLowerInvariant();
            return op switch
            {
                "single_span" => CalculateSingleSpan(args),
                "continuous" => CalculateContinuous(args),
                "foundation" => CalculateFoundation(args),
                "curved" => CalculateCurved(args),
                "shop_drawing" => GenerateShopDrawing(args),
                _ => DceProToolResult.Fail($"Unsupported operation '{op}' for dce_design_beam.")
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

        private static DceProToolResult CalculateSingleSpan(JsonElement? args)
        {
            var (concrete, steel) = ResolveMaterials(args);
            double width = JsonArgHelper.GetDouble(args, "sectionWidthMm", 220.0);
            double height = JsonArgHelper.GetDouble(args, "sectionHeightMm", 400.0);
            double moment = JsonArgHelper.GetDouble(args, "momentKnm", 80.0);
            double shear = JsonArgHelper.GetDouble(args, "shearKn", 90.0);

            var flexInput = new BeamFlexureInput(width, height, moment, concrete, steel);
            var flexResult = BeamFlexureCalculator.Calculate(flexInput);

            var shearInput = new BeamShearInput(width, height, shear, concrete, steel);
            var shearResult = BeamShearCalculator.Calculate(shearInput);

            var crackInput = new BeamCrackInput(width, height, moment * 0.75, flexResult.AsRequiredMm2, concrete, steel);
            var crackResult = BeamCrackCalculator.Calculate(crackInput);

            return DceProToolResult.Success(new
            {
                beamMark = JsonArgHelper.GetString(args, "beamMark", "D1"),
                flexure = flexResult,
                shear = shearResult,
                crack = crackResult
            });
        }

        private static DceProToolResult CalculateContinuous(JsonElement? args)
        {
            var (concrete, steel) = ResolveMaterials(args);
            string mark = JsonArgHelper.GetString(args, "beamMark", "D1");
            var spans = ParseSpans(args);

            var input = new ContinuousBeamInput(mark, concrete, steel, spans);
            var result = ContinuousBeamCalculator.Calculate(input);
            return DceProToolResult.Success(result);
        }

        private static DceProToolResult CalculateFoundation(JsonElement? args)
        {
            var (concrete, steel) = ResolveMaterials(args);
            string mark = JsonArgHelper.GetString(args, "beamMark", "GB1");
            var input = new FoundationBeamDesignInput(mark, concrete, steel)
            {
                SpanLengthMm = JsonArgHelper.GetDouble(args, "spanLengthMm", 6000.0),
                WidthMm = JsonArgHelper.GetDouble(args, "sectionWidthMm", 400.0),
                HeightMm = JsonArgHelper.GetDouble(args, "sectionHeightMm", 800.0),
                CoverMm = JsonArgHelper.GetDouble(args, "coverMm", 50.0),
                UpwardMidspanMomentKnm = JsonArgHelper.GetDouble(args, "upwardMidspanMomentKnm", 180.0),
                SupportBottomMomentKnm = JsonArgHelper.GetDouble(args, "supportBottomMomentKnm", 150.0),
                ShearForceKn = JsonArgHelper.GetDouble(args, "shearKn", 220.0)
            };

            var result = FoundationBeamCalculator.Calculate(input);
            return DceProToolResult.Success(result);
        }

        private static DceProToolResult CalculateCurved(JsonElement? args)
        {
            var (concrete, steel) = ResolveMaterials(args);
            string mark = JsonArgHelper.GetString(args, "beamMark", "CB1");
            var input = new CurvedBeamDesignInput(mark, concrete, steel)
            {
                RadiusMm = JsonArgHelper.GetDouble(args, "radiusMm", 6000.0),
                WidthMm = JsonArgHelper.GetDouble(args, "sectionWidthMm", 300.0),
                HeightMm = JsonArgHelper.GetDouble(args, "sectionHeightMm", 600.0),
                MomentKnm = JsonArgHelper.GetDouble(args, "momentKnm", 150.0),
                ShearKn = JsonArgHelper.GetDouble(args, "shearKn", 120.0),
                TorsionKnm = JsonArgHelper.GetDouble(args, "torsionKnm", 40.0)
            };

            var result = CurvedBeamCalculator.Calculate(input);
            return DceProToolResult.Success(result);
        }

        private static DceProToolResult GenerateShopDrawing(JsonElement? args)
        {
            var (concrete, steel) = ResolveMaterials(args);
            string mark = JsonArgHelper.GetString(args, "beamMark", "D1");
            var spans = ParseSpans(args);

            var input = new ContinuousBeamInput(mark, concrete, steel, spans);
            var beamResult = ContinuousBeamCalculator.Calculate(input);

            var generator = new BeamShopDrawingGenerator();
            var buffer = generator.GenerateShopDrawing(beamResult);

            return DceProToolResult.Success(new
            {
                beamMark = mark,
                totalEntities = buffer.Count,
                spansCount = beamResult.Spans.Count,
                totalLengthMm = beamResult.TotalLengthMm
            });
        }

        private static List<BeamSpanInput> ParseSpans(JsonElement? args)
        {
            var spans = new List<BeamSpanInput>();
            var spansProp = JsonArgHelper.GetProperty(args, "spans");

            if (spansProp.HasValue && spansProp.Value.ValueKind == JsonValueKind.Array)
            {
                int index = 0;
                foreach (var item in spansProp.Value.EnumerateArray())
                {
                    spans.Add(new BeamSpanInput
                    {
                        SpanIndex = index++,
                        ClearSpanMm = JsonArgHelper.GetDouble(item, "clearSpanMm", 6000.0),
                        SectionWidthMm = JsonArgHelper.GetDouble(item, "sectionWidthMm", 220.0),
                        SectionHeightMm = JsonArgHelper.GetDouble(item, "sectionHeightMm", 400.0),
                        TopMomentLeftKnm = JsonArgHelper.GetDouble(item, "topMomentLeftKnm", 75.0),
                        TopMomentRightKnm = JsonArgHelper.GetDouble(item, "topMomentRightKnm", 75.0),
                        BottomMomentMidKnm = JsonArgHelper.GetDouble(item, "bottomMomentMidKnm", 65.0),
                        ShearLeftKn = JsonArgHelper.GetDouble(item, "shearLeftKn", 80.0),
                        ShearRightKn = JsonArgHelper.GetDouble(item, "shearRightKn", 80.0)
                    });
                }
            }

            if (spans.Count == 0)
            {
                spans.Add(new BeamSpanInput { SpanIndex = 0, ClearSpanMm = 5500.0, SectionWidthMm = 220.0, SectionHeightMm = 400.0, TopMomentLeftKnm = 60.0, TopMomentRightKnm = 75.0, BottomMomentMidKnm = 55.0, ShearLeftKn = 70.0, ShearRightKn = 75.0 });
                spans.Add(new BeamSpanInput { SpanIndex = 1, ClearSpanMm = 6000.0, SectionWidthMm = 220.0, SectionHeightMm = 400.0, TopMomentLeftKnm = 75.0, TopMomentRightKnm = 60.0, BottomMomentMidKnm = 60.0, ShearLeftKn = 75.0, ShearRightKn = 70.0 });
            }

            return spans;
        }
    }
}
