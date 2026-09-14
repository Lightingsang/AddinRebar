using System.ComponentModel;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPRebar.Mcp.Server.Tools.Registry;

/// <summary>
///     Read side of the tool memory. `search_tools` is the first thing the AI should call for any host
///     task: a stored tool is reviewed, parameterised and cheaper than generating code again.
/// </summary>
[McpServerToolType]
public sealed class ToolRegistryQueryTools(ToolManager manager, ResultFormatter formatter)
{
    [McpServerTool(Name = "search_tools", Title = "Search the tool registry", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description(
        "Search the registry of stored tools for this host (Revit or AutoCAD) BEFORE writing code with the execute tool (execute_revit_code / execute_autocad_code). Full-text over name, description, tags and examples, " +
        "ranked by relevance × stability × status. Returns each tool's inputSchema so it can be called directly by name (published tools are real MCP tools) " +
        "or through run_tool. Empty query lists tools (optionally by category).")]
    public CallToolResult Search(
        [Description("What you want to do, in any language, e.g. 'tạo lưới trục', 'color beams by type', 'room schedule'")] string? query = null,
        [Description(RegistryToolText.Category)] string? category = null,
        [Description("Maximum results, 1–50 (default 5)")] int limit = 5,
        [Description("Also return draft / tested / pending tools (they need allowUnpublished=true in run_tool)")] bool includeUnpublished = false)
    {
        var hits = manager.Search(query, category, limit, includeUnpublished);
        return formatter.Text(new
        {
            query,
            category,
            count = hits.Count,
            hint = hits.Count == 0
                ? $"No stored tool matches. Write the task with {manager.Profile.ExecuteToolName}; a successful run can be packaged with propose_tool."
                : "Call the tool by name (published) or run_tool {name, args}. Use dryRun first for tools that modify the document.",
            tools = hits,
        });
    }

    [McpServerTool(Name = "get_tool", Title = "Get a registry tool", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Full record of one stored tool: metadata, input schema, examples, C# code (optional), run statistics and recent runs.")]
    public CallToolResult Get(
        [Description("Tool name from search_tools")] string name,
        [Description("Include the C# source")] bool includeCode = true,
        [Description("How many recent runs to include (0–50)")] int recentRuns = 10)
    {
        var details = manager.Get(name, Math.Clamp(recentRuns, 0, 50));
        if (details is null) return formatter.Error(new ToolNotFoundException(name).Message);

        var r = details.Record;
        return formatter.Text(new
        {
            name = r.Name,
            title = r.Title,
            description = r.Description,
            category = r.Category,
            tags = r.Tags,
            status = ToolRegistryDb.StatusText(r.Status),
            version = r.Version,
            transaction = r.Transaction,
            timeoutSeconds = r.TimeoutSeconds,
            destructive = r.Destructive,
            revitVersions = r.RevitVersions,
            author = r.Author,
            createdFromRunId = r.CreatedFromRunId,
            approvedBy = r.ApprovedBy,
            createdAt = r.CreatedAt,
            updatedAt = r.UpdatedAt,
            publishedAt = r.PublishedAt,
            notes = r.Notes,
            inputSchema = RegistryToolFunction.BuildSchema(r),
            examples = r.Examples,
            code = includeCode ? r.Code : null,
            stats = new { details.Stats.Runs, details.Stats.Successes, details.Stats.Failures, successRate = Math.Round(details.Stats.SuccessRate, 3), stability = details.Stability, lastRun = details.Stats.LastRun, lastError = details.Stats.LastError },
            recentRuns = details.RecentRuns.Select(run => new { run.Id, run.Kind, run.DryRun, run.Success, run.DurationMs, run.Error, run.RevitVersion, run.DocTitle, run.Timestamp }),
            folder = r.Folder,
        });
    }
}
