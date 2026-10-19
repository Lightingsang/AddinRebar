using System;
using System.Text.Json.Serialization;

namespace HPDcePro.Mcp.Models
{
    public sealed class DceProToolDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("inputSchema")]
        public object InputSchema { get; set; } = new object();

        public DceProToolDefinition()
        {
        }

        public DceProToolDefinition(string name, string description, object inputSchema)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            InputSchema = inputSchema ?? throw new ArgumentNullException(nameof(inputSchema));
        }
    }
}
