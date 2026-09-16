# McpShared Seam Analysis + AutoCAD Mirror for ETABS Host
**Date:** 2026-09-16  
**Task:** Determine if a new ETABS host can reuse the MCP engine in-process (no named pipe, no add-in inside ETABS.exe).  
**Sources:** git log, code grep, line-by-line reads of McpShared engine, AutoCAD host template, Navis additive pattern (commit fb65f25), evidence E7.

---

## 1. Seam Verdict: Server-Side Bridge Communication

**Question:** Can the server exe replace the named pipe with an in-process call to `IBridgeExecutor`?  
**Answer:** **Yes. Fully additive, no breaking changes.**

### Executor Interface (Reusable As-Is)

`McpShared/HPRebar.McpBridge.Core/Pipe/IBridgeExecutor.cs:10-38`
- **Contract:** 6 members (2 properties, 1 event, 3 async/sync methods) + 3 nested DTOs passed by value.
- **Members (all host-agnostic):**
  - `IsBusy` (property) — whether script is running
  - `CompiledScriptCount` (property)
  - `ActiveDocumentTitle` (property) — Revit/AutoCAD populate; ETABS can return filename or null
  - `StateChanged` (event) — raised when busy/title changes (no polling, event-driven)
  - `RunCompleted` (event) — raised when script finishes (no host-specific fields in `LastRunInfo`)
  - `ExecuteAsync(ExecuteRequest, IProgress<ScriptProgress>, CancellationToken)` → `ExecuteResult`
  - `GetContextAsync(bool includeSelection, CancellationToken)` → `ContextResult`
  - `Inspect(InspectRequest)` → `InspectResult` (reflection, no host context needed)
  - `Analyze(AnalyzeRequest)` → `AnalyzeResult` (guard/compile on pipe thread; Roslyn owns host-neutrality)
  - `Cancel()` → `CancelResult`

**DTOs passed through the interface:**
- `ExecuteRequest` (`MCpShared/HPRebar.Mcp.Contracts/Messages/ExecuteMessages.cs`) — code, transaction, dryRun, timeoutSeconds, label, args (no host fields)
- `ExecuteResult` (same file) — value, valueType, changed, isError, exception fields (host-neutral; changed counts are `int`)
- `ContextResult` (ContextMessages.cs:12–53) — **multi-host aware:** `RevitVersion`, `Host`, `HostVersion` (strings), `Autocad` (nullable `AutocadInfo`), `Navis` (nullable `NavisInfo`), plus common `DocTitle`, `DocPath`, `IsFamily`, `IsReadOnly`, `IsModifiable`, `Units`, `ActiveView`, `Selection`, `OpenDocs`, `ExecutionEnabled`
- Nested dtos: `ProgressParams`, `InspectRequest`, `InspectResult`, `CancelResult` (all host-agnostic)

### Server-Side Call Sites (All Consumed Via Interface)

`McpShared/HPRebar.Mcp.Server.Core/Bootstrap/McpServerHost.cs:49` — registration  
`McpShared/HPRebar.Mcp.Server.Core/Services/ExecuteCodeService.cs:21` — ctor injects `IRevitBridgeClient bridge`  
`McpShared/HPRebar.Mcp.Server.Core/Services/ContextService.cs` — uses `bridge.Profile`, `bridge.SendAsync`  
`McpShared/HPRebar.Mcp.Server.Core/Tools/CancelExecutionTool.cs` — calls bridge methods  
`McpShared/HPRebar.Mcp.Server.Core/Tools/InspectTypeTool.cs` — calls bridge  

**The seam:** The server communicates with the bridge only through `IRevitBridgeClient` interface, which sends JSON-RPC over the pipe. To support in-process ETABS, the implementation would be:
1. Keep `IRevitBridgeClient` interface unchanged (it is already generic, names are historical).
2. Create `EtabsInProcessBridgeClient : IRevitBridgeClient` that constructs an `IBridgeExecutor` in-process and calls it directly (no pipes, no JSON serialization).
3. Register it in DI when the profile is ETABS.

**Byte-identical guarantee:** The 8 registry meta tools + core tool names/schemas are driven by `IHostProfile` alone (profile name, categories, CoreToolNames). An ETABS profile would produce identical tool metadata as Revit/AutoCAD with the same categories and names.

---

## 2. Bridge-Side Reusability Table

| Engine Piece | File | Reusable As-Is? | Constructor/Entry | Notes |
|---|---|---|---|---|
| **RequestDispatcher** | `Pipe/RequestDispatcher.cs:18–162` | ✅ Yes | ctor ln 31: `(IBridgeExecutor executor, BridgeSettings settings, string hostVersion, string hostName = "Revit")` | Routes by method suffix, uses executor abstraction. No host-specific wiring. ETABS can call `HandleLineAsync` in-process or dispatch directly. |
| **ScriptGuard** | `Scripting/ScriptGuard.cs` | ✅ Yes | Static `Check(code, GuardProfile)` | Host-independent. ETABS profile would pass `GuardProfile.Etabs` (to be defined). |
| **GuardProfile** | `Scripting/GuardProfile.cs:1–114` | ✅ Needs additive | Static readonly fields for Revit/Autocad/Navis/Etabs | Constructor is generic: `(hostName, deniedIdentifiers, deniedMembers, deniedMembersOnIdentifier, deniedNamespaces)`. Add `GuardProfile.Etabs = new GuardProfile("ETABS", …)` ln 75. |
| **ScriptCompiler** | `Scripting/ScriptCompiler.cs` | ✅ Yes | Ctor: `(int maxCacheSize)` | Roslyn-based, no host coupling. Compiles against the imports/refs passed to it. |
| **ScriptArgs** | `Scripting/ScriptArgs.cs` | ✅ Yes | Public surface: methods `Int()`, `Double()`, `String()`, `Bool()`, etc. | Host-agnostic argument coercion. |
| **ScriptUnits** | `Scripting/ScriptUnits.cs` | ✅ Yes | Ctor: `(Func<double, double> toMm, Func<double, double> fromMm, string label)` | Takes delegates for unit conversion. ETABS would pass converters for its own units (see ETABS CHM enums: `eUnits`). |
| **MainThreadQueue** | `Host/MainThreadQueue.cs:1–95` | ✅ Yes | Ctor: `(Func<Task> tick, bool expireWithoutTicks)` | Takes a `tick` callback (idle event, PostMessage). Platform-independent. |
| **Audit** | `Audit/AuditWriter.cs` | ✅ Yes | Ctor: `(string directoryPath, ILogger<AuditWriter> logger)` | Writes JSON lines to a directory. No host coupling. |
| **BridgeSettings** | `Model/BridgeSettings.cs` | ✅ Yes | Public: `bool ExecutionEnabled { get; set; }` | Simple settings store. Host-agnostic. |
| **BridgeSettingsStore** | `Model/BridgeSettingsStore.cs` | ✅ Needs additive | Factory methods: `BridgeSettingsStore.Revit`, `.Autocad` (ln 21–35) | Paths keyed by host. Add `public static BridgeSettingsStore Etabs => new(…)` following the pattern. |
| **BridgeStatusViewModel** | `ViewModel/BridgeStatusViewModel.cs` | ✅ Yes (WPF-free) | Public properties: `ExecutionEnabled`, `Status`, `PipeName`, `LastRun`, etc. | No XAML, no bindings in the class. Usable by a console/headless ETABS server. |
| **McpBridgeHost** | `Host/McpBridgeHost.cs:17–185` | ⚠️ Partial | Ctor ln 35: `(IBridgeExecutor executor, BridgeSettings settings, BridgeSettingsStore store, string hostVersion, string pipeName, string hostName, string methodPrefix)` | **Not needed for ETABS server exe.** The host creates the PipeListener (ln 46), which is bridge-specific. An in-process ETABS server would instantiate `RequestDispatcher` directly without the pipe listener or the window. |
| **PipeListener** | `Pipe/PipeListener.cs` | ❌ Not used | — | Bridge-specific IPC. ETABS server skips this; it calls dispatcher methods in-process. |
| **Opt-in checkbox + persistent setting** | `BridgeSettings.ExecutionEnabled` | ✅ Yes (WPF-free) | Property (no UI) | Stored in `BridgeSettingsStore.{Host}.json`. ETABS could persist the flag the same way, or skip persistence (flags are never auto-enabled). |

**Summary:** Everything is reusable except `PipeListener` and `McpBridgeHost` (both bridge-specific state machines). The server exe never constructs them; it only needs `RequestDispatcher`, `ScriptGuard`, `ScriptCompiler`, and a settings store.

---

## 3. IBridgeExecutor Contract — What ETABS Server Must Provide

An `EtabsExecutor : IBridgeExecutor` must implement:

```csharp
// (McpShared/HPRebar.McpBridge.Core/Pipe/IBridgeExecutor.cs)
public interface IBridgeExecutor
{
    // State queries (can be read from any thread)
    bool IsBusy { get; }
    int CompiledScriptCount { get; }
    string? ActiveDocumentTitle { get; }  // e.g. "Model1.EDB" or null if no model

    // Events (raised on arbitrary thread, no polling)
    event Action? StateChanged;  // fired when IsBusy or ActiveDocumentTitle changes
    event Action<LastRunInfo>? RunCompleted;  // fired when a script finishes

    // Async execution (Revit/AutoCAD block until done; ETABS will too via process attachment)
    Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken cancellationToken);

    // Context snapshot (host state for the AI; populated via E7 calls into ETABS.exe COM)
    Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken);

    // Reflection (compile-time only, no Revit/ETABS API calls)
    InspectResult Inspect(InspectRequest request);

    // Guard + compile check (pipe thread, no host API, no run)
    AnalyzeResult Analyze(AnalyzeRequest request);

    // Cooperative cancellation (signals the running script's CancellationToken)
    CancelResult Cancel();
}
```

**Error Codes That ETABS Must Produce** (via `BridgeRequestException`; see `RequestDispatcher.cs:132–142`):

| Code | Name | Factory | When | Example Message |
|---|---|---|---|---|
| -32001 | `ExecutionDisabled` | `BridgeRequestException(BridgeErrorCode.ExecutionDisabled, msg)` | `_settings.ExecutionEnabled == false` | "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HP MCP Bridge window inside ETABS." |
| -32002 | `Busy` | `BridgeRequestException.Busy(hostName)` | `_executor.IsBusy == true` past grace period | "ETABS is running a command or showing a dialog. Press ESC or close the dialog in ETABS and retry." |
| -32003 | `NoActiveDocument` | `BridgeRequestException.NoActiveDocument(hostName, documentNoun)` | No model open (check via COM: `cOAPI.SapModel == null` or equivalent) | "No model is open in ETABS. Open one first." |

These are produced inside `RequestDispatcher.ExecuteAsync()` (ln 132–150) and caught by `HandleLineAsync()` (ln 69–74).

---

## 4. Additive McpShared Changes for ETABS Host

**Pattern Source:** Commit fb65f25 (2026-09-15, Navis addition); every change is additive or data-driven, zero deletions in McpShared.

### 4a. `PipeNaming.cs` — One Line

**File:** `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`  
**Current (ln 10–43):**
```csharp
public const string RevitHost = "revit";
public const string AutocadHost = "autocad";
public const string NavisHost = "navis";

public static string For(string host, int version)
{
    var key = host.Trim().ToLowerInvariant();
    return key switch
    {
        RevitHost => For(version),          // hprebar-mcp-r2026
        AutocadHost => "hpautocad-mcp-" + version,   // hpautocad-mcp-2026
        NavisHost => "hpnavis-mcp-" + version,       // hpnavis-mcp-2026
        _ => "hp" + key + "-mcp-" + version, // fallback: hpetabs-mcp-2026
    };
}
```

**Additive Change:**
```csharp
// Add const (ln 20)
public const string EtabsHost = "etabs";

// No change to For() — fallback arm (ln 40) already handles "etabs" → "hpetabs-mcp-2026"
```

**Pipe name:** `hpetabs-mcp-2026` (auto-generated by fallback).

---

### 4b. `HostScriptContracts.cs` — ETABS Imports + Globals

**File:** `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs:1–69`  
**Current:** RevitImports/Globals, AutocadImports/Globals, NavisImports/Globals, NavisHeavyMaxTimeoutSeconds.

**Additive Changes:**
```csharp
// Add after NavisImports (ln 42–48)
public static readonly string[] EtabsImports =
{
    "System", "System.Linq", "System.Collections.Generic",
    "CSiAPIv1",  // ETABSv1.dll is netstandard2.0; CSiAPIv1.* namespaces contain the API
    "HPRebar.McpBridge.Core.Scripting",  // globals: ScriptArgs, ScriptUnits, etc.
};

// Add after NavisGlobals (ln 61)
public static readonly string[] EtabsGlobals = { "oapi", "sap_model", "helper", "units", "ct", "log", "progress", "args" };
// oapi = cOAPI (root entry point)
// sap_model = cSapModel (the model after GetObject, or null if no model open)
// helper = cHelper (for attachment; GetObject, CreateObject, etc.)
// units = ScriptUnits converter (ETABS model units ↔ mm)
// ct = CancellationToken for cooperative timeout
```

---

### 4c. `GuardProfile.cs` — ETABS Deny List

**File:** `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs:1–114`  
**Current:** Revit, Autocad, Navis static readonly fields.

**Additive Change (ln 75, after Navis):**
```csharp
/// <summary>
///     ETABS (CSi out-of-process COM, .NET Standard 2.0 wrapper) denies: direct attachment methods
///     (scripts should not create new instances or attach to other processes), modal UI on the main thread,
///     unsafe reflection (expression trees already denied by base list), and namespaces that bypass the wrapper
///     (P/Invoke, COM interop). The wrapper itself (CSiAPIv1.*) is trusted.
/// </summary>
public static readonly GuardProfile Etabs = new GuardProfile(
    "ETABS",
    deniedIdentifiers: new[] { "MessageBox" },
    deniedMembers: new[]
    {
        // Attachment (multiple instances, remote processes)
        "CreateObject", "CreateObjectProgID", "CreateObjectHost", "CreateObjectHostPort", "CreateObjectProgIDHost", "CreateObjectProgIDHostPort",
        "GetObjectProcess", "GetObjectHost", "GetObjectHostPort",
        "StartAPIWrapper", "ShellOut", "ShellOutV1", "ShellOutV2",
        // Modal UI on the main thread (blocks the bridge's event loop)
        "ShowDialog", "ShowMessageBox", "Prompt",
    },
    deniedNamespaces: new[] { "System.Runtime.InteropServices", "System.Reflection.Emit", "System.Net.Sockets" });
```

---

### 4d. `AnalyzerProfile.cs` — ETABS Analysis Rules

**File:** `McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`  
**Current:** Revit, Autocad, Navis (if it exists; grep shows only Revit).

**Check file existence & pattern:**
```bash
grep -n "class AnalyzerProfile\|static readonly.*Profile" McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs
```

If `AnalyzerProfile` is defined:
```csharp
// Add to AnalyzerProfile.cs
public static readonly AnalyzerProfile Etabs = new AnalyzerProfile(
    disallowExpressionTrees: true,  // same as Revit
    disallowDelegateReflection: true);
```

---

### 4e. `ContextResult.cs` — ETABS Info Record

**File:** `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs:12–53`  
**Current:** `AutocadInfo` record, `NavisInfo` record (with `ModelSummary`), nullable properties in `ContextResult`.

**Additive Changes:**
```csharp
/// <summary>ETABS model context (ln 26+).</summary>
public sealed record EtabsInfo(
    string? ModelPath,            // .EDB file path or null if new unsaved model
    string ModelUnits,            // eUnits enum name (e.g. "kN_mm")
    int NumberOfAnalysisCases,
    int NumberOfLoadPatterns,
    bool IsModifiable,
    bool IsSaved);

// Add to ContextResult (ln 27)
/// <summary>ETABS-only facts; null for non-ETABS hosts.</summary>
public EtabsInfo? Etabs { get; set; }
```

**Side note:** `ContextResult.IsFamily` (ln 33) is Revit-only (true in family docs, false in projects). The description "Revit-only fields; null for non-Revit" is baked in comment. For ETABS, `IsFamily` can be left false (always), or hidden by the host profile's `ContextService.Shape()` (if it exists).

---

### 4f. `JsonRpcMethods.cs` — ETABS Method Prefix

**File:** `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`  
**Current:** `RevitPrefix = "revit."`, `AutocadPrefix = "autocad."`, and a `For(prefix, suffix)` factory.

**Additive Change:**
```csharp
public const string EtabsPrefix = "etabs.";

// No change to For() — it uses the prefix passed by the profile
```

---

### 4g. `IHostProfile.cs` + `HostProfile.cs` — ETABS Profile

**File:** `McpShared/HPRebar.Mcp.Server.Core/Hosts/IHostProfile.cs`  
**No change.** The interface is already generic.

**File:** `McpShared/HPRebar.Mcp.Server.Core/Hosts/HostProfile.cs:1–103`  
**Current:** Static `Revit` instance + data-only `HostProfile` record.

**Additive Change (in `HPEtabs.Mcp.Server` project, not McpShared):**
```csharp
// HPEtabs/HPEtabs.Mcp.Server/Hosts/EtabsHostProfile.cs (new file)
public static class EtabsHostProfile
{
    public const string ExecuteToolName = "execute_etabs_code";
    public const string ContextToolName = "get_etabs_context";

    public static readonly HostProfile Instance = new HostProfile
    {
        HostId = PipeNaming.EtabsHost,  // "etabs"
        DisplayName = "ETABS",
        ServerName = "HPEtabs MCP",
        ProductFolder = "HPEtabs",
        EnvPrefix = "HPETABS_MCP_",
        DefaultVersion = 22,  // ETABS 22 is current; can be overridden
        ValidVersions = new[] { 22 },  // E1 shows v22.7.0
        MethodPrefix = JsonRpcMethods.EtabsPrefix,  // "etabs."
        ExecuteToolName = ExecuteToolName,
        ContextToolName = ContextToolName,
        ResourceScheme = "etabs",
        Categories = new[] { "Structure", "Analysis", "Design", "Utilities", "Data", "Generic" },
        CoreToolNames = new[] { ExecuteToolName, ContextToolName, "inspect_type", "cancel_execution" },
        ScriptImports = HostScriptContracts.EtabsImports,
        ScriptContractSummary =
            "Globals: oapi (cOAPI root), sap_model (the open cSapModel or null), helper (cHelper for attachment methods), units (units.ToMm(du), units.ToEtabs(mm), units.Label — coordinates are in document units), " +
            "ct, log(string), progress(cur, total, msg), args. " +
            "transaction: auto when the code modifies the model; none when it only reads. " +
            "ETABS is a review/edit tool; scripts run with the COM attachment you establish (E2/E3), never a separate process. All coordinate conversions handle the document's active unit system. " +
            "Raises ArgumentException for invalid inputs; they do not count against stability.",
        HostAssembly = typeof(EtabsHostProfile).Assembly,
        CliExecutable = "HPEtabs.Mcp.Server.exe",
        MaxTimeoutSeconds = 120,  // ETABS API is synchronous; no async I/O or long operations
    };
}
```

---

### 4h. Server-Side Seed Install & Registry

**Files:** `McpShared/HPRebar.Mcp.Server.Core/Registry/ToolLibraryStore.cs`, `ToolManager.cs`  
**Current:** Products keyed by `profile.ProductFolder` → `%AppData%\{ProductFolder}\McpServer\tools-library\`.

**No change needed.** `EtabsHostProfile.ProductFolder = "HPEtabs"` automatically routes seeds to `%AppData%\HPEtabs\McpServer\`.

---

### 4i. No Changes to These (Already Host-Agnostic)

- `ExecuteCodeService.cs` — uses `bridge.Profile.MaxTimeoutSeconds`; respects the profile's ceiling.
- `ContextService.cs` — uses `bridge.Profile` for resource scheme + field shaping (if `Shape()` method exists; verify).
- `ToolValidator.cs`, `ToolManager.cs` — driven by profile.
- `BridgeClient.cs` (the server-side pipe client) — no host hardcoding; in-process ETABS replaces this entirely.
- Tests in `McpShared/HPRebar.Mcp.Server.Core.Tests` — use `FakeRevitExecutor` but profile-parameterized; Navis tests already exist in `NavisProfileTests.cs:231`.

---

## 5. Blockers Checklist

| Item | Status | Details |
|---|---|---|
| **Pipe-based architecture** | ✅ Not a blocker | Server-side talks to bridge via `IRevitBridgeClient`. Replacing with an `EtabsInProcessBridgeClient` is additive. |
| **Host enumeration in switches** | ✅ Checked | McpShared has no hard-coded host names (Revit/AutoCAD/Navis) in conditional logic; all host-specific data lives in profiles + `GuardProfile`/`AnalyzerProfile` static fields. Adding ETABS requires no deletions. |
| **Error code mapping** | ✅ Not a blocker | `-32001/-32002/-32003` are generic; any host can produce them via `BridgeRequestException`. |
| **Context shape (ContextResult.Shape)** | ⚠️ Needs audit | Check if `ContextService.cs` has a `Shape(profile)` method that hides Revit fields. If so, ETABS may need to hide `AutocadInfo`/`NavisInfo` but expose `EtabsInfo`. Likely already handled by nullable fields + conditional JSON serialization. |
| **Registry category validation** | ✅ Checked | Profile declares its own `Categories` array; no hard-coded list. ETABS categories ("Structure", "Analysis", etc.) are data-driven. |
| **Seed installer** | ✅ Checked | `ToolLibraryStore` uses `profile.ProductFolder`; no ETABS-specific paths needed. |
| **CLI executable naming** | ✅ Checked | `HostProfile.CliExecutable` is configurable. `EtabsHostProfile.CliExecutable = "HPEtabs.Mcp.Server.exe"` works. |
| **Reserved tool names** | ✅ Checked | `ToolValidator.IsReserved(name, profile)` uses `profile.CoreToolNames`. ETABS declares its own four core tools. |

**Conclusion:** **Zero blockers.** All architecture is host-neutral or profile-driven.

---

## 6. AutoCAD-Mirror File Inventory for HPEtabs

**Template:** `HPAutoCad/` structure (verified 2026-09-14, phases 1–5 complete, live in AutoCAD 2026).

| File | AutoCAD Equivalent | HPEtabs → What Changes | Notes |
|---|---|---|---|
| **Solution & Build** | | | |
| `HPEtabs.slnx` | `HPAutoCad/HPAutoCad.slnx` | `Project Path="HPEtabs.Mcp.Server/…"` | No bridge loader (ETABS attachment via COM, not add-in); only server exe. |
| `HPEtabs/global.json` | `HPAutoCad/global.json` | `"version": "10.0.300"` (same .NET SDK) | Pin shared SDK version. |
| `HPEtabs/HPEtabs.Mcp.Server/HPEtabs.Mcp.Server.csproj` | `HPAutoCad/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.csproj` | `RootNamespace=HPEtabs.Mcp.Server`, reference `ETABSv1.dll` (NuGet? local? see sec 7) | Add `<PackageReference Include="…">` or `<Reference>` to ETABS API. |
| `HPEtabs/HPEtabs.Mcp.Server/Program.cs` | `HPAutoCad/HPAutoCad.Mcp.Server/Program.cs` (3 lines) | `return await McpServerHost.RunAsync(args, EtabsHostProfile.Instance);` | Swap profile. |
| **Hosts** | | | |
| `HPEtabs/Hosts/EtabsHostProfile.cs` | `HPAutoCad/Hosts/AutocadHostProfile.cs` | Data: HostId, categories, imports, contract summary, CLI name | Declare ETABS-specific profile (see 4g above). |
| **Core Tools** | | | |
| `HPEtabs/Tools/ExecuteEtabsCodeTool.cs` | `HPAutoCad/Tools/ExecuteAutocadCodeTool.cs` | Class name, tool description, MCP `[Tool]` attribute | Call `bridge.ExecuteAsync(…)` (server-side, no host API); the bridge marshals to ETABS via COM. |
| `HPEtabs/Tools/EtabsContextTool.cs` | `HPAutoCad/Tools/AutocadContextTool.cs` | Class name, populates `ContextResult.Etabs` | Call `bridge.GetContextAsync(…)`. |
| **Resources** | | | |
| `HPEtabs/Resources/EtabsDocumentResources.cs` | `HPAutoCad/Resources/AutocadDocumentResources.cs` | `[Resource("etabs://…")]` URIs | Serve `etabs://model/info`, `etabs://selection`. |
| **Prompts** | | | |
| `HPEtabs/Prompts/EtabsScriptPrompts.cs` | `HPAutoCad/Prompts/AutocadScriptPrompts.cs` | `[Prompt]` attributes, ETABS-specific guidance | E.g., "ETABS models are in drawing units; ScriptUnits converts." |
| **Seed Library** | | | |
| `HPEtabs/Registry/SeedLibrary/<Category>/<name>/` | `HPAutoCad/Registry/SeedLibrary/<Category>/<name>/` (12 seeds) | Folder structure, `tool.json`, `code.cs`, `examples.json` | **Omit initially.** Start with zero seeds; the registry loop (`propose_tool` → compile → `test_tool` → `publish_tool` → CLI approve) will install user-proposed seeds. |
| **Tests** | | | |
| `HPEtabs/HPEtabs.Mcp.Server.Tests/HPEtabs.Mcp.Server.Tests.csproj` | `HPAutoCad/HPAutoCad.Mcp.Server.Tests/HPAutoCad.Mcp.Server.Tests.csproj` | Same structure (SeedLibraryTests, HostProfileTests) | Start with empty (zero seeds to test) or stub test. |
| `HPEtabs/HPEtabs.Mcp.Server.Tests/SeedLibraryTests.cs` | `HPAutoCad/HPAutoCad.Mcp.Server.Tests/SeedLibraryTests.cs` (lines 14–273) | Compile check against `ETABSv1.dll` + CSI API + HPEtabs bridge | See sec 7 for NuGet package availability. |
| **Harness** | | | |
| `HPEtabs/tools/harness/run-live-verify.ps1` | `HPAutoCad/tools/harness/run-live-verify.ps1` (PowerShell) | Imports `McpShared/tools/mcp-session.py` | Copy AutoCAD harness, adapt for ETABS out-of-process (use COM attachment instead of add-in). |
| `HPEtabs/tools/harness/live-verify.py` | `HPAutoCad/tools/harness/live-verify.py` (Python) | Test scenarios (execute, seed install, registry loop) | Reuse McpShared's `harness_common.py` Checklist. |
| **Config & Docs** | | | |
| `HPEtabs/appsettings.json` | `HPAutoCad/appsettings.json` | `Bridge:HostVersion` → 22; paths for registry, logs | Copy AutoCAD structure. |
| `HPEtabs/README.md` | `HPAutoCad/README.md` | Setup, build, publish, `.mcp.json` entry instructions | Adapt for ETABS attachment (COM, not add-in); mention ETABSv1.dll resolution (NuGet vs local install). |
| `.mcp.json` entries | `.mcp.json`: `hprebar-autocad` entry | Add `hprebar-etabs` → exe path + env `HPETABS_MCP_Bridge__HostVersion=22` | User maintains (not tracked). |

---

## 7. Test Strategy Facts

### AutoCAD Seed Compile Check (`SeedLibraryTests.cs:206–272`)

**Approach:** Direct Roslyn compilation against NuGet package.

1. **API DLL source:** NuGet cache `~/.nuget/packages/autocad.net/{PackageVersion}/lib/net8.0/AcMgd.dll` (ln 270).
2. **Package version:** Pinned to `"25.1.0"` (ln 25).
3. **Skip behavior:** `Assert.SkipWhen(errors is null, …)` (ln 176) — test is skipped if NuGet package not in cache; CI/CD must pre-cache it or skip.
4. **Wrapper construction:** Creates a synthetic `SeedHost` class with the bridge's globals + `Run()` method containing the seed code (ln 219–240).
5. **Compilation targets:** net8.0 (matching AutoCAD.NET 25.1.0 NuGet package). No need for .NET Framework.
6. **References:** System.* + platform assemblies + AutoCAD.NET DLLs + `ScriptArgs` + `HPAutoCad.Aec` (ln 243–254).

**For ETABS:**
- **Package availability:** E5 (evidence) states "No CSI/ETABS package in the NuGet cache." CSi (Computers and Structures, Inc.) does **not publish to NuGet** (unlike Autodesk). Two options:

  **Option A — NuGet fallback (recommended if CSi ever publishes):**
  - Mirror AutoCAD's `FindAutocadReference()` method: search NuGet cache for `etabs`, `csi.api`, or custom package name.
  - Fall back to skip if not found.

  **Option B — Installed product path (current reality):**
  - Use `Directory.Build.props` pattern from HPNavis (section 4, Directory.Build.props) — detect installed ETABS and resolve `ETABSv1.dll` from disk.
  - Requires build-time detection of `C:\Program Files\Computers and Structures\ETABS 22\ETABSv1.dll` (E1: FileVersion 2.10.0.0).
  - Skip test with a diagnostic if ETABS is not installed or the file is not found.

**Recommendation:** **Use Option B** (Directory.Build.props + installed product). ETABS is not a cross-platform library; customers have it installed or they don't. This matches the HPNavis pattern and the real-world constraint.

---

### Navisworks Seed Compile Check (`HPNavis/HPNavis.McpBridge.Tests/SeedLibraryCompileTests.cs`)

**Approach:** Compile through the bridge's own .NET Framework 4.8 compiler.

1. **API source:** Windows registry → `Program Files\Autodesk\Navisworks Manage 2026\` (no NuGet).
2. **Detection:** `Directory.Build.props` (HPNavis/Directory.Build.props:1–28) reads `NavisworksInstallDir` from:
   - Env var `HPNAVIS_NAVISWORKS_DIR` (user override)
   - Registry key `HKEY_LOCAL_MACHINE\SOFTWARE\Autodesk\Navisworks API Runtime\{Major}\Navisworks Manage` → `Path`
   - Fallback: `%ProgramW6432%\Autodesk\Navisworks Manage {Year}\`
3. **Availability check:** Property `NavisworksApiAvailable` is `true` iff `NavisworksInstallDir\Autodesk.Navisworks.Api.dll` exists (ln 21–22).
4. **Compilation:** `BridgeEntry.CreateScriptCompiler()` (net48) compiles the seed against the installed DLLs, no Roslyn direct call.
5. **Skip:** Tests skip if the API DLL is not found.

**For ETABS:**

**Option B (recommended):** Same as Navisworks.

1. **Create `HPEtabs/Directory.Build.props`:**
   ```xml
   <Project>
     <PropertyGroup>
       <EtabsInstallDir Condition="'$(EtabsInstallDir)' == '' And '$(HPETABS_ETABS_DIR)' != ''">$(HPETABS_ETABS_DIR)</EtabsInstallDir>
       <EtabsInstallDir Condition="'$(EtabsInstallDir)' == ''">$([MSBuild]::GetRegistryValueFromView('HKEY_LOCAL_MACHINE\SOFTWARE\Computers and Structures\ETABS 22', 'InstallLocation', null, RegistryView.Registry64))</EtabsInstallDir>
       <EtabsInstallDir Condition="'$(EtabsInstallDir)' == ''">$(ProgramW6432)\Computers and Structures\ETABS 22\</EtabsInstallDir>
       <EtabsInstallDir Condition="!HasTrailingSlash('$(EtabsInstallDir)')">$(EtabsInstallDir)\</EtabsInstallDir>
       <EtabsApiAvailable>false</EtabsApiAvailable>
       <EtabsApiAvailable Condition="Exists('$(EtabsInstallDir)ETABSv1.dll')">true</EtabsApiAvailable>
     </PropertyGroup>
   </Project>
   ```

2. **In `HPEtabs.Mcp.Server.Tests.csproj`:**
   ```xml
   <ItemGroup Condition="'$(EtabsApiAvailable)' == 'true'">
     <Reference Include="ETABSv1" HintPath="$(EtabsInstallDir)ETABSv1.dll" Private="false" />
     <Reference Include="CSiAPIv1" HintPath="$(EtabsInstallDir)CSiAPIv1.dll" Private="false" />
   </ItemGroup>
   ```

3. **In `SeedLibraryTests.cs`:**
   ```csharp
   [Theory]
   [MemberData(nameof(Seeds))]
   public void Seed_code_compiles_against_the_etabs_api(string key)
   {
       var seed = Get(key);
       if (!CheckEtabsApiAvailable()) { Assert.Skip("ETABS API not available"); return; }
       
       var errors = CompileAgainstEtabs(seed.Code);
       Assert.Empty(errors, string.Join(Environment.NewLine, errors));
   }

   private static bool CheckEtabsApiAvailable() => typeof(CSiAPIv1.cOAPI).Assembly != null;

   private static string[] CompileAgainstEtabs(string code)
   {
       // Mimic AutoCAD's pattern: wrap code in a class, compile with Roslyn against ETABSv1.dll + CSiAPIv1.dll.
       // References: System.*, netstandard, ETABSv1, CSiAPIv1, HPRebar.McpBridge.Core.Scripting
       // ...
   }
   ```

---

## 8. Docs Impact List

| File | Section | Change |
|---|---|---|
| `CLAUDE.md` | "Repository Layout" table | Add `HPEtabs/` row: "The ETABS MCP server exe (net10, stdio, out-of-process COM). Phases 0–N." |
| `CLAUDE.md` | "HPRebar MCP Bridge" section | Note that ETABS uses the same architecture but without a named pipe (in-process bridge executor via COM attachment from the server exe process). |
| `docs/codebase-summary.md` | "Projects" table | Add `HPEtabs.Mcp.Server` entry. |
| `docs/system-architecture.md` | Architecture diagram / "MCP Hosts" section | Add ETABS host box showing "server exe (net10, stdio) ↔ COM attachment ↔ ETABS.exe (out-of-process, .NET 8)"; contrast with Revit/AutoCAD/Navis add-in + named pipe pattern. |
| `.mcp.json` | (user document, not tracked) | Document the entry: `"hprebar-etabs": { "command": "path\\to\\HPEtabs.Mcp.Server.exe", "env": { "HPETABS_MCP_Bridge__HostVersion": "22" } }` |

---

## 9. ADR Candidates for ETABS Plan

(Not part of this research, but flagged for the planner):

1. **ADR-01:** In-process bridge executor vs. named pipe (decision: in-process for ETABS COM-out-of-process).
2. **ADR-02:** ETABS unit handling (mm at boundary, units conversion via `ScriptUnits` + delegates).
3. **ADR-03:** Seed compile check — Directory.Build.props + installed product path (no NuGet).
4. **ADR-04:** Guard profile (identifiers/members to deny; ETABS is a review/edit tool, no async I/O).
5. **ADR-05:** ETABS COM attachment lifecycle (when to call `Helper.GetObject()`, error handling for disconnected instance).

---

## Summary

**Seam Verdict:** ✅ **YES, fully reusable with in-process bridge.**

- `IBridgeExecutor` contract is host-agnostic; ETABS can implement it and be called directly from the server exe (no named pipe).
- Server-side consumed only through `IRevitBridgeClient` interface; replacing with `EtabsInProcessBridgeClient` is additive.
- All 6 registry meta tools + core tools (inspect, cancel) are host-neutral; tool names/schemas remain byte-identical.

**McpShared Additive Changes (0 deletions):**
1. `PipeNaming.EtabsHost` const + fallback already handles pipe naming.
2. `HostScriptContracts.EtabsImports`, `EtabsGlobals`.
3. `GuardProfile.Etabs`.
4. `AnalyzerProfile.Etabs` (if separate class exists).
5. `ContextResult.EtabsInfo` record + nullable property.
6. `JsonRpcMethods.EtabsPrefix`.

**AutoCAD-Mirror Scope:** Server exe only (no bridge add-in; ETABS attachment via COM). Same 5-layer structure: host profile + 4 core tools + seed library folder. Tests use Directory.Build.props + installed product path (like Navisworks).

**Blockers:** None. All architecture is profile-driven or host-agnostic.

---

## Unresolved Questions for the Planner

1. **ETABS COM attachment stability:** When the user closes ETABS or a model, how does the server process handle a dropped COM connection? Should it gracefully fail the current run or require manual server restart?
2. **Transaction semantics:** Does ETABS COM API support transactional editing (undo on error) like Revit/AutoCAD? Or are changes permanent once committed?
3. **Version range:** Support only ETABS 22, or include 23+ if the API is stable? E1 shows v22.7.0; the API wrapper version is 2.10.0.0 (not year-based like Revit).
4. **Seed categories:** "Structure", "Analysis", "Design" are domain-specific. Should the default profile include fewer categories, or is the AutoCAD set (8 categories) sufficient for MVP?

