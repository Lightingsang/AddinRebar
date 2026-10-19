namespace HPGrasshopper.Mcp.Server.Tools;

using System.ComponentModel;
using System.Text.Json;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

/// <summary>
///     Core visualization tool rendering high-resolution snapshots of the active Grasshopper canvas
///     into Base64 image data strings or saving PNG files directly to disk.
/// </summary>
[McpServerToolType]
public sealed class ExportGrasshopperCanvasImageTool(IRevitBridgeClient client, ResultFormatter formatter)
{
    public const string ToolDescription =
        "Renders a high-resolution snapshot of the Grasshopper canvas into PNG or Base64 data URL. " +
        "Supports full canvas extents, current viewport view, or selected component bounding boxes.";

    [McpServerTool(
        Name = GrasshopperHostProfile.ExportImageToolName,
        Title = "Export Grasshopper Canvas Image",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(ToolDescription)]
    public Task<CallToolResult> ExportImageAsync(
        [Description("Rendering scope: 'FullCanvas', 'Viewport', or 'Selection'. Default 'FullCanvas'.")]
        string mode = "FullCanvas",
        [Description("Image scaling factor, between 0.1 and 5.0. Default 1.0.")]
        double zoom = 1.0,
        [Description("Optional file path to save PNG on disk.")]
        string? outputPath = null,
        [Description("Return base64-encoded image string in JSON response. Default true.")]
        bool returnBase64 = true,
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            var requestParams = new
            {
                mode,
                zoom,
                outputPath,
                returnBase64
            };

            var result = await client.SendAsync<JsonElement>(
                client.Profile.Method(GrasshopperHostProfile.ExportImageToolName),
                requestParams,
                TimeSpan.FromSeconds(30),
                null,
                cancellationToken).ConfigureAwait(false);

            return GrasshopperToolResult.From(result, formatter);
        });
    }
}
