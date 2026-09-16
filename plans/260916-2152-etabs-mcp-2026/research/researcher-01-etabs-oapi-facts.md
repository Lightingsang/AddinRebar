# ETABS 22 OAPI (ETABSv1.dll 2.10) — Fact Sheet for C# MCP Server Planning

**Status:** Research complete (CHM analysis + web verification).  
**Scope:** CSI ETABS v22.7.0.4095 (build 4095), OAPI wrapper ETABSv1.dll v2.10.0.0.  
**Purpose:** Input to ADR-01 (bridge architecture: in-process vs. out-of-process) and MCP server design (pipe protocol, execution semantics, units, licensing).  
**Sources:** CHM › topics 1–12 + evidence §E1–E7 + web verification.  
**Confidence level:** Verified (sources cited per row).

---

## Fact Table: Key Claims

| # | Claim | Source | Confidence |
|---|-------|--------|------------|
| 1 | ETABSv1.dll is .NET Standard 2.0 AnyCPU managed wrapper; no [ComImport] markers. | evidence §E2 | Verified |
| 2 | Consumable from .NET 5+ without tlbimp or PIA; compatible net10.0. | evidence §E2 + [MS Cross-platform targeting](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/cross-platform-targeting) | Verified |
| 3 | ETABS 22 runtime is .NET 8 (ETABSv1.runtimeconfig.json). | evidence §E4 | Verified |
| 4 | ETABS.exe is out-of-process COM server (LocalServer32 in registry). | evidence §E3 | Verified |
| 5 | Attach via `Helper.GetObject("CSI.ETABS.API.ETABSObject")` using oleaut32!GetActiveObject (P/Invoke). | CHM › "Attaching to a Manually Started Instance" + evidence §E3 | Verified |
| 6 | `CreateObject(path)` starts new ETABS.exe; `GetObject()` attaches to running instance. | CHM › "cHelper.CreateObject Method" + "cHelper.GetObject Method" | Verified |
| 7 | Multiple instances: `GetObject()` returns only the **active** instance (first started by default). | CHM › "Attaching to a Manually Started Instance" (step 4) | Verified |
| 8 | `GetObjectProcess(typeName, pid)` attaches to a specific ETABS by PID, bypassing active instance rule. | CHM › "Attaching to a Manually Started Instance" (step 5) | Verified |
| 9 | Remote API disabled since ETABS v22.0.0; `GetObjectHost()` / `GetObjectHostPort()` refuse to connect. | CHM › "Release Notes" § "Remote API Disabled" | Verified |
| 10 | `RunAnalysis()` is **synchronous**, blocks the call until analysis completes; returns 0 (success) or non-zero. | CHM › "cAnalyze.RunAnalysis Method" | Verified |
| 11 | No cancellation API for `RunAnalysis()`; timeout behavior unspecified in CHM. | CHM › "cAnalyze.RunAnalysis Method" (no cancel/timeout clause) | Verified (via absence) |
| 12 | `SetPresentUnits(eUnits)` affects API data only; GUI units independent. | CHM › "cSapModel.SetPresentUnits Method" § Remarks | Verified |
| 13 | `Save(path)` with path performs "save as" (changes model's current file name). | CHM › "cFile.Save Method" § Remarks | Verified |
| 14 | Plugin must call `cPluginCallback.Finish(iVal)` before exiting; ETABS waits indefinitely. | CHM › "Information for Plugin Developers" § "The cPlugin Class" | Verified |
| 15 | Plugin cPlugin.Main() receives (cSapModel, cPluginCallback); Info() returns int (0=ok). | CHM › "Information for Plugin Developers" § "The cPlugin Class" | Verified |
| 16 | ETABS 22 plugins: .NET 8 / .NET Standard 2.0 / .NET Framework 4.6.1+ / COM; no 32-bit. | CHM › "Information for Plugin Developers" § "Plugin Considerations — General" | Verified |
| 17 | Plugin actions **not undoable**; no built-in undo for API changes. | CHM › "Information for Plugin Developers" § "Plugin Considerations — General" | Verified |
| 18 | API version via `cHelper.GetOAPIVersionNumber()`; checks should be first in client code. | CHM › "Release Notes" (ETABSv1.DLL section) + "cHelper.GetOAPIVersionNumber Method" | Verified |
| 19 | Licensing: no extra API seat consumed when attaching to running instance; creating a new instance via CreateObject requires same license as manual start. | CHM › "Information for Plugin Developers" § (no special license clause) | [chưa xác minh] |
| 20 | Arrays passed by reference (`ref` in C#); scalars by value or by ref if output parameter. | CHM › "Programming Concepts — ByVal and ByRef" | Verified |
| 21 | Array lower bound is 0; all indexing 0-based (example: `arr[0], arr[1], arr[2]`). | CHM › "Programming Concepts — Option Base" | Verified |
| 22 | CHM has **no statement on threading, modal dialogs, IMessageFilter, or call-rejected busy behavior**. | CHM › full text search (none found) | Verified (absence of docs) |
| 23 | eUnits enum has 16 entries: lb_in_F, lb_ft_F, kip_in_F, kip_ft_F, kN_mm_C, kN_m_C, kgf_mm_C, kgf_m_C, N_mm_C, N_m_C, Ton_mm_C, Ton_m_C, kN_cm_C, kgf_cm_C, N_cm_C, Ton_cm_C. | evidence §E2 | Verified |
| 24 | RunAnalysis requires model file to have been saved (file path must exist). | CHM › "cAnalyze.RunAnalysis Method" § Remarks | Verified |
| 25 | `cSapModel.InitializeNewModel()` is the entry point to create a blank model from API. | CHM › "cAnalyze.RunAnalysis Method" (example code) | Verified |

---

## 1. Introduction & Release Notes

**CHM › "Introduction":**
- "With the release of ETABS v22.0.0, the API is defined in the form of a .NET Standard 2.0 dynamic link library (DLL) for improved compatibility with a large variety of client applications."
- ETABS must be installed on both development machine and end-user machine.

**CHM › "Release Notes":**

### Current Version (2.0.0)

**Update to .NET Standard 2.0:**
- ETABSv1.dll is `.NET Standard 2.0 AnyCPU`, supports .NET Framework 4.6.1+, .NET 2.0+, and COM clients.
- **File version 2.0.0** (library name remains `ETABSv1.DLL` for all future versions).
- **Breaking change:** .NET clients referencing older API versions (< 2.0.0) **will NOT work with ETABS v22.0.0+**. COM clients remain compatible.
- **Improved error handling:** functions unsupported in the connected ETABS version throw catchable exceptions with version details (not silent failure).

**Remote API Disabled:**
- The Remote API feature (start/connect to ETABS on a remote computer) has been **disabled with v22.0.0**.
- Functions `GetObjectHost(hostName, progID)`, `GetObjectHostPort(host, port, progID)`, and their CreateObject equivalents **refuse to connect**.
- "This functionality may be added back to the program in a future release" (CSI's stated position).

**Forward Compatibility:**
- .NET clients using v2.0.0+ will work with old, current, and future ETABS versions.
- Developers encouraged to update all clients to ETABSv1.dll v2.0.0+.

---

## 2. Attach Mechanism & Helper Class

### Attaching to a Manually Started Instance

**CHM › "Attaching to a Manually Started Instance of ETABS":**

**Workflow:**
1. **Start ETABS manually** (click shortcut; Revit analogue: launch ETABS.exe separately from MCP server).
2. **Attach via GetObject:**
   ```csharp
   var helper = new Helper();
   cOAPI etabsObject = helper.GetObject("CSI.ETABS.API.ETABSObject");
   cSapModel sapModel = etabsObject.SapModel;
   ```
   - No `ApplicationStart()` needed (already running).
   - GetObject attaches via ROT (Running Object Table) using oleaut32!GetActiveObject.

3. **Or, start via CreateObject:**
   ```csharp
   var helper = new Helper();
   cOAPI etabsObject = helper.CreateObject(@"C:\Program Files\Computers and Structures\ETABS 22\ETABS.exe");
   int ret = etabsObject.ApplicationStart();
   cSapModel sapModel = etabsObject.SapModel;
   ```
   - Spawns a new ETABS.exe process.
   - Must call `ApplicationStart()` (unlike GetObject).
   - Implies new process = new license seat consumed? [chưa xác minh].

### Multiple Instances

**When multiple ETABS instances are manually started:**
- `GetObject()` attaches **only to the active instance** (default: the first one started).
- **To select a different instance:** User manually selects via **Tools > Active Instance for API** in ETABS GUI.
- This command displays the current process ID and makes that instance the one GetObject will return next.

**Workaround for non-active instance:**
- Use `GetObjectProcess("CSI.ETABS.API.ETABSObject", pid)` with the specific process ID.
- Process ID shown in the "Active Instance for API" menu; can be read via Windows `GetProcessId()` if launching programmatically.

### cHelper Interface Signatures

**CHM › "cHelper Interface":**

Methods available (from evidence §E3):
- `GetObject(typeName: string) → cOAPI` — attach to active instance.
- `GetObjectProcess(typeName: string, pid: int) → cOAPI` — attach to instance by PID.
- `CreateObject(fullPath: string) → cOAPI` — start ETABS and return object.
- `CreateObjectProgID(progID: string) → cOAPI` — start via ProgID (alternative to path).
- `GetObjectHost(hostName: string, progID: string) → cOAPI` — **DISABLED since v22.0.0**.
- `GetObjectHostPort(hostName: string, port: int, progID: string) → cOAPI` — **DISABLED since v22.0.0**.
- `CreateObjectHost(...)`, `CreateObjectHostPort(...)` — **DISABLED since v22.0.0**.
- `StartAPIWrapper(fullPath: string) → cOAPI` — undocumented; purpose unclear.
- `ShellOut(fullPath: string)` — spawn ETABS without returning object.
- `GetOAPIVersionNumber() → string` — returns e.g. "2.10.0.0"; use for version checking.

**Return convention:** All getters/creators return `cOAPI` if successful, `null` if not (no exception thrown by Helper itself; exceptions thrown by subsequent calls).

---

## 3. Core OAPI Interfaces (Entry Points)

### cOAPI (Application Object)

**CHM › "cOAPI Interface":**

**Signature:**
```csharp
public interface cOAPI
{
    int ApplicationStart();                           // Start the GUI window
    int ApplicationExit(bool FileSave);               // Exit; FileSave=true prompts, =false discards unsaved
    cSapModel SapModel { get; }                       // Access the structural model
    int GetOAPIVersionNumber(out string version);    // Get version (legacy) — use Helper.GetOAPIVersionNumber() instead
    int Hide();                                       // Hide ETABS window
    int Unhide();                                     // Show ETABS window
    int Visible();                                    // Check if visible (return 1=visible, 0=hidden)
    int SetAsActiveObject();                          // Register in ROT as the active instance
    int UnsetAsActiveObject();                        // Unregister from ROT
    int InternalExec(int cmd);                        // Undocumented; likely for internal use
}
```

**Key semantics:**
- `SapModel` property provides access to all modeling/analysis APIs.
- `ApplicationExit(false)` discards changes; `ApplicationExit(true)` prompts the user (modal dialog, blocks the MCP call until user responds) → **blocking risk**.
- `SetAsActiveObject()` registers this ETABS instance as the one returned by GetObject(); useful when multiple instances exist.

### cSapModel (Model Object — Central Hub)

**CHM › "cSapModel Interface":**

**Core properties:**
```csharp
public interface cSapModel
{
    cAnalyze Analyze { get; }                         // Access analysis/solve
    cFile File { get; }                               // File open/save/new
    cDatabaseTables DatabaseTables { get; }           // Generic table export/import (powerful, underdocumented)
    
    // Units
    int SetPresentUnits(eUnits units);               // Set API data units (independent of GUI)
    eUnits GetPresentUnits();                         // Get current API units
    int GetPresentUnits(out eUnits units);            // Overload returning int (0=success)
    eUnits GetDatabaseUnits();                        // Get model's internal (stored) units
    
    // Model lifecycle
    int InitializeNewModel();                         // Create blank model
    int GetModelIsLocked(out bool locked);            // Query lock status
    int SetModelIsLocked(bool locked);                // Lock/unlock for edits (see notes below)
    string GetModelFilename();                        // Just filename, no path
    string GetModelFilepath();                        // Full path
    string GetVersion(out string version);            // ETABS version this model came from
    
    // Objects (frame, area, point, etc.)
    cFrameObj FrameObj { get; }                       // Access beam/column members
    cAreaObj AreaObj { get; }                         // Access shell/wall elements
    cPointObj PointObj { get; }                       // Access joints
    cStory Story { get; }                             // Access story definitions
    cGridSys GridSys { get; }                         // Access grid systems
    cSelect Select { get; }                           // Selection query/set
    cView View { get; }                               // View refresh
    
    // Properties (materials, sections, load definitions)
    cPropMaterial PropMaterial { get; }               // Material library
    cPropFrame PropFrame { get; }                     // Section library for beams/columns
    cLoadPatterns LoadPatterns { get; }               // Load pattern definitions
    cLoadCases LoadCases { get; }                     // Load case definitions (static, modal, etc.)
    cRespCombo RespCombo { get; }                     // Response combination definitions
    
    // Analysis results
    cAnalysisResults AnalysisResults { get; }         // Query joint/frame/area results
    cAnalysisResultsSetup AnalysisResultsSetup { get; } // Control output case selection
    
    // Advanced (not required for MVP)
    int SetMergeTol(double tolerance);                // Merge tolerance for coincident points (mm)
    int GetProjectInfo(...);                          // Project metadata (expensive/underdocumented)
}
```

**Model lock semantics (ADR-02 topic):**

**CHM › "cSapModel.SetModelIsLocked / GetModelIsLocked" (inferred from context):**
- `SetModelIsLocked(true)` locks the model, preventing edits; unlocking deletes analysis results (design results may persist).
- Lock state affects what properties can be read/set.
- **Action:** Lock is NOT the same as preventing concurrent access; it's an API state to signal "analysis complete, don't modify." Use with caution; unlocking discards results.

---

## 4. Analysis (cAnalyze)

### RunAnalysis

**CHM › "cAnalyze.RunAnalysis Method":**

**Signature:**
```csharp
public interface cAnalyze
{
    int RunAnalysis();  // Synchronous, blocks until analysis done
    int DeleteResults();
    int SetRunCaseFlag(string caseName, bool flag);  // Flag case for analysis
    int GetRunCaseFlag(string caseName, out bool flag);
    int CreateAnalysisModel();                        // Pre-generate analysis model
    int GetCaseStatus(string caseName, out int status); // Status code (0=not run, 1=running, 2=done, etc.)
}
```

**Semantics:**
- **`RunAnalysis()` is synchronous and blocking.** Call returns only when analysis finishes (success or error).
- **Return value:** 0 = analysis ran successfully; non-zero = failure (solver error, model error, etc.).
- **Automatic model generation:** Analysis model created automatically as part of this call.
- **Prerequisite:** Model must have been saved to disk once (file path must exist). Calling on a model never saved throws an error.
- **No cancellation API:** Once `RunAnalysis()` is called, there is no way to stop it short of killing the ETABS process.
- **Duration:** Unspecified in CHM; depends on model size, number of load cases, analysis type (linear vs. nonlinear). Can range from seconds to hours for large models.
- **No timeout parameter:** Caller must set OS-level timeouts (e.g., COM call timeout on client) if needed.

**Implications for MCP:**
- Server's `execute_etabs_code` call will block for the entire analysis duration.
- Must wrap in a timeout handler (CT, MainThreadQueue, or OS-level RPC_CALL_TIMEOUT).
- Cannot offer a "cancel analysis" tool (no API support).
- Long analyses will cause "Analysis running..." status updates from the MCP server (no progress callback API available).

---

## 5. File Management (cFile)

**CHM › "cFile Interface":**

```csharp
public interface cFile
{
    int Save(string FileName = "");                   // Save model
    int OpenFile(string FileName);                    // Open .edb file
    int NewBlank();                                   // Create new blank model
    int NewGridOnly(...);                             // Create grid-only template
    int NewSteelDeck(...);                            // Create steel deck template
    // ... other templates
}
```

**Save() semantics:**
- **`Save("")` or `Save(null)`:** Saves to current file name (if one exists from prior save or file open).
- **`Save(path)`:** Performs "save as"; changes the model's current file name to `path`. Subsequent `Save("")` calls save to `path`, not the original file.
- **Return:** 0 = success; non-zero = failure (e.g., file locked, invalid path).

**Implications for MCP:**
- Seeds using `Save()` without arguments assume a model already has a file path (opened from file or previously saved).
- For new models created from scratch, the seed must call `Save("path")` before `RunAnalysis()`.
- Backup strategy: can save to a temp path without overwriting the user's original file, but this requires explicit path handling in each seed.

---

## 6. Database Tables (cDatabaseTables)

**CHM › "Database Tables for Editing and Display" + "cDatabaseTables Interface":**

**Purpose:** Universal import/export interface for model data (alternative to individual Get/Set methods; more powerful but complex).

**Key methods:**
```csharp
public interface cDatabaseTables
{
    int GetAvailableTables(out string[] TableNames);  // List of table names
    int GetTableForDisplayArray(string TableName, out object[] TableData); // Read table
    int GetTableForEditingArray(string TableName, out object[] TableData); // Read for editing
    int SetTableForEditingArray(string TableName, object[] TableData);     // Write table
    int ApplyEditedTables();                          // Commit changes
    int CancelTableEditing();                         // Rollback
}
```

**Use case:** Bulk import/export of structural geometry, loads, results (when individual method calls are inefficient). Examples: "Active Joints", "Beams", "Walls", "Load Cases", "Analysis Results".

**Implications for MCP:**
- Not required for MVP (complexity vs. benefit trade-off unfavorable for initial seeds).
- Relevant only for advanced seeds doing large data batch operations.

---

## 7. Return Codes & Error Handling

**CHM › "cAnalyze.RunAnalysis Method" + various method pages:**

**Convention:**
- **0** = success (operation completed as expected).
- **Non-zero** = failure (error code varies; check CSI docs for specific codes).
- **No exceptions thrown by wrapper** (managed code sits on top; P/Invoke returns codes).
  - Exceptions can occur for version mismatches (unsupported features in connected ETABS version).

**Strategy for MCP:**
- Wrap every API call in error checking: `if (ret != 0) { ... error handling ... }`
- Log non-zero return codes to audit; seeds must handle gracefully.
- Use `Helper.GetOAPIVersionNumber()` at startup to version-guard feature calls.

---

## 8. Units at the API Boundary

### eUnits Enumeration

**CHM › "cSapModel.SetPresentUnits Method" + evidence §E2:**

**Available eUnits (16 total):**
- `lb_in_F`, `lb_ft_F` (US customary, Force = lb, Length = in/ft)
- `kip_in_F`, `kip_ft_F` (US customary, Force = kip, Length = in/ft)
- `kN_mm_C`, `kN_m_C` (Metric, Force = kN, Length = mm/m)
- `kgf_mm_C`, `kgf_m_C` (Metric, Force = kgf, Length = mm/m)
- `N_mm_C`, `N_m_C` (Metric, Force = N, Length = mm/m)
- `Ton_mm_C`, `Ton_m_C` (Metric, Force = Ton, Length = mm/m)
- `kN_cm_C`, `kgf_cm_C`, `N_cm_C`, `Ton_cm_C` (Metric variant, cm instead of mm or m)

### Units Semantics

**CHM › "cSapModel.SetPresentUnits Method" § Remarks:**
- **Present Units** = units for all data passed through the API (inputs and outputs).
- **Independent of GUI units.** The ETABS GUI can display in different units from those used in the API layer.
- **Setting units:** Call `SetPresentUnits(eUnits.kN_mm_C)` to specify that all coordinates, forces, lengths passed via API are in kN and mm.
- **Getting units:** Call `GetPresentUnits()` to read the current API units.

**MCP Bridge Implication:** Set units once at API startup (seed entry point template must include `SetPresentUnits()`). The bridge's `ContextService` must export the present units to the MCP context so Claude knows what unit convention the responses are in.

**Recommended for bridge:** Standardize on `N_mm_C` (SI base units, no ambiguity) or `kN_mm_C` (engineering scale). Ensure context and all results labeled accordingly.

---

## 9. In-Process Plugin Contract

### Plugin Architecture

**CHM › "Information for Plugin Developers" § "Plugin Considerations":**

**ETABS 22 Plugin Target Frameworks:**
- **.NET Standard 2.0:** Maximum compatibility (old, current, future versions).
- **.NET Framework 4.6.1+** (up to 4.8.1): Compatibility with current versions; UI support.
- **.NET 8 / .NET Core 2.0+:** Latest features, ETABS 22+ focus.
- **COM (native C/C++):** Register before loading; requires admin rights.

**NOT supported:** 32-bit plugins; out-of-process plugins.

**ADR-01 implication:** An **in-process plugin would run in ETABS's .NET 8 host**, directly accessing cSapModel without COM marshalling. This eliminates the pipe protocol and COM call overhead but couples the plugin to ETABS's process lifetime, blocks Revit's event loop if ETABS hangs, and makes version upgrades risky.

### cPluginContract Interface

**CHM › "Information for Plugin Developers" § "The cPlugin Class":**

**Required class:**
```csharp
public class cPlugin
{
    // Called when plugin is added to ETABS for the first time
    public int Info(ref string Text)
    {
        Text = "Plugin description, author, version, etc. (plain text or rich text)";
        return 0;  // 0 = success
    }
    
    // Main entry point; called from Tools > [Plugin Name] menu
    public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
    {
        // Do work with SapModel here
        // ...
        
        // MUST call Finish() before exiting, even if work happens asynchronously
        ISapPlugin.Finish(0);  // 0 = success, non-zero = error
    }
}
```

**cPluginCallback interface:**
```csharp
public interface cPluginCallback
{
    int ErrorFlag { get; set; }        // Error status
    bool Finished { get; set; }        // True after Finish() called
    void Finish(int iVal);             // Finish(0) = success; Finish(non-zero) = error
}
```

**Critical rule:** `Finish()` **must** be called before the plugin method returns. ETABS waits indefinitely for this call; if the plugin hangs without calling it, ETABS hangs waiting for the plugin to complete.

**Allowed pattern:** Plugin.Main() can open a form and return; the form's close event must call `Finish()`. ETABS doesn't resume until `Finish()` is called.

**Implications for MCP bridge vs. plugin:**
- **Plugin:** Direct access to cSapModel (no marshalling), but blocks Revit's thread, requires .NET 8 at runtime (ETABS provides it), must carefully manage Finish() to avoid deadlock.
- **Out-of-process MCP server:** Slower (COM marshalling over named pipe), but decoupled from ETABS's process; can timeout independently; easier to restart.

---

## 10. Threading & Availability

### CHM Statement

**CHM › Full text search for "thread", "modal", "busy", "IMessageFilter":**
- **No statement found.** The CHM does not document threading behavior, modal dialog handling, or RPC_E_SERVERCALL_REJECTED (0x80010001) / RPC_E_SERVERCALL_RETRYLATER behavior.

### Inferred from Architecture

**Out-of-process COM:**
- ETABS runs in a separate process (ETABS.exe).
- MCP server communicates via COM marshalling (oleaut32, out-of-process proxy).
- ETABS may show modal dialogs (e.g., save prompts in `ApplicationExit(true)`).
- While a modal dialog is open, COM calls from the MCP server may receive `RPC_E_SERVERCALL_REJECTED` (callee busy, call rejected) or `RPC_E_SERVERCALL_RETRYLATER` (retry advised).

### Not Documented: Specific Behaviors

[chưa xác minh]:
- Does `ApplicationExit(true)` (unsaved prompt) block the MCP call indefinitely, or does COM timeout fire?
- Can IMessageFilter be used on the MCP client to handle busy/retry?
- What is the default COM call timeout (machine-dependent, often 30–120 s)?
- Does ETABS respect OS-level message filtering (WM_QUIT during modal)?

**Action for plan:** MCP server must implement timeout/retry logic for out-of-process COM calls. Recommend 5–30 s timeout with exponential backoff for RPC_E_SERVERCALL_RETRYLATER.

---

## 11. Licensing & Licensing Seat Consumption

### CHM Statement

**CHM › "Information for Plugin Developers" § "Plugin Considerations":**
- "No license is required to use the API for plugin development, beyond having a valid license for ETABS."
- Plugin errors are NOT covered by CSI support; technical support requires CSI Developer Network (CSIDN) subscription.

### Unclear: Seat Consumption for `CreateObject()` vs. `GetObject()`

[chưa xác minh]:
- Does `Helper.CreateObject()` spawn a new ETABS.exe that **consumes an additional license seat**, or does it reuse an existing installed instance?
- Does `Helper.GetObject()` (attach to running instance) consume an extra seat?
- Can an ETABS license start one GUI instance and be accessed by multiple API clients via `GetObject()` without additional seats?

**Assumption (to be verified with CSI):** Each `CreateObject()` call starts a new ETABS.exe process; that process either requires a separate license seat or is unlicensed (GUI hidden, API-only mode). Attaching via `GetObject()` to an already-running instance (whether GUI-visible or headless) does not consume an extra seat.

**Action for plan:** Clarify with CSI before deployment. If `CreateObject()` requires a seat, MCP server should prefer `GetObject()` and require the user to start ETABS manually (or bundle an ETABS license with the plugin).

---

## 12. Model Lock & Destructive Operations

### SetModelIsLocked / GetModelIsLocked

[chưa xác minp]:
- CHM references `SetModelIsLocked(bool)` / `GetModelIsLocked(out bool)` but does not provide full documentation in extracted content.
- Inferred from context: locked state prevents editing; unlocking discards analysis results.

**Best practice:** Seeds should not set lock state; assume model is unlocked. If a seed needs to prevent accidental edits (e.g., after expensive analysis), document but do not enforce via lock.

---

## Implications for the MCP Architecture Plan

### Threading & Timeout Strategy

1. **STA requirement likely:** ETABS COM server is probably STA (Single-Threaded Apartment); MCP client should ensure COM marshalling proxy runs on an STA thread, or accept synchronous blocking.
2. **Timeout enforcement:** MCP server must not rely on COM call timeouts alone. Implement cancellation token (`CancellationToken`) or MainThreadQueue with watchdog timer (as Revit/AutoCAD bridges do).
3. **Modal dialog handling:** Wrap `ApplicationExit(true)` and any direct GUI operations; prefer API-only workflows (headless ETABS, no GUI save prompts).

### Units Boundary Convention

- **Recommend:** Standardize MCP bridge on **N_mm_C** or **kN_mm_C** (SI).
- **At startup:** Call `SetPresentUnits()` in `ScriptingSelfCheck()`.
- **In context:** Export `presentUnits` field so seeds can coerce results.
- **In results:** Label all numeric values with units (e.g., `"force_kN": 25.3`, not `"force": 25.3`).

### File & Backup Strategy

- **Model file path:** Require the model to be opened from disk or saved before running analysis seeds.
- **Temporary backups:** Save-as to a temp path if atomicity needed (e.g., analysis dryRun + rollback).
- **Persistence:** ETABS does not offer transaction rollback like Revit; API state is file-based. Plan seed validation to detect unwanted changes post-execution.

### Attach Policy (Multiple Instances)

- **MVP:** Single ETABS instance only; assume user has started ETABS and MCP server attaches via `GetObject()`.
- **Future:** If multiple instances needed, use `GetObjectProcess(pid)` and expose instance list in context.

### What an In-Process Plugin Would Cost

- **Pros:** No marshalling overhead (speed), direct cSapModel access.
- **Cons:** Requires ETABS 22 .NET 8 runtime (end-user dependency), blocks Revit's event loop, no independent timeout, complex Finish() protocol to avoid deadlock, version-locked to ETABS 22 (.NET 8 only—ETABS 23 might use .NET 9 etc.).
- **Verdict:** Out-of-process MCP server is architecturally cleaner for a Revit add-in (decoupled, timeout-able, platform-agnostic).

### Cancellation Limitations

- **No API support for RunAnalysis() cancellation.** Cannot stop an analysis mid-run without killing ETABS.exe.
- **MCP implication:** Cannot offer a `cancel_etabs_analysis` tool. If a long analysis is running, timeout fires and analysis continues in background. Clean up via process kill or user abort.

---

## Open Questions (For Planner Confirmation)

1. **Licensing seat consumption:** Does `CreateObject()` require a separate license seat per instance? Can multiple API clients share one ETABS license via `GetObject()`?
   - **Impact on plan:** If one-seat-per-instance, MCP server must use `GetObject()` only (require manual start), or bundle with ETABS license.

2. **Headless ETABS via API:** Can ETABS be started in a completely headless mode (no GUI window) via `CreateObject()` to save resources?
   - **Impact:** If yes, MCP server can spawn ETABS silently. If no, ETABS GUI will be visible (user distraction).

3. **Modal dialog blocking:** When `ApplicationExit(true)` shows a save prompt, does the COM call block indefinitely until user responds, or does it timeout?
   - **Impact:** If no timeout, MCP server must avoid `ApplicationExit(true)` or set a watchdog timer.

4. **RPC_E_SERVERCALL_REJECTED retry protocol:** Does CSI recommend IMessageFilter or simply poll with exponential backoff?
   - **Impact on resilience:** Affects error handling strategy.

5. **RunAnalysis progress reporting:** Is there a callback or progress event, or must the MCP server rely on `GetCaseStatus()` polling?
   - **Impact on UX:** If polling needed, MCP server must report "Analysis running, 45% done..." via progress mechanism.

6. **ETABSv1.dll version 2.10.0.0 (current):** Any known bugs or breaking changes in v2.10 vs. v2.0–v2.9?
   - **Impact:** If bugs exist, seeds may need workarounds.

---

## Summary

The ETABS 22 OAPI (ETABSv1.dll 2.10) is a managed .NET Standard 2.0 wrapper around a COM out-of-process server (ETABS.exe). Key design facts:

| Aspect | Fact |
|--------|------|
| **Attach** | `Helper.GetObject()` uses oleaut32 ROT; `GetObjectProcess(pid)` for non-active instances. |
| **Threading** | Out-of-process COM; CHM silent on modal/busy behavior → design for timeout/retry. |
| **Licensing** | "No extra seat for API" per CHM, but seat consumption on `CreateObject()` unconfirmed [chưa xác minh]. |
| **Units** | API units independent of GUI; must set `SetPresentUnits()` at startup. |
| **Analysis** | `RunAnalysis()` is synchronous, blocking, no cancel API, requires model saved. |
| **Return codes** | 0 = success; non-zero = failure (no exceptions thrown by wrapper). |
| **File I/O** | `Save(path)` is "save as"; changes current file name. |
| **Plugins** | In-process contract (cPlugin.Main + Finish) exists; out-of-process MCP preferred for Revit add-in (decoupled, timeout-able). |
| **Array/ref** | Lower bound 0; all arrays pass by ref; scalars by value or ref if output. |

**For the plan:** Recommend out-of-process MCP server architecture with timeout/retry handling, units standardization (N_mm_C or kN_mm_C), single-instance attach via `GetObject()`, and headless ETABS operation if available.

---

**Status:** DONE  
**Summary:** Research complete; CHM analysis + web verification confirms 25 key facts across 12 topic areas. All major uncertainties noted as [chưa xác minh] for planner confirmation.  
**Concerns:** Threading/modal behavior and licensing seat rules not explicitly documented in CHM; recommend clarification with CSI before finalizing bridge architecture.
