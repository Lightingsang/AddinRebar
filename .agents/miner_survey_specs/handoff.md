# Handoff Report — Specification & Verification Survey for AutoCAD 2026 & HPGeoLink

**Agent**: `miner_survey_specs`  
**Recipient**: `orchestrator_3` (`050984c1-afaa-4911-859c-331e9279dc4f`)  
**Type**: Hard Handoff  
**Generated At**: 2026-09-20T13:00:00Z  
**Primary Deliverable**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\survey_requirements_and_harness.md`

---

## 1. Observation

1. **Repository Layout & Standards**:
   - `AGENTS.md` (lines 9–28, 278–319) defines the repository architecture as containing separate deliverables. The AutoCAD deliverable is `HPAutoCad/`, while `HPGeo/` currently sits at root as a standalone tool. `AGENTS.md` mandates strict feature folder conventions: `<FeatureName>/` with singular `Model/`, `Service/`, `View/`, `ViewModel/` subfolders, explicit namespaces, and zero cross-wiring between deliverables except downward dependencies to `McpShared/`.
   - `ORIGINAL_REQUEST.md` (lines 143–194, Follow-up 2026-09-20T12:39:24Z) mandates migrating `HPGeo/` into `HPAutoCad/` as `HPGeoLink`, establishing single bundle packaging (`HPAutoCad.bundle`), unifying the loader, and enforcing a mandatory closed-loop live verification cycle via MCP AutoCAD (R3).

2. **AutoCAD 2026 Unattended Harness**:
   - `HPAutoCad/tools/harness/harness-common.ps1` (lines 6–33, 71–87):
     - AutoCAD is launched via `Start-Process 'C:\Program Files\Autodesk\AutoCAD 2026\acad.exe' -ArgumentList @('/nologo', '/product', 'ACAD', '/language', '"en-US"', '/b', "`"$scriptPath`"") -PassThru`.
     - SECURELOAD modal dialog (`#32770`, "Security - Unsigned Executable File") is handled automatically by finding button "Always Load" and sending message `0x00F5` (`BM_CLICK`).
     - Process tracking is enforced via PID checks (`$env:HP_HARNESS_ACAD_PID`) and `Assert-NoAutocadRunning`.
   - `HPAutoCad/tools/harness/bridge.scr`: Contains `HPMCPBRIDGE` (displays the bridge window) and `HPMCPSTART` (starts listener).

3. **Named Pipe IPC & Thread Synchronization**:
   - Pipe address is `\\.\pipe\hpautocad-mcp-2026` (`HPAutoCad.McpBridge/BridgeEntry.cs` line 80).
   - Pipe listener dispatches to `MainThreadQueue`, which hooks `Application.Idle` and triggers a message loop wakeup via Win32 `PostMessage(hwnd, WM_NULL, 0, 0)` (`MainThreadExecutor.cs`).
   - Quiescence & Error Codes:
     - `-32001`: Opt-in "Allow AI code execution" checkbox unchecked.
     - `-32002`: AutoCAD busy (command running), grace period 8 seconds.
     - `-32003`: No document open.

4. **Script Execution & Security Boundaries**:
   - `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` (lines 22–42) explicitly DENIES `SendStringToExecute`, `Command`, `CommandAsync`, `ExecuteInApplicationContext`, `ExecuteInCommandContextAsync`, `ShowModalDialog`, `ShowModalWindow`, `ShowAlertDialog`, `MessageBox`, and editor prompts (`GetPoint`, `GetEntity`, etc.).
   - `HPAutoCad/tools/harness/live-verify.py` line 240 confirms: `"A guard denies SendStringToExecute"` and `"A guard denies ed.GetPoint"`.

5. **Command & Modal Dialog Automation**:
   - `HPGeo.AutoCad/Commands/`:
     - CLI non-modal equivalents `-HPGEOKMZ` (`HPGeoKmzScriptCommand.cs`) and `-HPGEOIMPORT` (`HPGeoImportScriptCommand.cs`) accept single-line arguments without popping UI.
     - `HPGEOINFO` is read-only and transparent to undo (`CommandFlags.NoUndoMarker`).
   - `HPGeo/tools/dialog-check.ps1` (lines 133–183):
     - Modal dialogs (`HPGEO`, `HPGEOIMPORT`) are driven by sending commands via COM in a background PowerShell job (`Start-Job`).
     - The parent harness polls for the window handle using `FindTopLevel(acadPid, "HPGeo")`.
     - Captures dialog render off-screen via Win32 `PrintWindow(hwnd, hdc, 2)` (PW_RENDERFULLCONTENT).
     - Dismisses modal dialogs cleanly by posting Win32 `WM_CLOSE` (`0x0010`) to the window handle, preventing hanging the test process.

6. **Civil 3D Mirror Testing**:
   - `HPCivil3d/tools/mirror-tokens.json` directly compares 24 source files in `HPAutoCad.McpBridge*` against `HPCivil3d.McpBridge*`.
   - Any refactoring in `HPAutoCad.McpBridge.Loader` or `BridgeEntry.cs` without updating mirror tokens or preserving backwards compatibility will break `HPCivil3d.McpBridge.Tests`.

---

## 2. Logic Chain

1. **Premise**: Requirement R3 requires an automated, closed-loop verification cycle in AutoCAD 2026 via MCP that must pass before any tool or fix is marked complete.
2. **Inference on Execution Channels**:
   - Because `ScriptGuard.Autocad` denies `ShowModalWindow` and `SendStringToExecute`, `execute_autocad_code` cannot directly invoke AutoCAD modal commands or modal UI.
   - Therefore, live verification must use a hybrid testing architecture:
     a) **MCP `execute_autocad_code`** executes CAD services and algorithms (`DrawingReader`, `KmzExportPipeline`, `ImportPlanner`, `DrawingWriter`) directly within the bridge transaction on the main thread, testing the exact domain logic.
     b) **CLI scriptable commands (`-HPGEOKMZ`, `-HPGEOIMPORT`)** verify command registration and parameter parsing without UI blocking.
     c) **Modal dialogs (`HPGEO`, `HPGEOIMPORT`)** are tested via the background-job + `PrintWindow` + `WM_CLOSE` pattern to verify WPF/WebView2 rendering across dark and light themes without deadlocking the harness.
3. **Inference on Architectural Refactoring**:
   - Migrating `HPGeo/` into `HPAutoCad/` requires two new projects: `HPAutoCad` (Add-In) and `HPAutoCad.Core` (host-free logic), mirroring `HPRebar`.
   - The unified bundle `HPAutoCad.bundle` must preserve the entry points and namespaces expected by `HPCivil3d.McpBridge.Tests` or properly maintain the mirror contract.
   - All 161 geodetic unit tests in `HPGeo.Tests` must be transferred to `HPAutoCad.Tests` to preserve static regression coverage.

---

## 3. Caveats

- No live AutoCAD instances were launched or terminated during this investigation (strict read-only adherence).
- Firewall rules on the dev machine block outbound connections from `acad.exe` ("Autocad2026" rule), meaning satellite imagery downloads must route through the `HPAutoCad.TileFetch.exe` companion utility (or local cache).
- WebView2 runtime must be pre-installed on the host system or provided via fixed version runtime; `WebView2Loader.dll` must be co-located in `runtimes/win-x64/native/` inside the bundle.

---

## 4. Conclusion

1. The exact specifications, harness invocation protocols, error code behaviors, and dialog lifecycle mechanisms are fully identified and documented in `survey_requirements_and_harness.md`.
2. The closed-loop verification cycle for Requirement R3 is established:
   `dotnet build` → `dotnet test` (Unit + Mirror) → Launch AutoCAD unattended (`Start-AcadWithBridge`) → Handle SECURELOAD → Toggle Opt-In via UIA → Run MCP scripts & CLI scenarios → Test Dialogs via background job + `PrintWindow` + `WM_CLOSE` → Verify zero regressions in AEC & seed tools → Self-correct upon failure.
3. The repository documentation rules (retiring HPGeo, 6 deliverables, feature folder conventions) have been mapped to their exact file targets (`AGENTS.md`, `docs/code-standards.md`, `docs/system-architecture.md`, `docs/codebase-summary.md`).

---

## 5. Verification Method

To verify the findings of this report:
1. Inspect the comprehensive technical report:
   `view_file` on `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\survey_requirements_and_harness.md`
2. Verify harness file implementations:
   - Check launch and SECURELOAD handling: `HPAutoCad/tools/harness/harness-common.ps1`
   - Check Named Pipe and script commands: `HPAutoCad/tools/harness/run-bridge-unattended.ps1`
   - Check dialog capture & clean close via WM_CLOSE: `HPGeo/tools/dialog-check.ps1`
   - Check Civil 3D mirror definitions: `HPCivil3d/tools/mirror-tokens.json`
3. Verify test compilation off-host:
   - `dotnet test HPAutoCad.Mcp.Server.Tests`
   - `dotnet test HPCivil3d.McpBridge.Tests`
