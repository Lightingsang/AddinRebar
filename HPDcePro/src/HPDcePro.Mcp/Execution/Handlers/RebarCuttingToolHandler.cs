using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HPDcePro.Core.Rebar;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Execution.Handlers
{
    public sealed class RebarCuttingToolHandler
    {
        public DceProToolResult Execute(JsonElement? args)
        {
            string op = JsonArgHelper.GetString(args, "operation", "optimize_cutting").ToLowerInvariant();
            return op switch
            {
                "optimize_cutting" => OptimizeCutting(args),
                "aggregate_schedule" => AggregateSchedule(args),
                _ => DceProToolResult.Fail($"Unsupported operation '{op}' for dce_optimize_rebar_cutting.")
            };
        }

        private static DceProToolResult OptimizeCutting(JsonElement? args)
        {
            double stockLen = JsonArgHelper.GetDouble(args, "stockBarLengthMm", 11700.0);
            if (stockLen <= 0.0)
            {
                return DceProToolResult.Fail("stockBarLengthMm must be strictly positive (> 0).");
            }
            string stratStr = JsonArgHelper.GetString(args, "strategy", "Auto");
            var strat = stratStr switch
            {
                "BestFitDecreasing" => CuttingStrategy.BestFitDecreasing,
                "FirstFitDecreasing" => CuttingStrategy.FirstFitDecreasing,
                _ => CuttingStrategy.Auto
            };

            var demands = ParseDemands(args);
            var distinctDias = demands.Select(d => d.DiameterMm).Distinct().OrderBy(d => d).ToList();
            var results = new List<CuttingOptimizationResult>();

            foreach (int dia in distinctDias)
            {
                var diaDemands = demands.Where(d => d.DiameterMm == dia);
                var optResult = RebarCuttingOptimizer.Optimize(diaDemands, dia, strat, stockLen);
                results.Add(optResult);
            }

            return DceProToolResult.Success(new
            {
                diameterCount = results.Count,
                results = results,
                totalStockBarsUsed = results.Sum(r => r.TotalStockBarsUsed),
                overallScrapPercentage = results.Count > 0 ? results.Average(r => r.ScrapPercentage) : 0.0
            });
        }

        private static List<CuttingDemandItem> ParseDemands(JsonElement? args)
        {
            var demands = new List<CuttingDemandItem>();
            var demandsProp = JsonArgHelper.GetProperty(args, "demands");

            if (demandsProp.HasValue && demandsProp.Value.ValueKind == JsonValueKind.Array)
            {
                int index = 1;
                foreach (var item in demandsProp.Value.EnumerateArray())
                {
                    string id = JsonArgHelper.GetString(item, "id", $"Bar_{index++}");
                    int dia = JsonArgHelper.GetInt(item, "diameterMm", 18);
                    double len = JsonArgHelper.GetDouble(item, "lengthMm", 3500.0);
                    int qty = JsonArgHelper.GetInt(item, "quantity", 1);
                    if (len > 0.0 && qty > 0)
                    {
                        demands.Add(new CuttingDemandItem(id, dia, len, qty));
                    }
                }
            }

            if (demands.Count == 0)
            {
                demands.Add(new CuttingDemandItem("D1_Top_L", 20, 5800.0, 4));
                demands.Add(new CuttingDemandItem("D1_Bot_Mid", 20, 4200.0, 4));
                demands.Add(new CuttingDemandItem("D1_Top_Extra", 20, 2400.0, 6));
                demands.Add(new CuttingDemandItem("D1_Bot_Extra", 20, 3100.0, 4));
            }

            return demands;
        }

        private static DceProToolResult AggregateSchedule(JsonElement? args)
        {
            var items = ParseScheduleItems(args);
            var report = RebarScheduleAggregator.Aggregate(items);
            return DceProToolResult.Success(report);
        }

        private static List<ScheduleItem> ParseScheduleItems(JsonElement? args)
        {
            var items = new List<ScheduleItem>();
            var itemsProp = JsonArgHelper.GetProperty(args, "items");

            if (itemsProp.HasValue && itemsProp.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsProp.Value.EnumerateArray())
                {
                    items.Add(new ScheduleItem
                    {
                        ElementMark = JsonArgHelper.GetString(item, "elementMark", "D1"),
                        BarMark = JsonArgHelper.GetString(item, "barMark", "1"),
                        DiameterMm = JsonArgHelper.GetInt(item, "diameterMm", 18),
                        ShapeCode = JsonArgHelper.GetString(item, "shapeCode", "00"),
                        LengthMm = JsonArgHelper.GetDouble(item, "lengthMm", 4000.0),
                        QuantityPerElement = JsonArgHelper.GetInt(item, "quantityPerElement", 2),
                        ElementCount = JsonArgHelper.GetInt(item, "elementCount", 1)
                    });
                }
            }

            if (items.Count == 0)
            {
                items.Add(new ScheduleItem { ElementMark = "D1", BarMark = "1", DiameterMm = 20, LengthMm = 6500.0, QuantityPerElement = 2, ElementCount = 4 });
                items.Add(new ScheduleItem { ElementMark = "D1", BarMark = "2", DiameterMm = 20, LengthMm = 4500.0, QuantityPerElement = 3, ElementCount = 4 });
                items.Add(new ScheduleItem { ElementMark = "D1", BarMark = "3", DiameterMm = 8, LengthMm = 1100.0, QuantityPerElement = 35, ElementCount = 4 });
            }

            return items;
        }
    }
}
