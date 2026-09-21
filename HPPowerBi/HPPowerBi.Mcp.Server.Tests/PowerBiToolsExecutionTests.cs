using System.Text.Json;
using HPPowerBi.Mcp.Server.Hosts;
using HPPowerBi.Mcp.Server.Prompts;
using HPPowerBi.Mcp.Server.Resources;
using HPPowerBi.Mcp.Server.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using Xunit;

namespace HPPowerBi.Mcp.Server.Tests;

/// <summary>
///     Comprehensive integration tests verifying all 12 Power BI tools, resources, and prompts
///     over a real named pipe connection with the Power BI JSON-RPC protocol wire contract.
/// </summary>
public sealed class PowerBiToolsExecutionTests : IAsyncLifetime
{
    private const string DisabledText =
        "Code execution is disabled. Ask the user to tick 'Allow Model Modifications / DAX Execution' in the HPPowerBi MCP Bridge window.";

    private readonly string _pipeName = "hppowerbi-mcp-test-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new FakeRevitExecutor();
    private readonly BridgeSettings _settings = new BridgeSettings { ExecutionEnabled = true };

    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ContextService _context = null!;
    private ExecuteCodeService _execute = null!;
    private ResultFormatter _formatter = null!;
    private IOptions<BridgeOptions> _options = null!;

    public string? LastCustomMethod { get; private set; }
    public JsonElement? LastCustomParams { get; private set; }

    public ValueTask InitializeAsync()
    {
        var dispatcher = new RequestDispatcher(
            _executor,
            _settings,
            "2026",
            "Power BI",
            DisabledText,
            customHandler: DispatchCustomAsync);

        _listener = new PipeListener(_pipeName, dispatcher);
        _listener.Start();

        _options = Options.Create(new BridgeOptions
        {
            HostId = "powerbi",
            HostVersion = 2026,
            PipeName = _pipeName,
            ConnectTimeoutMs = 3000,
            PingIntervalSeconds = 60
        });

        _client = new RevitBridgeClient(_options, NullLogger<RevitBridgeClient>.Instance, PowerBiHostProfile.Instance);
        _formatter = new ResultFormatter();
        _context = new ContextService(_client, _formatter);
        _execute = new ExecuteCodeService(_client, _formatter, _options);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _listener.StopAsync();
        _listener.Dispose();
    }

    private Task<JsonRpcEnvelope?> DispatchCustomAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        LastCustomMethod = request.Method;
        LastCustomParams = request.Params;

        var suffix = JsonRpcMethods.Suffix(request.Method ?? string.Empty);
        return suffix switch
        {
            "schema" => Task.FromResult<JsonRpcEnvelope?>(
                JsonRpcEnvelope.Success(id, new
                {
                    Tables = new[] { "FactSales", "DimCustomer" },
                    MeasureCount = 5,
                    RelationshipCount = 4
                })),

            "dax" => Task.FromResult<JsonRpcEnvelope?>(
                JsonRpcEnvelope.Success(id, "| TotalSales |\n|---|\n| 1500000 |")),

            "measure.upsert" => Task.FromResult<JsonRpcEnvelope?>(
                JsonRpcEnvelope.Success(id, new
                {
                    Success = true,
                    TableName = "FactSales",
                    MeasureName = "Total Sales",
                    Snapshot = "20260921-120000-upsert_Total_Sales.json"
                })),

            "measure.delete" => Task.FromResult<JsonRpcEnvelope?>(
                JsonRpcEnvelope.Success(id, new
                {
                    Deleted = true,
                    TableName = "FactSales",
                    MeasureName = "Old Measure",
                    Snapshot = "20260921-120000-delete_Old_Measure.json"
                })),

            "relationship.manage" => Task.FromResult<JsonRpcEnvelope?>(
                JsonRpcEnvelope.Success(id, new
                {
                    Success = true,
                    Action = "create",
                    FromTable = "FactSales",
                    FromColumn = "CustomerKey",
                    ToTable = "DimCustomer",
                    ToColumn = "CustomerKey",
                    IsActive = true
                })),

            "format_dax" => Task.FromResult<JsonRpcEnvelope?>(
                JsonRpcEnvelope.Success(id, "EVALUATE\nROW(\"Total\", [Total Sales])")),

            "cloud.workspaces" => Task.FromResult<JsonRpcEnvelope?>(
                JsonRpcEnvelope.Success(id, new[]
                {
                    new { Id = "ws-001", Name = "Finance Analytics" }
                })),

            "cloud.datasets" => Task.FromResult<JsonRpcEnvelope?>(
                JsonRpcEnvelope.Success(id, new[]
                {
                    new { Id = "ds-001", Name = "Executive Dashboard Model" }
                })),

            "cloud.refresh" => Task.FromResult<JsonRpcEnvelope?>(
                JsonRpcEnvelope.Success(id, new
                {
                    Status = "Submitted",
                    DatasetId = "ds-001",
                    RefreshType = "OnDemand"
                })),

            "cloud.dax" => Task.FromResult<JsonRpcEnvelope?>(
                JsonRpcEnvelope.Success(id, new
                {
                    Results = new[]
                    {
                        new { Tables = new[] { new { Rows = new[] { new { Sales = 42000 } } } } }
                    }
                })),

            _ => Task.FromResult<JsonRpcEnvelope?>(null)
        };
    }

    private static string TextOf(CallToolResult result) => ((TextContentBlock)result.Content[0]).Text;

    private static ContextResult PowerBiContext() => new ContextResult
    {
        RevitVersion = "2026",
        Host = "powerbi",
        HostVersion = "2026",
        DocTitle = "SalesModel.pbix",
        DocPath = @"C:\Models\SalesModel.pbix",
        IsModifiable = true,
        ExecutionEnabled = true,
        PowerBi = new PowerBiInfo(
            IsConnected: true,
            AttachedPid: 5432,
            LocalPort: 51234,
            DatabaseName: "d1e2f3-guid",
            CompatibilityLevel: "1600",
            MutationEnabled: true,
            TableCount: 15,
            MeasureCount: 45,
            RelationshipCount: 12)
    };

    [Fact]
    public async Task GetPowerBiContextTool_ReturnsPowerBiBlock_And_StripsRevitFields()
    {
        _executor.ContextHandler = _ => PowerBiContext();

        var tool = new GetPowerBiContextTool(_context);
        var result = await tool.GetContextAsync(false, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        using var json = JsonDocument.Parse(TextOf(result));
        var root = json.RootElement;

        Assert.Equal("powerbi", root.GetProperty("host").GetString());
        Assert.Equal("2026", root.GetProperty("hostVersion").GetString());
        Assert.Equal("SalesModel.pbix", root.GetProperty("docTitle").GetString());
        Assert.True(root.GetProperty("isModifiable").GetBoolean());
        Assert.True(root.GetProperty("executionEnabled").GetBoolean());

        // Confirms Revit-specific fields are cleanly stripped
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));

        var pbi = root.GetProperty("powerBi");
        Assert.True(pbi.GetProperty("isConnected").GetBoolean());
        Assert.Equal(5432, pbi.GetProperty("attachedPid").GetInt32());
        Assert.Equal(51234, pbi.GetProperty("localPort").GetInt32());
        Assert.Equal("d1e2f3-guid", pbi.GetProperty("databaseName").GetString());
        Assert.Equal("1600", pbi.GetProperty("compatibilityLevel").GetString());
        Assert.True(pbi.GetProperty("mutationEnabled").GetBoolean());
        Assert.Equal(15, pbi.GetProperty("tableCount").GetInt32());
        Assert.Equal(45, pbi.GetProperty("measureCount").GetInt32());
        Assert.Equal(12, pbi.GetProperty("relationshipCount").GetInt32());
    }

    [Fact]
    public async Task ExecutePowerBiCodeTool_ExecutesScript_And_ReturnsSnapshot()
    {
        _executor.ExecuteHandler = request => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new { updated = true }),
            ValueType = "object",
            Changed = new ChangedCounts(1, 0, 0),
            RolledBack = false,
            DurationMs = 25,
            Snapshot = "20260921-120000-add_measure.json"
        };

        var tool = new ExecutePowerBiCodeTool(_execute);
        var args = JsonSerializer.SerializeToElement(new { measure = "Revenue" });
        var result = await tool.ExecuteAsync(
            code: "return model.Tables.Count;",
            transaction: TransactionModes.Auto,
            dryRun: false,
            timeoutSeconds: 45,
            label: "add measure",
            args: args,
            progress: null,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var request = _executor.LastExecuteRequest!;
        Assert.Equal("return model.Tables.Count;", request.Code);
        Assert.Equal("auto", request.Transaction);
        Assert.Equal(45, request.TimeoutSeconds);
        Assert.Equal("add measure", request.Label);
        Assert.Equal("Revenue", request.Args!.Value.GetProperty("measure").GetString());

        using var json = JsonDocument.Parse(TextOf(result));
        Assert.Equal("20260921-120000-add_measure.json", json.RootElement.GetProperty("snapshot").GetString());
    }

    [Fact]
    public async Task ExecutePowerBiCodeTool_ClampsTimeout_To_600Seconds()
    {
        var tool = new ExecutePowerBiCodeTool(_execute);

        var accepted = await tool.ExecuteAsync("return 1;", timeoutSeconds: 600, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(accepted.IsError);
        Assert.Equal(600, _executor.LastExecuteRequest!.TimeoutSeconds);

        var clamped = await tool.ExecuteAsync("return 1;", timeoutSeconds: 999, cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(clamped.IsError);
        Assert.Equal(600, _executor.LastExecuteRequest!.TimeoutSeconds);
    }

    [Fact]
    public async Task ExecutePowerBiCodeTool_Surfaces_ExecutionDisabledError()
    {
        _settings.ExecutionEnabled = false;

        var tool = new ExecutePowerBiCodeTool(_execute);
        var result = await tool.ExecuteAsync("return 1;", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow Model Modifications / DAX Execution", TextOf(result));
    }

    [Fact]
    public async Task PowerBiSchemaTool_CallsWireMethod_WithParameters()
    {
        var tool = new PowerBiSchemaTool(_client, _formatter);
        var result = await tool.GetSchemaAsync("FactSales", true, true, true, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("powerbi.schema", LastCustomMethod);
        Assert.NotNull(LastCustomParams);

        var text = TextOf(result);
        Assert.Contains("FactSales", text);
        Assert.Contains("DimCustomer", text);
    }

    [Fact]
    public async Task PowerBiEvaluateDaxTool_CallsWireMethod_WithParameters()
    {
        var tool = new PowerBiEvaluateDaxTool(_client, _formatter);
        var result = await tool.EvaluateDaxAsync("EVALUATE DimCustomer", 50, "markdown", TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("powerbi.dax", LastCustomMethod);
        Assert.NotNull(LastCustomParams);

        var root = LastCustomParams.Value;
        Assert.Equal("EVALUATE DimCustomer", root.GetProperty("query").GetString());
        Assert.Equal(50, root.GetProperty("maxRows").GetInt32());

        var text = TextOf(result);
        Assert.Contains("TotalSales", text);
    }

    [Fact]
    public async Task PowerBiCreateOrUpdateMeasureTool_CallsWireMethod_WithParameters()
    {
        var tool = new PowerBiCreateOrUpdateMeasureTool(_client, _formatter);
        var result = await tool.CreateOrUpdateMeasureAsync(
            tableName: "FactSales",
            measureName: "Total Sales",
            expression: "SUM(Sales[Amount])",
            description: "Total revenue",
            formatString: "$#,##0",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("powerbi.measure.upsert", LastCustomMethod);
        Assert.NotNull(LastCustomParams);

        var root = LastCustomParams.Value;
        Assert.Equal("FactSales", root.GetProperty("tableName").GetString());
        Assert.Equal("Total Sales", root.GetProperty("measureName").GetString());
        Assert.Equal("SUM(Sales[Amount])", root.GetProperty("expression").GetString());
        Assert.Equal("Total revenue", root.GetProperty("description").GetString());
        Assert.Equal("$#,##0", root.GetProperty("formatString").GetString());

        var text = TextOf(result);
        Assert.Contains("20260921-120000-upsert_Total_Sales.json", text);
    }

    [Fact]
    public async Task PowerBiDeleteMeasureTool_CallsWireMethod_WithParameters()
    {
        var tool = new PowerBiDeleteMeasureTool(_client, _formatter);
        var result = await tool.DeleteMeasureAsync("FactSales", "Old Measure", TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("powerbi.measure.delete", LastCustomMethod);
        Assert.NotNull(LastCustomParams);

        var root = LastCustomParams.Value;
        Assert.Equal("FactSales", root.GetProperty("tableName").GetString());
        Assert.Equal("Old Measure", root.GetProperty("measureName").GetString());

        var text = TextOf(result);
        Assert.Contains("deleted", text);
    }

    [Fact]
    public async Task PowerBiManageRelationshipTool_CallsWireMethod_WithParameters()
    {
        var tool = new PowerBiManageRelationshipTool(_client, _formatter);
        var result = await tool.ManageRelationshipAsync(
            fromTable: "FactSales",
            fromColumn: "CustomerKey",
            toTable: "DimCustomer",
            toColumn: "CustomerKey",
            isActive: true,
            crossFilteringBehavior: "OneDirection",
            action: "create",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("powerbi.relationship.manage", LastCustomMethod);
        Assert.NotNull(LastCustomParams);

        var root = LastCustomParams.Value;
        Assert.Equal("create", root.GetProperty("action").GetString());
        Assert.Equal("FactSales", root.GetProperty("fromTable").GetString());
        Assert.Equal("DimCustomer", root.GetProperty("toTable").GetString());

        var text = TextOf(result);
        Assert.Contains("FactSales", text);
    }

    [Fact]
    public async Task PowerBiFormatDaxTool_CallsWireMethod_WithParameters()
    {
        var tool = new PowerBiFormatDaxTool(_client, _formatter);
        var result = await tool.FormatDaxAsync("evaluate row(\"test\", 1)", TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("powerbi.format_dax", LastCustomMethod);
        Assert.NotNull(LastCustomParams);
        Assert.Equal("evaluate row(\"test\", 1)", LastCustomParams.Value.GetProperty("dax").GetString());

        var text = TextOf(result);
        Assert.Contains("EVALUATE", text);
    }

    [Fact]
    public async Task CloudTools_CallWireMethods_WithExpectedPayloads()
    {
        // 1. List workspaces
        var wsTool = new PowerBiCloudListWorkspacesTool(_client, _formatter);
        var wsResult = await wsTool.ListWorkspacesAsync(TestContext.Current.CancellationToken);
        Assert.False(wsResult.IsError);
        Assert.Equal("powerbi.cloud.workspaces", LastCustomMethod);
        Assert.Contains("Finance Analytics", TextOf(wsResult));

        // 2. List datasets
        var dsTool = new PowerBiCloudListDatasetsTool(_client, _formatter);
        var dsResult = await dsTool.ListDatasetsAsync("ws-001", TestContext.Current.CancellationToken);
        Assert.False(dsResult.IsError);
        Assert.Equal("powerbi.cloud.datasets", LastCustomMethod);
        Assert.Equal("ws-001", LastCustomParams!.Value.GetProperty("workspaceId").GetString());
        Assert.Contains("Executive Dashboard Model", TextOf(dsResult));

        // 3. Trigger refresh
        var refTool = new PowerBiCloudTriggerRefreshTool(_client, _formatter);
        var refResult = await refTool.TriggerRefreshAsync("ds-001", "ws-001", "NoNotification", TestContext.Current.CancellationToken);
        Assert.False(refResult.IsError);
        Assert.Equal("powerbi.cloud.refresh", LastCustomMethod);
        Assert.Equal("ds-001", LastCustomParams!.Value.GetProperty("datasetId").GetString());
        Assert.Contains("Submitted", TextOf(refResult));

        // 4. Execute cloud DAX
        var cloudDaxTool = new PowerBiCloudExecuteDaxTool(_client, _formatter);
        var cloudDaxResult = await cloudDaxTool.ExecuteCloudDaxAsync("ds-001", "EVALUATE Sales", "ws-001", TestContext.Current.CancellationToken);
        Assert.False(cloudDaxResult.IsError);
        Assert.Equal("powerbi.cloud.dax", LastCustomMethod);
        Assert.Equal("ds-001", LastCustomParams!.Value.GetProperty("datasetId").GetString());
        Assert.Contains("42000", TextOf(cloudDaxResult));
    }

    [Fact]
    public async Task Resources_And_Prompts_ExecuteCorrectly()
    {
        _executor.ContextHandler = _ => PowerBiContext();

        // Schema resource
        var schemaRes = new PowerBiSchemaResource(_client, _context);
        var schemaJson = await schemaRes.GetSchemaAsync(TestContext.Current.CancellationToken);
        Assert.Contains("FactSales", schemaJson);

        // Document info resource
        var docJson = await schemaRes.DocumentInfoAsync(TestContext.Current.CancellationToken);
        Assert.Contains("SalesModel.pbix", docJson);

        // Prompt
        var messages = PowerBiDaxOptimizePrompt.Optimize(
            "CALCULATE([Sales], FILTER(Sales, Sales[Region]=\"West\"))",
            "FactSales");
        Assert.Equal(3, messages.Length);
        Assert.Contains("DIVIDE", messages[0].Text);
        Assert.Contains("CALCULATE([Sales]", messages[1].Text);
    }

    [Fact]
    public async Task ExecutePowerBiCodeTool_StaticPreview_ReturnsPreviewDiagnostic()
    {
        _executor.ExecuteHandler = _ => new ExecuteResult
        {
            IsError = true,
            RolledBack = true,
            Message = "static preview: script would mutate model",
            Diagnostics = [new ScriptDiagnostic(1, 1, "PREVIEW", "model.SaveChanges()")]
        };

        var tool = new ExecutePowerBiCodeTool(_execute);
        var result = await tool.ExecuteAsync(
            code: "model.SaveChanges();",
            transaction: TransactionModes.Auto,
            dryRun: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("PREVIEW", text);
        Assert.Contains("model.SaveChanges()", text);
    }

    [Fact]
    public async Task PowerBiSchemaTool_WithFilters_PassesAllFilterOptions()
    {
        var tool = new PowerBiSchemaTool(_client, _formatter);
        var result = await tool.GetSchemaAsync(
            tableName: "FactSales",
            includeColumns: false,
            includeMeasures: false,
            includeRelationships: false,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal("powerbi.schema", LastCustomMethod);
        Assert.NotNull(LastCustomParams);

        var root = LastCustomParams.Value;
        Assert.Equal("FactSales", root.GetProperty("tableName").GetString());
        Assert.False(root.GetProperty("includeColumns").GetBoolean());
        Assert.False(root.GetProperty("includeMeasures").GetBoolean());
        Assert.False(root.GetProperty("includeRelationships").GetBoolean());
    }

    [Fact]
    public async Task PowerBiEvaluateDaxTool_ClampsRowBounds()
    {
        var tool = new PowerBiEvaluateDaxTool(_client, _formatter);

        // Test lower bound clamp (< 1 -> 1)
        await tool.EvaluateDaxAsync("EVALUATE TopN", maxRows: -10, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, LastCustomParams!.Value.GetProperty("maxRows").GetInt32());

        // Test upper bound clamp (> 10000 -> 10000)
        await tool.EvaluateDaxAsync("EVALUATE TopN", maxRows: 50000, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(10000, LastCustomParams!.Value.GetProperty("maxRows").GetInt32());
    }

    [Fact]
    public void PowerBiDaxOptimizePrompt_WithoutTableContext_ProducesCleanPrompt()
    {
        var messages = PowerBiDaxOptimizePrompt.Optimize("EVALUATE ROW(\"Value\", 1)", null);
        Assert.Equal(3, messages.Length);
        Assert.DoesNotContain("within table", messages[1].Text);
        Assert.Contains("EVALUATE ROW", messages[1].Text);
    }
}
