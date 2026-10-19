using System.Collections.Generic;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Catalog.Schemas
{
    public static class SlabSecondaryToolSchema
    {
        public static DceProToolDefinition CreateDefinition()
        {
            var properties = new Dictionary<string, object>
            {
                ["operation"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "two_way_slab", "staircase", "lintel" },
                    ["description"] = "Secondary element operation to perform."
                },
                ["mark"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Mark identifier for element (e.g. 'S1', 'THANG1', 'LT1')."
                },
                ["shortSpanLxMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Short span Lx in millimeters for slab."
                },
                ["longSpanLyMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Long span Ly in millimeters for slab."
                },
                ["slabThicknessHMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Slab or waist thickness in millimeters."
                },
                ["deadLoadGKnM2"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Characteristic dead load in kN/m2."
                },
                ["liveLoadPKnM2"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Characteristic live load in kN/m2."
                },
                ["type"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "WaistSlab", "FoldedPlateZigZag" },
                    ["description"] = "Staircase structural type."
                },
                ["flightSpanLMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Horizontal flight span L in millimeters for staircase."
                },
                ["totalRiseHMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Total vertical rise H in millimeters for staircase."
                },
                ["numberOfSteps"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Number of risers/steps in staircase flight."
                },
                ["clearSpanL0Mm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Clear opening span L0 in millimeters for lintel."
                },
                ["bearingLengthMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Support bearing length in millimeters for lintel (>= 200mm)."
                },
                ["wallThicknessBMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Masonry wall thickness in millimeters for lintel."
                },
                ["lintelDepthHMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Total lintel section depth in millimeters."
                }
            };

            var schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = new[] { "operation" }
            };

            return new DceProToolDefinition(
                "dce_design_slab_stair_lintel",
                "Two-way Marcus slab, EngineBanThang/EngineZigZac staircase & opening lintel design.",
                schema);
        }
    }
}
