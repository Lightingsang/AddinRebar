using System.Collections.Generic;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Catalog.Schemas
{
    public static class RebarToolSchema
    {
        public static DceProToolDefinition CreateDefinition()
        {
            var properties = new Dictionary<string, object>
            {
                ["operation"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "optimize_cutting", "aggregate_schedule" },
                    ["description"] = "Rebar cutting or scheduling operation to perform."
                },
                ["stockBarLengthMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Stock bar length in millimeters (standard: 11700)."
                },
                ["minReusableRemnantLengthMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Minimum length in millimeters to qualify remnant as reusable (standard: 1500)."
                },
                ["strategy"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "Auto", "BestFitDecreasing", "FirstFitDecreasing" },
                    ["description"] = "1D cutting stock heuristic algorithm."
                },
                ["demands"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["description"] = "List of rebar cutting demands (id, diameterMm, lengthMm, quantity).",
                    ["items"] = new Dictionary<string, object>
                    {
                        ["type"] = "object"
                    }
                },
                ["items"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["description"] = "List of structural rebar schedule items (elementMark, barMark, diameterMm, shapeCode, lengthMm, quantityPerElement, elementCount).",
                    ["items"] = new Dictionary<string, object>
                    {
                        ["type"] = "object"
                    }
                }
            };

            var schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = new[] { "operation" }
            };

            return new DceProToolDefinition(
                "dce_optimize_rebar_cutting",
                "1D 11.7m commercial bar cutting stock optimizer ToiUuCatThep & TCVN 1651 schedule aggregator.",
                schema);
        }
    }
}
