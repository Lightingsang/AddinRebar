using System.Collections.Generic;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Catalog.Schemas
{
    public static class BeamToolSchema
    {
        public static DceProToolDefinition CreateDefinition()
        {
            var properties = new Dictionary<string, object>
            {
                ["operation"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "single_span", "continuous", "foundation", "curved", "shop_drawing" },
                    ["description"] = "Beam design operation to perform."
                },
                ["beamMark"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Mark identifier for the beam (e.g. 'D1', 'GB1', 'CB1')."
                },
                ["concreteGrade"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Concrete grade per TCVN 5574:2018 (e.g. 'B20', 'B25', 'B30', 'B35', 'B40')."
                },
                ["steelGrade"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Rebar grade per TCVN 1651:2018 (e.g. 'CB240-T', 'CB300-V', 'CB400-V', 'CB500-V')."
                },
                ["sectionWidthMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Section width b in millimeters."
                },
                ["sectionHeightMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Section height h in millimeters."
                },
                ["momentKnm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Design bending moment M in kNm."
                },
                ["shearKn"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Design shear force V in kN."
                },
                ["torsionKnm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Design torsional moment T in kNm (for curved beam)."
                },
                ["radiusMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Curvature radius R in millimeters (for curved beam)."
                },
                ["spans"] = new Dictionary<string, object>
                {
                    ["type"] = "array",
                    ["description"] = "Span geometry and internal force list for continuous beams or shop drawings.",
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
                "dce_design_beam",
                "TCVN 5574:2018 single/continuous/foundation/curved beam design, crack check, and shop drawing CAD primitive generation.",
                schema);
        }
    }
}
