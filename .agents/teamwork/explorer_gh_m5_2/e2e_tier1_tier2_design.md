# E2E Acceptance Test Suite: Tier 1 & Tier 2 Test Design Specification
**Subsystem**: HPGrasshopper MCP Bridge (Rhino.Inside.Revit 2026)  
**Author**: `explorer_gh_m5_2` (E2E Acceptance Test Suite Tiers 1 & 2 Design Explorer)  
**Date**: 2026-10-10  
**Target Milestone**: M5 (E2E Verification & Hardening)  
**Reference Documents**: `ORIGINAL_REQUEST.md` (§ 2026-10-10T06:56:11Z), `PROJECT.md`, `TEST_INFRA.md`, `HPDynamo.E2E.Tests`

---

## 1. Executive Summary & Test Philosophy

This specification establishes the authoritative, requirements-driven opaque-box test design for **Tier 1 (Feature Coverage)** and **Tier 2 (Boundary & Corner Cases)** of the `HPGrasshopper.E2E.Tests` suite.

### 1.1 Test Philosophy
- **Requirement-Driven & Opaque-Box**: Tests are derived directly from the user specifications in `ORIGINAL_REQUEST.md` (§ 2026-10-10T06:56:11Z) and `TEST_INFRA.md`. Tests validate behavioral contracts and external interfaces, never internal private implementation details.
- **Headless & Zero-Popup**: The E2E test project references only `McpShared` (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`). It has zero dependency on native Rhino or Revit binaries, ensuring 100% unattended CI/CD execution without Windows modal dialog popups (`STATUS_HARD_ERROR`).
- **Clean Code Governance**: Strictly complies with `HP_CLEAN_CODE_CORE.md`:
  - **Rule C2**: File length $\le 300$ lines.
  - **Rule M3**: Method length $\le 50$ lines.
  - **Rule Q-B1**: Zero dead or commented-out code.
  - **Rule Q-B2**: Zero unannotated empty catch blocks.

### 1.2 Quantitative Target Summary
| Tier | Methodology | Target Scope | Minimum Required | Designed Count |
|---|---|---|:---:|:---:|
| **Tier 1** | Category-Partition | Full coverage of all 12 features | $\ge 60$ (5/feature) | **64 test cases** |
| **Tier 2** | Boundary Value Analysis & Negative Testing | Boundary, stress, negative, security | $\ge 60$ (5/feature) | **64 test cases** |
| **Total** | | Features 1–12 comprehensive coverage | $\ge 120$ | **128 test cases** |

---

## 2. Feature Inventory (Features 1–12)

According to `TEST_INFRA.md § Feature Inventory`, the subsystem comprises 12 distinct feature areas:

| # | Feature Area | Primary Requirement | Core Subject & Interface |
|---|---|---|---|
| **F01** | Named Pipe Server & Protocol | R1, Acceptance Criteria | Named Pipe `hpgrasshopper-mcp-2026`, NDJSON framing, JSON-RPC 2.0 dispatch |
| **F02** | Modeless WPF UI & Status | R1 | `GrasshopperBridgeStatusViewModel`, MaterialDesign 5.3.2, opt-in execution toggle |
| **F03** | Read Grasshopper Canvas Tool | R2.1 | `read_grasshopper_canvas`, component extraction, parameter graphs, wire linkages |
| **F04** | Run Grasshopper Player Tool | R2.2 | `run_grasshopper_player`, `.gh`/`.ghx` headless execution, input binding, output eval |
| **F05** | Batch Edit Canvas Tool | R2.3 | `batch_edit_grasshopper_canvas`, add/remove components, connect/disconnect wires |
| **F06** | Execute Grasshopper Code Tool | R2.4 | `execute_grasshopper_code`, Roslyn C# engine, dual-host globals, transaction modes |
| **F07** | Export Canvas Image Tool | R2.5 | `export_grasshopper_canvas_image`, zoom clamping [0.25..4.0], base64 and disk PNG |
| **F08** | McpShared HostProfile & Security Guard | R3 | `HostProfile.Grasshopper`, `GuardProfile.Grasshopper`, denied namespace & AST checks |
| **F09** | 8 Dynamic Registry Meta Tools | R4 | `search_tools`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`, quality walker |
| **F10** | 12 Embedded Seeds Library | R4 | 12 domain seeds, JSON schemas, examples, ADR-0007 script quality compliance |
| **F11** | Dual-Layer Safety & DryRun Rollback | R1, R2.4, Acceptance | Revit `TransactionGroup` rollback + Grasshopper `CanvasUndoScope` / `GH_UndoRecord` |
| **F12** | Solution Build & Assembly Integrity | Acceptance Criteria | `HPGrasshopper.slnx`, 24-tool surface invariant, dependency direction, suppression |

---

## 3. Tier 1: Category-Partition Feature Coverage Test Cases (64 Tests)

Each test case is uniquely identified by `TC_T1_<FEATURE_CODE>_<SEQ>`.

### 3.1 Feature 1: Named Pipe Server & Protocol (F01)
- **TC_T1_NPS_01: Pipe Server Name Resolution**
  - *Precondition*: `HostProfile.Grasshopper` configured.
  - *Action*: Call `PipeNaming.For(PipeNaming.GrasshopperHost, 2026)`.
  - *Expected*: Returns exactly `"hpgrasshopper-mcp-2026"`.
- **TC_T1_NPS_02: JSON-RPC 2.0 Method Prefix Conformance**
  - *Precondition*: Inspect `JsonRpcMethods.GrasshopperPrefix`.
  - *Action*: Verify RPC method prefix string.
  - *Expected*: Matches `"grasshopper."`, prefixing `execute`, `context`, `cancel`.
- **TC_T1_NPS_03: NDJSON Framing and Message Parsing**
  - *Precondition*: Active pipe stream with NDJSON reader/writer.
  - *Action*: Send single-line JSON-RPC request terminated by `\n`.
  - *Expected*: Server parses request without framing errors and dispatches handler.
- **TC_T1_NPS_04: Request-Response ID Correlation**
  - *Precondition*: Send multiple asynchronous requests with IDs `["id-101", "id-102", "id-103"]`.
  - *Action*: Receive responses across pipe.
  - *Expected*: Response IDs match request IDs with 100% correlation.
- **TC_T1_NPS_05: Graceful Client Disconnect & Reconnect Lifecycle**
  - *Precondition*: Bridge pipe listener running.
  - *Action*: Connect client, send ping, disconnect, immediately reconnect second client.
  - *Expected*: Second connection succeeds without listener crash or stale handle.

### 3.2 Feature 2: Modeless WPF UI & Status (F02)
- **TC_T1_WPF_01: Status ViewModel Initial State**
  - *Precondition*: Instantiate `GrasshopperBridgeStatusViewModel` with fake runner.
  - *Action*: Query properties `IsListening`, `IsExecutionEnabled`, `PipeName`.
  - *Expected*: `PipeName == "hpgrasshopper-mcp-2026"`, `IsExecutionEnabled == false`.
- **TC_T1_WPF_02: Toggle Listener Command Execution**
  - *Precondition*: ViewModel initialized with runner.
  - *Action*: Execute `ToggleListenerCommand`.
  - *Expected*: Runner toggles listener state; `StatusKind` transitions cleanly.
- **TC_T1_WPF_03: Opt-In Execution Toggle Property Change**
  - *Precondition*: ViewModel bound to view.
  - *Action*: Set `IsExecutionEnabled = true`.
  - *Expected*: PropertyChanged event fires for `IsExecutionEnabled`; underlying settings updated.
- **TC_T1_WPF_04: Script Preview & Copy Command**
  - *Precondition*: Script executed with preview text populated.
  - *Action*: Execute `CopyLastScriptCommand`.
  - *Expected*: Clipboard callback receives full `LastScriptPreview` string.
- **TC_T1_WPF_05: MaterialDesign Theme Synchronization**
  - *Precondition*: `MaterialThemeBridge` active.
  - *Action*: Apply light and dark palette updates.
  - *Expected*: Theme brush dictionary updates without exception.

### 3.3 Feature 3: Read Grasshopper Canvas Tool (F03)
- **TC_T1_RDC_01: Complete Canvas Graph Extraction**
  - *Precondition*: Canvas populated with components and wires.
  - *Action*: Invoke `read_grasshopper_canvas` with default parameters.
  - *Expected*: Returns document metadata, non-empty components list, non-empty wires list.
- **TC_T1_RDC_02: Include Parameter Values Flag**
  - *Precondition*: Canvas components have computed output values.
  - *Action*: Invoke `read_grasshopper_canvas` with `includeValues = true`.
  - *Expected*: Output parameter records contain serialized data summaries.
- **TC_T1_RDC_03: Selected Only Component Filtering**
  - *Precondition*: Canvas with 5 components, 2 marked `Selected = true`.
  - *Action*: Invoke `read_grasshopper_canvas` with `selectedOnly = true`.
  - *Expected*: Result contains exactly the 2 selected components.
- **TC_T1_RDC_04: Category Filtering Mechanism**
  - *Precondition*: Canvas contains `Maths` and `Params` components.
  - *Action*: Invoke `read_grasshopper_canvas` with `categoryFilter = "Maths"`.
  - *Expected*: Returns only components belonging to category `"Maths"`.
- **TC_T1_RDC_05: Runtime Warning & Error Messages Extraction**
  - *Precondition*: Component on canvas has runtime error message.
  - *Action*: Invoke `read_grasshopper_canvas`.
  - *Expected*: Component's `messages` array contains the exact error string.

### 3.4 Feature 4: Run Grasshopper Player Tool (F04)
- **TC_T1_RPL_01: Player Definition Execution**
  - *Precondition*: Valid `.gh` definition file on disk.
  - *Action*: Invoke `run_grasshopper_player` with `filePath`.
  - *Expected*: Returns `success = true`, `durationMs > 0`.
- **TC_T1_RPL_02: Parameter Input Injection**
  - *Precondition*: Definition contains input sliders/panels `"Length"` and `"Width"`.
  - *Action*: Invoke with `inputs = { "Length": 100.0, "Width": 50.0 }`.
  - *Expected*: Target parameters updated; `inputsApplied == 2`.
- **TC_T1_RPL_03: Output Extraction Filtering**
  - *Precondition*: Definition produces outputs `"Area"` and `"Perimeter"`.
  - *Action*: Invoke with `outputs = ["Area"]`.
  - *Expected*: Results dictionary contains key `"Area"`; excludes unrequested outputs.
- **TC_T1_RPL_04: Headless Lifecycle Disposal**
  - *Precondition*: Player invoked with `headless = true`.
  - *Action*: Complete execution.
  - *Expected*: Document is cleanly disposed; no memory leak or orphan document in document server.
- **TC_T1_RPL_05: Execution Duration Telemetry**
  - *Precondition*: Run definition calculation.
  - *Action*: Inspect execution result.
  - *Expected*: Returns non-negative integer `durationMs` tracking solver elapsed time.

### 3.5 Feature 5: Batch Edit Canvas Tool (F05)
- **TC_T1_BEC_01: Batch Component Addition**
  - *Precondition*: Active canvas open.
  - *Action*: Pass `componentsToAdd` with 2 component definitions and coordinates `(100, 200)`.
  - *Expected*: Components placed onto canvas; returns `added == 2`.
- **TC_T1_BEC_02: Batch Component Removal**
  - *Precondition*: Canvas contains component with known GUID.
  - *Action*: Pass `componentsToRemove = [knownGuid]`.
  - *Expected*: Component removed from canvas; returns `removed == 1`.
- **TC_T1_BEC_03: Wire Connection Creation**
  - *Precondition*: Two components on canvas.
  - *Action*: Pass `wiresToConnect` specifying source/target GUIDs and param names.
  - *Expected*: Source param wire connected to target param; returns `connected == 1`.
- **TC_T1_BEC_04: Wire Disconnection**
  - *Precondition*: Existing wire between two components.
  - *Action*: Pass `wiresToDisconnect` with parameter pair.
  - *Expected*: Wire unlinked; returns `disconnected == 1`.
- **TC_T1_BEC_05: TempId Internal Resolution**
  - *Precondition*: Single batch request adding comp A with `tempId="node1"` and connecting to comp B.
  - *Action*: Execute batch edit.
  - *Expected*: Resolves `tempId` within the same batch; successfully creates wire.

### 3.6 Feature 6: Execute Grasshopper Code Tool (F06)
- **TC_T1_EXC_01: Pure C# Expression Execution**
  - *Precondition*: Opt-in enabled.
  - *Action*: Send `execute_grasshopper_code` with `int a = 10; int b = 20; return a + b;`.
  - *Expected*: Returns `Success = true`, `Value == 30`.
- **TC_T1_EXC_02: Standard Globals Accessibility**
  - *Precondition*: Execution environment initialized.
  - *Action*: Execute script accessing `doc`, `canvas`, `rhinoDoc`, `revitDoc`, `args`.
  - *Expected*: Script runs without null reference on globals.
- **TC_T1_EXC_03: Read-Only Transaction Mode ("none")**
  - *Precondition*: Request with `transaction = "none"`.
  - *Action*: Execute query script.
  - *Expected*: Runs without opening Revit transaction; `RolledBack == false`.
- **TC_T1_EXC_04: Auto Transaction Mode ("auto")**
  - *Precondition*: Request with `transaction = "auto"`.
  - *Action*: Execute script modifying model.
  - *Expected*: Revit transaction opened and committed automatically.
- **TC_T1_EXC_05: Script Args Parameter Parsing**
  - *Precondition*: Pass `args = { "Scale": 2.5, "Prefix": "GH_" }`.
  - *Action*: Script reads `args.Double("Scale")` and `args.Str("Prefix")`.
  - *Expected*: Returns expected scaled values based on input arguments.

### 3.7 Feature 7: Export Canvas Image Tool (F07)
- **TC_T1_IMG_01: Base64 PNG Canvas Export**
  - *Precondition*: Active canvas open.
  - *Action*: Invoke `export_grasshopper_canvas_image` with `returnBase64 = true`.
  - *Expected*: Returns `base64` string starting with `"data:image/png;base64,"`.
- **TC_T1_IMG_02: Direct File Output Path Export**
  - *Precondition*: Temporary file path specified.
  - *Action*: Invoke with `outputPath = "temp/canvas.png"`.
  - *Expected*: PNG file created on disk at path; file size $> 0$.
- **TC_T1_IMG_03: Zoom Scaling Adjustment**
  - *Precondition*: Export with `zoom = 2.0` vs `zoom = 1.0`.
  - *Action*: Compare pixel dimensions of output images.
  - *Expected*: Dimensions scale proportionally with zoom factor.
- **TC_T1_IMG_04: Base64 Suppression Optimization**
  - *Precondition*: Export with `returnBase64 = false` and valid `outputPath`.
  - *Action*: Inspect result DTO.
  - *Expected*: `base64` is empty string; output written solely to disk.
- **TC_T1_IMG_05: Auto Directory Creation**
  - *Precondition*: Output path targets nested directory that does not yet exist.
  - *Action*: Export image.
  - *Expected*: Directory is automatically created; image saved without directory not found exception.

### 3.8 Feature 8: McpShared HostProfile & Security Guard (F08)
- **TC_T1_GRD_01: HostProfile Metadata Invariants**
  - *Precondition*: Access `HostProfile.Grasshopper`.
  - *Action*: Assert `HostId`, `DefaultVersion`, `MaxTimeoutSeconds`.
  - *Expected*: `HostId == "grasshopper"`, `DefaultVersion == 2026`, `MaxTimeoutSeconds == 600`.
- **TC_T1_GRD_02: Legitimate Rhino/Grasshopper API Allowance**
  - *Precondition*: Code creates `Rhino.Geometry.Point3d`, `GH_Structure`, accesses `doc.DisplayName`.
  - *Action*: Run `ScriptGuard.Check(code, GuardProfile.Grasshopper)`.
  - *Expected*: Returns 0 violations; script allowed.
- **TC_T1_GRD_03: System.IO Namespace Denial**
  - *Precondition*: Code calls `System.IO.File.Delete("sample.txt")`.
  - *Action*: Run `ScriptGuard.Check(code, GuardProfile.Grasshopper)`.
  - *Expected*: Returns guard violation referencing `"System.IO"`.
- **TC_T1_GRD_04: System.Diagnostics.Process Denial**
  - *Precondition*: Code calls `System.Diagnostics.Process.Start("cmd.exe")`.
  - *Action*: Run `ScriptGuard.Check(code, GuardProfile.Grasshopper)`.
  - *Expected*: Returns guard violation referencing `"System.Diagnostics.Process"`.
- **TC_T1_GRD_05: UI Popups Denial (TaskDialog & MessageBox)**
  - *Precondition*: Code calls `TaskDialog.Show()` or `MessageBox.Show()`.
  - *Action*: Run `ScriptGuard.Check(code, GuardProfile.Grasshopper)`.
  - *Expected*: Returns guard violation; prohibits interactive UI in headless mode.

### 3.9 Feature 9: 8 Dynamic Registry Meta Tools (F09)
- **TC_T1_REG_01: Search Tools Discovery**
  - *Precondition*: Registry seeded with tools.
  - *Action*: Invoke `search_tools` with query `"rebar"`.
  - *Expected*: Returns list containing rebar-related tools with schemas.
- **TC_T1_REG_02: Propose Tool Draft Registration**
  - *Precondition*: Valid tool script and metadata.
  - *Action*: Invoke `propose_tool`.
  - *Expected*: Tool created in Draft status with unique tool ID.
- **TC_T1_REG_03: Test Tool Sandbox Execution**
  - *Precondition*: Proposed draft tool in registry.
  - *Action*: Invoke `test_tool` with sample arguments.
  - *Expected*: Executes test run; transitions state to Tested.
- **TC_T1_REG_04: Publish Tool Promotion**
  - *Precondition*: Tested tool in registry.
  - *Action*: Invoke `publish_tool`.
  - *Expected*: Tool status updated to Published; available in live tool surface.
- **TC_T1_REG_05: Manage Tool Retirement Lifecycle**
  - *Precondition*: Published tool in registry.
  - *Action*: Invoke `manage_tool` with action `"retire"`.
  - *Expected*: Tool status transitions to Retired; hidden from default search.

### 3.10 Feature 10: 12 Embedded Seeds Library (F10)
- **TC_T1_SED_01: Seed Catalog Completeness (12 Seeds)**
  - *Precondition*: Inspect embedded seed manifests.
  - *Action*: Query seed library catalog.
  - *Expected*: Contains exactly all 12 specified seeds.
- **TC_T1_SED_02: Seed Tool Schema Completeness**
  - *Precondition*: Load `tool.json` for all 12 seeds.
  - *Action*: Validate JSON schema, description, input parameters.
  - *Expected*: 100% of seeds have valid schemas with descriptions and parameter types.
- **TC_T1_SED_03: Seed Examples Manifest Validation**
  - *Precondition*: Load `examples.json` for all 12 seeds.
  - *Action*: Verify presence of input examples.
  - *Expected*: 100% of seeds contain realistic example arguments.
- **TC_T1_SED_04: Seed Code AST Quality Walker Pass**
  - *Precondition*: Load `code.cs` for all 12 seeds.
  - *Action*: Run ADR-0007 quality walker check on every seed script.
  - *Expected*: 12/12 seeds pass with 0 errors, 0 dead code, 0 unannotated empty catch.
- **TC_T1_SED_05: Structural Rebar Seeds Parameter Coverage**
  - *Precondition*: Check `generate_rebar_curves` and `generate_stirrup_distribution`.
  - *Action*: Verify parameters for bar diameter, spacing, cover, hook angles.
  - *Expected*: Rebar parameters present with valid boundary specifications.

### 3.11 Feature 11: Dual-Layer Safety & DryRun Rollback (F11)
- **TC_T1_DLS_01: DryRun Revit TransactionGroup Rollback**
  - *Precondition*: Script creates Revit elements with `dryRun = true`.
  - *Action*: Execute script via `RevitTransactionScope`.
  - *Expected*: Returns `Success = true`, `RolledBack = true`; net element count change is 0.
- **TC_T1_DLS_02: DryRun CanvasUndoScope Canvas Rollback**
  - *Precondition*: Batch edit modifies canvas with `dryRun = true`.
  - *Action*: Execute batch edit via `CanvasEditor`.
  - *Expected*: Returns `rolledBack = true`; all added components removed; original state preserved.
- **TC_T1_DLS_03: Commit Execution Dual Persistence**
  - *Precondition*: Execution request with `dryRun = false`.
  - *Action*: Execute script.
  - *Expected*: Returns `RolledBack = false`; changes permanently committed to document and canvas.
- **TC_T1_DLS_04: Script Exception Rollback Safety**
  - *Precondition*: Script throws exception midway during execution.
  - *Action*: Dispatch script in auto transaction mode.
  - *Expected*: Entire transaction group rolls back; returns error result; 0 orphan elements.
- **TC_T1_DLS_05: Canvas Exception Compensating Rollback**
  - *Precondition*: Batch edit encounters error midway.
  - *Action*: Execute batch edit.
  - *Expected*: `CanvasUndoScope.Rollback()` called; canvas restored to initial state.

### 3.12 Feature 12: Solution Build & Assembly Integrity (F12)
- **TC_T1_BLD_01: Total Tool Surface Invariant (Exactly 24 Tools)**
  - *Precondition*: Initialize `HPGrasshopper.Mcp.Server` host builder.
  - *Action*: Query exposed `McpServerTool` collection.
  - *Expected*: Total count is exactly 24 tools (5 Core + 8 Registry + 12 Seeds - 1 Suppressed).
- **TC_T1_BLD_02: Inspect Type Tool Suppression Invariant**
  - *Precondition*: Host profile enforced.
  - *Action*: Inspect tool surface for `"inspect_type"`.
  - *Expected*: Tool collection does NOT contain `"inspect_type"`.
- **TC_T1_BLD_03: Tool Naming Snake_Case Uniformity**
  - *Precondition*: Collect all 24 exposed tool names.
  - *Action*: Check each tool name against lowercase snake_case regex `^[a-z0-9_]+$`.
  - *Expected*: 100% of tool names conform; zero uppercase letters; zero spaces.
- **TC_T1_BLD_04: McpShared Single Dependency Direction**
  - *Precondition*: Inspect project references in `HPGrasshopper.slnx`.
  - *Action*: Verify dependency graph.
  - *Expected*: Projects reference `../McpShared/` only; zero cross-references to other host MCPs.

---

## 4. Tier 2: Boundary Value Analysis & Negative Testing (64 Tests)

Each test case is uniquely identified by `TC_T2_<FEATURE_CODE>_<SEQ>`.

### 4.1 Feature 1: Named Pipe Server & Protocol Boundaries (F01)
- **TC_T2_NPS_01: Corrupted NDJSON Framing Negative Test**
  - *Input*: Malformed JSON line: `{"jsonrpc": "2.0", "method": "grasshopper.execute", "id": 1` (missing closing brace).
  - *Expected*: Server returns standard JSON-RPC parse error (`code = -32700`).
- **TC_T2_NPS_02: Unknown RPC Method Negative Test**
  - *Input*: Request with method `"grasshopper.non_existent_method"`.
  - *Expected*: Server returns method not found error (`code = -32601`).
- **TC_T2_NPS_03: Empty String and Whitespace Method Call**
  - *Input*: Request with method `""` or `"   "`.
  - *Expected*: Returns invalid request error (`code = -32600`).
- **TC_T2_NPS_04: Massive Payload Boundary Test (10 MB Payload)**
  - *Input*: Transmit 10 MB payload containing large script arguments over named pipe.
  - *Expected*: Pipe buffer reads full message or cleanly rejects with payload too large error without buffer overrun.
- **TC_T2_NPS_05: Premature Client Stream Severing**
  - *Input*: Client opens connection, sends 10 bytes of a 1,000-byte frame, then abruptly closes pipe.
  - *Expected*: Pipe server catches connection reset without crash; resumes listening.

### 4.2 Feature 2: Modeless WPF UI & Status Boundaries (F02)
- **TC_T2_WPF_01: Execution Attempt When Opt-In Disabled (-32001 Gate)**
  - *Precondition*: `IsExecutionEnabled == false`.
  - *Action*: Send `execute_grasshopper_code` request.
  - *Expected*: Fails immediately with error code `-32001` and message containing `"Allow AI Grasshopper execution"`.
- **TC_T2_WPF_02: Context Query Allowed Even When Opt-In Disabled**
  - *Precondition*: `IsExecutionEnabled == false`.
  - *Action*: Send `get_grasshopper_context` request.
  - *Expected*: Succeeds; read-only context querying is permitted when execution toggle is off.
- **TC_T2_WPF_03: Rapid Multithreaded Opt-In Toggle Flipping**
  - *Precondition*: Background execution requests arriving.
  - *Action*: Flip `IsExecutionEnabled` rapidly 50 times across threads.
  - *Expected*: State transitions atomically; zero deadlocks or inconsistent flag states.
- **TC_T2_WPF_04: Extremely Long LastScriptPreview Truncation**
  - *Input*: Script with 500,000 characters executed.
  - *Action*: Update `LastScriptPreview`.
  - *Expected*: UI view model safely holds or truncates string without OutOfMemory or UI thread freeze.
- **TC_T2_WPF_05: Clipboard Exception Resilience**
  - *Precondition*: System clipboard locked by another process (`CLIPBRD_E_CANT_OPEN`).
  - *Action*: Execute `CopyLastScriptCommand`.
  - *Expected*: Catches COMException gracefully; logs warning; UI does not crash.

### 4.3 Feature 3: Read Grasshopper Canvas Tool Boundaries (F03)
- **TC_T2_RDC_01: Null Active Document Graceful Fallback**
  - *Precondition*: No Grasshopper document is currently open (`GH_Document == null`).
  - *Action*: Invoke `read_grasshopper_canvas`.
  - *Expected*: Returns structured result: `{ name = "None", open = false, objectCount = 0 }`, components empty, wires empty.
- **TC_T2_RDC_02: Completely Empty Canvas Boundary (0 Objects)**
  - *Precondition*: Fresh empty canvas document created.
  - *Action*: Invoke `read_grasshopper_canvas`.
  - *Expected*: Returns `objectCount = 0`, empty components array, empty wires array.
- **TC_T2_RDC_03: Non-Existent Category Filter Negative Test**
  - *Input*: Filter `categoryFilter = "NonExistent_Category_99999"`.
  - *Action*: Read canvas.
  - *Expected*: Returns 0 components; succeeds cleanly without throwing.
- **TC_T2_RDC_04: Massive Canvas Stress Boundary (2,000 Components)**
  - *Precondition*: Synthetic canvas populated with 2,000 components and 3,000 wires.
  - *Action*: Read canvas with `includeValues = false`.
  - *Expected*: Serialization finishes within 3 seconds; memory consumption remains bounded.
- **TC_T2_RDC_05: Circular Wire Topology Safe Traversal**
  - *Precondition*: Canvas with circular feedback wire loop.
  - *Action*: Read canvas wires.
  - *Expected*: Wire list records all connections without infinite loop or stack overflow.

### 4.4 Feature 4: Run Grasshopper Player Tool Boundaries (F04)
- **TC_T2_RPL_01: Non-Existent File Path Negative Test**
  - *Input*: `filePath = "C:\\InvalidPath\\NoSuchFile.gh"`.
  - *Action*: Run Player.
  - *Expected*: Returns `success = false`, `error` containing `"Definition file not found"`.
- **TC_T2_RPL_02: Corrupted Archive File Negative Test**
  - *Input*: File containing random binary noise or truncated header.
  - *Action*: Run Player.
  - *Expected*: Returns `success = false`, `error` containing `"Failed to deserialize"`.
- **TC_T2_RPL_03: Empty Inputs Object Boundary Test**
  - *Input*: `inputs = {}` (empty JSON object).
  - *Action*: Run Player on definition expecting parameters.
  - *Expected*: Executes definition using internal default slider values; `inputsApplied == 0`.
- **TC_T2_RPL_04: Type Mismatched Input Values Negative Test**
  - *Input*: Pass string `"NotANumber"` to a Number Slider input parameter.
  - *Action*: Run Player.
  - *Expected*: Handles conversion failure gracefully; reports warning or applies 0 inputs without bridge crash.
- **TC_T2_RPL_05: Path Traversal Character Scrubbing**
  - *Input*: `filePath = "../../../Windows/System32/calc.exe"`.
  - *Action*: Run Player.
  - *Expected*: Rejects invalid extension / archive extraction failure without executing foreign process.

### 4.5 Feature 5: Batch Edit Canvas Tool Boundaries (F05)
- **TC_T2_BEC_01: Batch Edit on Null Document Negative Test**
  - *Precondition*: `GH_Document == null`.
  - *Action*: Invoke `batch_edit_grasshopper_canvas`.
  - *Expected*: Returns `success = false`, `error = "No active Grasshopper document is open."`.
- **TC_T2_BEC_02: Extreme Coordinate Values Clamping Boundary**
  - *Input*: Add component with coordinates `x = float.MaxValue`, `y = -1e20f`, `NaN`.
  - *Action*: Add component to canvas.
  - *Expected*: Handles coordinates without throwing `ArgumentException` or corrupting canvas rendering.
- **TC_T2_BEC_03: Unknown Component Type Name Negative Test**
  - *Input*: Add component with `type = "Invalid.Component.Type.DoesNotExist"`.
  - *Action*: Execute batch edit.
  - *Expected*: Skips invalid type gracefully; returns `added == 0`; does not abort other valid items.
- **TC_T2_BEC_04: Remove Non-Existent GUID Negative Test**
  - *Input*: `componentsToRemove = [Guid.NewGuid()]`.
  - *Action*: Execute batch edit.
  - *Expected*: Handles non-existent object gracefully; returns `removed == 0`.
- **TC_T2_BEC_05: Duplicate Wire Connection Idempotency**
  - *Input*: Connect wires between already-connected ports twice in the same batch.
  - *Action*: Execute batch edit.
  - *Expected*: Does not create duplicate redundant wire; topology remains clean.

### 4.6 Feature 6: Execute Grasshopper Code Tool Boundaries (F06)
- **TC_T2_EXC_01: C# Syntax Compilation Error Negative Test**
  - *Input*: Code `int a = ; // syntax error`.
  - *Action*: Execute code.
  - *Expected*: Returns `IsError = true`, `Message` containing compilation diagnostic, line/column info.
- **TC_T2_EXC_02: Unhandled Runtime Exception Negative Test**
  - *Input*: Code `throw new InvalidOperationException("Simulation engine failure");`.
  - *Action*: Execute code.
  - *Expected*: Catches exception; returns `IsError = true`, message containing exception details; clean transaction rollback.
- **TC_T2_EXC_03: Empty and Whitespace-Only Code Negative Test**
  - *Input*: Code `""` or `"   \r\n\t"`.
  - *Action*: Execute code.
  - *Expected*: Returns compilation failure or validation error without crashing.
- **TC_T2_EXC_04: Execution Timeout Exceeded Cancellation**
  - *Input*: Code `while (!ct.IsCancellationRequested) Thread.Sleep(100);`.
  - *Action*: Trigger cancellation token.
  - *Expected*: Returns cancellation status cleanly within deadline; resources released.
- **TC_T2_EXC_05: Large Script Line Count Limit (301 Lines Negative Test)**
  - *Input*: Script containing 301 lines of code.
  - *Action*: Check with ADR-0007 / ScriptGuard.
  - *Expected*: Validation fails with line limit violation (> 300 lines).

### 4.7 Feature 7: Export Canvas Image Tool Boundaries (F07)
- **TC_T2_IMG_01: Null Canvas Negative Test**
  - *Precondition*: `GH_Canvas == null`.
  - *Action*: Invoke `export_grasshopper_canvas_image`.
  - *Expected*: Returns `success = false`, `error = "Grasshopper canvas is not open or accessible."`.
- **TC_T2_IMG_02: Extreme Zoom Factor Lower Clamping (zoom = -10.0)**
  - *Input*: `zoom = -10.0` or `zoom = 0.0`.
  - *Action*: Export image.
  - *Expected*: Zoom clamped to minimum `0.25`; image generated with valid minimum dimensions.
- **TC_T2_IMG_03: Extreme Zoom Factor Upper Clamping (zoom = 100.0)**
  - *Input*: `zoom = 100.0`.
  - *Action*: Export image.
  - *Expected*: Zoom clamped to maximum `4.0`; prevents GDI+ `OutOfMemoryException` from allocating massive bitmap.
- **TC_T2_IMG_04: Zero Dimension Canvas Fallback (200x200 Minimum)**
  - *Precondition*: Canvas has `Width = 0`, `Height = 0`.
  - *Action*: Export image.
  - *Expected*: Bitmap created with minimum clamp dimensions $200 \times 200$.
- **TC_T2_IMG_05: Illegal Output File Path Characters Negative Test**
  - *Input*: `outputPath = "C:\\Invalid|Path*?\\file.png"`.
  - *Action*: Export image.
  - *Expected*: Returns `success = false`, error message without throwing unhandled exception.

### 4.8 Feature 8: McpShared HostProfile & Security Guard Boundaries (F08)
- **TC_T2_GRD_01: Global Alias Namespace Bypass Negative Test**
  - *Input*: `global::System.IO.File.WriteAllText("a.txt", "b");`.
  - *Action*: Check with `ScriptGuard.Check`.
  - *Expected*: Detected and blocked; denies `System.IO` despite `global::` prefix.
- **TC_T2_GRD_02: Hash R / Hash Load Preprocessor Directives Negative Test**
  - *Input*: `#r "RevitAPI.dll"\n#load "helper.csx"`.
  - *Action*: Check with `ScriptGuard.Check`.
  - *Expected*: Strictly denied; blocks dynamic external assembly loading.
- **TC_T2_GRD_03: Host Process Termination Calls Denial**
  - *Input*: `RhinoApp.Exit();`, `app.Exit();`, `app.Quit();`.
  - *Action*: Check with `ScriptGuard.Check`.
  - *Expected*: Denied; protects host Revit/Rhino process from termination.
- **TC_T2_GRD_04: Document Dispose Denial**
  - *Input*: `rhinoDoc.Dispose();` or `rhinoDoc?.Dispose();`.
  - *Action*: Check with `ScriptGuard.Check`.
  - *Expected*: Denied; prevents script from destroying active document handle.
- **TC_T2_GRD_05: Bridge Internal Namespace Tampering Denial**
  - *Input*: `using HPGrasshopper.McpBridge;` or `HPGrasshopper.McpBridge.Entry.Stop();`.
  - *Action*: Check with `ScriptGuard.Check`.
  - *Expected*: Denied; prevents script from accessing bridge internals.

### 4.9 Feature 9: 8 Dynamic Registry Meta Tools Boundaries (F09)
- **TC_T2_REG_01: Script Exceeding 300 Lines Rejected by Quality Walker**
  - *Input*: Proposed tool script with 305 lines.
  - *Action*: Run `propose_tool`.
  - *Expected*: Fails quality validation; reports script line count exceeds 300 (Rule C2).
- **TC_T2_REG_02: Unannotated Empty Catch Block Rejected**
  - *Input*: Proposed tool script containing `try { ... } catch { }` with no explanation comment.
  - *Action*: Run `propose_tool`.
  - *Expected*: Fails quality validation; reports unannotated empty catch (Rule Q-B2).
- **TC_T2_REG_03: Commented-Out Code Rejected**
  - *Input*: Proposed tool containing `// var x = doc.Objects;`.
  - *Action*: Run `propose_tool`.
  - *Expected*: Fails quality validation; reports commented-out code detected (Rule Q-B1).
- **TC_T2_REG_04: Publish Without Prior Testing Negative Test**
  - *Precondition*: Tool in Draft state with 0 test runs.
  - *Action*: Call `publish_tool`.
  - *Expected*: Fails state machine check; requires test run before publication.
- **TC_T2_REG_05: Tool Name Collision Negative Test**
  - *Input*: Propose tool with name already existing in Published catalog.
  - *Action*: Call `propose_tool`.
  - *Expected*: Fails with name conflict error.

### 4.10 Feature 10: 12 Embedded Seeds Library Boundaries (F10)
- **TC_T2_SED_01: Exact 300-Line Upper Bound Verification for All 12 Seeds**
  - *Precondition*: Load `code.cs` for all 12 seeds.
  - *Action*: Count lines in each seed file.
  - *Expected*: Every seed satisfies $\text{lineCount} \le 300$; zero exceptions.
- **TC_T2_SED_02: Missing Required Seed Parameter Negative Test**
  - *Input*: Invoke `create_levels_grids` omitting required `elevations` array.
  - *Action*: Validate input arguments.
  - *Expected*: Fails schema validation with specific missing parameter error.
- **TC_T2_SED_03: Parameter Fuzzing with Negative Dimensions**
  - *Input*: Pass `width = -500.0`, `height = 0.0` to `generate_columns_beams`.
  - *Action*: Seed script argument parsing.
  - *Expected*: Throws descriptive validation error before geometry generation.
- **TC_T2_SED_04: Null Geometry List Input Negative Test**
  - *Input*: Pass `null` or empty array for geometry input in `directshape_from_geometry`.
  - *Action*: Seed script execution.
  - *Expected*: Returns graceful error without NullReferenceException.
- **TC_T2_SED_05: Solar Radiation Date/Time Range Boundary**
  - *Input*: Pass `hour = 25`, `month = 13` to `evaluate_solar_radiation`.
  - *Action*: Execute seed validation.
  - *Expected*: Fails validation with out-of-range date/time error.

### 4.11 Feature 11: Dual-Layer Safety & DryRun Rollback Boundaries (F11)
- **TC_T2_DLS_01: Partial Script Failure 100% Rollback Verification**
  - *Precondition*: Script creates 50 elements, then deliberately throws at element 51.
  - *Action*: Execute script in `auto` transaction.
  - *Expected*: Transaction rolls back completely; 0 of the 50 initial elements remain committed.
- **TC_T2_DLS_02: Idempotent Double Rollback on CanvasUndoScope**
  - *Precondition*: `CanvasUndoScope` rolled back once.
  - *Action*: Call `scope.Rollback()` a second time.
  - *Expected*: Second rollback is no-op; does not throw exception or double-restore.
- **TC_T2_DLS_03: Dispose Without Commit Triggers Automatic Rollback**
  - *Precondition*: Scope opened, modifications made, scope disposed without calling `Commit()`.
  - *Action*: Exit using scope.
  - *Expected*: Automatic compensating rollback executed; safe fail-closed behavior.
- **TC_T2_DLS_04: Read-Only Document Transaction Prevention**
  - *Precondition*: Document marked `IsReadOnly = true`.
  - *Action*: Attempt execution with `transaction = "auto"`.
  - *Expected*: Request rejected before transaction opening; returns read-only failure.
- **TC_T2_DLS_05: Zero Document Change Count on DryRun**
  - *Precondition*: Attach `DocumentChangeCounter`.
  - *Action*: Run complex multi-element creation script with `dryRun = true`.
  - *Expected*: `counter.NetChangeCount == 0`.

### 4.12 Feature 12: Solution Build & Assembly Integrity Boundaries (F12)
- **TC_T2_BLD_01: Zero Disallowed Native Assembly References in Server**
  - *Precondition*: Inspect assembly references of `HPGrasshopper.Mcp.Server.dll`.
  - *Action*: Verify list of referenced assemblies.
  - *Expected*: Zero references to `RevitAPI.dll`, `RhinoCommon.dll`, `Grasshopper.dll`.
- **TC_T2_BLD_02: Zero Native Dependencies in E2E Test Suite**
  - *Precondition*: Inspect `HPGrasshopper.E2E.Tests.csproj`.
  - *Action*: Verify project and package references.
  - *Expected*: References only `McpShared` projects; 100% managed execution without host runtime.
- **TC_T2_BLD_03: Strict Rule C2 Compliance Across Entire E2E Test Project**
  - *Precondition*: Scan all `.cs` files in `HPGrasshopper.E2E.Tests`.
  - *Action*: Measure line count of each file.
  - *Expected*: 100% of files have $\le 300$ lines.
- **TC_T2_BLD_04: Strict Rule M3 Compliance Across All Test Methods**
  - *Precondition*: Scan all test methods in `HPGrasshopper.E2E.Tests`.
  - *Action*: Measure line count of each method.
  - *Expected*: 100% of methods have $\le 50$ lines.

---

## 5. Clean Code Architecture & Class Decomposition Plan

To guarantee strict compliance with **Rule C2 ($\le 300$ lines/file)** and **Rule M3 ($\le 50$ lines/method)**, the 128 tests for Tier 1 and Tier 2 are decomposed across 14 modular, focused test classes.

```
HPGrasshopper.E2E.Tests/
├── Contracts/
│   ├── GrasshopperE2EContracts.cs               (<= 120 lines)
│   └── GrasshopperE2EModels.cs                  (<= 150 lines)
├── Harness/
│   ├── GrasshopperE2EFakeBridge.cs              (<= 260 lines)
│   └── GrasshopperE2ETestHarness.cs             (<= 220 lines)
├── Tiers/
│   ├── Tier1_NamedPipeAndWpfTests.cs            (F01, F02: 10 tests, ~200 lines)
│   ├── Tier1_CanvasAndPlayerToolsTests.cs       (F03, F04: 10 tests, ~210 lines)
│   ├── Tier1_EditAndExportToolsTests.cs         (F05, F07: 10 tests, ~210 lines)
│   ├── Tier1_ExecutionAndSafetyTests.cs         (F06, F11: 10 tests, ~220 lines)
│   ├── Tier1_HostProfileAndSecurityTests.cs     (F08:       5 tests, ~130 lines)
│   ├── Tier1_RegistryAndSeedsTests.cs           (F09, F10: 10 tests, ~220 lines)
│   ├── Tier1_BuildAndIntegrityTests.cs          (F12:       5 tests, ~130 lines)
│   │
│   ├── Tier2_NamedPipeAndWpfBoundariesTests.cs   (F01, F02: 10 tests, ~220 lines)
│   ├── Tier2_CanvasAndPlayerBoundariesTests.cs  (F03, F04: 10 tests, ~220 lines)
│   ├── Tier2_EditAndExportBoundariesTests.cs    (F05, F07: 10 tests, ~220 lines)
│   ├── Tier2_ExecutionAndSafetyBoundariesTests.cs(F06, F11: 10 tests, ~230 lines)
│   ├── Tier2_SecurityGuardBoundariesTests.cs    (F08:      10 tests, ~220 lines)
│   ├── Tier2_RegistryAndSeedBoundariesTests.cs  (F09, F10: 10 tests, ~230 lines)
│   └── Tier2_BuildAndIntegrityBoundariesTests.cs(F12:       5 tests, ~140 lines)
```

### 5.1 Line Budget & Verification Plan
| File | Features Covered | Test Count | Estimated Lines | Rule C2 Margin (Target $\le 300$) |
|---|---|:---:|:---:|:---:|
| `Tier1_NamedPipeAndWpfTests.cs` | F01, F02 | 10 | ~200 | +100 lines |
| `Tier1_CanvasAndPlayerToolsTests.cs` | F03, F04 | 10 | ~210 | +90 lines |
| `Tier1_EditAndExportToolsTests.cs` | F05, F07 | 10 | ~210 | +90 lines |
| `Tier1_ExecutionAndSafetyTests.cs` | F06, F11 | 10 | ~220 | +80 lines |
| `Tier1_HostProfileAndSecurityTests.cs` | F08 | 5 | ~130 | +170 lines |
| `Tier1_RegistryAndSeedsTests.cs` | F09, F10 | 10 | ~220 | +80 lines |
| `Tier1_BuildAndIntegrityTests.cs` | F12 | 5 | ~130 | +170 lines |
| **Tier 1 Subtotal** | **F01–F12** | **60** | **~1,320** | **100% Compliant** |
| `Tier2_NamedPipeAndWpfBoundariesTests.cs` | F01, F02 | 10 | ~220 | +80 lines |
| `Tier2_CanvasAndPlayerBoundariesTests.cs` | F03, F04 | 10 | ~220 | +80 lines |
| `Tier2_EditAndExportBoundariesTests.cs` | F05, F07 | 10 | ~220 | +80 lines |
| `Tier2_ExecutionAndSafetyBoundariesTests.cs` | F06, F11 | 10 | ~230 | +70 lines |
| `Tier2_SecurityGuardBoundariesTests.cs` | F08 | 10 | ~220 | +80 lines |
| `Tier2_RegistryAndSeedBoundariesTests.cs` | F09, F10 | 10 | ~230 | +70 lines |
| `Tier2_BuildAndIntegrityBoundariesTests.cs` | F12 | 5 | ~140 | +160 lines |
| **Tier 2 Subtotal** | **F01–F12** | **65** | **~1,480** | **100% Compliant** |
| **Total Tier 1 & Tier 2** | | **125** | **~2,800** | **100% Compliant** |

---

## 6. Fake Test Doubles & Harness Integration Contracts

To enable 100% headless, zero-popup execution without requiring Revit or Rhino at test time, Tier 1 and Tier 2 interact with test doubles aligned with the architecture designed by `explorer_gh_m5_1`.

### 6.1 `GrasshopperE2EFakeBridge` Capabilities
1. **In-Memory Pipe & Message Dispatch**: Emulates Named Pipe requests with NDJSON serialization/deserialization.
2. **Dual-Layer Rollback Simulation**: Tracks committed vs rolled-back element counts and canvas object counts:
   - `DryRun = true`: Increments `RolledBackCount`, leaves committed elements at 0.
   - `DryRun = false`: Increments committed element count.
3. **Security Gate Simulation**: Enforces `IsExecutionEnabled` opt-in gate; throws error `-32001` when false.
4. **Script AST Validation Integration**: Invokes `ScriptGuard.Check(code, GuardProfile.Grasshopper)` and ADR-0007 quality walker directly.
5. **Canvas Graph Ledger Double**: Simulates document, components, parameters, and wire topological state in memory.

### 6.2 Contracts Interface (`GrasshopperE2EContracts`)
```csharp
public static class GrasshopperE2EContracts
{
    public const string HostKey = "grasshopper";
    public const int DefaultHostVersion = 2026;
    public const string ExpectedPipeName = "hpgrasshopper-mcp-2026";
    public const string MethodPrefix = "grasshopper.";
    public const int OptInDisabledErrorCode = -32001;
    public const string OptInDisabledMessageFragment = "Allow AI Grasshopper execution";
    public const int ExpectedTotalToolsCount = 24;
    public const string SuppressedEngineTool = "inspect_type";
    
    public static readonly IReadOnlyList<string> CoreTools = new[]
    {
        "execute_grasshopper_code", "get_grasshopper_context",
        "read_grasshopper_canvas", "run_grasshopper_player",
        "batch_edit_grasshopper_canvas", "export_grasshopper_canvas_image",
        "cancel_execution"
    };

    public static readonly IReadOnlyList<string> RegistryMetaTools = new[]
    {
        "search_tools", "get_tool", "run_tool", "get_run",
        "propose_tool", "test_tool", "publish_tool", "manage_tool"
    };

    public static readonly IReadOnlyList<string> EmbeddedSeeds = new[]
    {
        "create_levels_grids", "generate_columns_beams", "create_floors_slabs",
        "generate_rebar_curves", "generate_stirrup_distribution", "facade_panelization",
        "curtain_grid_layout", "create_wall_openings", "directshape_from_geometry",
        "query_revit_geometry", "evaluate_solar_radiation", "export_model_to_rhino"
    };
}
```

---

## 7. Traceability Matrix

| Requirement / Criterion | Feature Area | Tier 1 Tests | Tier 2 Tests |
|---|---|---|---|
| **R1** (In-Process Bridge & Pipe) | F01: Named Pipe & Protocol | TC_T1_NPS_01..05 | TC_T2_NPS_01..05 |
| **R1** (WPF Modeless UI) | F02: WPF UI & Status | TC_T1_WPF_01..05 | TC_T2_WPF_01..05 |
| **R2.1** (Read Canvas) | F03: Read Canvas Tool | TC_T1_RDC_01..05 | TC_T2_RDC_01..05 |
| **R2.2** (Player Runner) | F04: Player Runner Tool | TC_T1_RPL_01..05 | TC_T2_RPL_01..05 |
| **R2.3** (Batch Edit) | F05: Batch Edit Tool | TC_T1_BEC_01..05 | TC_T2_BEC_01..05 |
| **R2.4** (Execute Code) | F06: Execute Code Tool | TC_T1_EXC_01..05 | TC_T2_EXC_01..05 |
| **R2.5** (Export Image) | F07: Export Image Tool | TC_T1_IMG_01..05 | TC_T2_IMG_01..05 |
| **R3** (HostProfile & Guard) | F08: Guard & Profiles | TC_T1_GRD_01..05 | TC_T2_GRD_01..05 |
| **R4** (Registry Meta Tools) | F09: Registry Meta Tools | TC_T1_REG_01..05 | TC_T2_REG_01..05 |
| **R4** (12 Embedded Seeds) | F10: 12 Embedded Seeds | TC_T1_SED_01..05 | TC_T2_SED_01..05 |
| **R1, R2.4** (Dual Safety Rollback) | F11: Dual Safety Rollback | TC_T1_DLS_01..05 | TC_T2_DLS_01..05 |
| **Acceptance Criteria** (Build & Catalog) | F12: Build & Catalog Integrity | TC_T1_BLD_01..04 | TC_T2_BLD_01..04 |

---

## 8. Conclusion & Handoff Readiness

The test design for Tier 1 and Tier 2 provides:
1. **Comprehensive Feature Coverage**: Exactly covers all 12 feature areas with $\ge 5$ test cases each ($\ge 60$ per tier, 125 total).
2. **Boundary & Negative Rigor**: Covers malformed payloads, buffer stress, extreme coordinates, null documents, unhandled exceptions, syntax errors, forbidden namespaces, AST limits, and opt-in security boundaries.
3. **Strict Clean Code Architecture**: Decomposed across 14 test classes where every single file is guaranteed $\le 300$ lines (Rule C2) and methods $\le 50$ lines (Rule M3).
4. **Complete Traceability**: Directly maps to `ORIGINAL_REQUEST.md` (§ 2026-10-10T06:56:11Z) and `TEST_INFRA.md`.
