using System.Collections.Generic;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Catalog.Schemas
{
    public static class ColumnWallToolSchema
    {
        public static DceProToolDefinition CreateDefinition()
        {
            var properties = new Dictionary<string, object>
            {
                ["operation"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "column_pmm", "column_detailing", "shear_wall" },
                    ["description"] = "Column or wall operation to perform."
                },
                ["elementMark"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Mark identifier for column or wall (e.g. 'C1', 'W1')."
                },
                ["concreteGrade"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Concrete grade (e.g. 'B25', 'B30', 'B35')."
                },
                ["steelGrade"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Rebar grade (e.g. 'CB400-V', 'CB500-V')."
                },
                ["shape"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "Rectangular", "Circular" },
                    ["description"] = "Section shape for column."
                },
                ["widthBMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Section width b in millimeters (or wall thickness)."
                },
                ["heightHMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Section height h in millimeters (or column diameter / wall length)."
                },
                ["effectiveLengthL0Mm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Effective buckling length L0 in millimeters."
                },
                ["axialNKn"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Design axial compression force N in kN."
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
                ["shearVuKn"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Design shear force Vu in kN (for shear wall)."
                },
                ["webVerticalBarDiameter"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Diameter of wall web vertical rebar in mm."
                },
                ["webVerticalBarSpacingMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Spacing of wall web vertical rebar in mm."
                }
            };

            var schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = new[] { "operation" }
            };

            return new DceProToolDefinition(
                "dce_design_column_wall",
                "TCVN 5574:2018 biaxial P-Mx-My column & shear wall boundary element check.",
                schema);
        }
    }
}
