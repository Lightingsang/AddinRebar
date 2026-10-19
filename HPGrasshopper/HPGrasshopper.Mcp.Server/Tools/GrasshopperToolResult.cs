namespace HPGrasshopper.Mcp.Server.Tools;

using System.Text.Json;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;

/// <summary>
///     Maps JSON-RPC results from the Grasshopper MCP bridge to MCP CallToolResult,
///     ensuring failure payloads (success == false or isError == true) set CallToolResult.IsError = true
///     while preserving full diagnostic JSON in Content.
/// </summary>
public static class GrasshopperToolResult
{
    public static CallToolResult From(JsonElement result, ResultFormatter formatter)
    {
        if (IsFailure(result))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = BridgeJson.Serialize(result) }]
            };
        }

        return formatter.Text(result);
    }

    public static bool IsFailure(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return false;

        if (element.TryGetProperty("isError", out var ie) && ie.ValueKind == JsonValueKind.True)
            return true;

        if (element.TryGetProperty("success", out var s) && s.ValueKind == JsonValueKind.False)
            return true;

        return false;
    }
}
