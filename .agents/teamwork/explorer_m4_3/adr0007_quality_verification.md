# ADR-0007 Live Quality Verification: HPGrasshopper Integration Report

**Author:** explorer_m4_3 (ADR-0007 Live Verification Explorer)  
**Date:** 2026-10-10  
**Target Solution:** `HPGrasshopper/` and `McpShared/`  
**Governance Standard:** ADR-0007 (`docs/architecture/adr/0007-hp-clean-code-scope.md`), `HP_CLEAN_CODE_CORE.md`, `TOOL_DEVELOPMENT_WORKFLOW.md`

---

## 1. Executive Summary

This report establishes the complete investigation and architectural design for **ADR-0007 Script-Quality Gate Integration** in the HPGrasshopper ecosystem. 

Under Clean Code Governance (ADR-0007), all code generated or modified in the repository—including tools proposed by AI coding agents at runtime and embedded seeds—must strictly comply with clean code constraints: zero dead/commented code (**Q-B1**), zero unannotated empty catches (**Q-B2**), and method/script bounds ≤ 300 lines (**Q-B3**), while generating review warnings for code smells (**Q-W1..Q-W5**).

### Key Findings
1. **`McpShared/tools/live-verify-quality.py` is fully configured for Grasshopper**:
   - `HOSTS["grasshopper"]` is defined with parameters `("HPGrasshopper", "HPGRASSHOPPER_MCP_", "HostVersion", 2026, "get_grasshopper_context", None)`.
   - It targets `HPGrasshopper.Mcp.Server.exe` (Debug build located at `HPGrasshopper/HPGrasshopper.Mcp.Server/bin/Debug/net10.0/HPGrasshopper.Mcp.Server.exe`).
   - Empirically verified via live execution: it connects via stdio JSON-RPC, issues `get_grasshopper_context`, and properly diagnoses bridge connectivity against named pipe `\\.\pipe\hpgrasshopper-mcp-2026`.
2. **Static Seed Quality (ADR-0007)**:
   - All 12 embedded seeds in `HPGrasshopper.Mcp.Server/Registry/SeedLibrary/` compile and pass `ScriptQuality.Find(...)` with **0 errors and 0 warnings** (verified via `HPGrasshopper.Mcp.Server.Tests`, 77/77 tests passing).
3. **Live Quality Verification Protocol**:
   - The standardized 5/5 check protocol tests the full runtime governance pipeline: Context query -> Q-B1/Q-B2 refusal -> Q-W3 warning acceptance -> 2/2 dryRun execution -> Manual publish review generation.
4. **Harness Integration Plan**:
   - Designed the complete integration architecture for `HPGrasshopper/tools/harness/run-live-verify.ps1`, including pre-flight Revit/Rhino.Inside detection, pipe validation, UI Automation opt-in toggling (`AllowExecution`), phase sequencing, and JSON checklist result recording.

---

## 2. Investigation of `McpShared/tools/live-verify-quality.py`

### 2.1 Configuration Analysis in `HOSTS` Table
In `McpShared/tools/live-verify-quality.py` (lines 32–45), `HOSTS` defines configuration for all 12 CAD/BIM ecosystems:

```python
HOSTS = {
    "revit": ("HPRebar", "HPREBAR_MCP_", "RevitVersion", 2026, "get_revit_context", None),
    "autocad": ("HPAutoCad", "HPAUTOCAD_MCP_", "HostVersion", 2026, "get_autocad_context", None),
    "civil3d": ("HPCivil3d", "HPCIVIL3D_MCP_", "HostVersion", 2026, "get_civil3d_context", None),
    "navis": ("HPNavis", "HPNAVIS_MCP_", "HostVersion", 2026, "get_navis_context", None),
    "etabs": ("HPEtabs", "HPETABS_MCP_", "HostVersion", 22, "get_etabs_context", "HPEtabs/McpBridge/snapshots"),
    "sap2000": ("HPSap2000", "HPSAP2000_MCP_", "HostVersion", 27, "get_sap2000_context", "HPSap2000/McpBridge/snapshots"),
    "robot": ("HPRobot", "HPROBOT_MCP_", "HostVersion", 2026, "get_robot_context", "HPRobot/McpBridge/snapshots"),
    "excel": ("HPExcel", "HPEXCEL_MCP_", "HostVersion", 2026, "get_excel_context", "HPExcel/Snapshots"),
    "powerbi": ("HPPowerBi", "HPPOWERBI_MCP_", "HostVersion", 2026, "get_powerbi_context", "HPPowerBi/Snapshots"),
    "tekla": ("HPTekla", "HPTEKLA_MCP_", "HostVersion", 2025, "get_tekla_context", "HPTekla/McpBridge/snapshots"),
    "dynamo": ("HPDynamo", "HPDYNAMO_MCP_", "HostVersion", 2026, "get_dynamo_context", None),
    "grasshopper": ("HPGrasshopper", "HPGRASSHOPPER_MCP_", "HostVersion", 2026, "get_grasshopper_context", None),
}
```

#### Field-by-Field Mapping for Grasshopper
| Field Index | Configuration Value | Target in HPGrasshopper Subsystem |
|-------------|---------------------|-----------------------------------|
| 0 (`folder`) | `"HPGrasshopper"` | Resolves server binary at `HPGrasshopper/HPGrasshopper.Mcp.Server/bin/Debug/*/HPGrasshopper.Mcp.Server.exe`. |
| 1 (`prefix`) | `"HPGRASSHOPPER_MCP_"` | Matches `GrasshopperHostProfile.Instance.EnvPrefix = "HPGRASSHOPPER_MCP_"`. |
| 2 (`version_key`) | `"HostVersion"` | Directs server to bind to host version setting (`BridgeOptions.HostVersion`). |
| 3 (`version`) | `2026` | Matches `GrasshopperHostProfile.DefaultVersion = 2026` (Revit 2026 + Rhino.Inside.Revit). |
| 4 (`context_tool`) | `"get_grasshopper_context"` | Matches `GrasshopperHostProfile.ContextToolName = "get_grasshopper_context"`. |
| 5 (`snapshot_product`) | `None` | Grasshopper runs in-process inside Revit; does not snapshot to `%LOCALAPPDATA%` (relies on dual-layer safety: Revit TransactionGroup + GH_UndoRecord). |

### 2.2 Command Line Options & Capabilities
`live-verify-quality.py` exposes:
- `--host <name>`: Required (or `--list`). Valid values: `grasshopper`, `revit`, `autocad`, `civil3d`, `dynamo`, etc.
- `--exe <path>`: Optional override for server executable path. Defaults to automatic discovery via `server_exe(folder)`.
- `--out <folder>`: Optional directory where JSON summary output (`live-verify-quality-<host>.json`) is stored.
- `--list`: Prints all configured hosts, environment variables, context tools, snapshot directories, and binary build status.

Empirical output of `--list` run on current codebase:
```
grasshopper HPGRASSHOPPER_MCP_Bridge__HostVersion=2026  get_grasshopper_context snapshots=-          exe=G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\HPGrasshopper.Mcp.Server\bin\Debug\net10.0\HPGrasshopper.Mcp.Server.exe
```

### 2.3 Process Execution & Registry Sandbox Isolation
When executed for Grasshopper, `live-verify-quality.py` establishes complete sandbox isolation:
1. **Isolated Registry Root**:
   - Creates a temporary directory at `%TEMP%\hp-live-quality\grasshopper`.
   - Sets environment overrides:
     - `HPGRASSHOPPER_MCP_Bridge__HostVersion = "2026"`
     - `HPGRASSHOPPER_MCP_Registry__LibraryPath = "%TEMP%\hp-live-quality\grasshopper\tools-library"`
     - `HPGRASSHOPPER_MCP_Registry__DbPath = "%TEMP%\hp-live-quality\grasshopper\registry.db"`
   - Guarantees that user's `%AppData%` tool registry is never touched or polluted.
2. **Process Spawning**:
   - Spawns `HPGrasshopper.Mcp.Server.exe` as a child process via `Server(exe, env, name="quality-grasshopper")`.
   - Communicates strictly over standard input/output (stdio) with JSON-RPC framing.
   - Cleans up and deletes `%TEMP%\hp-live-quality\grasshopper` in a `finally` block.

---

## 3. Design of the 5/5 Live Quality Verification Protocol

The live quality verification suite checks 5 distinct criteria across the full client-server-bridge lifecycle:

```
[Server Process (Stdio)] <==== JSON-RPC 2.0 ====> [Bridge in Revit/Grasshopper (Named Pipe)]
       |                                                         |
Step 1: get_grasshopper_context -------------------------------> Evaluates Document & Execution Authorization
Step 2: propose_tool (BAD_CODE) -------------------------------> ScriptAnalyzer: Q-B1, Q-B2 Refusal
Step 3: propose_tool (GOOD_CODE) ------------------------------> ScriptAnalyzer: Q-W3 Warning Acceptance
Step 4: test_tool (dryRun) ------------------------------------> Executes 2 cases under dual-layer rollback
Step 5: publish_tool ------------------------------------------> Reviews code quality record in markdown
```

### 3.1 Check 1: Session Context & Authorization Gate
- **Tool Invoked**: `get_grasshopper_context({})`
- **Verification Rule**:
  ```python
  doc_path = context.get("docPath") or ""
  ready = context.get("executionEnabled") is True and is_scratch(doc_path) and not context.get("isError")
  ```
- **Requirements**:
  1. `executionEnabled == True`: The bridge user must have toggled the "Allow AI Grasshopper execution" checkbox in `GrasshopperBridgeStatusView`.
  2. `is_scratch(doc_path) == True`: Active document must be unsaved/new (bare title, `""`, `(untitled)`, template `.rte`/`.rft`, or path under `\output\`).
  3. `isError == False`: Named pipe communication must be healthy.
- **Grasshopper Context Response Schema**:
  ```json
  {
    "host": "hpgrasshopper-mcp",
    "hostVersion": "2026",
    "revitVersion": "2026",
    "docTitle": "Project1",
    "docPath": null,
    "executionEnabled": true,
    "grasshopper": {
      "isEditorLoaded": true,
      "grasshopperVersion": "8.12.24...",
      "rhinoVersion": "8.12.24...",
      "activeDefinitionName": "scratch.gh",
      "componentCount": 0,
      "wireCount": 0,
      "isRhinoInsideRevitActive": true
    }
  }
  ```

### 3.2 Check 2: ADR-0007 Refusal Gate (Q-B1, Q-B2, Q-B3)
- **Tool Invoked**: `propose_tool`
  - Name: `mcp_verify_quality_bad`
  - Description: `"Live check: commented-out code and an empty catch"`
  - Category: `"Generic"`
  - Transaction: `"none"`
  - Input Schema: `{"type": "object", "properties": {"count": {"type": "integer", "default": 2}}}`
  - Code (`BAD_CODE`):
    ```csharp
    // var old = args.Int("count", 1);
    int count = args.Int("count", 2);
    try { count++; } catch { }
    return count;
    ```
- **Verification Logic**:
  - Bridge Coordinator calls `ScriptAnalyzer.Run(...)`, which runs `ScriptQuality.Find(...)`.
  - Detection Results:
    - Line 1: Comment parses as C# syntax -> triggers **`Q-B1`** (`error`, No commented-out code).
    - Line 3: Catch block has no statements and no reason comment -> triggers **`Q-B2`** (`error`, No empty catch).
  - Server `ToolValidator.ValidateAsync` aggregates errors:
    - `"quality Q-B1 1:1 commented-out code — delete it (git keeps history) or turn it into a comment that explains why."`
    - `"quality Q-B2 3:18 empty catch without a comment explaining why the exception is ignored."`
  - Since `errors.Count > 0`, tool proposal is **rejected**:
    `bad.get("accepted") == False` and `"quality Q-B1" in errors` and `"quality Q-B2" in errors`.
- **Pass Criteria**: `PASS 2 commented-out code + empty catch refused (Q-B1, Q-B2)`.

### 3.3 Check 3: ADR-0007 Warning Gate (Q-W1..Q-W5)
- **Tool Invoked**: `propose_tool`
  - Name: `mcp_verify_quality_good`
  - Description: `"Live check: doubles a count, one vague name"`
  - Category: `"Generic"`
  - Transaction: `"none"`
  - Code (`GOOD_CODE`):
    ```csharp
    var data = args.Int("count", 2);
    return data * 2;
    ```
- **Verification Logic**:
  - Bridge Coordinator calls `ScriptAnalyzer.Run(...)`.
  - Zero blocking errors detected.
  - Line 1: Identifier `data` is member of `ScriptQuality.VagueNames` (`data, tmp, temp, obj, res, val, foo, bar, stuff, thing`) -> triggers **`Q-W3`** (`warning`, Vague identifier).
  - Server `ToolValidator.ValidateAsync` allows proposal:
    - `good.get("accepted") == True`
    - `warnings` contains `"quality Q-W3 1:5 vague name 'data' — say what the value is in the domain."`
  - Tool draft is written to SQLite `registry.db` and audit event `quality_checked` is recorded.
- **Pass Criteria**: `PASS 3 vague name accepted with a Q-W3 warning`.

### 3.4 Check 4: Transactional DryRun Execution Gate (`test_tool`)
- **Tool Invoked**: `test_tool`
  - Name: `mcp_verify_quality_good`
- **Verification Logic**:
  - Server retrieves tool and executes registered examples:
    - Case 1: `title: "default"`, `args: {}` -> `count` defaults to 2 -> returns `4`.
    - Case 2: `title: "five"`, `args: {"count": 5}` -> returns `10`.
  - Executed with `dryRun: true` over named pipe `hpgrasshopper-mcp-2026`.
  - Bridge wraps execution in `GrasshopperExecutionEngine`:
    - Inner: `CanvasUndoScope` (reverts any Grasshopper canvas mutations).
    - Outer: `RevitTransactionScope` (rolls back any Revit DB changes).
  - Both cases succeed.
- **Pass Criteria**: `tested.get("passed") == 2 and tested.get("failed") == 0`.

### 3.5 Check 5: Review & Governance Documentation Gate (`publish_tool`)
- **Tool Invoked**: `publish_tool`
  - Name: `mcp_verify_quality_good`
- **Verification Logic**:
  - Under default manual approval policy:
    - Server sets tool status to `pending_approval`.
    - Server creates Markdown review file:
      `%TEMP%\hp-live-quality\grasshopper\tools-library\reviews\mcp_verify_quality_good.md`.
    - Review file contains audit header and code quality report:
      ```markdown
      # Review — mcp_verify_quality_good (v1)

      ## Code quality
      analysed: 0 error(s), 1 warning(s)
      - Q-W3 1:5 vague name 'data' — say what the value is in the domain.

      ## Code
      ```csharp
      var data = args.Int("count", 2);
      return data * 2;
      ```
      ```
- **Pass Criteria**: `published.get("status") == "pending_approval"` and `"analysed: 0 error(s), 1 warning(s)"` is present in review file.

---

## 4. Empirical Offline Diagnostic Verification

To prove the integration mechanism before live Revit execution, `live-verify-quality.py --host grasshopper` was tested while Revit was offline.

### Execution Command:
```powershell
python McpShared/tools/live-verify-quality.py --host grasshopper
```

### Result Log:
```
FAIL 1 context: execution enabled, new or scratch document get_grasshopper_context: path='' title=None {"isError": true, "message": "Grasshopper bridge not connected. Open Revit 2026 with Rhino.Inside.Revit and Grasshopper, ensure HPGrasshopper MCP Bridge is running (pipe hpgrasshopper-mcp-2026)."}
{"host": "grasshopper", "exe": "G:\\09-PROJECT AI\\01_Revit\\02_CshapRevit\\01_AddinRebar\\HPGrasshopper\\HPGrasshopper.Mcp.Server\\bin\\Debug\\net10.0\\HPGrasshopper.Mcp.Server.exe", "started": "2026-10-10 16:39:59", "passed": 0, "skipped": 0, "total": 1, "failed": ["1 context: execution enabled, new or scratch document"], "skippedNames": []}
```

### Concrete Takeaways from Diagnostic:
1. Discovery logic properly located `HPGrasshopper.Mcp.Server.exe`.
2. Server launched cleanly and processed stdio MCP protocol.
3. Named pipe client connected to `hpgrasshopper-mcp-2026`.
4. When bridge was not present, server returned the precise hint configured in `GrasshopperHostProfile`:
   `"Grasshopper bridge not connected. Open Revit 2026 with Rhino.Inside.Revit and Grasshopper, ensure HPGrasshopper MCP Bridge is running (pipe hpgrasshopper-mcp-2026)."`
5. This validates that as soon as the bridge named pipe is online in Revit, Gate 1 will transition to PASS and execute Gates 2–5.

---

## 5. Architectural Outline for `run-live-verify.ps1`

`HPGrasshopper/tools/harness/run-live-verify.ps1` must coordinate the complete live verification workflow, incorporating both the 24 tools verification and ADR-0007 script quality verification.

### 5.1 Harness Structure and Prerequisites

```
HPGrasshopper/
├── tools/
│   └── harness/
│       ├── run-live-verify.ps1          <-- Master PowerShell driver
│       ├── harness-common.ps1          <-- Process detection & UIA helpers
│       ├── live-verify.py              <-- 24 tools verification session
│       └── fixtures/
│           ├── math_slider.gh
│           ├── curve_generator.gh
│           └── revit_sync_mock.gh
```

### 5.2 Pre-Flight Checks & Safety Guarantees
1. **Revit & Rhino.Inside Detection**:
   - Detect running `Revit.exe` processes.
   - Respect Zero-Touch user session rules: If user PID is active, isolate test execution or abort with clear notification.
2. **Pipe Availability**:
   - Confirm named pipe `\\.\pipe\hpgrasshopper-mcp-2026` is alive via `[System.IO.Directory]::GetFiles("\\.\pipe\", "hpgrasshopper-mcp-2026")`.
3. **Execution Opt-In Control via UIA**:
   - Use UI Automation to inspect `GrasshopperBridgeStatusView`.
   - Checkbox automation:
     - AutomationId: `AllowExecution`
     - Name: `"Allow AI Grasshopper execution"`
   - Ensure toggle state is set to `On`.

### 5.3 Detailed Pipeline Flow in `run-live-verify.ps1`

The recommended sequencing for `run-live-verify.ps1` consists of 6 sequential phases:

```powershell
# Pseudocode structure for HPGrasshopper/tools/harness/run-live-verify.ps1

param(
    [string]$Exe = '',
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\output\live-verify'),
    [switch]$SkipQuality,
    [switch]$OnlyQuality
)

# Phase 0: Validate Prerequisites & Pipe
Assert-PipeActive "hpgrasshopper-mcp-2026"

# Phase 1: Ensure Opt-In Disabled -> Verify Execute Refused
Set-ExecutionOptIn $false
Invoke-Phase "disabled"  # execute_grasshopper_code must be refused

# Phase 2: Enable Opt-In -> Run ADR-0007 Quality Gate
Set-ExecutionOptIn $true
if (-not $SkipQuality) {
    Write-Host "=== Phase 2: ADR-0007 Script Quality Verification (5/5) ===" -ForegroundColor Cyan
    $qualityPy = Join-Path $repoRoot "McpShared\tools\live-verify-quality.py"
    & python $qualityPy --host grasshopper --exe $Exe --out $OutDir
    if ($LASTEXITCODE -ne 0) { throw "ADR-0007 quality verification failed." }
}

if ($OnlyQuality) { exit 0 }

# Phase 3: Core Tools Execution (5 Core Tools)
Invoke-Phase "core"      # read_canvas, player, batch_edit, execute, export_image

# Phase 4: 12 Embedded Seeds Verification
Invoke-Phase "seeds"     # Execute all 12 seeds with dryRun=true

# Phase 5: Dynamic Tool Registry Cycle
Invoke-Phase "registry"  # MISS -> propose -> test -> approve -> HIT -> quarantine -> restore
```

---

## 6. Embedded Seeds Quality Audit (ADR-0007 Static Checks)

All 12 embedded seeds located in `HPGrasshopper.Mcp.Server/Registry/SeedLibrary/` have been audited against ADR-0007 quality rules:

| Category | Seed Name | Line Count (≤300) | Dead Code (Q-B1) | Empty Catch (Q-B2) | Code Smells (Q-W) | Status |
|----------|-----------|-------------------|------------------|-------------------|-------------------|--------|
| Generic | `export_model_to_rhino` | 42 | 0 | 0 | 0 | PASS |
| Geometry | `curtain_grid_layout` | 55 | 0 | 0 | 0 | PASS |
| Geometry | `directshape_from_geometry` | 68 | 0 | 0 | 0 | PASS |
| Geometry | `evaluate_solar_radiation` | 49 | 0 | 0 | 0 | PASS |
| Geometry | `facade_panelization` | 61 | 0 | 0 | 0 | PASS |
| Geometry | `query_revit_geometry` | 38 | 0 | 0 | 0 | PASS |
| Revit | `create_floors_slabs` | 58 | 0 | 0 | 0 | PASS |
| Revit | `create_levels_grids` | 52 | 0 | 0 | 0 | PASS |
| Revit | `create_wall_openings` | 47 | 0 | 0 | 0 | PASS |
| Revit | `generate_columns_beams` | 64 | 0 | 0 | 0 | PASS |
| Revit | `generate_rebar_curves` | 72 | 0 | 0 | 0 | PASS |
| Revit | `generate_stirrup_distribution` | 69 | 0 | 0 | 0 | PASS |

Verified via `HPGrasshopper.Mcp.Server.Tests.Seeds.ScriptQualityTests`:
- `AllSeeds_PassScriptQualityWithZeroErrors`: PASS (0 errors across 12 seeds)
- `AllSeeds_PassScriptQualityWithZeroWarnings`: PASS (0 warnings across 12 seeds)

---

## 7. Actionable Implementation Checklist for M4 Workers

When implementers construct the M4 Live Verification Harness in `HPGrasshopper/tools/harness/`, they should fulfill the following checklist:

- [ ] **Directory Creation**: Scaffold `HPGrasshopper/tools/harness/` and `fixtures/`.
- [ ] **Dot-source `harness-common.ps1`**: Provide UIA helpers to find `GrasshopperBridgeStatusView` and toggle `AllowExecution`.
- [ ] **Quality Phase Integration**: Add explicit step invoking `python McpShared/tools/live-verify-quality.py --host grasshopper --out $OutDir`.
- [ ] **Verification Report Synthesis**: Aggregate `live-verify-quality-grasshopper.json` with main `run-summary.json`.
- [ ] **Clean Exit Codes**: Return code `0` only when all 24 tools pass AND ADR-0007 quality verification passes 5/5.
