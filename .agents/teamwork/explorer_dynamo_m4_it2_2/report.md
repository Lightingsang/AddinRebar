# Investigation Report: HPDynamo MCP Tool Contract & SKILL.md Remediation

**Agent**: `explorer_dynamo_m4_it2_2`  
**Date**: 2026-10-09  
**Target File**: `.agents/skills/hp-mcp-dynamo/SKILL.md`  
**Associated Project**: HPDynamo MCP Subsystem (`HPDynamo/`)  

---

## 1. Executive Summary

An exhaustive audit of all 25 tools in the HPDynamo MCP subsystem (4 Core Tools + 8 Registry Meta Tools + 13 Embedded Seed Tools) was conducted across source code, JSON Schema manifests (`tool.json`), script implementations (`code.cs`), test suites, and the user-facing skill definition `.agents/skills/hp-mcp-dynamo/SKILL.md`.

### Key Findings:
1. **Tool Discrepancies Confirmed (7/7)**: All 7 parameter and schema defects identified by `challenger_dynamo_m4_2` in `SKILL.md` are 100% verified against production C# implementations and `tool.json` manifests. Because all seed manifests enforce `"additionalProperties": false`, invocations constructed according to current `SKILL.md` documentation will be rejected at the JSON Schema validation layer or will throw runtime `ArgumentException` in the seed execution code.
2. **Additional Refinements Identified**:
   - `execute_designscript`: also supports optional `transaction` ("auto"/"manual"/"none"), `timeoutSeconds` (int), `label` (string), and `args` (object).
   - `run_dyn_file`: also supports `runHeadless` (bool) and `extractOutputs` (bool).
   - `create_code_block_node`: optional `title` parameter exists in schema.
   - `set_node_properties`: supports `isVisible` (bool), `x` (number), `y` (number) in addition to `name`, `value`, `isFrozen`.
3. **E2E Test Contract Status**:
   - `DynamoE2EContracts.cs` (lines 61–76) already includes `"batch_edit_graph"` (13 seeds total).
   - `Tier1_ContractsAndServerTests.cs` (lines 78–82) already asserts `TC_T1_SRV_01_ToolSurface_Count_IsExactly25Tools` (`Assert.Equal(25, tools.Count)`).
   - `HPDynamo.E2E.Tests` passes 209/209 tests (100% Green).

---

## 2. Complete 25-Tool Audit Matrix

| # | Tool Name | Category | C# Implementation / Manifest Source | Parameters in Code/Schema | Current Parameters in `SKILL.md` | Audit Verdict |
|---|---|---|---|---|---|---|
| 1 | `get_dynamo_context` | Core | `Tools/DynamoContextTool.cs` | `includeSelection` (bool, default `false`) | `includeSelection` (bool) | ✅ PASS |
| 2 | `execute_designscript` | Core | `Tools/ExecuteDesignScriptTool.cs` | `code` (string, req), `transaction` (string, "auto"), `dryRun` (bool, false), `timeoutSeconds` (int, 30), `label` (string?), `args` (JsonElement?) | `script` (string), `dryRun` (bool) | ❌ **MISMATCH**: `code` not `script` |
| 3 | `execute_dynamo_code` | Core | `Tools/ExecuteDynamoCodeTool.cs` | `code` (string, req), `transaction` (string, "auto"), `dryRun` (bool, false), `timeoutSeconds` (int, 30), `label` (string?), `args` (JsonElement?) | `code` (string), `args` (object), `dryRun` (bool), `transaction` ("auto"/"manual"/"none") | ✅ PASS |
| 4 | `cancel_execution` | Core | `McpShared/.../Tools/CancelExecutionTool.cs` | *(no parameters)* | *(không có tham số)* | ✅ PASS |
| 5 | `search_tools` | Registry | `McpShared/.../Tools/Registry/ToolRegistryQueryTools.cs` | `query`, `category`, `tag`, `limit` | Listed in Section 4.3 | ✅ PASS |
| 6 | `get_tool` | Registry | `McpShared/.../Tools/Registry/ToolRegistryQueryTools.cs` | `name` | Listed in Section 4.3 | ✅ PASS |
| 7 | `run_tool` | Registry | `McpShared/.../Tools/Registry/RunToolTool.cs` | `name`, `args`, `dryRun`, `timeoutSeconds` | Listed in Section 4.3 | ✅ PASS |
| 8 | `get_run` | Registry | `McpShared/.../Tools/Registry/RunHistoryTools.cs` | `runId` | Listed in Section 4.3 | ✅ PASS |
| 9 | `propose_tool` | Registry | `McpShared/.../Tools/Registry/ToolLifecycleTools.cs` | `name`, `code`, `title`, `description`, etc. | Listed in Section 4.3 | ✅ PASS |
| 10 | `test_tool` | Registry | `McpShared/.../Tools/Registry/ToolLifecycleTools.cs` | `name`, `testArgs`, `dryRun` | Listed in Section 4.3 | ✅ PASS |
| 11 | `publish_tool` | Registry | `McpShared/.../Tools/Registry/ToolLifecycleTools.cs` | `name` | Listed in Section 4.3 | ✅ PASS |
| 12 | `manage_tool` | Registry | `McpShared/.../Tools/Registry/ToolLifecycleTools.cs` | `name`, `action`, `reason` | Listed in Section 4.3 | ✅ PASS |
| 13 | `batch_edit_graph` | Seed (Graph) | `SeedLibrary/Graph/batch_edit_graph/tool.json` | `nodes` (arr), `wires` (arr), `properties` (arr), `runEvaluation` (bool, false), `dryRun` (bool, false) | `nodes` (array), `wires` (array), `properties` (array), `runEvaluation` (bool), `dryRun` (bool) | ✅ PASS |
| 14 | `run_dyn_file` | Seed (Graph) | `SeedLibrary/Graph/run_dyn_file/tool.json` | `filePath` (string, req), `inputs` (object), `runHeadless` (bool, false), `extractOutputs` (bool, true) | `filePath` (string), `parameters` (object), `dryRun` (bool) | ❌ **MISMATCH**: `inputs` not `parameters` |
| 15 | `validate_dyn_file` | Seed (Graph) | `SeedLibrary/Graph/validate_dyn_file/tool.json` & `code.cs` | `fileContent` (string), `filePath` (string), `checkPackages` (bool, true) | `filePath` (string) | ⚠️ **INCOMPLETE**: Requires `fileContent` offline |
| 16 | `get_active_graph_info` | Seed (Workspace) | `SeedLibrary/Workspace/get_active_graph_info/tool.json` | `includeNodeList` (bool, false), `includeErrors` (bool, true) | `includeNodeList` (bool), `includeErrors` (bool) | ✅ PASS |
| 17 | `search_nodes_library` | Seed (Library) | `SeedLibrary/Library/search_nodes_library/tool.json` | `query` (string, req), `category` (string), `maxResults` (int, 50) | `query` (string), `category` (string), `maxResults` (int) | ✅ PASS |
| 18 | `inspect_node_type` | Seed (Library) | `SeedLibrary/Library/inspect_node_type/tool.json` | `nodeType` (string, req) | `nodeType` (string) | ✅ PASS |
| 19 | `add_node_to_workspace` | Seed (Node) | `SeedLibrary/Node/add_node_to_workspace/tool.json` | `nodeType` (string, req), `x` (num, req), `y` (num, req), `name` (string), `initialValue` (string) | `nodeType` (string), `x` (number), `y` (number), `nickname` (string) | ❌ **MISMATCH**: `name` not `nickname` |
| 20 | `create_code_block_node` | Seed (CodeBlock) | `SeedLibrary/CodeBlock/create_code_block_node/tool.json` | `code` (string, req), `x` (num, 100), `y` (num, 100), `title` (string) | `code` (string), `x` (number), `y` (number) | ✅ PASS (can add `title`) |
| 21 | `set_node_properties` | Seed (Node) | `SeedLibrary/Node/set_node_properties/tool.json` | `nodeId` (string, req), `name` (string), `value` (string), `isFrozen` (bool), `isVisible` (bool), `x` (num), `y` (num) | `nodeId` (string), `value` (string), `name` (string), `isFrozen` (bool) | ✅ PASS (can add `isVisible`, `x`, `y`) |
| 22 | `connect_node_ports` | Seed (Wire) | `SeedLibrary/Wire/connect_node_ports/tool.json` | `sourceNodeId` (str, req), `sourcePortIndex` (int, req), `targetNodeId` (str, req), `targetPortIndex` (int, req) | `startNodeId, startPortIndex, endNodeId, endPortIndex` | ❌ **CRITICAL MISMATCH**: all 4 port params wrong |
| 23 | `disconnect_node_ports` | Seed (Wire) | `SeedLibrary/Wire/disconnect_node_ports/tool.json` | `nodeId` (str, req), `portIndex` (int, 0), `portType` ("input"/"output", "input"), `targetNodeId` (str), `targetPortIndex` (int) | `startNodeId, startPortIndex, endNodeId, endPortIndex` | ❌ **CRITICAL MISMATCH**: wrong parameter schema |
| 24 | `get_node_output_values` | Seed (Data) | `SeedLibrary/Data/get_node_output_values/tool.json` | `nodeIds` (arr[str]), `includeGeometry` (bool, false), `limit` (int, 50) | `nodeIds` (array of string), `includeGeometry` (bool), `limit` (int) | ✅ PASS |
| 25 | `export_graph_to_image` | Seed (Export) | `SeedLibrary/Export/export_graph_to_image/tool.json` | `outputPath` (string), `format` ("PNG"/"JPEG", "PNG") | `outputFilePath` (string), `format` ("png"/"svg") | ❌ **MISMATCH**: `outputPath` not `outputFilePath`; `svg` invalid |

---

## 3. Discrepancy Forensic Breakdown

### Discrepancy 1: `execute_designscript` Parameter Name
- **Location in `SKILL.md`**:
  - Table 4.1 line 94: `script (string), dryRun (bool)`
  - Section 5 Example 3 line 166: `"script": "pts = Point.ByCoordinates(...); ..."`
- **Production Truth**:
  - `HPDynamo.Mcp.Server/Tools/ExecuteDesignScriptTool.cs` line 35:
    ```csharp
    public Task<CallToolResult> ExecuteAsync(
        [Description("...")] string code,
        string transaction = TransactionModes.Auto,
        bool dryRun = false,
        int timeoutSeconds = 30,
        string? label = null,
        JsonElement? args = null, ...
    ```
- **Failure Mechanism**: The ModelContextProtocol SDK checks parameters against method arguments. The parameter name is `code`. When an agent passes `"script"`, the SDK reports missing required argument `code`.

### Discrepancy 2: `run_dyn_file` Parameter Name
- **Location in `SKILL.md`**:
  - Table 4.2 line 102: `filePath (string), parameters (object), dryRun (bool)`
  - Section 5 Example 1 line 128: `"parameters": { "ChieuCaoTang": 3600, ... }`
- **Production Truth**:
  - `SeedLibrary/Graph/run_dyn_file/tool.json` lines 34-37:
    ```json
    "inputs": {
      "type": "object",
      "description": "Dictionary of node names or GUIDs mapped to override values..."
    }
    ```
  - `SeedLibrary/Graph/run_dyn_file/code.cs` line 10:
    ```csharp
    var inputOverrides = args.Obj("inputs");
    ```
- **Failure Mechanism**: `tool.json` specifies `"additionalProperties": false`. Passing `"parameters"` is rejected by schema validation. Furthermore, `code.cs` only accesses `args.Obj("inputs")`.

### Discrepancy 3: `connect_node_ports` Parameter Names
- **Location in `SKILL.md`**:
  - Table 4.2 line 110: `startNodeId, startPortIndex, endNodeId, endPortIndex`
  - Section 5 Example 2 lines 154-157: `"startNodeId"`, `"startPortIndex"`, `"endNodeId"`, `"endPortIndex"`
- **Production Truth**:
  - `SeedLibrary/Wire/connect_node_ports/tool.json` lines 26-31:
    ```json
    "required": [
      "sourceNodeId",
      "sourcePortIndex",
      "targetNodeId",
      "targetPortIndex"
    ]
    ```
  - `SeedLibrary/Wire/connect_node_ports/code.cs` lines 1-4:
    ```csharp
    string sourceNodeIdentifier = args.Require("sourceNodeId");
    int sourceOutputPortIndex = args.Int("sourcePortIndex", 0);
    string targetNodeIdentifier = args.Require("targetNodeId");
    int targetInputPortIndex = args.Int("targetPortIndex", 0);
    ```
- **Failure Mechanism**: Schema enforces `"additionalProperties": false`. Passing `startNodeId` causes schema failure. Even if bypassed, `args.Require("sourceNodeId")` throws `ArgumentException("args.sourceNodeId is required.")`.

### Discrepancy 4: `disconnect_node_ports` Parameter Schema
- **Location in `SKILL.md`**:
  - Table 4.2 line 111: `startNodeId, startPortIndex, endNodeId, endPortIndex`
- **Production Truth**:
  - `SeedLibrary/Wire/disconnect_node_ports/tool.json` lines 27-59:
    ```json
    "required": ["nodeId"],
    "properties": {
      "nodeId": { "type": "string" },
      "portIndex": { "type": "integer", "default": 0 },
      "portType": { "type": "string", "enum": ["input", "output"], "default": "input" },
      "targetNodeId": { "type": "string" },
      "targetPortIndex": { "type": "integer" }
    }
    ```
  - `SeedLibrary/Wire/disconnect_node_ports/code.cs` line 1:
    ```csharp
    string primaryNodeIdentifier = args.Require("nodeId");
    ```
- **Failure Mechanism**: `startNodeId` is completely nonexistent. `code.cs` immediately throws `ArgumentException("args.nodeId is required.")`.

### Discrepancy 5: `add_node_to_workspace` Parameter Name
- **Location in `SKILL.md`**:
  - Table 4.2 line 107: `nodeType (string), x (number), y (number), nickname (string)`
- **Production Truth**:
  - `SeedLibrary/Node/add_node_to_workspace/tool.json` lines 44-51:
    ```json
    "name": { "type": "string", "description": "Optional custom display title for the created node" },
    "initialValue": { "type": "string", "description": "Optional initial value or code expression..." }
    ```
  - `SeedLibrary/Node/add_node_to_workspace/code.cs` lines 8-9:
    ```csharp
    string customDisplayName = args.Str("name", "");
    string initialCodeValue = args.Str("initialValue", "");
    ```
- **Failure Mechanism**: Schema specifies `name`, NOT `nickname`. With `"additionalProperties": false`, `nickname` is rejected.

### Discrepancy 6: `export_graph_to_image` Parameter & Format Enum
- **Location in `SKILL.md`**:
  - Table 4.2 line 113: `outputFilePath (string), format ("png"/"svg")`
- **Production Truth**:
  - `SeedLibrary/Export/export_graph_to_image/tool.json` lines 29-41:
    ```json
    "outputPath": { "type": "string" },
    "format": { "type": "string", "enum": ["PNG", "JPEG"], "default": "PNG" }
    ```
- **Failure Mechanism**: Parameter is `outputPath`. Passing `outputFilePath` fails schema validation. Passing `"svg"` fails enum validation because Dynamo canvas export only supports raster rendering (`PNG` / `JPEG`).

### Discrepancy 7: `validate_dyn_file` Missing `fileContent`
- **Location in `SKILL.md`**:
  - Table 4.2 line 103: `filePath (string)`
- **Production Truth**:
  - `SeedLibrary/Graph/validate_dyn_file/code.cs` lines 124-127:
    ```csharp
    else
    {
        throw new ArgumentException("Provide 'fileContent' (raw JSON of the .dyn graph) or ensure the graph is loaded in the active Dynamo workspace.");
    }
    ```
- **Failure Mechanism**: In the MCP sandbox, `System.IO` is denied by `ScriptGuard`. Therefore, the script cannot read an arbitrary file from disk via `filePath`. Validating an unopened file requires passing `fileContent` (raw JSON). `filePath` is only used when matching against the currently active workspace.

---

## 4. Exact Drop-In Text Corrections for `SKILL.md`

Below are the exact replacement blocks for `.agents/skills/hp-mcp-dynamo/SKILL.md`.

### Drop-In Block A: Section 4 (Tool Surface Reference)
*Target: Lines 88–117 of `.agents/skills/hp-mcp-dynamo/SKILL.md`*

```markdown
## 4. Danh mục 25 Công cụ Chi tiết (Tool Surface Reference: 4 Core + 8 Registry + 13 Seeds)

### 4.1. Core Tools (4 công cụ nền tảng)
| Tool | Mô tả | Tham số chính |
|---|---|---|
| `get_dynamo_context` | Lấy ngữ cảnh hiện tại của Dynamo & Revit | `includeSelection` (bool, default `false`) |
| `execute_designscript` | Thực thi đoạn mã DesignScript trên Revit doc | `code` (string, required), `transaction` ("auto"/"manual"/"none"), `dryRun` (bool), `timeoutSeconds` (int, 5–600), `label` (string), `args` (object) |
| `execute_dynamo_code` | Chạy C# Roslyn script với quyền truy cập Dynamo API | `code` (string, required), `transaction` ("auto"/"manual"/"none"), `dryRun` (bool), `timeoutSeconds` (int, 5–600), `label` (string), `args` (object) |
| `cancel_execution` | Hủy lệnh script hoặc graph đang chạy dở dang | *(không có tham số)* |

### 4.2. Embedded Seed Tools (13 công cụ nghiệp vụ hạt nhân)
| Nhóm | Tool | Mô tả & Tham số chính |
|---|---|---|
| **Graph Automation** | `batch_edit_graph` | Tạo cụm node, nối dây và đổi thuộc tính theo lô trong 1 transaction: `nodes` (array), `wires` (array), `properties` (array), `runEvaluation` (bool), `dryRun` (bool). Hỗ trợ ID tạm `$n1, $n2...`. |
| | `run_dyn_file` | Chạy file `.dyn` với tham số đè: `filePath` (string, required), `inputs` (object, dictionary tên/GUID node -> giá trị), `runHeadless` (bool), `extractOutputs` (bool), `dryRun` (bool). |
| | `validate_dyn_file` | Kiểm tra cú pháp, node thiếu, cyclic wire: `fileContent` (string, bắt buộc khi kiểm tra file offline chưa mở), `filePath` (string, khi kiểm tra workspace hiện hành), `checkPackages` (bool, default `true`). |
| | `get_active_graph_info` | Đọc thông tin canvas: `includeNodeList` (bool, default `false`), `includeErrors` (bool, default `true`). |
| **Node Authoring** | `search_nodes_library` | Tìm node: `query` (string, required), `category` (string), `maxResults` (int, default 50). |
| | `inspect_node_type` | Xem chi tiết node: `nodeType` (string, required). |
| | `add_node_to_workspace` | Đặt node mới: `nodeType` (string, required), `x` (number, required), `y` (number, required), `name` (string, optional), `initialValue` (string, optional). |
| | `create_code_block_node` | Tạo CodeBlock: `code` (string, required), `x` (number, default 100), `y` (number, default 100), `title` (string, optional). |
| | `set_node_properties` | Sửa giá trị/tọa độ node: `nodeId` (string, required), `name` (string), `value` (string), `isFrozen` (bool), `isVisible` (bool), `x` (number), `y` (number). |
| **Wiring & Data** | `connect_node_ports` | Nối dây: `sourceNodeId` (string, required), `sourcePortIndex` (int, required), `targetNodeId` (string, required), `targetPortIndex` (int, required). |
| | `disconnect_node_ports`| Ngắt dây: `nodeId` (string, required), `portIndex` (int, default 0), `portType` ("input"/"output", default "input"), `targetNodeId` (string, optional), `targetPortIndex` (int, optional). |
| | `get_node_output_values`| Lấy dữ liệu đã tính toán: `nodeIds` (array of string, optional), `includeGeometry` (bool, default `false`), `limit` (int, 1–200, default 50). |
| | `export_graph_to_image`| Chụp ảnh canvas: `outputPath` (string, optional), `format` ("PNG"/"JPEG", default "PNG"). |

### 4.3. Registry Meta Tools (8 công cụ quản lý thư viện mở rộng)
`search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
```

---

### Drop-In Block B: Section 5 (Practical Examples)
*Target: Lines 120–193 of `.agents/skills/hp-mcp-dynamo/SKILL.md`*

```markdown
## 5. Các Ví dụ Thực hành Điển hình (Practical Examples)

### Ví dụ 1: Chạy tự động file `.dyn` với tham số đầu vào
```json
{
  "name": "run_dyn_file",
  "arguments": {
    "filePath": "C:\\DynamoScripts\\TaoCotTuCad.dyn",
    "inputs": {
      "ChieuCaoTang": 3600,
      "TietDienCot": "C400x400"
    },
    "dryRun": true
  }
}
```

### Ví dụ 2: Tạo node CodeBlock DesignScript và nối dây
1. Tạo node CodeBlock:
```json
{
  "name": "create_code_block_node",
  "arguments": {
    "code": "x = 5000;\ny = 3000;\nPoint.ByCoordinates(x, y, 0);",
    "x": 200,
    "y": 150
  }
}
```
2. Nối đầu ra của CodeBlock vào một node tạo đối tượng:
```json
{
  "name": "connect_node_ports",
  "arguments": {
    "sourceNodeId": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
    "sourcePortIndex": 0,
    "targetNodeId": "a1c2d3e4-5678-90ab-cdef-1234567890ab",
    "targetPortIndex": 0
  }
}
```

### Ví dụ 3: Thực thi trực tiếp đoạn mã DesignScript
```json
{
  "name": "execute_designscript",
  "arguments": {
    "code": "pts = Point.ByCoordinates(0..10000..#5, 0, 0);\nlines = Line.ByStartPointEndPoint(pts, Point.ByCoordinates(0..10000..#5, 5000, 0));",
    "dryRun": false
  }
}
```

### Ví dụ 4: Tạo cụm nodes và nối dây theo lô bằng `batch_edit_graph`
```json
{
  "name": "batch_edit_graph",
  "arguments": {
    "nodes": [
      { "tempId": "$code", "nodeType": "CodeBlock", "x": 100, "y": 100, "initialValue": "x = 1000;\ny = 2000;" },
      { "tempId": "$pt", "nodeType": "Point.ByCoordinates", "x": 400, "y": 100 },
      { "tempId": "$watch", "nodeType": "CoreNodeModels.Watch", "x": 700, "y": 100 }
    ],
    "wires": [
      { "fromNode": "$code", "fromPort": 0, "toNode": "$pt", "toPort": 0 },
      { "fromNode": "$code", "fromPort": 1, "toNode": "$pt", "toPort": 1 },
      { "fromNode": "$pt", "fromPort": 0, "toNode": "$watch", "toPort": 0 }
    ],
    "runEvaluation": true,
    "dryRun": false
  }
}
```
```

---

## 5. Verification Checklist for Implementation Agent

When applying these changes to `.agents/skills/hp-mcp-dynamo/SKILL.md`:
1. [ ] Check line 94: `execute_designscript` parameter replaced `script` with `code`.
2. [ ] Check line 102: `run_dyn_file` parameter replaced `parameters` with `inputs`.
3. [ ] Check line 103: `validate_dyn_file` documents `fileContent`.
4. [ ] Check line 107: `add_node_to_workspace` replaced `nickname` with `name` and `initialValue`.
5. [ ] Check line 110: `connect_node_ports` replaced `startNodeId, startPortIndex, endNodeId, endPortIndex` with `sourceNodeId, sourcePortIndex, targetNodeId, targetPortIndex`.
6. [ ] Check line 111: `disconnect_node_ports` replaced `startNodeId...` with `nodeId, portIndex, portType, targetNodeId, targetPortIndex`.
7. [ ] Check line 113: `export_graph_to_image` replaced `outputFilePath` with `outputPath` and formats `PNG`/`JPEG`.
8. [ ] Check Example 1: `"inputs": { ... }`.
9. [ ] Check Example 2: `"sourceNodeId"`, `"sourcePortIndex"`, `"targetNodeId"`, `"targetPortIndex"`.
10. [ ] Check Example 3: `"code": "..."`.
