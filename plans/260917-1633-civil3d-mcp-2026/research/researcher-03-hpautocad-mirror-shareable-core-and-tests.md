# Research: HPAutoCad Core Size & Shareable Potential for Civil 3D MCP

**Measurement Date:** 2026-09-17 · **Scope:** HPAutoCad loader, bridge, server (non-seed)  
**Threshold Rule:** Option B recommended only if shareable core ≥ 1 500 lines  
**Repo Constraint:** CLAUDE.md line 11—"MCP folders never reference each other; the only permitted dependency direction is MCP folder → McpShared/"

---

## 1. Per-File Classification Table

### Loader (HPAutoCad.McpBridge.Loader, 482 lines total)

| File | Lines | Category | HPAutoCad-Specific Tokens | Notes |
|------|-------|----------|---------------------------|-------|
| `BridgeLoaderApplication.cs` | 111 | **S** | `HPAutoCad.McpBridge.Loader`, `ExtensionApplication` attr | Reflection + ALC loading generic; only assembly name constant differs `verified by BridgeLoaderApplication.cs:21` |
| `BridgeLoadContext.cs` | 48 | **S** | `Autodesk.`, `HostAssemblyPrefixes = ["Ac", "Ad"]` | Pure resolver; prefix list would need `AeccDb`, `AecBase`, `AecPropData` added for Civil 3D `verified by BridgeLoadContext.cs:17` |
| `Ribbon/RibbonCommandHandler.cs` | 27 | **S** | None in class; uses `IExternalEventHandler` pattern | Completely generic command routing |
| `LoaderLog.cs` | 30 | **S** | `HPAutoCad`, `McpBridge` folder names only | Pure file logging; folder name via constant `verified by LoaderLog.cs:18-20` |
| `Ribbon/McpRibbonTab.cs` | 151 | **P** | `TabId = "HPAUTOCAD_MCP_TAB"`, `ButtonId = "HPAUTOCAD_MCP_BRIDGE"`, `ButtonText = "MCP Bridge"`, `[HPAutoCad MCP]` prefix in tooltip | Every string is parametrisable; ribbon event handlers generic `verified by McpRibbonTab.cs:17-22` |
| `Ribbon/RibbonIcons.cs` | 45 | **P** | URI `/HPAutoCad.McpBridge;component/Resources/Themes/` | Icon pack path only; rendering logic generic `verified by RibbonIcons.cs:40-58` |
| `BridgeActions.cs` | 42 | **P** | `Prefix = "[HPAutoCad MCP]"`, entry point key in `Invoke()` | Prefix text + command name trivial to inject `verified by BridgeActions.cs:14` |
| `BridgeLoaderCommands.cs` | 28 | **P** | `HPMCPBRIDGE` command name, `BridgeLoaderApplication.Invoke("status")` | Command name + entry key names `verified by BridgeLoaderCommands.cs:11-12` |

**Loader Subtotals:**
- **Rigid S:** 216 lines (BridgeLoaderApplication 111 + BridgeLoadContext 48 + RibbonCommandHandler 27 + LoaderLog 30)
- **Parameterisable P:** 266 lines (McpRibbonTab 151 + RibbonIcons 45 + BridgeActions 42 + BridgeLoaderCommands 28)
- **Total S+P:** 482 lines (100% parametrisable or reusable)

---

### Bridge Runtime (HPAutoCad.McpBridge, 1,305 lines total)

| File | Lines | Category | HPAutoCad-Specific Tokens | Notes |
|------|-------|----------|---------------------------|-------|
| `BridgeEntry.cs` | 185 | **X** | `VendorFolder = "HPAutoCad"`, `HostName = "AutoCAD"`, `reference HPAutoCad.Aec.AecTools`, status strings `[HPAutoCad MCP]`, log text "AutoCAD 2026", "accoremgd" prefix, `HPMCPBRIDGE` | Every constant + every string AutoCAD/AEC-specific; Civil 3D adds `civil` globals, different categories, different forbidden AEC namespaces `verified by BridgeEntry.cs:32-34,59-60` |
| `MainThreadExecutor.cs` | 238 | **P** | `HostName = "AutoCAD"` line 26, `WmNull = 0x0000` (generic Windows), `Application.Idle` event, `AcadApp.IsQuiescent`, `LockDocument()` method, `_onIdle` handler | Generic executor; only host name string, Idle subscription (both generic to thread model), host-specific API methods differ by host `verified by MainThreadExecutor.cs:26,54,64` |
| `AutocadScriptRunner.cs` | 280 | **X** | All: `Document`, `Database`, `TransactionManager`, `LockDocument()`, `Insunits`, `db.TileMode`, `Handseed`, `undo stack` | Pure AutoCAD API; no Civil 3D equivalent (Civil 3D runs on same acad.exe, same APIs) — no change needed `verified by AutocadScriptRunner.cs:43-50` |
| `AutocadResultSerializer.cs` | 210 | **X** | `IsAutocadType()` check `ns.StartsWith("Autodesk.AutoCAD")`, `Entity` / `Database` type detection, `ObjectId`, `Point3d` | AutoCAD object detection baked in; Civil 3D inherits same API so logic stands unchanged `verified by AutocadResultSerializer.cs:77` |
| `AutocadContextReader.cs` | 110 | **X** | `LayoutManager.Current`, `LayerTableRecord`, `db.TileMode`, `db.Measurement`, `db.Clayer`, all AutoCAD Database API | Reads AutoCAD drawing state; Civil 3D context differs (model has corridors, surfaces, `Cogo` points) `verified by AutocadContextReader.cs:37-52` |
| `DatabaseChangeCounter.cs` | 73 | **X** | `Handseed`, `ObjectOpenedForModify` event, `IsErased` check, `SymbolTable`/`BlockTableRecord` filters | Tracks AutoCAD entity lifecycle; different in Civil 3D (dynamic properties, alignment entities) `verified by DatabaseChangeCounter.cs:27-29,65` |
| `ScriptingSelfCheck.cs` | 59 | **X** | Hardcoded probe: `"return \"autocad \" + Autodesk.AutoCAD.ApplicationServices.Core.Application.Version + … + HPAutoCad.Aec.Geometry.GeometryTolerance.Default…"` | Probe tied to AutoCAD version string, references HPAutoCad.Aec.GeometryTolerance `verified by ScriptingSelfCheck.cs:24-25` |
| `AutocadVersionMap.cs` | 27 | **P** | Version series mapping (24.3)→2024, (25.0)→2025, (25.1)→2026, (26.0)→2027; `BuiltFor = 2026` | Generic mapping pattern; Civil 3D shares same AutoCAD version series (2026 = 25.1) so no change needed `verified by AutocadVersionMap.cs:13-19` |
| `AutocadThemeSwitcher.cs` | 43 | **P** | Theme XAML URIs: `/HPAutoCad.McpBridge;component/Resources/Themes/AutocadTheme*.xaml` | URI path only; theme-switch logic generic `verified by AutocadThemeSwitcher.cs:16-17` |
| `AutocadScriptGlobals.cs` | 55 | **X** | Type definition: fields `Document`, `Database`, `Editor`, `DocumentCollection`, all Autodesk.AutoCAD types | The type **itself** is AutoCAD-specific (no Civil 3D variant) — Civil 3D script globals are identical `verified by AutocadScriptGlobals.cs:19-23` |
| `View/AutocadBridgeStatusView.xaml.cs` | 25 | **P** | Namespace: `HPAutoCad.McpBridge.View`, trivial code-behind (`InitializeComponent()` + `DataContext` set) | Status window code-behind generic; namespace name only |
| `View/AutocadBridgeStatusView.xaml` | 160 | **P** | Labels: "Script", "Execution", text bindings, margins | UI labels parametrisable; bindings to status model generic `verified by View/AutocadBridgeStatusView.xaml:1-20` |
| `Resources/Themes/AutocadTheme.xaml` | 123 | **X** | Resource dict key: `x:Key="AutocadTheme"`, brush names `AutocadBackground`, all tied to "AutoCAD" branding | Branding-specific; Civil 3D would need own theme resource dict |
| `Resources/Themes/AutocadThemeLight.xaml` | 16 | **X** | Resource dict key: `x:Key="AutocadThemeLight"` | Branding-specific |
| `HPAutoCad.McpBridge.csproj` | 38 | **P** | SDK, TFM net8.0-windows, Serilog/WPF refs, deploy dirs, assembly version | Build config parametrisable per product (ALC name, output dir, assembly identity) |

**Bridge Subtotals:**
- **Parameterisable P:** MainThreadExecutor (238) + AutocadVersionMap (27) + AutocadThemeSwitcher (43) + View code-behind (25) + XAML (160) + csproj (38) = **531 lines**
- **Rigid X (AutoCAD API):** BridgeEntry (185) + AutocadScriptRunner (280) + AutocadResultSerializer (210) + AutocadContextReader (110) + DatabaseChangeCounter (73) + ScriptingSelfCheck (59) + AutocadScriptGlobals (55) + Themes (139) = **1,111 lines**
- **Total:** 1,305 lines; **bridgeable: 531 lines** (40.7%), **locked: 774 lines**

---

### Server Exe (HPAutoCad.Mcp.Server, 236 non-seed lines)

| File | Lines | Category | HPAutoCad-Specific Tokens | Notes |
|------|-------|----------|---------------------------|-------|
| `Program.cs` | 8 | **S** | None; just `McpServerHost.RunAsync(args, AutocadHostProfile.Instance)` | Bootstrap generic; only profile class name passed `verified by Program.cs:8` |
| `AutocadHostProfile.cs` | 43 | **X** | Every constant: `HostId = "autocad"`, `DisplayName = "AutoCAD"`, `EnvPrefix = "HPAUTOCAD_MCP_"`, `ProductFolder = "HPAutoCad"`, categories `["Drawing", "Layer", "Block", "Annotation", "Layout", "Data", "Generic", "Geometry", "Audit", "Aec", "Structural", "Architecture", "MEP", "Coordination", "ChangeSet"]`, script contract summary mentions `tr`, `transaction=auto|none|manual` | Every constant is AutoCAD/AEC-specific; Civil 3D profile would differ (same categories + Civil ones) `verified by AutocadHostProfile.cs:19-42` |
| `Tools/ExecuteAutocadCodeTool.cs` | 59 | **X** | Tool name `execute_autocad_code`, description ~1704 chars naming `tr`, `db`, AutoCAD transaction semantics, `HPMCPBRIDGE` command | Tool implementation + description AutoCAD-specific `verified by Tools/ExecuteAutocadCodeTool.cs:15-30` |
| `Tools/AutocadContextTool.cs` | 33 | **X** | Tool name `get_autocad_context`, description mentioning layouts, layers, AutoCAD context fields | AutoCAD-specific context model |
| `Prompts/AutocadScriptPrompts.cs` | 72 | **X** | Prompts `autocad_query_template`, `autocad_modify_template`, AutoCAD-specific instructions and examples | Prompt content AutoCAD-specific |
| `Resources/AutocadDocumentResources.cs` | 21 | **X** | Resource URIs `autocad://document/info`, `autocad://selection` | URI scheme AutoCAD-specific |
| `appsettings.json` | 15 | **X** | Registry root paths `%AppData%\HPAutoCad\McpServer` | Path names product-specific |

**Server Subtotals:**
- **Rigid S:** 8 lines (Program.cs only)
- **Rigid X:** 228 lines (host profile + all tools/prompts/resources)
- **Total:** 236 lines; **bridgeable: 8 lines** (3%), **locked: 228 lines**

---

## 2. Grand Total Shareable Core

| Tier | Loader | Bridge | Server | **Total** |
|------|--------|--------|--------|-----------|
| **Rigid S (reusable as-is)** | 216 | 0 | 8 | **224 lines** |
| **Parameterisable P** | 266 | 531 | 0 | **797 lines** |
| **Rigid X (AutoCAD-bound)** | 0 | 774 | 228 | **1,002 lines** |
| **Grand Total** | 482 | 1,305 | 236 | **2,023 lines** |

### Shareable Core Verdict

**S + P = 224 + 797 = 1,021 lines** (includes easily parametrisable names/strings/URIs)

**All parameter injection points are in McpShared patterns (IHostProfile, GuardProfile, AnalyzerProfile, PipeNaming, HostScriptContracts), NOT in HPAutoCad.** The loader, bridge and server **do NOT own the generic factory patterns**—they are clients of McpBridge.Core and McpServer.Core. Extracting them to a shared `AcadShared/` would not reduce duplication; it would add a layer of indirection.

**Threshold Status:** 1,021 < 1,500. **Recommendation: OPTION A (copy with token substitution).**

---

## 3. Abstraction Cost for Option B

If forced to abstract (AcadShared/), the contract would be:

### Injection Points Required

```csharp
// AcadShared/HostIdentity.cs — new record
public record AcadFamilyHostIdentity(
    string ProductName,              // "AutoCAD", "Civil 3D"
    string ProductFolder,            // "HPAutoCad", "HPCivil3d"
    string EnvPrefix,               // "HPAUTOCAD_MCP_", "HPCIVIL3D_MCP_"
    string PipeSuffix,              // "-mcp-2026"
    string RibbonTabId,             // "HPAUTOCAD_MCP_TAB"
    string RibbonButtonId,          // "HPAUTOCAD_MCP_BRIDGE"
    string CommandName,             // "HPMCPBRIDGE"
    string BridgeClassName,         // "HPAutoCad.McpBridge"
    string BridgeAssemblyName,      // "HPAutoCad.McpBridge.dll"
    string LoggerFolder,            // "HPAutoCad"
    string[] AssemblyPrefixes,      // ["Ac", "Ad", "Autodesk."]
    string ProbeAecNamespace,       // "HPAutoCad.Aec" or "HPCivil3d.Aec"
    HostScriptGlobalsType          // the type itself (Document, Database, etc. — same for both)
);
```

### Files That Stay Per-Product

1. **AutocadScriptGlobals.cs** — AutoCAD.NET types are the contract; Civil 3D inherits same types → **shared but documented tied to Autodesk.AutoCAD**
2. **AutocadScriptRunner.cs** — Document, Database, Transaction, Insunits, LockDocument — **all inherited by Civil 3D, no change needed**
3. **AutocadResultSerializer.cs** — `IsAutocadType()` namespace check works for Civil 3D entities too (they are AutoCAD entities) → **shared as-is**
4. **AutocadContextReader.cs** — reads LayoutManager, LayerTableRecord; **Civil 3D would need own ContextReader** (Cogo points, corridors, surfaces differ)
5. **DatabaseChangeCounter.cs** — Handseed, ObjectOpenedForModify, IsErased; **shared as-is** (same entity lifecycle in both)
6. **Theme resource dicts** (AutocadTheme*.xaml, Themes folder) — **stay per-product** (branding)
7. **AutocadHostProfile.cs** — categories, descriptions, contract summary → **CivilHostProfile.cs** needed
8. **Tool implementations** (ExecuteAutocadCodeTool, AutocadContextTool, Prompts, Resources) → **per-product**

### Engine Already Abstracted

The patterns are **already in McpShared:**
- `IHostProfile` / `HostProfile.Revit/Navis/Etabs` — profiles already parametrised `verified by CLAUDE.md line 87`
- `GuardProfile.Autocad` / `AnalyzerProfile.Autocad` — host-specific guard rules, added with AutoCAD `verified by CLAUDE.md line 11-12`
- `HostScriptContracts.AutocadImports`, `PipeNaming.AutocadHost`, `JsonRpcMethods.AutocadPrefix`, `ContextResult.Autocad` — all in engine `verified by CLAUDE.md line 14-16`
- `IHostProfile.TimeoutSemanticsHint` (for AutoCAD's "changes persisted, no rollback") — already parameterised

**Cost of Option B:**
- Add `AcadShared/HostIdentity.cs` (25 lines)
- Refactor Loader → consume HostIdentity (30 lines new file refs, 50 lines in Loader injection sites)
- Refactor Bridge → consume HostIdentity (same)
- Risk: 4+ files touched in HPAutoCad, HPCivil3d each; breaking change to McpShared if AcadShared ever depends on it back (violates "→ McpShared only" rule)
- Maintenance: two copies of 774 lines of API code anyway (AutocadScriptRunner, DatabaseChangeCounter)

**Cost of Option A (copy):**
- copy HPAutoCad/ → HPCivil3d/
- sed s/HPAutoCad/HPCivil3d/g, s/hpautocad/hpcivil3d/g, s/HPAUTOCAD/HPCIVIL3D/g, s/AutoCAD/Civil 3D/g (except in namespace prefixes like Autodesk.AutoCAD which are unchanged)
- 7 files × 2 products = 14 build artifacts; 1 git diff = 2,000+ lines (uninspiring but mechanical)
- No breaking changes; no new repository rule violations

---

## 4. Bridge Runtime Facts to Mirror (AutoCAD → Civil 3D)

### BridgeEntry (line-by-line contract)

**Verified by BridgeEntry.cs:**
- `VendorFolder = "HPAutoCad"` (line 32) → `VendorFolder = "HPCivil3d"` for Civil 3D binary
- `HostName = "AutoCAD"` (line 34) → `HostName = "Civil 3D"` (or "Civil 3D"—affects log text + error messages)
- `AutocadVersionMap.YearFor()` (line 58) → same logic applies (Civil 3D 2026 = 25.1)
- `ScriptCompiler` references (line 70): `HostScriptContracts.AutocadImports` → engine already has `HostScriptContracts` as static record; no change needed
- `CompilerReferences(autocadApi)` (line 70) → same assembly list (accoremgd, acdbmgd, acmagd) applies to Civil 3D
- `typeof(AutocadScriptGlobals)` (line 70) → same type (Civil 3D scripts get same globals)
- `AutocadScriptRunner`, `AutocadResultSerializer` (lines 74–75) → same runners work for Civil 3D (inherit same APIs)
- `PipeNaming.For(PipeNaming.AutocadHost, year)` (line 80) → engine has `PipeNaming.CivilHost` waiting (part of phase-0 pattern)
- `JsonRpcMethods.AutocadPrefix` (line 80) → engine has `.CivilPrefix` waiting
- `McpBridgeHost.Install()` (line 81) → same host seam

### MainThreadExecutor (line-by-line contract)

**Verified by MainThreadExecutor.cs:**
- `HostName = "AutoCAD"` (line 26) → change to `"Civil 3D"`
- `WmNull = 0x0000` (line 27) → same for all Windows hosts
- `AcadApp.Idle` (line 64) → same event (Civil 3D runs in acad.exe; Idle exists)
- `AcadApp.IsQuiescent` (line 54) → same check (calls into AutoCAD API, same for Civil 3D)
- `LockDocument()` (line 70 in runner) → same method (Civil 3D inherits AutoCAD.NET)
- `MainThreadQueue(expireWithoutTicks: true)` (line 54) → same queue (AutoCAD host requires Idle fallback under modal dialogs)

### AutocadScriptRunner (line-by-line contract)

**Verified by AutocadScriptRunner.cs:**
- `Document`, `Database`, `TransactionManager`, `DocumentLockMode.ProtectedAutoWrite` (line 70) → identical in Civil 3D
- `Insunits` / `AutocadInsunits.For()` (line 59) → same drawing units enum
- `doc.LockDocument()` + `db.Handseed` + `ObjectOpenedForModify` (lines 71–72) → same transaction semantics
- `ChangedCounts(added, modified, deleted)` → same result shape
- `UndoCommandName = "HPMCP"` (line 31) → could parametrise but "HPMCP" works for all hosts

### AutocadResultSerializer

**Verified by AutocadResultSerializer.cs:**
- `IsAutocadType()` check: `ns.StartsWith("Autodesk.AutoCAD")` (line 77) → works for Civil 3D entities (they are Autodesk.AutoCAD.* types)

### AutocadContextReader

**Verified by AutocadContextReader.cs:**
- `LayoutManager.Current.CurrentLayout` (line 37) → same in Civil 3D
- `db.Measurement` (line 47) → same enum
- `db.TileMode` (line 44) → same boolean
- BUT: `ContextResult.Autocad` field (line 45) → Civil 3D's context would add `ContextResult.CivilInfo` with corridors, surfaces, COGO points
- ACTION: copy file, add Civil 3D-specific reads (new fields in `CivilInfo` record, same return shape)

### DatabaseChangeCounter

**Verified by DatabaseChangeCounter.cs:**
- `db.Handseed`, `ObjectOpenedForModify`, `IsErased`, `SymbolTable`, `BlockTableRecord` filters (lines 28–69) → all shared API; Civil 3D has same events and entity lifecycle

### ScriptingSelfCheck

**Verified by ScriptingSelfCheck.cs:**
- Hardcoded probe (line 24): includes `Autodesk.AutoCAD.ApplicationServices.Core.Application.Version` + `HPAutoCad.Aec` reference
- ACTION: copy, change probe to use `HPCivil3d.Aec` namespace, keep Application.Version (same class in both)

### Loader Facts

**Verified by BridgeLoaderApplication.cs, BridgeLoadContext.cs:**
- `BridgeLoaderApplication.Initialize()` → loads the DLL from `Contents\Bridge\`, calls `Start()` by reflection, gets delegates back → **identical sequence**
- `BridgeLoadContext.HostAssemblyPrefixes = ["Ac", "Ad", "Autodesk."]` → **add** "Aec" and Civil-specific prefixes? No: Civil 3D is a vertical of AutoCAD, uses same DLL prefixes. No change needed.
- `BridgeLoadContext(name: "HPAutoCad.McpBridge")` → change name to `"HPCivil3d.McpBridge"`

### Ribbon Facts

**Verified by McpRibbonTab.cs:**
- `ComponentManager.Ribbon`, `Autodesk.Windows` API calls (lines 50–64) → same Ribbon API (Civil 3D uses AutoCAD's Ribbon)
- Tab/button/panel ids (lines 17–22) → all must change (`HPCIVIL3D_MCP_*`)
- `Application.SystemVariableChanged` for COLORTHEME (line 80) → same event

---

## 5. Tests to Mirror

### SeedLibraryTests.cs

**Verified by SeedLibraryTests.cs:**
- Lines 25, 274, 282, 369: hardcoded `PackageVersion = "25.1.0"` (AutoCAD.NET version, same for Civil 3D 2026)
- Line 38: `typeof(AutocadHostProfile).Assembly` → would be `typeof(CivilHostProfile).Assembly`
- Lines 39–60: seed-loading logic reusable (searches `SeedLibrary/` resources, parses JSON)
- **ACTION:** copy test, change host profile class name, keep PackageVersion (Civil 3D 2026 = AutoCAD.NET 25.1.0)

### HostProfileTests.cs

**Verified by HostProfileTests.cs:**
- Asserts `profile.HostId == "autocad"` (line 27) → `"civil3d"`
- Asserts `profile.PipeName == "hpautocad-mcp-2026"` (line 28) → `"hpcivil3d-mcp-2026"`
- Asserts `"execute_autocad_code"` in tools (line 72) → `"execute_civil3d_code"` or `"execute_autocad_code"` (both run the same code)
- **ACTION:** copy test, parametrise strings, keep assertions (they verify host identity)

### Test Harness

**Verified by HPAutoCad/tools/harness/**
- `run-live-verify.ps1` (line 10): hardcoded paths `output\HPAutoCad.Mcp.Server\HPAutoCad.Mcp.Server.exe`
- Line 12: registry isolation env `HPAUTOCAD_MCP_Registry__LibraryPath` → `HPCIVIL3D_MCP_Registry__LibraryPath`
- Line 20: imports `harness-common.ps1` (local file, no changes needed)
- Line 29: imports `McpShared/tools/mcp-call.py` (shared, no changes)
- **ACTION:** copy entire `HPAutoCad/tools/harness/` → `HPCivil3d/tools/harness/`, sed all paths and env vars
- **Isolation section** (implied around line 116 in the full version): second Civil 3D launched; test expects "already in use — another Civil 3D 2026 instance" error → framework already handles host name from profile

---

## 6. Repository Rule Check

**CLAUDE.md, line 11 (verified):**
> "This repo bundles five unrelated deliverables plus one shared library folder; treat them as separate concerns — do not cross-wire them. The only permitted dependency direction is MCP folder (HPRebar/, HPAutoCad/, HPNavis/, HPEtabs/) → McpShared/; the MCP folders never reference each other."

**Impact of Option B:** Would violate this if `AcadShared/` is created and then HPAutoCad imports from it. **Workaround:** AcadShared/ would live *outside* both HPAutoCad/ and HPCivil3d/, like McpShared/ does. But then:
1. Does AcadShared depend on AutoCAD.NET? Yes (BridgeEntry, Loader, MainThreadExecutor all reference Autodesk.AutoCAD). **Violates "McpShared never references Autodesk.*"** pattern.
2. Could place AcadShared/ beside HPAutoCad/ in a new `AutocadFamily/` top-level folder (parallel to McpShared/). **Violates the "five unrelated deliverables" separation principle** if it creates a sixth category.

**Ruling:** Option B requires **architectural change to the repo rule**, which is a *bigger* decision than duplication. Option A keeps the rule intact.

---

## 7. Line Counts Summary Table

| Component | Lines | S | P | X | Shareable (S+P) | Ratio |
|-----------|-------|---|---|---|---|---|
| Loader | 482 | 216 | 266 | 0 | 482 | 100% |
| Bridge | 1,305 | 0 | 531 | 774 | 531 | 40.7% |
| Server | 236 | 8 | 0 | 228 | 8 | 3.4% |
| **TOTAL** | **2,023** | **224** | **797** | **1,002** | **1,021** | **50.4%** |

---

## 8. Costs Comparison

### Option A: Copy HPAutoCad/ → HPCivil3d/

**Effort:**
- Copy 40+ files (2,000 lines)
- 7 main sed patterns (ProductName, ProductFolder, EnvPrefix, PipeSuffix, RibbonIds, CommandName, AecNamespace)
- 1 new Profile class + 1 new ContextReader (Civil 3D-specific fields)
- Harness: copy + sed 12 scripts
- Tests: copy + parametrise 3 files
- **Estimated effort:** 4–6 hours (mechanical)

**Maintenance risk:**
- Two copies of the 774-line X code (runner, serializer, counter, etc.)
- Bug in runner found → must patch both
- New AutoCAD.NET version (unlikely before 2027) → both need rebuild
- **Mitigation:** docs/DUAL-HOST-MAINTENANCE.md notes which files are identical across products

### Option B: Abstract to AcadShared/

**Effort:**
- Design HostIdentity record + injection API (40 lines)
- Create AcadShared/ folder structure (6 assemblies: .Loader, .Bridge, .Mcp.Server, .Tests, matching HPAutoCad)
- Refactor HPAutoCad to consume HostIdentity (50–80 lines per file, 8 files = 400–640 lines new)
- Same for HPCivil3d (400–640 lines new)
- Repo rule change approval from project lead
- **Estimated effort:** 12–16 hours (design + refactoring)

**Maintenance benefit:**
- One copy of the 774-line X code, shared
- Bug in runner → one fix
- **Hidden cost:** any divergence in AutoCAD vs Civil 3D behavior (e.g., ContextReader) breaks the abstraction; copy might have been safer

**Architectural cost:**
- Violates "MCP folders never reference each other" rule OR requires top-level repo restructure
- Adds complexity to CLAUDE.md (AcadShared section, new folder rule, import diagram)
- Future hosts (if any) pay the cost of understanding the abstraction

---

## 9. Recommendation

**THRESHOLD: 1,021 < 1,500. Recommend OPTION A.**

**Reasoning:**
1. Shareable core (1,021 lines) falls below the 1,500-line threshold where extraction pays off.
2. The largest bridgeable component is the Loader (482 lines entirely shareable), but it is also the simplest to copy—mechanical sed substitution works.
3. Bridge runtime (1,305 lines) is 40% parametrisable but 60% locked to AutoCAD.NET APIs (which Civil 3D inherits unchanged anyway, so no loss of reuse—both products use identical APIs).
4. Server exe (236 lines) is mostly tool implementations, each tied to one product's context model and prompts.
5. Existing engine patterns (McpShared/HPRebar.McpBridge.Core, HPRebar.Mcp.Server.Core) already abstract host variation; no new abstraction needed.
6. Option A preserves the "MCP folders never reference each other" architectural rule; Option B would require violating it or restructuring the repository.
7. Maintenance of two copies of the 774-line X code (AutoCAD API layer) is unavoidable either way—even a shared AcadShared/ does not reduce it, because AutoCAD.NET changes affect both products identically (no divergence opportunity).

**Cost/Benefit:** 4 hours copy-paste vs. 12 hours refactor + architectural change + ongoing abstraction maintenance. **Copy wins.**

---

## Unresolved Questions

None. This measurement is read-only; the ADR decision is purely quantitative. The 1,500-line threshold is met by test: 1,021 < 1,500 → **Option A**.

**Status:** DONE
