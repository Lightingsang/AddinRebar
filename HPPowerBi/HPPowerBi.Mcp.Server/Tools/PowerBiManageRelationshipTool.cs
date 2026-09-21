using System.ComponentModel;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace HPPowerBi.Mcp.Server.Tools;

/// <summary>Tool to manage table relationships in the Power BI tabular model.</summary>
[McpServerToolType]
public sealed class PowerBiManageRelationshipTool(IRevitBridgeClient bridge, ResultFormatter formatter)
{
    [McpServerTool(
        Name = "powerbi_manage_relationship",
        Title = "Manage Table Relationship",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description(
        "Creates, updates, or deletes a relationship between two tables in the Power BI model. " +
        "Action can be 'create', 'delete', or 'set_active'. " +
        "Requires 'Allow Model Modifications / DAX Execution' checked on the Bridge UI.")]
    public Task<CallToolResult> ManageRelationshipAsync(
        [Description("From table name (e.g. 'FactSales').")]
        string fromTable,
        [Description("From column name (e.g. 'CustomerKey').")]
        string fromColumn,
        [Description("To table name (e.g. 'DimCustomer').")]
        string toTable,
        [Description("To column name (e.g. 'CustomerKey').")]
        string toColumn,
        [Description("Whether the relationship is active (default true).")]
        bool isActive = true,
        [Description("Cross filtering behavior: 'OneDirection' or 'BothDirections' (default 'OneDirection').")]
        string crossFilteringBehavior = "OneDirection",
        [Description("Action: 'create', 'delete', or 'set_active' (default 'create').")]
        string action = "create",
        CancellationToken cancellationToken = default)
    {
        return formatter.RunAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(fromTable))
            {
                return formatter.Error("FromTable cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(fromColumn))
            {
                return formatter.Error("FromColumn cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(toTable))
            {
                return formatter.Error("ToTable cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(toColumn))
            {
                return formatter.Error("ToColumn cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(action))
            {
                return formatter.Error("Action cannot be empty.");
            }

            var normalizedAction = action.Trim().ToLowerInvariant();
            if (normalizedAction is not ("create" or "delete" or "set_active"))
            {
                return formatter.Error($"Unknown relationship action '{action}'. Supported actions: 'create', 'delete', 'set_active'.");
            }

            var parameters = new
            {
                Action = action,
                FromTable = fromTable,
                FromColumn = fromColumn,
                ToTable = toTable,
                ToColumn = toColumn,
                IsActive = isActive,
                CrossFilteringBehavior = crossFilteringBehavior,
            };

            var result = await bridge.SendAsync<object>(
                bridge.Profile.MethodPrefix + "relationship.manage",
                parameters,
                TimeSpan.FromSeconds(30),
                null,
                cancellationToken).ConfigureAwait(false);

            return formatter.Text(result);
        });
    }
}
