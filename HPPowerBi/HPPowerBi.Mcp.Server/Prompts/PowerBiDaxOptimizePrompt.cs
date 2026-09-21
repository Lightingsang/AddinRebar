using System.ComponentModel;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Prompts;

/// <summary>
///     Prompts guiding the AI on DAX optimization, best practices, and model exploration.
/// </summary>
[McpServerPromptType]
public static class PowerBiDaxOptimizePrompt
{
    private const string Persona =
        "You are an expert Power BI and DAX optimization engineer writing high-performance DAX queries and measures. " +
        "Best practices: " +
        "1. Prefer DIVIDE(numerator, denominator, alternateResult) over the division operator '/' to avoid division-by-zero errors. " +
        "2. Cache intermediate scalar or table results with VAR / RETURN syntax to prevent redundant recalculations. " +
        "3. Avoid using FILTER over entire fact tables; filter on dimension tables or use CALCULATE with boolean filter predicates. " +
        "4. Rely on star schema relationships with RELATED / RELATEDTABLE instead of expensive CROSSJOIN or manual Lookups. " +
        "5. For queries, use EVALUATE, SUMMARIZECOLUMNS, and TOPN with proper sorting. " +
        "6. Always use formatted expressions with standard indentation and uppercase keywords.";

    [McpServerPrompt(Name = "powerbi_dax_optimize", Title = "Optimize DAX Expression")]
    [Description("Optimizes a DAX measure or query for performance, readability, and best practices.")]
    public static ChatMessage[] Optimize(
        [Description("The DAX expression, query, or measure to optimize.")]
        string dax,
        [Description("Optional table name or semantic model context.")]
        string? tableContext = null)
    {
        var contextNote = string.IsNullOrWhiteSpace(tableContext) ? string.Empty : $" within table '{tableContext}'";
        return
        [
            new ChatMessage(ChatRole.System, Persona),
            new ChatMessage(ChatRole.User, $"Please optimize the following DAX expression{contextNote}:\n\n{dax}"),
            new ChatMessage(ChatRole.Assistant,
                "I will analyze the DAX expression, apply performance best practices (such as variable caching with VAR/RETURN, safe division with DIVIDE, and efficient filter context modification), and provide the optimized version with a step-by-step explanation.")
        ];
    }
}
