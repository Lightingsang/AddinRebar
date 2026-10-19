using System.Collections.Generic;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Catalog.Schemas
{
    public static class MayHocToolSchema
    {
        public static DceProToolDefinition CreateDefinition()
        {
            var properties = new Dictionary<string, object>
            {
                ["operation"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "tokenize_text", "query_database", "query_history", "model_info" },
                    ["description"] = "Machine learning or text tokenization operation to perform."
                },
                ["text"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Vietnamese CAD rebar text annotation to normalize and tokenize (e.g. '2d18 + 1d20 L=3500')."
                },
                ["dbPath"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Optional file path to mauhoc.db SQLite database."
                },
                ["limit"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Maximum number of database records to return (default: 20)."
                },
                ["offset"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Database query offset for pagination."
                }
            };

            var schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = new[] { "operation" }
            };

            return new DceProToolDefinition(
                "dce_query_mayhoc_rebar",
                "Query MayHoc/mauhoc.db ML patterns & tokenize Vietnamese rebar callout strings.",
                schema);
        }
    }
}
