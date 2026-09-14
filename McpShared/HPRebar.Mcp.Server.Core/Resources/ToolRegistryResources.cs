using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Resources;

/// <summary>The library as resources, for hosts that let the user attach context explicitly.</summary>
[McpServerResourceType]
public sealed class ToolRegistryResources(ToolManager manager)
{
    [McpServerResource(UriTemplate = "registry://tools", Name = "registry_tools", Title = "Tool registry", MimeType = "application/json")]
    [Description("Every tool in the library with status, category, version and stability.")]
    public string List()
    {
        var stats = manager.Db.AllStats(manager.Options.RunWindow);
        return BridgeJson.Serialize(manager.Tools
            .OrderBy(t => t.Category).ThenBy(t => t.Name)
            .Select(t =>
            {
                var s = stats.TryGetValue(t.Name, out var st) ? st : RunStats.Empty;
                return new { t.Name, t.Title, t.Category, status = ToolRegistryDb.StatusText(t.Status), t.Version, t.Transaction, stability = StabilityScorer.Score(s), runs = s.Runs, t.Description };
            }));
    }

    [McpServerResource(UriTemplate = "registry://tools/{name}", Name = "registry_tool", Title = "Registry tool", MimeType = "application/json")]
    [Description("One tool: metadata, input schema, examples and code.")]
    public string Get(string name)
    {
        var details = manager.Get(name) ?? throw new McpException($"No tool named '{name}'.");
        var r = details.Record;
        return BridgeJson.Serialize(new
        {
            r.Name, r.Title, r.Description, r.Category, r.Tags, status = ToolRegistryDb.StatusText(r.Status), r.Version, r.Transaction, r.TimeoutSeconds,
            r.Destructive, r.RevitVersions, r.Author, r.ApprovedBy, r.CreatedAt, r.PublishedAt, r.Notes,
            inputSchema = RegistryToolFunction.BuildSchema(r), examples = r.Examples, code = r.Code,
            stats = new { details.Stats.Runs, details.Stats.Successes, stability = details.Stability, details.Stats.LastRun, details.Stats.LastError },
        });
    }
}
