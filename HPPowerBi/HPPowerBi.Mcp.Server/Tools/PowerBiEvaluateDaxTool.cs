using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Tools;

/// <summary>Tool to execute arbitrary DAX queries against the local Power BI Desktop tabular model.</summary>
[McpServerToolType]
public sealed class PowerBiEvaluateDaxTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    [McpServerTool(
        Name = "powerbi_evaluate_dax",
        Title = "Evaluate DAX Query",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description(
        "Executes a DAX query (typically starting with EVALUATE) against the active Power BI Desktop model via ADOMD.NET. " +
        "Returns tabular results formatted as Markdown table or JSON rows with execution duration and row count. " +
        "Safely capped by maxRows (default 100, max 10000) to protect context.")]
    public Task<CallToolResult> EvaluateDaxAsync(
        [Description("The DAX query to execute (e.g. 'EVALUATE TOPN(10, DimCustomer)' or 'EVALUATE ROW(\"Revenue\", [Total Revenue])').")]
        string query,
        [Description("Maximum number of rows to return (default 100, max 10000).")]
        int maxRows = 100,
        [Description("Output format: 'markdown' (default table) or 'json' (array of objects).")]
        string format = "markdown",
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return formatter.Error("DAX query cannot be empty.");
            }

            if (!string.IsNullOrEmpty(format) &&
                !string.Equals(format, "markdown", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                return formatter.Error($"Unsupported format '{format}'. Supported formats: 'markdown', 'json'.");
            }

            var clamped = Math.Clamp(maxRows, 1, 10000);
            var parameters = new
            {
                Query = query,
                MaxRows = clamped,
                TopN = clamped,
                Format = format,
            };

            var result = await bridge.SendAsync<object>(
                bridge.Profile.Method("dax"),
                parameters,
                TimeSpan.FromSeconds(60),
                null,
                cancellationToken).ConfigureAwait(false);

            return formatter.Text(result);
        });
    }
}
