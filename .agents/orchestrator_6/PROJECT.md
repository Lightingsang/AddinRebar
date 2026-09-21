# Project: HPExcel MCP Ecosystem

## Architecture
The HPExcel deliverable provides an end-to-end Model Context Protocol (MCP) subsystem enabling AI coding agents and LLMs to interact safely, performantly, and expressively with Microsoft Excel.

### 1. Architectural Isolation & Boundaries
- Reference Direction: `HPExcel/` references strictly `../McpShared/` projects (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`).
- Absolute Isolation: `HPExcel/` NEVER cross-references sibling host projects (`HPRebar`, `HPAutoCad`, `HPNavis`, `HPEtabs`, `HPCivil3d`, `HPSap2000`, `HPPowerBi`).
- Shared Host Neutrality: All shared contracts, pipe naming rules, JSON-RPC prefixes, context shaping, and security guard profiles reside in `McpShared/` without adding external Office/Excel dependencies to `McpShared`.

### 2. Component Architecture
```
┌────────────────────────────────────────────────────────┐
│               LLM / AI Client (Stdio)                  │
└───────────────────────────┬────────────────────────────┘
                            │ Stdio JSON-RPC
                            ▼
┌────────────────────────────────────────────────────────┐
│            HPExcel.Mcp.Server (.NET 10)                │
│   - ExcelHostProfile (hpexcel-mcp-2026)                │
│   - Core Tools (get_excel_context, execute_excel_code) │
│   - 12 Embedded Seed Tools Catalog                     │
│   - 8 Registry Meta Tools                              │
└───────────────────────────┬────────────────────────────┘
                            │ Named Pipe: hpexcel-mcp-2026
                            ▼
┌────────────────────────────────────────────────────────┐
│          HPExcel.McpBridge (.NET 8 Windows WPF)        │
│   - MaterialDesign 5.3.2 UI with Auto Dark/Light Theme │
│   - PipeListener + RequestDispatcher                   │
│   - 3-Tier Safety Gating (R / W / D)                   │
│   - ExcelSnapshotManager (.hpexcel_snapshots/)         │
└─────────────┬────────────────────────────┬─────────────┘
              │                            │
   Target Workbook Open in Excel           │ Target File Closed / Excel Not Running
              ▼                            ▼
┌───────────────────────────┐  ┌─────────────────────────┐
│     COM Interop Engine    │  │     ClosedXML Engine    │
│ - Dedicated STA Worker    │  │ - Direct OpenXML Access │
│ - ROT & GetActiveObject   │  │ - Headless Processing   │
│ - IMessageFilter Retries  │  │ - Offline Reading/Write │
│ - Live GUI Interaction    │  │ - No Office Dependency  │
│ - VBA Macro Execution     │  │ - Fast In-Memory Calc   │
└───────────────────────────┘  └─────────────────────────┘
```

### 3. Safety & Snapshot Engine
- **Tier R (Read)**: `read_range`, `read_worksheet_info`, `find_cells`, `read_table`, `evaluate_formula`, `get_excel_context`. Gated only by execution toggle; no snapshot required.
- **Tier W (Write)**: `write_range`, `format_range`, `create_table`, `create_chart`, `export_worksheet`, `manage_worksheet` (add, rename, duplicate, hide). Gated by Write toggle; triggers automatic `.xlsx` snapshot.
- **Tier D (Destructive)**: Sheet deletion, clearing cells, `run_macro`. Gated by Destructive toggle (memory-only, off by default); triggers automatic `.xlsx` snapshot.
- **Snapshot Storage**: Saved to `.hpexcel_snapshots/` adjacent to workbook (fallback to `%TEMP%\.hpexcel_snapshots\`). Retains newest 20 snapshots. Snapshot filename returned in `ExecuteResult.Snapshot`.

---

## Feature Inventory

Every feature from requirements and survey is cataloged and assigned to a milestone below:

| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | PipeNaming.ExcelHost | Add ExcelHost ("excel") and mapping to "hpexcel-mcp-{version}" in McpShared | M1 | Survey 1 |
| 2 | JsonRpcMethods.ExcelPrefix | Add ExcelPrefix ("excel.") in McpShared | M1 | Survey 1 |
| 3 | HostScriptContracts.Excel | Add ExcelImports, ExcelGlobals, and ExcelHeavyMaxTimeoutSeconds (600s) | M1 | Survey 1 |
| 4 | ContextMessages.ExcelInfo | Add ExcelInfo DTO and ContextResult.Excel property in McpShared | M1 | Survey 1 |
| 5 | GuardProfile.Excel & AnalyzerProfile.Excel | Add Excel safety profile (blocking Quit, dialogs, bridge internals) & empty analyzer | M1 | Survey 1 |
| 6 | McpShared Excel Unit Tests | Add ExcelTestProfile and ExcelProfileTests in Mcp.Server.Core.Tests | M1 | Survey 1 |
| 7 | HPExcel Solution Configuration | Create HPExcel.slnx, Directory.Build.props, global.json, README.md | M2 | Survey 2 |
| 8 | McpBridge Project Skeleton | Setup HPExcel.McpBridge.csproj (net8.0-windows, WPF, MaterialDesign 5.3.2, ClosedXML, Interop) | M2 | Survey 2 |
| 9 | COM Attachment & STA Worker | ExcelAttachment, ExcelStaWorker, ComInteropHelper, oleaut32!GetActiveObject, IMessageFilter | M2 | Survey 2 |
| 10 | Headless ClosedXML Engine | ClosedXmlWorkbookService for offline reading, writing, table creation, and formulas | M2 | Survey 2 |
| 11 | 3-Tier Safety Engine | ExcelSafetyGuard, ExcelTierTable, ExcelTierAnalyzer, UI cascading checkboxes, PREVIEW mode | M2 | Survey 2 |
| 12 | Automatic Snapshot Engine | ExcelSnapshotManager (.hpexcel_snapshots/), SaveCopyAs / file-copy, retention pruning | M2 | Survey 2 |
| 13 | MaterialDesign 5.3.2 UI | MainWindow.xaml, MainWindowViewModel.cs, MaterialThemeBridge, WindowsHostTheme, Excel green brand | M2 | Survey 2 |
| 14 | Bridge Pipe Listener & Dispatcher | PipeListener on hpexcel-mcp-2026, ExcelDispatcher, ExcelBridgeExecutor | M2 | Survey 2 |
| 15 | Mcp.Server Project Skeleton | HPExcel.Mcp.Server.csproj (.NET 10 console), Program.cs, appsettings.json, ExcelHostProfile | M3 | Survey 1/3 |
| 16 | Core Tool: get_excel_context | Context tool returning active workbook, sheet, selection, open files, Excel version | M3 | Survey 3 |
| 17 | Core Tool: execute_excel_code | Roslyn C# script tool with excel, workbook, sheet, closedXml, args, log, progress, ct | M3 | Survey 3 |
| 18 | Registry Meta Tools | 8 dynamic tool registry meta tools integrated from McpShared | M3 | Survey 1/3 |
| 19 | Seed Tool: read_range | Read cell values, formulas, or formatted text into JSON (Tier R, 30s) | M3 | Survey 3 |
| 20 | Seed Tool: read_worksheet_info | Enumerate worksheets, used ranges, tables, charts, visibility (Tier R, 30s) | M3 | Survey 3 |
| 21 | Seed Tool: find_cells | Search text/numbers/formulas across worksheet or workbook (Tier R, 30s) | M3 | Survey 3 |
| 22 | Seed Tool: read_table | Read structured ListObject data rows and totals row into JSON (Tier R, 30s) | M3 | Survey 3 |
| 23 | Seed Tool: write_range | Batch write 2D arrays/formulas with auto-sizing and snapshot (Tier W, 60s) | M3 | Survey 3 |
| 24 | Seed Tool: format_range | Apply number formats, fonts, hex colors, borders, alignment (Tier W, 60s) | M3 | Survey 3 |
| 25 | Seed Tool: manage_worksheet | Add, rename, duplicate, delete, hide/unhide worksheets (Tier W/D, 60s) | M3 | Survey 3 |
| 26 | Seed Tool: create_table | Convert range to structured ListObject with style and totals (Tier W, 60s) | M3 | Survey 3 |
| 27 | Seed Tool: create_chart | Create embedded Column, Line, Pie, Bar, Area, Scatter chart (Tier W, 60s) | M3 | Survey 3 |
| 28 | Seed Tool: evaluate_formula | Evaluate dynamic Excel formula expression with error detection (Tier R, 30s) | M3 | Survey 3 |
| 29 | Seed Tool: export_worksheet | Export worksheet/workbook to PDF or CSV with page setup (Tier W, 60s) | M3 | Survey 3 |
| 30 | Seed Tool: run_macro | Execute VBA macro with parameters and snapshot (Tier D, 120s) | M3 | Survey 3 |
| 31 | Test Suite: Mcp.Server.Tests | Catalog completeness, schema tests, FakeExecutor round-trips, Roslyn compilation of all 12 seeds | M4 | Survey 3 |
| 32 | Test Suite: McpBridge.Tests | Safety tier classification, snapshot manager backup/restore, ClosedXML headless tests, pipe tests | M4 | Survey 3 |
| 33 | Ecosystem Documentation | .agents/skills/hp-mcp-excel/SKILL.md documenting tools, schemas, workflows | M5 | Context |
| 34 | AGENTS.md Registration | Register HPExcel deliverable in repo AGENTS.md table and guidelines | M5 | Context |
| 35 | E2E Dual Track Final Verification | Pass 100% of automated tests and clean solution build of HPExcel.slnx | M6 | Context |

---

## Milestones

| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | McpShared Extension | Register Excel host in McpShared (PipeNaming, JsonRpcMethods, HostScriptContracts, ContextMessages, GuardProfile, AnalyzerProfile, ExcelProfileTests) | none | DONE |
| M2 | HPExcel Bridge Engine & UI | Build HPExcel.McpBridge: Solution setup, COM Interop (STA worker, ROT), ClosedXML headless engine, 3-tier safety engine, snapshot backup manager, MaterialDesign 5.3.2 WPF UI | M1 | DONE |
| M3 | HPExcel Stdio Server & Tools | Build HPExcel.Mcp.Server: ExcelHostProfile, Core Tools (`get_excel_context`, `execute_excel_code`), 12 embedded seed tools with schemas, examples, and Roslyn scripts | M1 | DONE |
| M4 | Automated Test Suites | Build and verify HPExcel.Mcp.Server.Tests (.NET 10) and HPExcel.McpBridge.Tests (.NET 8 Windows) | M2, M3 | DONE |
| M5 | Skill & Repo Documentation | Create `.agents/skills/hp-mcp-excel/SKILL.md` and update `AGENTS.md` | M3, M4 | DONE |
| M6 | Final Verification & E2E Track | Execute clean solution build of `HPExcel.slnx` (0 errors), run all test suites (100% pass), and verify dual-track compliance | M1, M2, M3, M4, M5 | DONE |

---

## Interface Contracts

### 1. Server ↔ Bridge Named Pipe Wire Contract
- Named Pipe: `hpexcel-mcp-2026` (via `PipeNaming.For(PipeNaming.ExcelHost, 2026)`)
- Wire Methods:
  - `excel.ping` -> `PingResult`
  - `excel.context` -> `ContextResult` (with `ExcelInfo`)
  - `excel.execute` -> `ExecuteResult` (with `Snapshot` filename)
  - `excel.cancel` -> `CancelResult`
  - `excel.analyze` -> `AnalyzeResult` (with AST tier classification)

### 2. Context Service Contract (`ContextResult.Excel` / `ExcelInfo`)
```csharp
public sealed record ExcelInfo(
    bool IsAttached,
    int? AttachedPid,
    string? ExcelVersion,
    string? ActiveWorkbookName,
    string? ActiveWorksheetName,
    string? SelectionAddress,
    bool WriteEnabled,
    bool DestructiveEnabled,
    int OpenWorkbookCount,
    int WorksheetCount,
    bool HasActiveWorkbook);
```

### 3. Roslyn Script Environment Contract (`execute_excel_code`)
- Globals:
  - `excel`: `Microsoft.Office.Interop.Excel.Application` (or bridge helper in headless mode)
  - `workbook`: `Microsoft.Office.Interop.Excel.Workbook` (or ClosedXML `IXLWorkbook`)
  - `sheet`: `Microsoft.Office.Interop.Excel.Worksheet` (or ClosedXML `IXLWorksheet`)
  - `closedXml`: `ClosedXmlHelper`
  - `args`: `ScriptArgs`
  - `log`: `Action<string>`
  - `progress`: `Action<int, int?, string?>`
  - `ct`: `CancellationToken`
- Default Usings: `System`, `System.Linq`, `System.Collections.Generic`, `Microsoft.Office.Interop.Excel`, `ClosedXML.Excel`, `HPRebar.McpBridge.Core.Scripting`
- Guard Deny-List: `Quit`, `ApplicationExit`, `InputBox`, `GetOpenFilename`, `GetSaveAsFilename`, `MessageBox`, `System.Diagnostics.Process`, `#r`/`#load` directives, reflection, threading.

---

## Code Layout

```
HPExcel/
├── HPExcel.slnx
├── Directory.Build.props
├── global.json
├── README.md
├── HPExcel.McpBridge/
│   ├── HPExcel.McpBridge.csproj
│   ├── Program.cs
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── BridgeEntry.cs
│   ├── Discovery/
│   │   ├── ExcelProcessDetector.cs
│   │   └── ExcelInstanceInfo.cs
│   ├── Com/
│   │   ├── ExcelAttachment.cs
│   │   ├── ExcelStaWorker.cs
│   │   └── ComInteropHelper.cs
│   ├── Headless/
│   │   └── ClosedXmlWorkbookService.cs
│   ├── Safety/
│   │   ├── ExcelSafetyGuard.cs
│   │   ├── ExcelTier.cs
│   │   ├── ExcelTierTable.cs
│   │   ├── ExcelTierAnalyzer.cs
│   │   └── ExcelSnapshotManager.cs
│   ├── Host/
│   │   ├── ExcelBridgeExecutor.cs
│   │   ├── ExcelDispatcher.cs
│   │   └── ExcelScriptGlobals.cs
│   ├── ViewModels/
│   │   └── MainWindowViewModel.cs
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   └── Resources/Themes/
│       ├── ThemeInfo.cs
│       ├── IHostTheme.cs
│       ├── WindowsHostTheme.cs
│       ├── MaterialThemeBridge.cs
│       ├── MaterialBridge.xaml
│       ├── ThemeLight.xaml
│       ├── ThemeDark.xaml
│       └── ExcelTheme.xaml
├── HPExcel.McpBridge.Tests/
│   ├── HPExcel.McpBridge.Tests.csproj
│   ├── SafetyGatingTests.cs
│   ├── ExcelTierAnalyzerTests.cs
│   ├── ExcelSnapshotManagerTests.cs
│   ├── ClosedXmlHeadlessTests.cs
│   └── ExcelDispatcherTests.cs
├── HPExcel.Mcp.Server/
│   ├── HPExcel.Mcp.Server.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Hosts/Excel/
│   │   ├── ExcelHostProfile.cs
│   │   ├── ExcelContextService.cs
│   │   └── Tools/
│   │       ├── GetExcelContextTool.cs
│   │       └── ExecuteExcelCodeTool.cs
│   └── Registry/SeedLibrary/
│       ├── read_range/
│       ├── read_worksheet_info/
│       ├── find_cells/
│       ├── read_table/
│       ├── write_range/
│       ├── format_range/
│       ├── manage_worksheet/
│       ├── create_table/
│       ├── create_chart/
│       ├── evaluate_formula/
│       ├── export_worksheet/
│       └── run_macro/
└── HPExcel.Mcp.Server.Tests/
    ├── HPExcel.Mcp.Server.Tests.csproj
    ├── ExcelHostProfileTests.cs
    ├── SeedCatalogTests.cs
    ├── SeedExecutionTests.cs
    └── SeedCompilationTests.cs
```
