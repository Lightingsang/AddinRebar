using System.Text.Json;
using System.Text.Json.Serialization;

namespace HPRebar.Mcp.Contracts.JsonRpc;

/// <summary>The `error` member of a JSON-RPC 2.0 response. Codes come from <see cref="BridgeErrorCode"/>.</summary>
public sealed class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}
