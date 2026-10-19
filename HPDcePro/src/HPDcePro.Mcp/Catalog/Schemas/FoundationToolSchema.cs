using System.Collections.Generic;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Catalog.Schemas
{
    public static class FoundationToolSchema
    {
        public static DceProToolDefinition CreateDefinition()
        {
            var properties = new Dictionary<string, object>
            {
                ["operation"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "pile_capacity", "pile_cap", "pad_footing" },
                    ["description"] = "Foundation design operation to perform."
                },
                ["elementMark"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Mark identifier for foundation element (e.g. 'DC1', 'M1')."
                },
                ["diameterM"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Pile diameter in meters."
                },
                ["lengthM"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Pile length in meters."
                },
                ["installationType"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "DrivenPile", "BoredPile" },
                    ["description"] = "Pile installation method."
                },
                ["tipSoilN60"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "SPT N60 value at pile tip."
                },
                ["widthBMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Foundation width B in millimeters."
                },
                ["lengthLMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Foundation length L in millimeters."
                },
                ["thicknessHMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Footing or pile cap thickness H in millimeters."
                },
                ["axialNKn"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Design vertical axial load N in kN."
                },
                ["momentMxKnm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Design bending moment Mx in kNm."
                },
                ["momentMyKnm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Design bending moment My in kNm."
                },
                ["pileCapacityRcKn"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Design single pile capacity Rc in kN."
                },
                ["allowableSoilPressureKpa"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Allowable soil pressure Rtc in kPa for pad footing."
                }
            };

            var schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = new[] { "operation" }
            };

            return new DceProToolDefinition(
                "dce_design_foundation_pile",
                "TCVN 10304:2025 SPT/friction pile capacity, pile cap & pad footing design.",
                schema);
        }
    }
}
