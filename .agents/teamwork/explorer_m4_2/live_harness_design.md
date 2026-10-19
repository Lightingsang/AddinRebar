# HPGrasshopper Automated Live Verification Harness Design
**Target File**: `HPGrasshopper/tools/harness/run-live-verify.ps1` (accompanied by `live-verify.py` and `mock_bridge.py`)  
**Deliverable**: Milestone M4 (Requirement R5 & Acceptance Criteria)  
**Author**: explorer_m4_2 (Automated Live Harness Explorer)  
**Date**: 2026-10-10  

---

## 1. Executive Summary & Objective

The primary objective of the HPGrasshopper Automated Live Verification Harness is to provide an end-to-end, unattended test harness for the `HPGrasshopper` Model Context Protocol (MCP) subsystem. The harness verifies:
1. **Stdio Server Lifecycle**: Clean startup, JSON-RPC 2.0 framing, MCP protocol handshake, and graceful shutdown of `HPGrasshopper.Mcp.Server.exe`.
2. **Named Pipe Communication**: Robust inter-process communication over `\\.\pipe\hpgrasshopper-mcp-2026` connecting the stdio server to `HPGrasshopper.McpBridge` inside Revit 2026 / Rhino.Inside.Revit.
3. **Complete 24-Tool Catalog**:
   - **5 Core Tools**: `read_grasshopper_canvas`, `run_grasshopper_player`, `batch_edit_grasshopper_canvas`, `execute_grasshopper_code`, `export_grasshopper_canvas_image` (along with `get_grasshopper_context` and `cancel_execution`).
   - **8 Dynamic Registry Meta Tools**: `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool` (covering candidate proposal, ADR-0007 quality walker, approval, retirement, and execution).
   - **12 Embedded Seed Tools**: Covering Generic, Geometry, and Revit categories with 100% parameter validation and `dryRun=true` non-destructive execution.
4. **Dual-Layer Safety Verification**: Proving that `dryRun=true` executes mutations and rolls back cleanly both at the outer Revit `TransactionGroup` level and the inner Grasshopper canvas `GH_UndoRecord` level without leaving orphan elements.
5. **Multi-Environment Execution Flexibility**: Supporting full Live Mode (interactive Revit 2026 + Rhino.Inside session), Headless/Mock Mode (isolated in-memory pipe double for automated CI/CD without requiring GUI Revit), and Detached Mode (validating graceful degradation and actionable error messages when the bridge is offline).

---

## 2. Architecture & Execution Flow

### 2.1 Ecosystem Overview

Following the proven architecture from sibling harnesses (`HPAutoCad`, `HPNavis`, `HPEtabs`, `HPTekla`), the HPGrasshopper harness is structured as a dual-layer test runner:

```
+------------------------------------------------------------------------------------------------+
|                                    RUN-LIVE-VERIFY.PS1                                         |
|  - Process launcher & environment controller (PowerShell 7.x / 5.1 Desktop)                    |
|  - Manages build artifacts (HPGrasshopper.Mcp.Server.exe, HPGrasshopper.McpBridge.dll)        |
|  - Detects host state (Revit.exe, Rhino.Inside) & Named Pipe (\\.\pipe\hpgrasshopper-mcp-2026) |
|  - Manages isolated Registry Root ($OutDir\registry) to protect %APPDATA%                      |
|  - Conditionally starts mock_bridge.py when -Mock is enabled                                   |
|  - Collects exit codes, logs, and outputs final JSON summary                                   |
+------------------------------------------------------------------------------------------------+
                                              |
                                              v Spawns via Stdio
+------------------------------------------------------------------------------------------------+
|                                      LIVE-VERIFY.PY                                            |
|  - Scenario test driver using McpShared/tools/harness_common.py (Checklist, Server, ok)        |
|  - Spawns HPGrasshopper.Mcp.Server.exe as a child process via Stdio pipes                      |
|  - Executes 7 Sequential Stages:                                                               |
|      Stage A: Handshake & 24 Tools Catalog Surface Check                                       |
|      Stage B: Context Snapshot, Resources & Prompts                                            |
|      Stage C: 5 Core Tools Live / Dry-Run Invocations                                          |
|      Stage D: Dynamic Registry Meta-Tools Lifecycle (MISS -> Propose -> Publish -> HIT)        |
|      Stage E: 12 Embedded Seed Tools Verification (dryRun=true)                                |
|      Stage F: Dual-Layer Safety, Guardrails & Cancellation                                     |
|      Stage G: Error Handling, Resilience & Edge Cases                                          |
+------------------------------------------------------------------------------------------------+
                 |                                                              |
                 v Stdio (JSON-RPC)                                             v Stdio (JSON-RPC)
+----------------------------------+                          +----------------------------------+
|    HPGrasshopper.Mcp.Server      |                          |    HPGrasshopper.Mcp.Server      |
+----------------------------------+                          +----------------------------------+
                 |                                                              |
                 v Named Pipe: hpgrasshopper-mcp-2026                           v Named Pipe: hpgrasshopper-mcp-2026
+----------------------------------+                          +----------------------------------+
|    [LIVE MODE]                   |                          |    [HEADLESS / MOCK MODE]        |
|    HPGrasshopper.McpBridge       |   -- OR --               |    mock_bridge.py                |
|    (Inside Revit.exe + Rhino 8)  |                          |    (In-memory Named Pipe server) |
+----------------------------------+                          +----------------------------------+
```

### 2.2 Execution Modes

The harness dynamically supports three operational modes determined by command-line parameters and environment detection:

| Mode | Trigger Condition | Host Requirement | Scope of Verification |
|---|---|---|---|
| **Live Mode** | Named Pipe `hpgrasshopper-mcp-2026` is detected online, or `-Live` switch passed | Revit 2026 with Rhino.Inside.Revit & Grasshopper running | Full live canvas reading, headless definition solving via Player, Roslyn C# execution against live Revit & Rhino documents, real dual-layer rollback. |
| **Mock Mode** | `-Mock` switch passed, or pipe missing and auto-mock enabled | No Revit or Rhino required (runs anywhere, including GitHub Actions / Linux CI with Python) | Full stdio protocol handshake, 24 tools surface, complete registry lifecycle, mock tool dispatch, parameter serialization, and error mapping. |
| **Detached Mode** | Pipe offline and neither `-Live` nor `-Mock` specified | Server only | Verifies that server advertises 24 tools, registry functions offline, and bridge-dependent tool calls fail fast with actionable, informative error messages referencing `hpgrasshopper-mcp-2026`. |

### 2.3 Detailed Step-by-Step Execution Sequence

1. **Pre-Flight Validation**:
   - Resolve repository paths and verify or build `HPGrasshopper.Mcp.Server.exe`.
   - Setup isolated registry directory at `$OutDir\registry` with overrides:
     `$env:HPGRASSHOPPER_MCP_Registry__LibraryPath = "$OutDir\registry\tools-library"`
     `$env:HPGRASSHOPPER_MCP_Registry__DbPath = "$OutDir\registry\registry.db"`
   - Check if Named Pipe `\\.\pipe\hpgrasshopper-mcp-2026` is available via `[System.IO.Directory]::GetFiles("\\.\pipe\")`.
2. **Mock Bridge Startup (If in Mock Mode)**:
   - If `-Mock` is requested, launch `mock_bridge.py` in a background daemon thread/process listening on `\\.\pipe\hpgrasshopper-mcp-2026`.
3. **Stdio Server Process Initialization**:
   - `live-verify.py` spawns `HPGrasshopper.Mcp.Server.exe` with redirected `stdin`, `stdout`, `stderr`.
   - Dedicated reader thread drains `stderr` to prevent pipe deadlock.
4. **JSON-RPC Protocol Handshake**:
   - Client sends `initialize` request with client info `{"name": "harness", "version": "1.0"}`.
   - Server returns protocol version, server info `{"name": "HPGrasshopper MCP", ...}`, and capabilities.
   - Client sends notification `notifications/initialized`.
5. **Execution of Stages A Through G**:
   - Sequential execution of all test assertions using `Checklist.check(name, condition, detail)`.
6. **Graceful Teardown & Reporting**:
   - Close stdio session (`stdin.close()`, wait up to 10 seconds, fallback kill).
   - Stop mock bridge if started.
   - Export structured JSON summary `run-summary.json` and append output to `live-verify.log`.
   - Print human-readable summary table with total counts (PASS/FAIL/SKIP).
   - Exit with standard exit codes (0 = Success, 1 = Verification Failed).

---

## 3. Comprehensive Testing of All 24 Tools

The HPGrasshopper subsystem exposes 24 total tools (5 Core + 8 Registry Meta + 11-12 Embedded Seeds). In addition, `get_grasshopper_context` and `cancel_execution` are core utilities, while engine tool `inspect_type` is suppressed.

### 3.1 The 5 Core Tools

Each core tool has explicit input schemas, annotations, and execution requirements tested by the harness:

| Tool Name | Annotations | Required Parameters | Optional Parameters | Verification Action in Harness |
|---|---|---|---|---|
| `read_grasshopper_canvas` | ReadOnly: `true`<br>Destructive: `false` | None | `includeValues` (bool)<br>`selectedOnly` (bool)<br>`categoryFilter` (string) | Query active canvas. Assert response contains JSON object with `components` (array), `wires` (array), `solverStatus` (string), and `componentCount` (int). |
| `run_grasshopper_player` | ReadOnly: `false`<br>Destructive: `true` | `filePath` (string) | `inputs` (object)<br>`outputs` (string[])<br>`headless` (bool) | Execute sample definition `math_slider_add.ghx` with `inputs: {"A": 10, "B": 25}`, `outputs: ["Result"]`, `headless: true`. Assert returned output equals `{"Result": 35}`. |
| `batch_edit_grasshopper_canvas` | ReadOnly: `false`<br>Destructive: `true` | None | `componentsToAdd` (array)<br>`componentsToRemove` (string[])<br>`wiresToConnect` (array)<br>`wiresToDisconnect` (array)<br>`dryRun` (bool) | Call with `dryRun: true` adding component `{"type": "NumberSlider", "x": 100, "y": 200, "nickName": "TestVal", "tempId": "slider1"}`. Assert success and verify `rolledBack: true` without canvas modification. |
| `execute_grasshopper_code` | ReadOnly: `false`<br>Destructive: `true` | `code` (string) | `transaction` (string, default "auto")<br>`dryRun` (bool)<br>`timeoutSeconds` (int)<br>`label` (string)<br>`args` (object) | 1. Test read-only script: `return doc.Objects.Count;` with `transaction: "none"`.<br>2. Test dual-layer write with `dryRun: true`: create a point in Rhino and a reference plane in Revit; assert `rolledBack: true`. |
| `export_grasshopper_canvas_image` | ReadOnly: `true`<br>Destructive: `false` | None | `mode` ("FullCanvas"\|"Viewport"\|"Selection")<br>`zoom` (float)<br>`outputPath` (string)<br>`returnBase64` (bool) | Call with `mode: "FullCanvas"`, `zoom: 1.0`, `returnBase64: true`. Assert response contains base64-encoded image string with PNG header (`iVBORw0KGgo...`). |

*Auxiliary Core Tools:*
- `get_grasshopper_context`: Call with `includeSelection: true`. Validate host is `"grasshopper"`, contains `rhinoInsideRevit: true`, `docTitle`, and zero foreign host fields (`autocad`, `etabs`).
- `cancel_execution`: Call during a long-running work request or test correlation token.

### 3.2 The 8 Dynamic Registry Meta Tools

The host-neutral MCP engine `HPRebar.Mcp.Server.Core` manages tool discovery, sandboxed proposing, testing, and lifecycle management through 8 standardized meta-tools:

```
[search_tools] ---> [propose_tool] ---> [test_tool] ---> [publish_tool]
      |                                                        |
      v                                                        v
 [get_tool] <--------------------------------------------- [run_tool]
      |                                                        |
      v                                                        v
[manage_tool] (retire / quarantine / restore) <----------- [get_run]
```

The harness tests this complete 8-step lifecycle:
1. `search_tools`:
   - Search with query `"rebar"`, verify seeds `generate_rebar_curves` and `generate_stirrup_distribution` are found.
   - List available categories (`Canvas`, `Component`, `Parameter`, `Geometry`, `Revit`, `Python`, `Player`, `Generic`).
2. `propose_tool`:
   - Submit candidate `grasshopper_sine_curve` with valid metadata, input schema, and C# script.
   - Assert ADR-0007 script quality walker runs: checks for absence of commented-out code, no empty catches, max 300 lines.
3. `test_tool`:
   - Run candidate tool in dry-run mode with sample input parameters `{"amplitude": 100.0, "frequency": 2.0}`.
   - Assert execution succeeds and execution time / test output is returned.
4. `publish_tool`:
   - Approve and publish tool to registry.
   - Verify tool status changes to Published in SQLite database.
5. `get_tool`:
   - Retrieve published tool details. Assert source code, manifest, documentation, and schema match proposal.
   - Fulfills the `get_tool_source` and `view_tool_docs` operations.
6. `run_tool`:
   - Invoke published tool by name with parameters. Assert result formatting and execution completion.
7. `get_run`:
   - Retrieve execution audit log entry by run ID. Verify timestamps, arguments, and outcome.
8. `manage_tool`:
   - Test `action: "retire"`. Verify tool is hidden from default search results.
   - Test `action: "restore"`. Verify tool is re-activated.

### 3.3 The 12 Embedded Seed Tools

The harness asserts that all 12 embedded seeds are present in the catalog and executes them with test vectors in `dryRun=true` mode:

| Seed Tool Name | Category | Primary Function | Test Vector Arguments |
|---|---|---|---|
| `create_levels_grids` | Revit | Parametric creation of Revit levels and grids from GH coordinates | `{"levels": [{"name": "L1", "elevation": 0.0}, {"name": "L2", "elevation": 3600.0}], "dryRun": true}` |
| `generate_columns_beams` | Revit | Generates column and beam framings between parametric points | `{"columns": [{"x": 0, "y": 0, "baseLevel": "L1", "topLevel": "L2"}], "dryRun": true}` |
| `create_floors_slabs` | Revit | Creates horizontal floor boundaries and slabs from boundary curves | `{"boundaryPoints": [[0,0,0], [5000,0,0], [5000,5000,0], [0,5000,0]], "levelName": "L1", "dryRun": true}` |
| `generate_rebar_curves` | Revit | Computes 3D structural concrete reinforcement curves | `{"beamSpan": 6000.0, "width": 300.0, "height": 500.0, "cover": 30.0, "dryRun": true}` |
| `generate_stirrup_distribution` | Revit | Calculates shear stirrup spacing along beam spans (dense/sparse) | `{"clearSpan": 5400.0, "supportSpacing": 100.0, "midSpanSpacing": 200.0, "dryRun": true}` |
| `facade_panelization` | Geometry | Subdivides double-curved surface into planar/curved facade panels | `{"surfaceUCount": 10, "surfaceVCount": 10, "panelType": "Diamond", "dryRun": true}` |
| `curtain_grid_layout` | Geometry | Parametric layout of curtain wall grids and mullion positions | `{"width": 12000.0, "height": 4000.0, "verticalSpacing": 1500.0, "dryRun": true}` |
| `create_wall_openings` | Revit | Computes rectangular and curved opening boundaries on host walls | `{"wallId": 12345, "openings": [{"x": 1000, "z": 900, "width": 1200, "height": 1500}], "dryRun": true}` |
| `directshape_from_geometry` | Geometry | Generates Revit DirectShape elements from Rhino Breps/Meshes | `{"category": "GenericModel", "geometryData": "<brep_wire_data>", "dryRun": true}` |
| `query_revit_geometry` | Geometry | Extracts Revit element solids/curves into Grasshopper data types | `{"categoryFilter": "OST_StructuralColumns", "detailLevel": "Fine"}` |
| `evaluate_solar_radiation` | Geometry | Computes solar ray vectors and incident radiation values | `{"latitude": 21.0285, "longitude": 105.8542, "surfacePoints": [[0,0,0]], "dryRun": true}` |
| `export_model_to_rhino` | Generic | Serializes selected Revit BIM geometry into Rhino 3DM document | `{"elementIds": [12345], "exportFormat": "Rhino7_8", "dryRun": true}` |

---

## 4. Headless & Mock Bridge Subsystem

In modern CI/CD pipelines, Autodesk Revit and Rhino.Inside.Revit cannot run interactive GUI sessions. To guarantee 100% automated test coverage without blocking on host GUI availability, the harness includes a lightweight Python mock bridge `mock_bridge.py`.

### 4.1 Mock Bridge Design

`mock_bridge.py` acts as a protocol-compliant test double:
- **Named Pipe Server**: Creates `\\.\pipe\hpgrasshopper-mcp-2026` using Windows Named Pipe APIs (`_winapi.CreateNamedPipe` or `win32pipe`).
- **Framing**: Reads and writes newline-delimited JSON (NDJSON) framing matching `NdjsonPipeWriter`.
- **Request Dispatcher**:
  - `ping` -> Returns `{"pong": true, "revitVersion": "2026", "executionEnabled": true}`.
  - `grasshopper.context` -> Returns synthetic `ContextResult.Grasshopper` with `isConnected: true`, `componentCount: 15`, `rhinoInsideRevit: true`.
  - `grasshopper.execute` -> Inspects request `Label`:
    - `read_grasshopper_canvas`: Returns mock components (`NumberSlider`, `Addition`, `Panel`) and wire connections.
    - `run_grasshopper_player`: Solves mock formula and returns evaluated parameters.
    - `batch_edit_grasshopper_canvas`: Simulates edits, returning `{"success": true, "rolledBack": true}` when `dryRun=true`.
    - `export_grasshopper_canvas_image`: Returns a valid 1x1 transparent PNG base64 string.
    - Roslyn execution: Returns `{"success": true, "value": "mock_executed", "rolledBack": <dryRun>}`.
  - `grasshopper.cancel` -> Returns `{"cancelled": true}`.

---

## 5. Script Structure & Specifications

### 5.1 PowerShell Runner Specification: `run-live-verify.ps1`

```powershell
<#
.SYNOPSIS
    Automated Live Verification Harness for HPGrasshopper MCP Subsystem.
.DESCRIPTION
    Drives HPGrasshopper.Mcp.Server.exe over stdio against HPGrasshopper.McpBridge
    (or mock_bridge.py in headless mode) to verify all 24 tools, protocol handshake,
    registry lifecycle, dual-layer rollback, and error handling.
.PARAMETER Exe
    Explicit path to HPGrasshopper.Mcp.Server.exe. Defaults to bin/Debug or Release.
.PARAMETER Stages
    Comma-separated list of stages to run (A,B,C,D,E,F,G). Defaults to 'A,B,C,D,E,F,G'.
.PARAMETER Mock
    Runs against in-memory mock bridge (for headless CI without Revit/Rhino).
.PARAMETER Out
    Directory for logs and JSON summaries. Defaults to HPGrasshopper/output/live-verify.
.PARAMETER SkipBuild
    Skips dotnet build of HPGrasshopper.slnx.
.PARAMETER TimeoutSec
    Overall execution timeout in seconds.
.PARAMETER Json
    Outputs machine-readable JSON only.
.EXAMPLE
    pwsh -File tools/harness/run-live-verify.ps1 -Stages A,B,C,D,E,F,G -Mock
#>
[CmdletBinding()]
param(
    [string]$Exe = '',
    [ValidatePattern('^[A-Ga-g,\s]+$')]
    [string]$Stages = 'A,B,C,D,E,F,G',
    [switch]$Mock,
    [string]$Out = '',
    [switch]$SkipBuild,
    [int]$TimeoutSec = 300,
    [switch]$Json
)
```

#### Detailed Execution Logic of `run-live-verify.ps1`:
1. **Edition Check**: Re-launches in `powershell.exe` if running in PowerShell Core under conditions requiring Windows UIA.
2. **Path Resolution**: Sets `$repoRoot` and `$harnessDir = "$repoRoot/HPGrasshopper/tools/harness"`.
3. **Artifact Discovery & Build**:
   - Locates `$candidateDebug = "$repoRoot/HPGrasshopper/HPGrasshopper.Mcp.Server/bin/Debug/net10.0/HPGrasshopper.Mcp.Server.exe"`.
   - If missing and `-SkipBuild` is not set, executes `dotnet build HPGrasshopper.slnx -c Debug`.
4. **Isolated Registry Configuration**:
   - Generates `$registryDir = "$OutDir/registry"`.
   - Cleans and recreates `$registryDir/tools-library` and `$registryDir/registry.db`.
   - Sets `$env:HPGRASSHOPPER_MCP_Registry__LibraryPath` and `$env:HPGRASSHOPPER_MCP_Registry__DbPath`.
5. **Host & Pipe Verification**:
   - Checks `[System.IO.Directory]::GetFiles("\\.\pipe\", "hpgrasshopper-mcp-2026")`.
   - If pipe is absent:
     - If `-Mock` is set: Spawns `python mock_bridge.py --pipe hpgrasshopper-mcp-2026` in background.
     - Else: Emits warning and sets mode to `Detached`.
6. **Execution of Python Test Harness**:
   - Executes `python live-verify.py --exe "$Exe" --stages "$Stages" --out "$OutDir" $(if ($Mock) { "--mock" })`.
7. **Cleanup & Exit Code Processing**:
   - Kills background mock bridge process if spawned.
   - Evaluates `$exitCode = $LASTEXITCODE`.
   - Reads `$OutDir/run-summary.json` and prints console table.
   - Exits with `$exitCode`.

### 5.2 Python Scenario Driver: `live-verify.py`

#### Architecture & Modules
- Utilizes `McpShared/tools/harness_common.py` directly:
  ```python
  import os, sys, argparse, json, time
  HERE = os.path.dirname(os.path.abspath(__file__))
  sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "McpShared", "tools"))
  from harness_common import Checklist, Server, ok, short, utf8_console
  ```
- Modular Stage Handlers:
  - `stage_a_handshake(s, cl)`: Verifies protocol handshake and all 24 tools in `tools/list`.
  - `stage_b_context_and_resources(s, cl)`: Verifies `get_grasshopper_context`, `resources/list`, `prompts/list`.
  - `stage_c_core_tools(s, cl)`: Verifies the 5 core tools with dryRun and live execution.
  - `stage_d_registry_lifecycle(s, cl)`: Executes MISS -> propose -> test -> publish -> HIT -> retire loop.
  - `stage_e_seed_library(s, cl)`: Iterates and executes all 12 embedded seed tools with `dryRun=true`.
  - `stage_f_dual_safety(s, cl)`: Validates Revit TransactionGroup and GH_UndoRecord rollback behavior.
  - `stage_g_error_handling(s, cl)`: Tests invalid args, ScriptGuard rejections, and timeouts.

### 5.3 Test Fixtures Specification

To support deterministic automated testing of `run_grasshopper_player`, the harness includes sample definitions in `HPGrasshopper/tools/harness/fixtures/`:

1. `math_slider_add.ghx` (XML format):
   - Definition containing two number sliders `A` (value 10) and `B` (value 20).
   - Component: `Grasshopper.Kernel.Components.Component_Addition`.
   - Output: `Grasshopper.Kernel.Parameters.Param_GenericObject` named `Result`.
   - Used to verify JSON parameter binding and evaluation in player mode.
2. `curve_generator.ghx`:
   - Generates a 3D sine curve from input parameters `Count`, `Step`, `Amplitude`.
   - Used to verify geometry extraction and volatile data queries.
3. `revit_sync_mock.ghx`:
   - Contains components referencing Revit parameters (Level, Grid).
   - Used to test Rhino.Inside.Revit contextual parameter binding.

---

## 6. Error Handling, Timeouts & Reporting Format

### 6.1 Exit Codes Definition

| Exit Code | Classification | Cause & Remediation |
|---|---|---|
| `0` | **PASS (Success)** | All requested stages passed 100% with 0 failed assertions. |
| `1` | **FAIL (Verification Failure)** | One or more assertions in `live-verify.py` failed (recorded in `run-summary.json`). |
| `2` | **CONFIGURATION / BUILD ERROR** | `HPGrasshopper.Mcp.Server.exe` not found, build failed, or Python environment missing dependencies. |
| `3` | **TIMEOUT / PIPE FAULT** | Named Pipe failed to initialize within timeout period or server hung during execution. |

### 6.2 JSON Reporting Schema: `run-summary.json`

Every execution produces a structured JSON report saved to `$OutDir/run-summary.json`:

```json
{
  "startedAt": "2026-10-10T09:45:00Z",
  "completedAt": "2026-10-10T09:46:12Z",
  "durationSeconds": 72,
  "exe": "G:\\...\\HPGrasshopper.Mcp.Server.exe",
  "mode": "Live",
  "stages": ["A", "B", "C", "D", "E", "F", "G"],
  "summary": {
    "passed": 48,
    "failed": 0,
    "skipped": 2,
    "total": 50,
    "failedNames": [],
    "skippedNames": ["F4 cancellation live test"]
  },
  "results": [
    { "name": "A1 Server advertises exactly 24 tools total", "pass": true, "detail": "Tools count = 24" },
    { "name": "A2 All 5 core tools present in catalog", "pass": true, "detail": "Core: read_grasshopper_canvas, run_grasshopper_player, ..." },
    { "name": "A3 All 8 registry meta tools present", "pass": true, "detail": "Meta: search_tools, get_tool, ..." },
    { "name": "A4 All 12 embedded seed tools present", "pass": true, "detail": "Seeds: create_levels_grids, generate_columns_beams, ..." },
    { "name": "B1 Context tool returns host 'grasshopper'", "pass": true, "detail": "Host: grasshopper, rhinoInside: true" },
    { "name": "C1 read_grasshopper_canvas executes successfully", "pass": true, "detail": "Components: 15, Wires: 22" },
    { "name": "C2 export_grasshopper_canvas_image returns valid PNG", "pass": true, "detail": "Base64 length: 48920 bytes" },
    { "name": "C3 batch_edit_grasshopper_canvas dryRun rolls back", "pass": true, "detail": "rolledBack: true" },
    { "name": "C4 run_grasshopper_player evaluates math definition", "pass": true, "detail": "Result: 35" },
    { "name": "C5 execute_grasshopper_code dual-layer rollback", "pass": true, "detail": "Revit Tx rolled back, GH_UndoRecord executed" },
    { "name": "D1 search_tools finds seeds by category", "pass": true, "detail": "Found 6 tools in Revit category" },
    { "name": "D2 propose_tool passes ADR-0007 quality walker", "pass": true, "detail": "0 quality errors" },
    { "name": "D3 test_tool executes candidate in sandbox", "pass": true, "detail": "Executed in 28ms" },
    { "name": "D4 publish_tool adds tool to registry", "pass": true, "detail": "Status: Published" },
    { "name": "E1-E12 All embedded seeds dryRun successfully", "pass": true, "detail": "12/12 seeds verified" },
    { "name": "F1 ScriptGuard blocks System.IO access", "pass": true, "detail": "Denied namespace: System.IO" },
    { "name": "G1 Server handles invalid JSON gracefully", "pass": true, "detail": "RPC error -32700" }
  ]
}
```

---

## 7. Concrete Next Steps for Implementation (Milestone M4)

When Milestone M4 implementation commences, the developer should follow this exact sequence:
1. **Scaffold Directory**: Create `HPGrasshopper/tools/harness/` and `HPGrasshopper/tools/harness/fixtures/`.
2. **Create Test Fixtures**: Populate `math_slider_add.ghx` with valid Grasshopper XML.
3. **Implement Mock Bridge**: Write `mock_bridge.py` supporting Named Pipe `hpgrasshopper-mcp-2026`.
4. **Implement Python Scenario Driver**: Write `live-verify.py` implementing Stages A–G with `harness_common.py`.
5. **Implement PowerShell Wrapper**: Write `run-live-verify.ps1` managing process lifecycle, isolated registry, and exit codes.
6. **Execute Verification**: Run `pwsh -File tools/harness/run-live-verify.ps1 -Mock` and verify 100% green test results.
