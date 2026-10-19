# Handoff Report — explorer_dynamo_m4_it2_2

## 1. Observation

Direct empirical observations and forensic source comparisons on `SKILL.md` vs. HPDynamo MCP code & manifests:

### 1.1 Tool Parameter Discrepancies in `.agents/skills/hp-mcp-dynamo/SKILL.md`
1. **`execute_designscript` (Table 4.1 line 94 & Section 5 Example 3 line 166)**:
   - `SKILL.md:94`: `script (string), dryRun (bool)`
   - `SKILL.md:166`: `"script": "pts = Point.ByCoordinates(...); ..."`
   - `HPDynamo/HPDynamo.Mcp.Server/Tools/ExecuteDesignScriptTool.cs:35`:
     ```csharp
     public Task<CallToolResult> ExecuteAsync(
         [Description("...")] string code,
         string transaction = TransactionModes.Auto,
         bool dryRun = false,
         int timeoutSeconds = 30, ...
     ```
   - Observed fact: Parameter name is `code`, NOT `script`. Passing `script` results in MCP SDK error rejecting call due to missing required argument `code`.

2. **`run_dyn_file` (Table 4.2 line 102 & Section 5 Example 1 line 128)**:
   - `SKILL.md:102`: `filePath (string), parameters (object), dryRun (bool)`
   - `SKILL.md:128`: `"parameters": { "ChieuCaoTang": 3600, "TietDienCot": "C400x400" }`
   - `HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/run_dyn_file/tool.json:34-49`:
     ```json
     "inputs": {
       "type": "object",
       "description": "Dictionary of node names or GUIDs mapped to override values..."
     },
     "additionalProperties": false
     ```
   - `HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/run_dyn_file/code.cs:10`:
     ```csharp
     var inputOverrides = args.Obj("inputs");
     ```
   - Observed fact: Parameter name is `inputs`, NOT `parameters`. Because `additionalProperties: false`, passing `parameters` fails schema validation, and `code.cs` strictly reads `args.Obj("inputs")`.

3. **`connect_node_ports` (Table 4.2 line 110 & Section 5 Example 2 lines 154-157)**:
   - `SKILL.md:110`: `startNodeId, startPortIndex, endNodeId, endPortIndex`
   - `SKILL.md:154-157`:
     ```json
     "startNodeId": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
     "startPortIndex": 0,
     "endNodeId": "a1c2d3e4-5678-90ab-cdef-1234567890ab",
     "endPortIndex": 0
     ```
   - `HPDynamo.Mcp.Server/Registry/SeedLibrary/Wire/connect_node_ports/tool.json:26-31`:
     ```json
     "required": [
       "sourceNodeId",
       "sourcePortIndex",
       "targetNodeId",
       "targetPortIndex"
     ]
     ```
   - `HPDynamo.Mcp.Server/Registry/SeedLibrary/Wire/connect_node_ports/code.cs:1-4`:
     ```csharp
     string sourceNodeIdentifier = args.Require("sourceNodeId");
     int sourceOutputPortIndex = args.Int("sourcePortIndex", 0);
     string targetNodeIdentifier = args.Require("targetNodeId");
     int targetInputPortIndex = args.Int("targetPortIndex", 0);
     ```
   - Observed fact: All 4 parameter names differ (`sourceNodeId` vs `startNodeId`, `sourcePortIndex` vs `startPortIndex`, `targetNodeId` vs `endNodeId`, `targetPortIndex` vs `endPortIndex`). Calling this tool per SKILL.md fails schema validation and throws `ArgumentException("args.sourceNodeId is required.")`.

4. **`disconnect_node_ports` (Table 4.2 line 111)**:
   - `SKILL.md:111`: `startNodeId, startPortIndex, endNodeId, endPortIndex`
   - `HPDynamo.Mcp.Server/Registry/SeedLibrary/Wire/disconnect_node_ports/tool.json:27-59`:
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
   - `HPDynamo.Mcp.Server/Registry/SeedLibrary/Wire/disconnect_node_ports/code.cs:1`:
     ```csharp
     string primaryNodeIdentifier = args.Require("nodeId");
     ```
   - Observed fact: Parameters `startNodeId`, `endNodeId` do not exist. Invocations per SKILL.md throw `ArgumentException("args.nodeId is required.")`.

5. **`add_node_to_workspace` (Table 4.2 line 107)**:
   - `SKILL.md:107`: `nodeType (string), x (number), y (number), nickname (string)`
   - `HPDynamo.Mcp.Server/Registry/SeedLibrary/Node/add_node_to_workspace/tool.json:44-51`:
     ```json
     "name": { "type": "string", "description": "Optional custom display title for the created node" },
     "initialValue": { "type": "string", "description": "Optional initial value or code expression..." }
     ```
   - Observed fact: Parameter is `name`, NOT `nickname`. Passing `nickname` is rejected by `additionalProperties: false`.

6. **`export_graph_to_image` (Table 4.2 line 113)**:
   - `SKILL.md:113`: `outputFilePath (string), format ("png"/"svg")`
   - `HPDynamo.Mcp.Server/Registry/SeedLibrary/Export/export_graph_to_image/tool.json:29-41`:
     ```json
     "outputPath": { "type": "string" },
     "format": { "type": "string", "enum": ["PNG", "JPEG"], "default": "PNG" }
     ```
   - Observed fact: Parameter is `outputPath`, NOT `outputFilePath`. Format enum is `["PNG", "JPEG"]`, NOT `"svg"`.

7. **`validate_dyn_file` (Table 4.2 line 103)**:
   - `SKILL.md:103`: `filePath (string)`
   - `HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/validate_dyn_file/code.cs:124-127`:
     ```csharp
     else
     {
         throw new ArgumentException("Provide 'fileContent' (raw JSON of the .dyn graph) or ensure the graph is loaded in the active Dynamo workspace.");
     }
     ```
   - Observed fact: Offline validation of unopened files requires `fileContent` (raw JSON) because `System.IO.File` is forbidden by ScriptGuard.

### 1.2 Status of Test Suites and Tool Counts
- `dotnet test HPDynamo/HPDynamo.E2E.Tests`: 209/209 passed.
- `HPDynamo/HPDynamo.E2E.Tests/Contracts/DynamoE2EContracts.cs:61-76`: Contains 13 seeds (`batch_edit_graph` is present at line 75).
- `HPDynamo/HPDynamo.E2E.Tests/Tiers/Tier1_ContractsAndServerTests.cs:78-82`: `TC_T1_SRV_01_ToolSurface_Count_IsExactly25Tools` asserts `Assert.Equal(25, tools.Count);`.
- `HPDynamo/HPDynamo.Mcp.Server.Tests/Surface/ToolSurfaceTests.cs:71-74`: Asserts `Assert.Equal(12, staticTools.Count); Assert.Equal(13, seeds.Count); Assert.Equal(25, combined.Count);`.

---

## 2. Logic Chain

1. In accordance with Section 4 of `ORIGINAL_REQUEST.md` (R4B) and `PROJECT.md` (item 12), `SKILL.md` is the primary AI operational guide for interacting with HPDynamo MCP.
2. The MCP protocol server strictly validates all tool call arguments against JSON Schema definitions (`tool.json`) where `"additionalProperties": false` is enforced.
3. Observations 1.1.1 through 1.1.6 confirm that calling tools using the parameter names published in `SKILL.md` directly violates the schemas or code arguments, causing immediate runtime failure (`ArgumentException` or JSON RPC error).
4. Observation 1.1.7 confirms that calling `validate_dyn_file` with only a file path for an offline file fails because ScriptGuard sandbox restrictions prevent `System.IO` file reads, requiring `fileContent`.
5. Therefore, remediation requires editing `.agents/skills/hp-mcp-dynamo/SKILL.md` Sections 4.1, 4.2, and Section 5 Examples 1, 2, 3 to reflect the verified production parameter contracts.

---

## 3. Caveats

- Investigation is strictly read-only; no modifications to `.agents/skills/hp-mcp-dynamo/SKILL.md` were performed by this agent (remediation text is delivered via `report.md` and this handoff).
- Active Revit process (PID 26264) was not touched.

---

## 4. Conclusion

The 7 discrepancies identified by `challenger_dynamo_m4_2` in `SKILL.md` are 100% verified and reproducible. Complete drop-in replacement text blocks have been drafted in `report.md`.

### Summary of Drop-In Changes:
- **Table 4.1 line 94**: Change `script (string), dryRun (bool)` to `code (string, required), transaction ("auto"/"manual"/"none"), dryRun (bool), timeoutSeconds (int, 5–600), label (string), args (object)`.
- **Table 4.2 line 102**: Change `parameters (object)` to `inputs (object, dictionary tên/GUID node -> giá trị), runHeadless (bool), extractOutputs (bool)`.
- **Table 4.2 line 103**: Document `fileContent (string, bắt buộc khi kiểm tra file offline chưa mở), filePath (string, khi kiểm tra workspace hiện hành), checkPackages (bool, default true)`.
- **Table 4.2 line 107**: Change `nickname (string)` to `name (string, optional), initialValue (string, optional)`.
- **Table 4.2 line 110**: Change `startNodeId, startPortIndex, endNodeId, endPortIndex` to `sourceNodeId (string, required), sourcePortIndex (int, required), targetNodeId (string, required), targetPortIndex (int, required)`.
- **Table 4.2 line 111**: Change `startNodeId, startPortIndex, endNodeId, endPortIndex` to `nodeId (string, required), portIndex (int, default 0), portType ("input"/"output", default "input"), targetNodeId (string, optional), targetPortIndex (int, optional)`.
- **Table 4.2 line 113**: Change `outputFilePath (string), format ("png"/"svg")` to `outputPath (string, optional), format ("PNG"/"JPEG", default "PNG")`.
- **Example 1 line 128**: Replace `"parameters": { ... }` with `"inputs": { ... }`.
- **Example 2 lines 154-157**: Replace `"startNodeId"..."endPortIndex"` with `"sourceNodeId"..."targetPortIndex"`.
- **Example 3 line 166**: Replace `"script": "..."` with `"code": "..."`.

---

## 5. Verification Method

To verify these corrections:
1. Review `report.md` at `.agents/teamwork/explorer_dynamo_m4_it2_2/report.md`.
2. Inspect tool manifests:
   - `HPDynamo/HPDynamo.Mcp.Server/Tools/ExecuteDesignScriptTool.cs` (line 35)
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/run_dyn_file/tool.json` (lines 34–37)
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/validate_dyn_file/tool.json` (lines 29–41)
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Node/add_node_to_workspace/tool.json` (lines 44–51)
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Wire/connect_node_ports/tool.json` (lines 26–50)
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Wire/disconnect_node_ports/tool.json` (lines 27–59)
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Export/export_graph_to_image/tool.json` (lines 29–41)
3. Run test command:
   ```powershell
   dotnet test HPDynamo/HPDynamo.E2E.Tests
   ```
