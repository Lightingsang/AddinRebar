using System.Collections.Generic;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Catalog.Schemas
{
    public static class SteelPebToolSchema
    {
        public static DceProToolDefinition CreateDefinition()
        {
            var properties = new Dictionary<string, object>
            {
                ["operation"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "portal_frame", "moment_connection", "base_plate", "wind_load" },
                    ["description"] = "Steel/PEB design operation to perform."
                },
                ["spanMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Clear bay span in millimeters for portal frame."
                },
                ["eaveHeightMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Eave height in millimeters for portal frame."
                },
                ["roofSlopePercent"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Roof slope percentage (e.g. 10 for 10%)."
                },
                ["baySpacingMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Longitudinal frame bay spacing in millimeters."
                },
                ["bendingMomentKnm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Design bending moment in kNm."
                },
                ["axialForceKn"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Design axial force in kN."
                },
                ["shearForceKn"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Design shear force in kN."
                },
                ["boltDiameterMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "High-strength bolt diameter in millimeters."
                },
                ["boltGrade"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Bolt grade (e.g. 'Grade8_8', 'Grade10_9')."
                },
                ["endPlateThicknessMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Moment connection end plate thickness in millimeters."
                },
                ["plateWidthMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Base plate width in millimeters."
                },
                ["plateLengthMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Base plate length in millimeters."
                },
                ["plateThicknessMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Base plate thickness in millimeters."
                },
                ["anchorBoltCount"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Number of anchor bolts (e.g. 4, 6, 8)."
                },
                ["anchorBoltDiameterMm"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Anchor bolt diameter in millimeters."
                },
                ["windZone"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "ZoneI", "ZoneII", "ZoneIII", "ZoneIV", "ZoneV" },
                    ["description"] = "TCVN 2737:2023 wind zone."
                },
                ["terrainCategory"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "A", "B", "C", "D" },
                    ["description"] = "Terrain roughness category."
                },
                ["heightZM"] = new Dictionary<string, object>
                {
                    ["type"] = "number",
                    ["description"] = "Elevation z above ground level in meters."
                }
            };

            var schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = new[] { "operation" }
            };

            return new DceProToolDefinition(
                "dce_design_steel_peb",
                "TCVN 5575:2024 PEB portal frame, bolted moment connection, anchor bolt base plate & TCVN 2737:2023 wind load.",
                schema);
        }
    }
}
