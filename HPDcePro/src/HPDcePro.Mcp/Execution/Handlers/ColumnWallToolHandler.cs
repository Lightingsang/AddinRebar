using System;
using System.Collections.Generic;
using System.Text.Json;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Calculations.Column;
using HPDcePro.Core.Calculations.Wall;
using HPDcePro.Core.Drafting;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Execution.Handlers
{
    public sealed class ColumnWallToolHandler
    {
        public DceProToolResult Execute(JsonElement? args)
        {
            string op = JsonArgHelper.GetString(args, "operation", "column_pmm").ToLowerInvariant();
            return op switch
            {
                "column_pmm" => CalculateColumnPmm(args),
                "column_detailing" => GenerateColumnDetailing(args),
                "shear_wall" => CalculateShearWall(args),
                _ => DceProToolResult.Fail($"Unsupported operation '{op}' for dce_design_column_wall.")
            };
        }

        private static (ConcreteProperties Concrete, RebarProperties Steel) ResolveMaterials(JsonElement? args)
        {
            string cName = JsonArgHelper.GetString(args, "concreteGrade", "B30");
            string sName = JsonArgHelper.GetString(args, "steelGrade", "CB400-V");

            if (!MaterialLibrary.TryGetConcrete(cName, out var concrete) || concrete == null)
            {
                concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B30);
            }
            if (!MaterialLibrary.TryGetRebar(sName, out var steel) || steel == null)
            {
                steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            }
            return (concrete, steel);
        }

        private static DceProToolResult CalculateColumnPmm(JsonElement? args)
        {
            var (concrete, steel) = ResolveMaterials(args);
            string mark = JsonArgHelper.GetString(args, "elementMark", "C1");
            string shapeStr = JsonArgHelper.GetString(args, "shape", "Rectangular");
            var shape = shapeStr.Equals("Circular", StringComparison.OrdinalIgnoreCase)
                ? ColumnSectionShape.Circular
                : ColumnSectionShape.Rectangular;

            double widthB = JsonArgHelper.GetDouble(args, "widthBMm", 300.0);
            double heightH = JsonArgHelper.GetDouble(args, "heightHMm", 300.0);
            double l0 = JsonArgHelper.GetDouble(args, "effectiveLengthL0Mm", 3300.0);
            double n = JsonArgHelper.GetDouble(args, "axialNKn", 850.0);
            double mx = JsonArgHelper.GetDouble(args, "momentMxKnm", 75.0);
            double my = JsonArgHelper.GetDouble(args, "momentMyKnm", 60.0);

            var bars = GenerateDefaultBars(shape, widthB, heightH);
            var input = new ColumnPmmInput(mark, shape, widthB, heightH, l0, concrete, steel, bars, n, mx, my, 1.0);
            var result = ColumnPmmInteractionCalculator.Calculate(input);

            return DceProToolResult.Success(result);
        }

        private static List<ColumnBarPlacement> GenerateDefaultBars(ColumnSectionShape shape, double b, double h)
        {
            var bars = new List<ColumnBarPlacement>();
            if (shape == ColumnSectionShape.Circular)
            {
                double radius = (h * 0.5) - 40.0;
                int count = 8;
                for (int i = 0; i < count; i++)
                {
                    double angle = 2.0 * Math.PI * i / count;
                    bars.Add(new ColumnBarPlacement(radius * Math.Cos(angle), radius * Math.Sin(angle), 20));
                }
            }
            else
            {
                double cx = (b * 0.5) - 40.0;
                double cy = (h * 0.5) - 40.0;
                bars.Add(new ColumnBarPlacement(-cx, -cy, 20));
                bars.Add(new ColumnBarPlacement(cx, -cy, 20));
                bars.Add(new ColumnBarPlacement(cx, cy, 20));
                bars.Add(new ColumnBarPlacement(-cx, cy, 20));
            }
            return bars;
        }

        private static DceProToolResult GenerateColumnDetailing(JsonElement? args)
        {
            string mark = JsonArgHelper.GetString(args, "elementMark", "C1");
            double widthB = JsonArgHelper.GetDouble(args, "widthBMm", 400.0);
            double heightH = JsonArgHelper.GetDouble(args, "heightHMm", 400.0);

            var stories = new List<ColumnStoryDetail>
            {
                new ColumnStoryDetail("TẦNG 1", 0.0, 3900.0, 500.0, widthB, heightH),
                new ColumnStoryDetail("TẦNG 2", 3900.0, 3600.0, 450.0, widthB, heightH)
            };

            var opt = new ColumnDetailingOptions
            {
                ColumnMark = mark,
                Stories = stories,
                IncludeCrossSections = true
            };

            var generator = new ColumnDetailingGenerator();
            var buffer = generator.GenerateElevationAndSlices(opt);

            return DceProToolResult.Success(new
            {
                columnMark = mark,
                totalEntities = buffer.Count,
                storiesCount = stories.Count
            });
        }

        private static DceProToolResult CalculateShearWall(JsonElement? args)
        {
            var (concrete, steel) = ResolveMaterials(args);
            string mark = JsonArgHelper.GetString(args, "elementMark", "W1");
            double lw = JsonArgHelper.GetDouble(args, "heightHMm", 3000.0);
            double tw = JsonArgHelper.GetDouble(args, "widthBMm", 250.0);
            double hw = JsonArgHelper.GetDouble(args, "effectiveLengthL0Mm", 3300.0);
            double n = JsonArgHelper.GetDouble(args, "axialNKn", 1500.0);
            double vu = JsonArgHelper.GetDouble(args, "shearVuKn", 350.0);
            double mu = JsonArgHelper.GetDouble(args, "momentMxKnm", 650.0);
            int webDia = JsonArgHelper.GetInt(args, "webVerticalBarDiameter", 12);
            double webSpacing = JsonArgHelper.GetDouble(args, "webVerticalBarSpacingMm", 200.0);

            var input = new ShearWallInput(mark, lw, tw, hw, concrete, steel, n, vu, mu, webDia, webSpacing);
            var result = ShearWallCalculator.Calculate(input);

            return DceProToolResult.Success(result);
        }
    }
}
