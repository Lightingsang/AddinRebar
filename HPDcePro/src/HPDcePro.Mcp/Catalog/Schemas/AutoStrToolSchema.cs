using System.Collections.Generic;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Catalog.Schemas
{
    public static class AutoStrToolSchema
    {
        public static DceProToolDefinition CreateDefinition()
        {
            var properties = new Dictionary<string, object>
            {
                ["operation"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "run_pipeline", "load_config", "export_boq" },
                    ["description"] = "AutoStr pipeline operation to perform."
                },
                ["autostrFilePath"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Path to .autostr project JSON configuration file."
                },
                ["checkpointPath"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Path to checkpoint JSON file for resuming an interrupted pipeline."
                },
                ["outputBoqPath"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Destination Excel (.xlsx) file path for multi-sheet BOQ export."
                },
                ["projectName"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Project title for reporting and BOQ workbooks."
                },
                ["maxSteps"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Maximum pipeline steps to execute (1-24, default: 24)."
                },
                ["enableAutoResizing"] = new Dictionary<string, object>
                {
                    ["type"] = "boolean",
                    ["description"] = "Enable automated section resizing loops when capacity checks fail."
                }
            };

            var schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = new[] { "operation" }
            };

            return new DceProToolDefinition(
                "dce_run_autostr_pipeline",
                "Execute the 24-step AutoStrOrchestrator on a .autostr project with auto-resizing & multi-sheet ClosedXML BOQ export.",
                schema);
        }
    }
}
