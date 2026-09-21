# Investigation Report: HPPowerBi.Mcp.Server (R2), Automated Tests (R3), and Skill/Docs (R4)

**Date**: 2026-09-21  
**Agent**: `explorer_server_1` (teamwork_preview_explorer)  
**Deliverable Scope**: Technical architecture, tool parameter contracts, testing strategies, and ecosystem documentation for the Power BI MCP Subsystem.

---

## Executive Summary

The Power BI MCP Subsystem (`HPPowerBi`) extends the repository's host-neutral MCP architecture (`McpShared`) to Microsoft Power BI. It mirrors the proven standalone bridge/server architecture established by `HPEtabs` and `HPSap2000`, operating as an out-of-process client connecting to Power BI Desktop's local Analysis Services engine (AMO-TOM / ADOMD.NET) and Power BI Service Cloud REST API (via MSAL).

- **Server Architecture**: `HPPowerBi.Mcp.Server` (.NET 10 console) runs via Stdio transport, bootstrapped with `McpServerHost.RunAsync(args, PowerBiHostProfile.Instance)`.
- **Tool Surface**: Exposes **12 core/cloud tools** + **8 registry meta-tools** + **2 diagnostic tools** (`inspect_type`, `cancel_execution`).
- **Safety Architecture**: 3-layer gating (Bridge UI opt-in checkbox, ScriptGuard validation using `GuardProfile.PowerBi`, and automatic pre-run TMDL/metadata snapshot backup before write operations).
- **Test Strategy**: 100% deterministic, offline CI execution using synthetic files for port discovery, in-memory `IDataReader` mocks for DAX serialization, temporary directory snapshot managers, `FakeRevitExecutor` over real named pipes, and mocked `HttpMessageHandler` for Cloud REST tools.
- **Skill & Repository Sync**: Dedicated skill `.agents/skills/hp-mcp-powerbi/SKILL.md` and registration entry in `AGENTS.md` as the 7th deliverable.

---

## 1. HPPowerBi.Mcp.Server (.NET 10 Stdio Console)

### 1.1 `PowerBiHostProfile` Implementation

`McpShared` already contains preliminary hooks for Power BI:
- `PipeNaming.PowerBiHost = "powerbi"`
- `JsonRpcMethods.PowerBiPrefix = "powerbi."`
- `HostScriptContracts.PowerBiImports` and `PowerBiGlobals`
- `GuardProfile.PowerBi` and `AnalyzerProfile.PowerBi`
- `ContextMessages.cs`: `PowerBiInfo` and `ContextResult.PowerBi`

`PowerBiHostProfile` implements `IHostProfile` (via `HostProfile` data record) defined in `HPRebar.Mcp.Server.Hosts`:

```csharp
namespace HPPowerBi.Mcp.Server.Hosts;

public static class PowerBiHostProfile
{
    public const string ExecuteToolName = "execute_powerbi_code";
    public const string ContextToolName = "get_powerbi_context";
    public const int Version = 2026;
    public const int HeavyMaxTimeoutSeconds = HostScriptContracts.PowerBiHeavyMaxTimeoutSeconds; // 600 s
    public const string BridgeExecutable = "HPPowerBi.McpBridge.exe";

    public static readonly HostProfile Instance = new HostProfile
    {
        HostId = PipeNaming.PowerBiHost, // "powerbi"
        DisplayName = "Power BI",
        ServerName = "HPPowerBi MCP",
        ProductFolder = "HPPowerBi",
        EnvPrefix = "HPPOWERBI_MCP_",
        DefaultVersion = Version,
        ValidVersions = new[] { 2024, 2025, 2026 },
        MethodPrefix = JsonRpcMethods.PowerBiPrefix, // "powerbi."
        ExecuteToolName = ExecuteToolName,
        ContextToolName = ContextToolName,
        ResourceScheme = PipeNaming.PowerBiHost, // "powerbi"
        Categories = new[] { "Model", "DAX", "Measure", "Relationship", "Cloud", "Data", "Generic" },
        CoreToolNames = new[]
        {
            ExecuteToolName,
            ContextToolName,
            "inspect_type",
            "cancel_execution",
            "powerbi_get_schema",
            "powerbi_evaluate_dax",
            "powerbi_create_or_update_measure",
            "powerbi_delete_measure",
            "powerbi_manage_relationship",
            "powerbi_format_dax",
            "powerbi_cloud_list_workspaces",
            "powerbi_cloud_list_datasets",
            "powerbi_cloud_trigger_refresh",
            "powerbi_cloud_execute_dax"
        },
        ScriptImports = HostScriptContracts.PowerBiImports,
        ScriptContractSummary =
            "Globals: model (Model of the active Tabular model via AMO-TOM), server (Server connected to local msmdsrv), " +
            "adomd (AdomdConnection for DAX queries), ct (CancellationToken), log(string), progress(cur,total,msg), args. " +
            "Reads (DAX EVALUATE, schema queries) run with transaction=none. " +
            "Writes (measures, relationships, calculated columns) require transaction=auto and must call model.SaveChanges(). " +
            "The bridge automatically creates a TMDL/metadata snapshot before write operations. " +
            "Mutations require ticking 'Allow Model Modifications / DAX Execution' in the bridge window.",
        MaxTimeoutSeconds = HeavyMaxTimeoutSeconds,
        HostAssembly = typeof(PowerBiHostProfile).Assembly,
        CliExecutable = "HPPowerBi.Mcp.Server.exe",
        BridgeNotConnectedHint =
            $"Start {BridgeExecutable}, select a Power BI Desktop instance, and tick 'Allow Model Modifications / DAX Execution' (pipe {PipeNaming.For(PipeNaming.PowerBiHost, Version)}).",
        TimeoutSemanticsHint =
            "Power BI Analysis Services may still be executing the query or TOM commit; check the snapshot named in the bridge window before retrying."
    };
}
```

### 1.2 Program.cs Bootstrap
Following the repository standard (`HPSap2000` / `HPEtabs`), `Program.cs` is a minimal 2-line file:
```csharp
using HPPowerBi.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;

return await McpServerHost.RunAsync(args, PowerBiHostProfile.Instance);
```

### 1.3 Core Local Tools Specification & Parameter Contracts

Core local tools are implemented as classes annotated with `[McpServerToolType]` and methods annotated with `[McpServerTool]`. They are placed under `HPPowerBi.Mcp.Server/Tools/`.

#### 1. `get_powerbi_context`
- **Class**: `PowerBiContextTool(ContextService service)`
- **Tool Name**: `"get_powerbi_context"`
- **Annotations**: `ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false`
- **Description**: Returns connected Power BI Desktop session info: hostVersion, document title (`.pbix`), local port (`msmdsrv.port.txt`), attached PID, database name, compatibility level, mutationEnabled status, and table/measure/relationship counts.
- **Parameters**:
  - `includeSelection` (`bool`, default `false`): Include currently focused table or visual selection if available.
- **Underlying Protocol**: Calls `service.GetAsync(includeSelection, ct)` via pipe method `powerbi.context`.

#### 2. `execute_powerbi_code`
- **Class**: `ExecutePowerBiCodeTool(ExecuteCodeService service)`
- **Tool Name**: `"execute_powerbi_code"`
- **Annotations**: `ReadOnly = false, Destructive = true, Idempotent = false, OpenWorld = false`
- **Description**: Executes a C# Roslyn script against the local Power BI Tabular model. Globals: `model` (TOM `Model`), `server` (TOM `Server`), `adomd` (`AdomdConnection`), `ct`, `log`, `progress`, `args`.
- **Parameters**:
  - `code` (`string`, required): C# script body (max 32 KB). End with `return <value>;`.
  - `transaction` (`string`, default `"auto"`): `"none"` for read-only, `"auto"` for write operations.
  - `dryRun` (`bool`, default `false`): Static preview without executing.
  - `timeoutSeconds` (`int`, default `30`, range `5..600`): Script execution timeout.
  - `label` (`string?`, optional): Label for audit log and snapshot filename.
  - `args` (`JsonElement?`, optional): Key-value arguments passed into script global `args`.

#### 3. `powerbi_get_schema`
- **Class**: `PowerBiSchemaTool(ExecuteCodeService service)`
- **Tool Name**: `"powerbi_get_schema"`
- **Annotations**: `ReadOnly = true, Destructive = false, Idempotent = true`
- **Description**: Inspects tabular model metadata and returns tables, columns (name, data type, format, isHidden), measures (name, expression, formatString, isHidden), relationships, and partitions in structured JSON.
- **Parameters**:
  - `includeHidden` (`bool`, default `true`): Whether to include hidden columns and tables.
  - `tableFilter` (`string?`, optional): Filter to specific table name.
- **Implementation Mechanism**: Invokes a standardized Roslyn script with `transaction: "none"` reading `model.Tables`, `model.Relationships`.

#### 4. `powerbi_evaluate_dax`
- **Class**: `PowerBiDaxTool(ExecuteCodeService service)`
- **Tool Name**: `"powerbi_evaluate_dax"`
- **Annotations**: `ReadOnly = true, Destructive = false, Idempotent = true`
- **Description**: Executes a DAX query (typically starting with `EVALUATE`) against the local Power BI model using `adomd` connection. Returns formatted JSON rows, columns metadata, row count, and query duration in milliseconds.
- **Parameters**:
  - `query` (`string`, required): DAX query expression.
  - `maxRows` (`int`, default `1000`, clamp `1..10000`): Maximum rows to return.
- **Implementation Mechanism**: Executes Roslyn script using `using var cmd = new AdomdCommand(query, adomd); using var reader = cmd.ExecuteReader();` and returns serialized tabular results.

#### 5. `powerbi_create_or_update_measure`
- **Class**: `PowerBiMeasureTool(ExecuteCodeService service)`
- **Tool Name**: `"powerbi_create_or_update_measure"`
- **Annotations**: `ReadOnly = false, Destructive = true, Idempotent = false`
- **Description**: Creates or updates a DAX measure in the specified table. Automatic pre-run snapshot is captured; changes are committed with `model.SaveChanges()`.
- **Parameters**:
  - `tableName` (`string`, required): Target table name.
  - `measureName` (`string`, required): Name of the measure.
  - `expression` (`string`, required): DAX formula (e.g. `SUM(Sales[Amount])`).
  - `formatString` (`string?`, optional): Number/date format string (e.g. `#,##0.00`).
  - `description` (`string?`, optional): Measure documentation description.
  - `displayFolder` (`string?`, optional): Folder grouping in the field list.
  - `isHidden` (`bool`, default `false`): Visibility state.
- **Implementation Mechanism**: Invokes Roslyn script with `transaction: "auto"`, updates `table.Measures`, calls `model.SaveChanges()`.

#### 6. `powerbi_delete_measure`
- **Class**: `PowerBiMeasureTool(ExecuteCodeService service)`
- **Tool Name**: `"powerbi_delete_measure"`
- **Annotations**: `ReadOnly = false, Destructive = true, Idempotent = false`
- **Description**: Removes a DAX measure from the specified table. Captured in snapshot prior to deletion.
- **Parameters**:
  - `tableName` (`string`, required): Name of the table containing the measure.
  - `measureName` (`string`, required): Name of the measure to delete.
- **Implementation Mechanism**: Roslyn script with `transaction: "auto"` calling `table.Measures.Remove(measure)` and `model.SaveChanges()`.

#### 7. `powerbi_manage_relationship`
- **Class**: `PowerBiRelationshipTool(ExecuteCodeService service)`
- **Tool Name**: `"powerbi_manage_relationship"`
- **Annotations**: `ReadOnly = false, Destructive = true, Idempotent = false`
- **Description**: Creates, updates, or deletes a relationship between two tables in the tabular model.
- **Parameters**:
  - `action` (`string`, required): One of `"create"`, `"update"`, `"delete"`.
  - `fromTable` (`string`, required): From table name (Foreign Key side).
  - `fromColumn` (`string`, required): From column name.
  - `toTable` (`string`, required): To table name (Primary Key side).
  - `toColumn` (`string`, required): To column name.
  - `isActive` (`bool`, default `true`): Active state of relationship.
  - `crossFilteringBehavior` (`string`, default `"OneDirection"`): `"OneDirection"` or `"BothDirections"`.

#### 8. `powerbi_format_dax`
- **Class**: `PowerBiDaxTool`
- **Tool Name**: `"powerbi_format_dax"`
- **Annotations**: `ReadOnly = true, Destructive = false, Idempotent = true`
- **Description**: Formats and beautifies raw DAX code with standard indentation, uppercase keywords, and structured line breaks. Operates fully offline without requiring a running Power BI Desktop instance.
- **Parameters**:
  - `dax` (`string`, required): Raw DAX expression to format.
- **Implementation Mechanism**: Offline deterministic DAX tokenizer and indentation formatter (with optional external DaxFormatter API fallback).

---

### 1.4 Cloud REST Tools Specification & Parameter Contracts

Cloud REST tools communicate with the Power BI Service REST API (`https://api.powerbi.com/v1.0/myorg/`). Authentication tokens are acquired using MSAL (`Microsoft.Identity.Client`) via Service Principal or interactive OAuth, managed by `IPowerBiCloudService`.

#### 1. `powerbi_cloud_list_workspaces`
- **Class**: `PowerBiCloudTool(IPowerBiCloudService cloudService)`
- **Tool Name**: `"powerbi_cloud_list_workspaces"`
- **Annotations**: `ReadOnly = true, Destructive = false, Idempotent = true`
- **Description**: Lists Power BI Service workspaces (groups) accessible by the current credentials via `GET /v1.0/myorg/groups`.
- **Parameters**:
  - `filter` (`string?`, optional): Substring filter for workspace name.
  - `top` (`int`, default `100`): Maximum workspaces to return.

#### 2. `powerbi_cloud_list_datasets`
- **Class**: `PowerBiCloudTool`
- **Tool Name**: `"powerbi_cloud_list_datasets"`
- **Annotations**: `ReadOnly = true, Destructive = false, Idempotent = true`
- **Description**: Lists datasets / semantic models in a workspace via `GET /v1.0/myorg/groups/{workspaceId}/datasets` (or `GET /v1.0/myorg/datasets` for "My Workspace").
- **Parameters**:
  - `workspaceId` (`string?`, optional): Workspace GUID (omit for "My Workspace").

#### 3. `powerbi_cloud_trigger_refresh`
- **Class**: `PowerBiCloudTool`
- **Tool Name**: `"powerbi_cloud_trigger_refresh"`
- **Annotations**: `ReadOnly = false, Destructive = false, Idempotent = false`
- **Description**: Triggers an on-demand dataset refresh in Power BI Service via `POST /v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/refreshes`.
- **Parameters**:
  - `datasetId` (`string`, required): Target dataset GUID.
  - `workspaceId` (`string?`, optional): Workspace GUID.
  - `notifyOption` (`string`, default `"NoNotification"`): `"NoNotification"` or `"MailOnFailure"`.

#### 4. `powerbi_cloud_execute_dax`
- **Class**: `PowerBiCloudTool`
- **Tool Name**: `"powerbi_cloud_execute_dax"`
- **Annotations**: `ReadOnly = true, Destructive = false, Idempotent = true`
- **Description**: Executes a DAX query against a published cloud dataset via `POST /v1.0/myorg/datasets/{datasetId}/executeQueries` (or workspace-scoped endpoint).
- **Parameters**:
  - `datasetId` (`string`, required): Target dataset GUID.
  - `query` (`string`, required): DAX query string (`EVALUATE ...`).
  - `workspaceId` (`string?`, optional): Workspace GUID.
  - `maxRows` (`int`, default `1000`): Maximum rows to return.

---

### 1.5 Dynamic Tool Registry & Meta-Tools Integration

`McpServerHost.CreateBuilder` from `McpShared` automatically wires up:
1. **Dynamic Tool Registrar**: Reflects over published tools in SQLite DB (`%AppData%\HPPowerBi\McpServer\registry.db`) and registers them dynamically into MCP server capabilities with `list_changed = true`.
2. **Meta Tools from Engine Assembly**:
   - `propose_tool`: Submit a new tool proposal with code, schema, and examples.
   - `test_tool`: Test tool in isolation with test arguments.
   - `publish_tool`: Mark tool as published.
   - `deprecate_tool`: Deprecate or unpublish a tool.
   - `search_tools`: Full-text search (SQLite FTS5) over published tool registry.
   - `get_tool_info`: Retrieve metadata and documentation for a registered tool.
   - `run_tool`: Invoke a registry tool dynamically.
   - `inspect_type`: Discover members of AMO-TOM or ADOMD.NET types dynamically.
   - `cancel_execution`: Cooperatively cancel running operations.
3. **Core Tool Protection**: `PowerBiHostProfile.CoreToolNames` prevents registry tools from colliding with or shadowing any of the 14 core/cloud tools.

### 1.6 Power BI Resources & Prompts

#### Resources (`HPPowerBi.Mcp.Server/Resources/PowerBiDocumentResources.cs`):
- `powerbi://document/info`: Returns JSON snapshot of active Power BI Desktop session (port, PID, database name, compatibility level, table count, measure count).
- `powerbi://schema`: Returns full tabular schema (tables, columns, measures, relationships).
- `powerbi://instances`: Discovers all currently running `PBIDesktop.exe` instances and their allocated ports.
- `powerbi://selection`: Returns active table or field selection.

#### Prompts (`HPPowerBi.Mcp.Server/Prompts/PowerBiScriptPrompts.cs`):
- `powerbi_dax_query_template`: Guides the AI on writing performant read-only DAX queries (using `EVALUATE`, `SUMMARIZECOLUMNS`, `TOPN`), explaining measure dependencies and handling blank values.
- `powerbi_measure_authoring_template`: Guides the AI on best practices for measure authoring (using `DIVIDE` instead of `/`, formatted expressions, proper format strings, adding comments).
- `powerbi_model_exploration_template`: Guides the AI on discovering dimensions, fact tables, star schema relationships, and key business metrics.

---

## 2. Automated Test Strategy (R3)

To ensure **100% reliability in CI and offline environments** without requiring a running `PBIDesktop.exe` instance or real Azure cloud credentials, testing is divided cleanly across two test assemblies.

```
+---------------------------------------------------------------------------------------+
|                                    Automated Tests                                    |
+---------------------------------------------------+-----------------------------------+
|            HPPowerBi.McpBridge.Tests              |      HPPowerBi.Mcp.Server.Tests   |
|               (.NET 8.0-windows)                  |               (.NET 10)           |
+---------------------------------------------------+-----------------------------------+
| - PortDiscoveryTests (Synthetic filesystem)       | - PowerBiHostProfileTests         |
| - DaxResultSerializerTests (In-memory IDataReader)| - ToolCatalogRegistrationTests    |
| - PowerBiSnapshotManagerTests (Temp directory)    | - PowerBiToolsOverPipeTests       |
| - SafetyGatingTests (BridgeSettings + Guard)      |   (FakeRevitExecutor over pipe)   |
| - ExternalToolRegistrationTests (.pbitool.json)   | - PowerBiCloudToolsTests          |
|                                                   |   (Mock HttpMessageHandler)       |
+---------------------------------------------------+-----------------------------------+
```

### 2.1 HPPowerBi.McpBridge.Tests (.NET 8.0-windows)

1. **Port Discovery Parser Tests (`PortDiscoveryTests.cs`)**:
   - **Isolation**: Uses `TestFileSystem` / isolated temporary directory structures mimicking `%LocalAppData%\Microsoft\Power BI Desktop\AnalysisServicesWorkspaces\`.
   - **Test Scenarios**:
     * Parses valid single port number (`54321`) with UTF-8 and UTF-16 LE encoding.
     * Handles trailing whitespace, carriage returns, or null terminators (`\0`).
     * Multiple workspace directories: selects the directory matching specific PID or the most recently modified workspace.
     * Corrupted file handling: handles non-numeric strings, 0-byte files, or partial writes gracefully by returning descriptive failure without crashing.
     * Port range boundary validation (`1024..65535`).

2. **DAX Serialization Tests (`DaxResultSerializerTests.cs`)**:
   - **Isolation**: Uses mock `IDataReader` / in-memory `DataTable` populated with synthetic test rows.
   - **Test Scenarios**:
     * Primitive and complex data types (int, long, double, decimal, bool, string, DateTime, DBNull).
     * Output capping: result sets exceeding `maxRows` (e.g. 1,000 rows) are properly truncated with `truncated: true` and count metadata.
     * Empty result sets: returns column headers with 0 rows.
     * Single-cell scalar queries: `EVALUATE ROW("Result", 42)`.
     * Performance: serialization of 10,000 rows benchmarked to complete in < 50 ms.

3. **Snapshot Manager Tests (`PowerBiSnapshotManagerTests.cs`)**:
   - **Isolation**: Directed to an isolated temporary directory cleaned up in `Dispose()`.
   - **Test Scenarios**:
     * `Prepare()` creates timestamped snapshot directory and files (e.g. `20260921-100000-add_measure.tmdl`).
     * Presave vs. prerun separation: verifies user state is preserved before any script writes.
     * Label sanitization: converts invalid filesystem characters (`/ \ : * ? " < > |`) to underscores.
     * Pruning policy: preserves the last 10 snapshots and prunes older entries based on timestamp.
     * Preconditions: rejects invalid or nonexistent database names.

4. **Safety Gating Tests (`SafetyGatingTests.cs`)**:
   - **Isolation**: Pure unit tests on `RequestDispatcher` and `BridgeSettings`.
   - **Test Scenarios**:
     * When `ExecutionEnabled == false` or `MutationEnabled == false`: write operations (`transaction: "auto"`) are refused with `BridgeErrorCode.ExecutionDisabled` (-32001).
     * ScriptGuard verification: scripts referencing `server.Disconnect()`, `adomd.Close()`, `System.Windows.Forms`, `System.IO`, or `#r` directives are rejected before compilation.
     * Read-only operations (`transaction: "none"`) pass without requiring mutation opt-in.

5. **External Tool JSON Generator Tests (`ExternalToolRegistrationTests.cs`)**:
   - **Isolation**: Written to a mock folder structure.
   - **Test Scenarios**:
     * Validates that emitted `.pbitool.json` adheres to Power BI External Tools JSON schema.
     * Verifies executable path points to `HPPowerBi.McpBridge.exe`.
     * Verifies argument tokens (`"%server%"` and `"%database%"`).
     * Verifies icon embedding or icon path resolution.

---

### 2.2 HPPowerBi.Mcp.Server.Tests (.NET 10)

1. **Host Profile Verification (`PowerBiHostProfileTests.cs`)**:
   - Asserts all metadata properties (`HostId == "powerbi"`, `MethodPrefix == "powerbi."`, `MaxTimeoutSeconds == 600`, `Categories`, `CoreToolNames`).
   - Asserts hint texts name `HPPowerBi.McpBridge.exe` and specify pipe `hppowerbi-mcp-2026`.

2. **Tool Catalog Registration Tests (`ToolCatalogRegistrationTests.cs`)**:
   - Builds `McpServerHost.CreateBuilder([], PowerBiHostProfile.Instance).Build()`.
   - Asserts that `McpServerTool` collection contains all 12 core and cloud tools plus 8 registry tools and 2 diagnostics.
   - Asserts no cross-contamination from Revit/AutoCAD/Navis/ETABS/SAP2000 tool names.
   - Asserts resources (`powerbi://document/info`, `powerbi://schema`, `powerbi://instances`) and prompts (`powerbi_dax_query_template`, etc.) are registered.

3. **Pipe Execution Routing via Fake Executor (`PowerBiToolsOverPipeTests.cs`)**:
   - Sets up a real named pipe listener (`hppowerbi-mcp-test-{guid}`) backed by `FakeRevitExecutor`.
   - Tests `get_powerbi_context`: verifies `ContextResult.PowerBi` serialization, and confirms `ContextService.Shape` removes Revit-specific fields (`revitVersion`, `isFamily`).
   - Tests `execute_powerbi_code`: verifies parameter passing, timeout clamping, and error translation.
   - Tests `powerbi_evaluate_dax`, `powerbi_get_schema`, `powerbi_create_or_update_measure`, `powerbi_manage_relationship`: verifies exact script generation and JSON result decoding.

4. **Cloud REST Tools Offline Tests (`PowerBiCloudToolsTests.cs`)**:
   - Uses `MockHttpMessageHandler` with `HttpClient` injected into `PowerBiCloudService`.
   - Tests `powerbi_cloud_list_workspaces`: returns simulated JSON groups response, verifies deserialization.
   - Tests `powerbi_cloud_trigger_refresh`: simulates HTTP 202 Accepted response.
   - Tests `powerbi_cloud_execute_dax`: simulates Power BI REST `executeQueries` JSON schema response.
   - Tests error handling: simulates HTTP 401 Unauthorized, 403 Forbidden, 404 Not Found, 429 Rate Limited.

---

## 3. Skill and Ecosystem Documentation (R4)

### 3.1 Skill Specification: `.agents/skills/hp-mcp-powerbi/SKILL.md`

Following repository standards (modeled after `hp-mcp-sap2000/SKILL.md` and `hp-mcp-revit/SKILL.md`), the skill file is structured as follows:

```markdown
---
name: hp-mcp-powerbi
description: "Kết nối và điều khiển Power BI Desktop & Service qua HPPowerBi MCP (server hprebar-powerbi, tool mcp__hprebar-powerbi__*): đọc schema (bảng, cột, measure, quan hệ), thực thi DAX (EVALUATE), tạo/sửa/xoá measure, quản lý quan hệ bảng, format DAX, gọi Power BI Service Cloud REST API (workspace, dataset, refresh, cloud DAX), viết C# Roslyn qua execute_powerbi_code với AMO-TOM / ADOMD.NET + snapshot TMDL, registry tool (propose/test/publish). TRIGGER when: user nhắc 'Power BI', 'PBI', 'DAX', 'PBIX', 'TOM', 'AMO-TOM', 'ADOMD', 'measure', 'semantic model', 'Power BI Desktop', 'Power BI Service', 'hprebar-powerbi', hoặc lỗi -32001/-32002 từ tool Power BI. Keywords: powerbi, dax, pbix, tom, amotom, adomd, measure, relationship, semantic model, dataset, mcp, bridge, tmdl, refresh. Khi cần phân tích/sửa model Power BI đang mở, viết/đo query DAX, tạo measure, hoặc tương tác cloud dataset."
metadata:
  author: hoang
  version: "1.0.0"
  mcp-server: hprebar-powerbi
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.
...
<!-- portable-host-contract:end -->

# HP MCP Power BI — điều khiển Power BI Desktop & Cloud qua `hprebar-powerbi`

## Overview
Dạy Claude/Antigravity dùng đúng 24 tool của MCP server `hprebar-powerbi` (`mcp__hprebar-powerbi__*`) để làm việc với mô hình Power BI Desktop đang mở hoặc Power BI Service Cloud: chuỗi **Claude → HPPowerBi.Mcp.Server (stdio) → pipe `hppowerbi-mcp-2026` → HPPowerBi.McpBridge.exe (WPF) → AMO-TOM/ADOMD.NET → msmdsrv.exe**. An toàn 3 lớp: UI opt-in checkbox + DAX/Roslyn ScriptGuard + TMDL snapshot tự động trước khi ghi.

## Bước 0 — Kết nối (checklist)
1. Mở **Power BI Desktop** với file `.pbix`.
2. Khởi chạy **HPPowerBi.McpBridge.exe** (hoặc bấm nút từ External Tools ribbon).
3. Chọn instance PBIDesktop trong dropdown (hiển thị PID + Port).
4. Tick **Allow Model Modifications / DAX Execution** (chỉ khi cần sửa model).
5. `.mcp.json` chứa entry `hprebar-powerbi` trỏ đến `HPPowerBi.Mcp.Server.exe`.
6. Gọi `get_powerbi_context` — **luôn là call đầu tiên** để kiểm tra trạng thái kết nối, port, và quyền ghi.

## Workflow Decision Tree
- **Đọc cấu trúc mô hình**: dùng `powerbi_get_schema`
- **Chạy query DAX**: dùng `powerbi_evaluate_dax` (hoặc `powerbi_cloud_execute_dax` cho cloud)
- **Format công thức DAX**: dùng `powerbi_format_dax` (offline, an toàn tuyệt đối)
- **Tạo hoặc sửa Measure**: dùng `powerbi_create_or_update_measure`
- **Xoá Measure**: dùng `powerbi_delete_measure`
- **Tạo/sửa quan hệ (Relationship)**: dùng `powerbi_manage_relationship`
- **Tác vụ phức tạp / Custom Logic**: dùng `execute_powerbi_code`
- **Cloud REST Operations**: `powerbi_cloud_list_workspaces`, `powerbi_cloud_list_datasets`, `powerbi_cloud_trigger_refresh`

## Bảng tra cứu Tool Surface (24 Tools)
[Bảng tổng hợp chi tiết 12 Core/Cloud tools + 8 Registry Meta-tools + 4 Core/Diagnostic tools]
```

### 3.2 Registration in `AGENTS.md`

`AGENTS.md` must be updated to register `HPPowerBi/` as the **7th deliverable**.

#### Repository Layout Table Entry:
```markdown
| `HPPowerBi/` | The **Power BI MCP** (standalone WPF desktop app `HPPowerBi.McpBridge` connecting to Power BI Desktop local Analysis Services via AMO-TOM / ADOMD.NET + Power BI Service Cloud REST API via MSAL, and `HPPowerBi.Mcp.Server` net10 stdio exe, **24 tools** = 12 core/cloud tools + 8 registry + 4 meta/diagnostic tools, external tool `.pbitool.json` auto-registration, 3-layer safety with TMDL snapshot before writes). Out-of-process named pipe `hppowerbi-mcp-2026`. Own `HPPowerBi.slnx` + `global.json` + `Directory.Build.props`; references `../McpShared/` only. Tests `HPPowerBi.Mcp.Server.Tests` (net10) + `HPPowerBi.McpBridge.Tests` (net8.0-windows). | C# / net8.0-windows · net10 / AMO-TOM + ADOMD.NET + MSAL |
```

#### Dedicated Architecture Section:
```markdown
## HPPowerBi MCP Bridge (Power BI Desktop & Cloud MCP Server)

Design & Implementation: 2026-09-21. Standalone WPF desktop bridge (`HPPowerBi.McpBridge`, net8.0-windows) + stdio MCP server (`HPPowerBi.Mcp.Server`, net10), modeled after `HPSap2000` and `HPEtabs`.

- **Architecture:** Standalone WPF desktop app connects to Power BI Desktop's local Analysis Services instance (`msmdsrv.exe`) via AMO-TOM (`Microsoft.AnalysisServices.NetCore.retail`) and ADOMD.NET (`Microsoft.AnalysisServices.AdomdClient.NetCore.retail`). Serves named pipe `hppowerbi-mcp-2026` with method prefix `powerbi.`. Server is stdio MCP executable launched by host coding agents; references `../McpShared/` only.
- **External Tools Auto-Registration:** Automatically writes `HPPowerBiBridge.pbitool.json` to `%CommonProgramFiles%\Microsoft Shared\Power BI Desktop\External Tools\` allowing 1-click launch directly from the Power BI Desktop ribbon.
- **3-Layer Safety Architecture:**
  1. UI Opt-in: Mutation checkbox in bridge window required for write operations.
  2. Roslyn ScriptGuard: Validates C# scripts against `GuardProfile.PowerBi` (denies `server.Disconnect()`, `adomd.Close()`, modal dialogs, and filesystem access).
  3. Pre-run Snapshots: Automatic TMDL/metadata backup saved to `%LocalAppData%\HPPowerBi\McpBridge\snapshots\<model>\prerun\` before any structural mutation or `SaveChanges()`.
- **Tool Surface (24 tools):**
  - 8 Core Local Tools: `get_powerbi_context`, `execute_powerbi_code`, `powerbi_get_schema`, `powerbi_evaluate_dax`, `powerbi_create_or_update_measure`, `powerbi_delete_measure`, `powerbi_manage_relationship`, `powerbi_format_dax`.
  - 4 Cloud REST Tools: `powerbi_cloud_list_workspaces`, `powerbi_cloud_list_datasets`, `powerbi_cloud_trigger_refresh`, `powerbi_cloud_execute_dax`.
  - 8 Registry Meta-Tools: `propose_tool`, `test_tool`, `publish_tool`, `deprecate_tool`, `search_tools`, `get_tool_info`, `run_tool`, `manage_tool`.
  - 4 Host Diagnostics: `inspect_type`, `cancel_execution`, resources (`powerbi://*`), prompts (`powerbi_*_template`).
- **Tests:** `HPPowerBi.McpBridge.Tests` (net8.0-windows: port parser, DAX serialization, snapshot manager, safety gating) and `HPPowerBi.Mcp.Server.Tests` (net10: profile, catalog registration, pipe routing via fake executor, offline Cloud REST mocking).
```

---

## 4. Synthesis & Recommendations for Implementation

1. **Shared Foundation Integrity**:
   `McpShared` already contains all required constants (`PipeNaming.PowerBiHost`, `JsonRpcMethods.PowerBiPrefix`, `HostScriptContracts.PowerBiImports/Globals/HeavyMaxTimeoutSeconds`, `ContextMessages.PowerBiInfo`, `GuardProfile.PowerBi`, `AnalyzerProfile.PowerBi`). **Zero changes are required to `McpShared`**, guaranteeing that `HPRebar.Mcp.Server.Core.Tests` will continue passing with 100% success.

2. **Project Layout**:
   Create `HPPowerBi/` with:
   - `HPPowerBi.slnx`
   - `Directory.Build.props`
   - `global.json` (pinned to SDK 10.0.300)
   - `HPPowerBi.McpBridge/` (net8.0-windows)
   - `HPPowerBi.Mcp.Server/` (net10.0 console)
   - `HPPowerBi.McpBridge.Tests/` (net8.0-windows)
   - `HPPowerBi.Mcp.Server.Tests/` (net10.0)

3. **Tool Dispatch Implementation**:
   For local tools (`powerbi_get_schema`, `powerbi_evaluate_dax`, `powerbi_create_or_update_measure`, etc.), implement them by generating standardized C# scripts and invoking `ExecuteCodeService.ExecuteAsync`. This cleanly reuses the named pipe `execute` protocol without modifying `RequestDispatcher` or breaking contract invariants.

4. **Offline CI Readiness**:
   Ensure all tests in `HPPowerBi.McpBridge.Tests` and `HPPowerBi.Mcp.Server.Tests` use synthetic in-memory mocks, temporary directories, and `FakeRevitExecutor` so that `dotnet test` succeeds 100% out of the box on machines without Power BI Desktop or Azure login.
