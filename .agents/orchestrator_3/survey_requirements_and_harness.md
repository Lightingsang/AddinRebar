# Technical Specification & Verification Harness Survey: AutoCAD 2026 & HPGeoLink

**Investigator**: `miner_survey_specs`  
**Target Orchestrator**: `orchestrator_3` (Conversation ID: `050984c1-afaa-4911-859c-331e9279dc4f`)  
**Date**: 2026-09-20  
**Authority**: `ORIGINAL_REQUEST.md` (Follow-up 2026-09-20T12:39:24Z), `AGENTS.md`, repository architecture documentation.

---

## 1. Executive Summary & Architectural Standards

### 1.1 The 6-Deliverable Repository Layout
The repository consolidates engineering add-ins and autonomous MCP servers across Autodesk and CSI ecosystems into **six distinct deliverables** and **one shared host-neutral engine**:

| Deliverable | Description | Stack & Runtimes | Dependency Policy |
|---|---|---|---|
| `HPRebar/` | Production Revit Add-In (R23–R27) + Revit Dynamic MCP Server | C# / Nice3point.Revit.Sdk / WPF / net48, net8, net10 | References `McpShared/` only |
| `McpShared/` | Host-neutral MCP engine (`Contracts`, `Bridge.Core`, `Server.Core`) | netstandard2.0, net48, net8.0, net10.0 | **Zero host dependencies** (no Autodesk, no CSI) |
| `HPAutoCad/` | **AutoCAD 2026 Add-In** (WPF, ALC) + **HPGeoLink** + **AEC Engine** + **AutoCAD MCP Server** | C# / net8.0-windows, net10.0 / AutoCAD.NET 25.1.0 | References `McpShared/` only |
| `HPNavis/` | Navisworks Manage 2026 Plugin + MCP Server | C# / net48, net10.0 / Navisworks API 23.0 | References `McpShared/` only |
| `HPEtabs/` | ETABS 22 MCP Bridge (WPF standalone desktop app) + MCP Server | C# / net8.0-windows, net10.0 / ETABSv1 2.10 (COM) | References `McpShared/` only |
| `HPCivil3d/` | Civil 3D 2026 MCP Bridge + MCP Server (AutoCAD vertical) | C# / net8.0-windows, net10.0 / AutoCAD.NET 25.1.0 + Civil 3D API | References `McpShared/` only; mirrored from `HPAutoCad/` |

> **Repository Reorganization Mandate**: The legacy standalone `HPGeo/` project is officially retired and migrated into `HPAutoCad/` as a unified feature folder named `HPGeoLink/`. Standalone `HPGeo/` will be completely removed upon verification.

### 1.2 Mandatory Feature Folder Conventions
Following the architectural rules established in `AGENTS.md` and `docs/code-standards.md`, all new tools and features inside `HPAutoCad/` must strictly observe feature encapsulation:

```
HPAutoCad/
├── HPAutoCad/                            ← AutoCAD Add-In UI & Host Integration Project (net8.0-windows)
│   └── <FeatureName>/                    ← e.g., HPGeoLink/ (PascalCase, NO SPACES)
│       ├── Model/                        ← Singular: View-facing models, DTOs, sessions
│       ├── Service/                      ← Singular: Drawing readers, writers, converters, pipelines
│       ├── View/                         ← Singular: WPF Views, Dialogs, UserControls
│       ├── ViewModel/                    ← Singular: CommunityToolkit.Mvvm ViewModels
│       ├── <FeatureName>Command.cs       ← Command entry points (AutoCAD [CommandMethod])
│       └── ...
├── HPAutoCad.Core/                       ← Host-Free Algorithm Library (.NET 8.0)
│   └── <FeatureName>/                    ← e.g., HPGeoLink/ (zero references to Autodesk.AutoCAD.*)
│       ├── Catalog/                      ← Geodetic datums, provinces, central meridians
│       ├── Conversion/                   ← Projection engines (VN-2000 <-> WGS84, Gauss-Kruger)
│       ├── Kml/                          ← KMZ/KML packaging, ZIP compression, styling
│       └── Model/                        ← Pure geometric points, polygons, bounding boxes
├── HPAutoCad.Tests/                      ← Pure Unit Test Project (net10.0-windows / xUnit v3 / MTP)
│   └── <FeatureName>/                    ← Tests covering pure math, calculators, and viewmodels
└── HPAutoCad.TileFetch/                  ← Companion background worker console utility
```

**Key Architectural Rules**:
1. **Host Boundary Isolation**: `HPAutoCad.Core` must NEVER reference `Autodesk.AutoCAD.*` or `Autodesk.Windows`. Any logic dependent on AutoCAD databases, transactions, or entities must reside in `HPAutoCad/<FeatureName>/Service/`.
2. **ViewModel Purity**: ViewModels inherit `ObservableObject`, utilize `[ObservableProperty]` and `[RelayCommand]`, and isolate OS shell operations behind interfaces (`IGeoExportShell`) to permit 100% headless testing in `HPAutoCad.Tests`.
3. **WPF & MaterialDesign Theming**: Windows and controls must merge `Theme.xaml` (backed by `MaterialBridge.xaml`, `ThemeDark.xaml`, and `ThemeLight.xaml`) using `{DynamicResource Brush.X}` and `{DynamicResource Spacing.X}`. `MaterialThemeBridge.Attach` handles live runtime theme switches when AutoCAD's `COLORTHEME` system variable toggles.

---

## 2. AutoCAD 2026 Live Verification Harness Architecture

The AutoCAD live verification harness located in `HPAutoCad/tools/harness/` provides a completely unattended, deterministic test execution environment.

### 2.1 Harness Components & Roles
- `harness-common.ps1`: Shared PowerShell module providing Win32 native P/Invoke, UI Automation helpers, SECURELOAD dialog handling, and process lifecycle management.
- `bridge.scr`: AutoCAD script file executed via the command line `/b` switch on startup. Runs:
  ```text
  HPMCPBRIDGE
  HPMCPSTART
  ```
  `HPMCPBRIDGE` instantiates and shows the bridge status window; `HPMCPSTART` activates the named pipe server listener.
- `run-bridge-unattended.ps1`: Exercises the direct Named Pipe JSON-RPC endpoint (`pipe-scenarios.py`) without the MCP server exe.
- `run-live-verify.ps1`: Comprehensive integration harness driving the published `HPAutoCad.Mcp.Server.exe` over a stdio session (`live-verify.py`) on an isolated test registry (`output/live-verify/registry`).
- `run-ribbon-check.ps1`: UI Automation suite checking Ribbon tab creation, deduplication across workspace switching (`WSCURRENT`), and dynamic icon ink rebuilding across theme changes (`COLORTHEME`).
- `McpShared/tools/mcp-session.py`: Shared Python stdio client that maintains a long-lived persistent JSON-RPC session, pumps output through background threads, and handles asynchronous events.

### 2.2 Unattended AutoCAD Launch & Lifecycle Protocol
AutoCAD 2026 (`R25.1`, .NET 8) is launched unattended using the following strict protocol:
```powershell
$p = Start-Process -FilePath 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe' `
    -ArgumentList @('/nologo', '/product', 'ACAD', '/language', '"en-US"', '/b', "`"$scriptPath`"") `
    -PassThru
$script:acadPid = $p.Id
$env:HP_HARNESS_ACAD_PID = $p.Id
```

1. **Pre-flight Assertion (`Assert-NoAutocadRunning`)**: Refuses execution if any `acad.exe` instance is running on the machine, guaranteeing test isolation and preventing accidental termination of user work.
2. **SECURELOAD Handling (`Answer-SecureLoad`)**:
   - AutoCAD displays a Win32 modal dialog (`#32770`) titled `"Security - Unsigned Executable File"` when loading unsigned plugins.
   - The harness continuously scans top-level windows matching this class/title and uses Win32 `SendMessage(hwndButton, 0x00F5, 0, 0)` (`BM_CLICK`) on the `"Always Load"` button. This permanently registers the bundle path in the Windows user registry.
3. **PID Tracking & Scoped COM**: All COM automation (`[Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application')`) asserts that `Get-Process acad` matches `$env:HP_HARNESS_ACAD_PID` exactly.
4. **Cleanup & Termination**: Harness scripts execute cleanup inside `finally` blocks: closing drawings without saving and terminating only the specific `acad.exe` process spawned by that run.

### 2.3 Named Pipe IPC & Thread Synchronization
- **Pipe Address**: `\\.\pipe\hpautocad-mcp-2026`
- **Security ACL**: `PipeSecurity` configured for `CurrentUserOnly` (matching Windows SID).
- **Transport Format**: JSON-RPC 2.0 NDJSON (newline-delimited JSON).
- **Thread Marshaling & Quiescence**:
  1. The bridge pipe listener runs on a background worker thread.
  2. Scripts are checked against `ScriptGuard` and compiled via Roslyn on the pipe thread.
  3. Work items are enqueued into `MainThreadQueue` (`ConcurrentQueue<T>`).
  4. The bridge hooks `Application.Idle` and triggers a message loop wakeup via Win32:
     ```csharp
     PostMessage(doc.Window.Handle, WM_NULL, IntPtr.Zero, IntPtr.Zero);
     ```
  5. On the AutoCAD main thread, `IsQuiescent` verifies that AutoCAD is not executing user commands, dragging, or displaying modal dialogs.
  6. The script is executed inside a Document Lock (`doc.LockDocument()`) and Transaction.

### 2.4 Error Code Semantics & Diagnostic Protocol
The bridge implements standardized error codes defined in `HPRebar.McpBridge.Core`:

| Error Code | Constant / Condition | Cause & Behavior | Recovery / Autonomous Action |
|---|---|---|---|
| `-32001` | `ExecutionDisabled` | "Allow AI code execution" checkbox is unchecked. Opt-in is per-session and never persisted. | Automated harness must toggle checkbox via UI Automation (`Set-OptIn $true`). |
| `-32002` | `HostBusy` | Editor is non-quiescent (e.g. active command waiting for input) after an 8-second grace period. | Automated harness posts ESC (`0x1B`) keydown/keyup events via `PostMessage` to clear command state, then retries. |
| `-32003` | `NoDocument` | No drawing document is currently open in AutoCAD. | Harness opens a new drawing via COM (`Documents.Add()`) or script. |
| Roslyn CS | Compilation Diagnostic | C# script syntax, namespace, or type resolution error. | Inspect error message and diagnostic range; correct code. |
| Guard Error | Security Denial | Script references denied namespaces, reflection, modal UI, or manual transaction lifecycle. | Remove disallowed members; adhere to bridge scripting contract. |

---

## 3. Tool & Command Execution Mechanisms

### 3.1 Channel A: MCP Script Execution via Roslyn (`execute_autocad_code`)
Scripts executed via `execute_autocad_code` run directly inside AutoCAD's main thread with pre-bound globals:
- Globals: `doc` (Document), `db` (Database), `ed` (Editor), `app` (Application), `tr` (active Transaction), `units` (ScriptUnits), `ct` (CancellationToken), `log` (Action<string>), `progress` (Action<int, int, string>), `args` (ScriptArgs).
- **ScriptGuard Boundaries** (`GuardProfile.Autocad`):
  - **Denied Members**: `SendStringToExecute`, `Command`, `CommandAsync`, `ExecuteInApplicationContext`, `ExecuteInCommandContextAsync`, `ShowModalDialog`, `ShowModalWindow`, `ShowAlertDialog`, `MessageBox`, `GetPoint`, `GetSelection`, `GetEntity`, `GetString`, `StartTransaction`, `Commit`, `Abort`, `LockDocument`.
  - **Implication**: Scripts executed via MCP **cannot** invoke modal UI or simulate interactive command-line prompts. Instead, scripts must execute functionality by calling domain and CAD service classes directly.

#### Direct Service Invocation Pattern for HPGeoLink:
Rather than calling AutoCAD commands, `execute_autocad_code` exercises `HPGeoLink` by invoking its underlying service layer within the transaction:
```csharp
// Example: Verifying DrawingReader and KmzExportPipeline directly via MCP
var ctx = HPGeo.AutoCad.Cad.DrawingContext.Read(doc);
var ids = HPGeo.AutoCad.Cad.DrawingReader.ModelSpaceIds(tr, db, null);
var read = HPGeo.AutoCad.Cad.DrawingReader.Read(tr, ids, 0.005);

var conversion = new HPGeo.Core.Conversion.ConversionOptions(
    new HPGeo.Core.Catalog.TransverseMercatorParameters(105.75, 0.9999, 500000.0, 0.0), 
    1.0);
var kml = new HPGeo.Core.Kml.KmlExportOptions("VerificationRun");
var outcome = HPGeo.AutoCad.Commands.KmzExportPipeline.Run(
    new HPGeo.AutoCad.Commands.KmzExportRequest(read.Points, read.Boundaries, conversion, kml, @"C:\temp\verify.kmz"));

return new { success = outcome.Success, points = read.Points.Count, boundaries = read.Boundaries.Count };
```

### 3.2 Channel B: Scriptable CLI Commands (`-HPGEOKMZ`, `-HPGEOIMPORT`, `HPGEOINFO`)
To allow non-interactive automation from the command line, AutoCAD commands provide hyphenated (`-`) equivalents that bypass modal dialogs and accept structured string arguments:
- `-HPGEOKMZ`: Accepts single-line key-value pairs:
  ```text
  -HPGEOKMZ cm=105.75 type=both out=C:\output\site.kmz layer=0 name=Verification
  ```
- `-HPGEOIMPORT`: Imports KML/KMZ without UI:
  ```text
  -HPGEOIMPORT file=C:\output\site.kmz cm=105.75 unit=m
  ```
- `HPGEOINFO`: Inspects active drawing coordinate settings, survey points count, boundaries count, and RasterImage entities without modifying document state (`CommandFlags.NoUndoMarker`).

These commands can be driven via AutoCAD script files (`.scr`) or via COM `SendCommand` in test harnesses.

### 3.3 Channel C: Unattended Testing of Modal Dialog Windows (WPF + WebView2)
Automating modal dialogs (`HPGEODIALOG`, `HPGEOIMPORT`) without blocking test execution requires the asynchronous pattern proven in `HPGeo/tools/dialog-check.ps1`:

```
PowerShell Harness Main Process                  AutoCAD Process (acad.exe)             Background PowerShell Job
────────────────────────────────                ──────────────────────────             ─────────────────────────
1. Spawns Background Job ─────────────────────────────────────────────────────────────▶  2. Invokes SendCommand over COM:
                                                                                           _.HPGEO\n (Blocks until dialog closes)
3. Enters polling loop
4. Win32 FindTopLevel(pid, "HPGeo") ──────────▶ 5. Modal Dialog Window appears
6. Wait 5-8s for WPF + WebView2 init
7. Capture: PrintWindow(PW_RENDERFULLCONTENT)
8. UI Automation: Invoke-DialogButton
9. Close: PostMessage(hwnd, WM_CLOSE, 0, 0) ──▶ 10. Dialog closes cleanly
                                                11. AutoCAD command completes ────────▶ 12. SendCommand returns & Job exits
```

1. **Non-Blocking Invocation**: Because `SendCommand` blocks until the modal dialog terminates, it is dispatched inside a PowerShell background job (`Start-Job`).
2. **Win32 Window Discovery**: The parent harness polls for the window handle using `EnumWindows`, filtering by AutoCAD's Process ID and the window title prefix `"HPGeo"`.
3. **Off-Screen Rendering Verification**:
   - Uses Win32 `PrintWindow(hwnd, hdc, 2)` (`PW_RENDERFULLCONTENT`).
   - Renders the dialog into an in-memory bitmap and exports to PNG without bringing the window to foreground or relying on desktop screen scraping.
4. **Clean Window Teardown**:
   - The harness sends `WM_CLOSE` (`0x0010`) via Win32 `PostMessage`:
     ```powershell
     [HPGeoWin]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero)
     ```
   - This cleanly dismisses the modal loop on the AutoCAD main thread without triggering native crashes or leaving dangling threads.

---

## 4. AEC Tools & Seed Tools Validation Ecosystem

The AutoCAD MCP Server provides 40 published seed tools across 8 categories:
- **Core (4)**: `execute_autocad_code`, `get_autocad_context`, `inspect_type`, `cancel_execution`.
- **Registry Engine (8)**: `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
- **Standard Drawing Seeds (12)**: `list_layers`, `list_block_definitions`, `get_entities`, `list_layouts`, `get_drawing_info`, `get_selected_entities`, `draw_polyline`, `draw_circle`, `add_text`, `create_layer`, `insert_block`, `add_linear_dimension`.
- **AEC Specialized Seeds (16+)**: `get_drawing_context`, `query_entities`, `query_entities_spatial`, `measure_geometry`, `detect_geometry_issues`, `classify_aec_entities`, `get_entity_relationships`, `create_entities_batch`, `update_entities_batch`, `manage_blocks_attributes`, `manage_annotations`, `manage_hatches`, `manage_xrefs`, `cad_standards_check`, `audit_aec_drawing`, `create_issue_markup`, etc.

### Validation Pattern in `live-verify.py`:
1. **Geometric Defect Scene**: A script sets up reference geometries with known intentional errors (overlapping lines, open polylines, 7 mm endpoint gaps, unclosed loops).
2. **Deterministic Querying**: Tools are invoked via `tools/call` with strict arguments.
3. **Response Assertions**: Envelopes are validated for correct counts, spatial intersections, issue codes (`DUPLICATE`, `ENDPOINT_GAP`, `SELF_INTERSECTION`), and stability logging.
4. **Transaction Integrity**: Every write tool is verified with both `dryRun: true` (rolled back, 0 database changes) and commit, followed by undo verification (`_REGEN` boundary + `_U`).

---

## 5. Civil 3D Mirror Compatibility Protocol

`HPCivil3d/` was created as an exact mirrored clone of `HPAutoCad/` with Civil-specific tokens (`ADR-01 Option A`). This synchronization is enforced by `HPCivil3d.McpBridge.Tests/MirrorTests.cs` using `HPCivil3d/tools/mirror-tokens.json`.

```
HPAutoCad/ (Source Files) ──[mirror-tokens.json transformation]──▶ HPCivil3d/ (Target Files)
                                  │
                       MirrorTests.cs validates:
                       - 24 Mirrored files match 100% after token substitution
                       - SHA256 hashes of hand-ported files match pins
                       - Zero unmapped source drift
```

**Guardrail for HPAutoCad Modification**:
- Any modification to `HPAutoCad.McpBridge` or `HPAutoCad.McpBridge.Loader` files that are registered in `mirroredFiles` will cause `HPCivil3d.McpBridge.Tests` to **FAIL immediately**.
- When creating `HPAutoCad.bundle` and the unified `HPAutoCad.Loader`, the existing `HPAutoCad.McpBridge.Loader` classes must be cleanly preserved or mirrored so that Civil 3D mirror test assertions remain 100% passing.

---

## 6. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|---|---|---|---|---|---|---|
| 1 | Infrastructure | Unattended Launch & SECURELOAD Auto-Answer | Launches `acad.exe` with `/b` script and programmatically dismisses `#32770` security dialog | Path to script (`.scr`), timeout (s) | Spawned process object, PID | Throws if another `acad.exe` is already running | `HPAutoCad/tools/harness/harness-common.ps1` |
| 2 | IPC | Named Pipe JSON-RPC Server | Listens on `\\.\pipe\hpautocad-mcp-2026` for NDJSON commands | JSON-RPC request lines | JSON-RPC responses / notifications | Returns `-32001`, `-32002`, `-32003` error envelopes | `HPAutoCad.McpBridge/BridgeEntry.cs` |
| 3 | Security | Execution Opt-In Toggle | Modeless window checkbox "Allow AI code execution", required per session | UI Automation toggle state | Boolean state | Code execution rejected with `-32001` if unchecked | `HPAutoCad/tools/harness/harness-common.ps1` |
| 4 | UI | Shared Ribbon Tab | Tab `HPAUTOCAD_MCP_TAB` ("HPAutoCad") hosting MCP and tool panels | Workspace / Theme system variables | Rendered ribbon tab and buttons | Logs failure to `loader.log` without crashing AutoCAD | `HPAutoCad.McpBridge.Loader/Ribbon/` |
| 5 | Command | HPGEOINFO | Reports active drawing geodetic settings and entity counts | None | Console text output | Transparent to undo stack (`NoUndoMarker`) | `HPGeo.AutoCad/Commands/HPGeoInfoCommand.cs` |
| 6 | Command | -HPGEOKMZ (Script Mode) | Exports survey points and boundary polylines to KMZ via CLI args | String line: `cm=<KTT> out=<path> [type=] [unit=]` | Status message in editor, KMZ file | Prints error issues, refuses invalid zones | `HPGeo.AutoCad/Commands/HPGeoKmzScriptCommand.cs` |
| 7 | Command | -HPGEOIMPORT (Script Mode) | Imports WGS84 KML/KMZ features as CAD points and polylines | String line: `file=<path> cm=<KTT> [unit=]` | Status message, generated AutoCAD entities | Refuses missing files, invalid KTT | `HPGeo.AutoCad/Commands/HPGeoImportScriptCommand.cs` |
| 8 | UI Automation | Off-Screen Modal Dialog Capture | Captures WPF modal dialogs without desktop occlusion | Window handle (`IntPtr`), output path | PNG image file | Handled via Win32 `PrintWindow` | `HPGeo/tools/dialog-check.ps1` |
| 9 | UI Automation | Clean Modal Window Dismissal | Dismisses modal dialogs cleanly in unattended runs | Window handle (`IntPtr`) | Boolean success | Posts `WM_CLOSE` (`0x0010`) to message queue | `HPGeo/tools/dialog-check.ps1` |
| 10 | AEC Engine | Scene Generation via Scripting | Generates reference drawings with deliberate geometrical defects | C# script in `execute_autocad_code` | Created entity handles and layer IDs | Rolls back completely if `dryRun: true` | `HPAutoCad/tools/harness/aec-tools-live.py` |

---

## 7. Edge Cases

| # | Feature | Input / Condition | Observed Behavior |
|---|---|---|---|
| 1 | Named Pipe Server | Second instance of AutoCAD launched with bridge script | Second instance reports pipe in use in status window; logs `could not create pipe` in shared log; first instance continues serving without interruption. |
| 2 | Code Execution | Editor active with `LINE` command waiting for pick | Request waits for 8s grace period, then fails with error `-32002` ("AutoCAD is busy... Press ESC"). |
| 3 | Command Busy Recovery | Automated ESC sequence sent via `PostMessage` | Win32 `WM_KEYDOWN`/`WM_KEYUP` with `VK_ESCAPE` cancels pending command; subsequent execution succeeds immediately. |
| 4 | Document State | Active document closed via COM (`ActiveDocument.Close(false)`) | Request fails with error `-32003` ("No document is open. Open one first."). Context reader returns `docTitle: null`. |
| 5 | Script Security | Script calls `doc.SendStringToExecute(...)` | `ScriptGuard` refuses execution on the pipe thread; CS diagnostics returned without executing code on main thread. |
| 6 | Script Security | Script calls `AcadApp.ShowModalWindow(...)` | `ScriptGuard` refuses `ShowModalWindow` identifier, preventing main thread deadlock. |
| 7 | Ribbon Tab | User switches workspace (`WSCURRENT`) back and forth | Loader catches event, rebuilds panel without duplicating the shared tab; exactly one `HPAutoCad` tab remains. |
| 8 | Ribbon Tab Theme | User toggles `COLORTHEME` between 0 (dark) and 1 (light) | Loader removes and rebuilds panel; vector icon ink dynamically flips between `#E6E6E6` and `#3C3C3C`. |
| 9 | Modal Dialog | HPGEO dialog launched via script during unattended run | `SendCommand` in background job keeps AutoCAD responsive; parent harness captures screenshot via `PrintWindow` and closes via `WM_CLOSE` within 2 seconds. |
| 10 | Civil 3D Isolation | `HPAutoCad.bundle` deployed and Civil 3D 2026 launched | Bundle manifest contains `Platform="AutoCAD"`; Civil 3D completely ignores the bundle and never writes to `loader.log`. |

---

## 8. Closed-Loop Live Verification Cycle for Requirement R3

To satisfy Requirement R3, no tool implementation, migration task, or bug fix may be marked complete based solely on unit tests. The following automated closed-loop verification cycle must be executed:

```
[Phase 1: Build & Deploy]
  dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
  (Output deployed to %AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\)
            │
            ▼
[Phase 2: Local Static & Unit Tests Gate]
  - dotnet test HPAutoCad.Tests (161 geodetic unit tests)
  - dotnet test HPAutoCad.Mcp.Server.Tests
  - (cd HPCivil3d && dotnet test HPCivil3d.McpBridge.Tests) (Mirror tests)
            │
            ▼
[Phase 3: Launch AutoCAD 2026 Unattended]
  Start-AcadWithBridge (with bridge.scr: HPMCPBRIDGE + HPMCPSTART)
  Answer-SecureLoad (Auto-clicks 'Always Load')
  Poll \\.\pipe\hpautocad-mcp-2026
            │
            ▼
[Phase 4: Harness Execution & MCP Driving]
  - Toggle Opt-in ON via UI Automation (Set-OptIn $true)
  - Execute live test scenarios:
      1. MCP Named Pipe Health & Core Context
      2. HPGeoLink Domain & CAD Service Execution via execute_autocad_code
      3. Scriptable CLI Commands (-HPGEOKMZ, -HPGEOIMPORT, HPGEOINFO)
      4. Dialog Windows (HPGEO, HPGEOIMPORT) via Background Job + PrintWindow + WM_CLOSE
      5. Shared Ribbon Tab & Dynamic Theming (run-ribbon-check.ps1)
      6. AEC Seeds & Drawing Tools Zero-Regression Suite
            │
            ▼
[Phase 5: Diagnostics & Self-Correction]
  Inspect Exit Codes & Logs:
  - %LocalAppData%\HPAutoCad\McpBridge\logs\mcpbridge-*.log
  - %LocalAppData%\HPAutoCad\logs\loader.log
  - %AppData%\HPAutoCad\McpBridge\audit\*.log
  If Failure Detected:
    - Diagnose error code (-32001, -32002, -32003, CSxxxx)
    - Terminate AutoCAD process
    - Patch source code
    - Re-run cycle from Phase 1
            │
            ▼
[Phase 6: Final Clean Verification]
  100% PASS across all suites with zero unhandled exceptions.
```

### 8.1 Exact Test Commands & Automated Assertions

#### Step 1: Solution Build & Bundle Deployment
```bash
dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
```
*Assertion*: `Build succeeded. 0 Error(s), 0 Warning(s)`. Bundle deployed to `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`.

#### Step 2: Unit & Mirror Tests Suite
```bash
dotnet test HPAutoCad/HPAutoCad.Tests
dotnet test HPAutoCad/HPAutoCad.Mcp.Server.Tests
dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests
```
*Assertions*:
- `HPAutoCad.Tests`: 161 passed, 0 failed, 0 skipped.
- `HPAutoCad.Mcp.Server.Tests`: All tests passed.
- `HPCivil3d.McpBridge.Tests`: 55 passed (zero drift against mirrored tokens).

#### Step 3: Shared Ribbon & Theming Verification
```powershell
pwsh HPAutoCad/tools/harness/run-ribbon-check.ps1
```
*Assertions*:
- Exactly one `HPAUTOCAD_MCP_TAB` visible.
- Tab contains both `MCP` panel and `HPGeoLink` panel.
- Tab persists without duplication across `WSCURRENT` switch.
- Panel rebuilds and vector icon switches ink cleanly across `COLORTHEME` flip.
- `summary.json` reports 0 failures.

#### Step 4: Live Bridge & Tool Execution Harness
```powershell
pwsh HPAutoCad/tools/harness/run-bridge-unattended.ps1
pwsh HPAutoCad/tools/harness/run-live-verify.ps1 -IncludeIsolation
```
*Assertions*:
- Bridge pipe listener comes up within 45s.
- Opt-in toggle correctly enforces security (-32001 when disabled).
- Standard seeds (12) and AEC tools (40) pass with 0 errors.
- Change counter records accurate entity modifications.
- Second instance fails gracefully with pipe conflict.

#### Step 5: HPGeoLink Live Functional Verification Script
A dedicated test script (or test runner) executes:
1. Draw 13 golden survey points + closed boundary polyline in model space.
2. Invoke `-HPGEOKMZ cm=105.75 out=%LocalAppData%\HPGeo\site.kmz`. Assert file created and valid.
3. Invoke `-HPGEOIMPORT file=%LocalAppData%\HPGeo\site.kmz cm=105.75`. Assert entities created on `HPGEO-IMPORT`.
4. Launch `_.HPGEO` in background job, detect window, capture `dialog-dark.png` via `PrintWindow`, close via `WM_CLOSE`. Assert exit code 0.
5. Launch `_.HPGEOIMPORT`, capture `dialog-import.png`, close via `WM_CLOSE`. Assert exit code 0.
6. Verify no error entries in `loader.log` or `mcpbridge-*.log`.
