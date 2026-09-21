# Dispatch: worker_m3
Role: teamwork_preview_worker
Target: Implement Milestone 3: MCP Stdio Server & Tools Catalog (HPPowerBi.Mcp.Server)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Project Blueprint: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_5\PROJECT.md
Server Explorer Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_server_1\report.md
Output: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3\handoff.md

## 2026-09-21T07:22:35Z
You are worker_m3, a teamwork_preview_worker.
Your working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3
Project root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Authoritative user request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (specifically section ## 2026-09-21T06:10:48Z)
Project Blueprint: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_5\PROJECT.md
Server Explorer Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_server_1\report.md

Objective:
Implement Milestone 3: MCP Stdio Server & Tools Catalog (HPPowerBi.Mcp.Server).

Tasks:
1. PowerBiHostProfile in HPPowerBi/HPPowerBi.Mcp.Server/Hosts/PowerBi/PowerBiHostProfile.cs:
   - Implement IHostProfile:
     * HostName: "Power BI"
     * DefaultVersion: "2026"
     * PipeName: PipeNaming.For(PipeNaming.PowerBiHost, 2026) ("hppowerbi-mcp-2026")
     * MethodPrefix: JsonRpcMethods.PowerBiPrefix ("powerbi.")
     * MaxTimeoutSeconds: HostScriptContracts.PowerBiHeavyMaxTimeoutSeconds (600)
     * HostAssembly: typeof(PowerBiHostProfile).Assembly
     * ScriptImports: HostScriptContracts.PowerBiImports
     * BridgeNotConnectedHint & TimeoutSemanticsHint providing clear user guidance
2. Core Local Tools in HPPowerBi/HPPowerBi.Mcp.Server/Tools/:
   Annotate classes with [McpServerToolType] and methods with [McpServerTool]:
   - GetPowerBiContextTool: "get_powerbi_context" -> calls ContextService to retrieve model summary, counts, and safety status.
   - ExecutePowerBiCodeTool: "execute_powerbi_code" -> compiles and runs C# script with globals { model, server, adomd, ct, log, progress, args } via ExecuteCodeService.
   - PowerBiSchemaTool: "powerbi_get_schema" -> calls bridge.SendAsync with "powerbi.schema" to get tables, columns, measures, partitions, relationships.
   - PowerBiEvaluateDaxTool: "powerbi_evaluate_dax" -> calls bridge.SendAsync with "powerbi.dax" (passing query, maxRows, format: "markdown"|"json").
   - PowerBiCreateOrUpdateMeasureTool: "powerbi_create_or_update_measure" -> calls bridge.SendAsync with "powerbi.measure.upsert" (tableName, measureName, expression, description, formatString).
   - PowerBiDeleteMeasureTool: "powerbi_delete_measure" -> calls bridge.SendAsync with "powerbi.measure.delete" (tableName, measureName).
   - PowerBiManageRelationshipTool: "powerbi_manage_relationship" -> calls bridge.SendAsync with "powerbi.relationship.manage" (fromTable, fromColumn, toTable, toColumn, isActive, crossFilteringBehavior).
   - PowerBiFormatDaxTool: "powerbi_format_dax" -> calls bridge.SendAsync with "powerbi.format_dax" to format DAX queries deterministically.
3. Cloud REST Tools in HPPowerBi/HPPowerBi.Mcp.Server/Tools/:
   - PowerBiCloudListWorkspacesTool: "powerbi_cloud_list_workspaces" -> calls bridge.SendAsync with "powerbi.cloud.workspaces".
   - PowerBiCloudListDatasetsTool: "powerbi_cloud_list_datasets" -> calls bridge.SendAsync with "powerbi.cloud.datasets" (workspaceId).
   - PowerBiCloudTriggerRefreshTool: "powerbi_cloud_trigger_refresh" -> calls bridge.SendAsync with "powerbi.cloud.refresh" (workspaceId, datasetId).
   - PowerBiCloudExecuteDaxTool: "powerbi_cloud_execute_dax" -> calls bridge.SendAsync with "powerbi.cloud.dax" (datasetId, query).
4. Resources & Prompts in HPPowerBi/HPPowerBi.Mcp.Server/Resources/ and Prompts/:
   - PowerBiSchemaResource: "powerbi://schema"
   - PowerBiDaxOptimizePrompt: "powerbi_dax_optimize"
5. Program.cs in HPPowerBi/HPPowerBi.Mcp.Server/:
   - Single line: return await McpServerHost.RunAsync(args, PowerBiHostProfile.Instance);
6. Automated Server Tests in HPPowerBi/HPPowerBi.Mcp.Server.Tests/:
   - Add comprehensive tests:
     * PowerBiHostProfileTests: verifies all profile invariants (name, version, pipe, prefix, timeout, imports).
     * PowerBiToolCatalogTests: builds host with McpServerHost.CreateBuilder([], profile).Build() and verifies all 12 Power BI tools + 8 McpShared dynamic registry tools are registered with valid schemas and descriptions.
     * PowerBiToolsExecutionTests: tests tool execution over pipe client backed by mock executor or fake pipe listener.
7. Verification:
   Run:
   dotnet build HPPowerBi/HPPowerBi.slnx -c Debug
   dotnet run --project HPPowerBi/HPPowerBi.Mcp.Server.Tests/HPPowerBi.Mcp.Server.Tests.csproj
   dotnet run --project HPPowerBi/HPPowerBi.McpBridge.Tests/HPPowerBi.McpBridge.Tests.csproj
   dotnet run --project McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj
   Ensure 0 errors, 0 warnings, and 100% tests pass.
   Write handoff report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m3\handoff.md
   Send completion message via send_message to orchestrator_5 (conversation ID: 4d88b310-8910-4f85-b5a8-50216392bc6b).
