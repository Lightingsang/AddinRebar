# Survey Report: HPExcel MCP Ecosystem Architecture, Hybrid COM/ClosedXML Engine, 12 Seed Tools & Test Suites

**Agent**: Survey Explorer 3 (`explorer_survey_3`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_3`  
**Date**: 2026-09-21  
**Scope**: Excel COM Interop & ClosedXML hybrid models, 12 embedded seed tools specifications, core tools architecture (`get_excel_context`, `execute_excel_code`), and test suite patterns (`HPExcel.Mcp.Server.Tests`, `HPExcel.McpBridge.Tests`).

---

## 1. Observation

### 1.1 Architectural Lineage in Repository
Across the repository's MCP deliverables (`HPRebar`, `HPAutoCad`, `HPNavis`, `HPEtabs`, `HPCivil3d`, `HPSap2000`, `HPPowerBi`), two bridge hosting models exist:
1. **In-Process Add-in Bridges**: `HPRebar.McpBridge` (Revit add-in DLL), `HPAutoCad.McpBridge` (AutoCAD bundle DLL), `HPNavis.McpBridge` (Navisworks .NET 4.8 plugin), `HPCivil3d.McpBridge` (Civil 3D vertical bundle DLL).
2. **Out-of-Process Standalone Desktop WPF Bridges**: `HPEtabs.McpBridge` (COM out-of-process via `ETABSv1.dll`), `HPSap2000.McpBridge` (COM out-of-process via `SAP2000v1.dll`), `HPPowerBi.McpBridge` (Tabular AMO-TOM / ADOMD.NET out-of-process).

Both `HPEtabs` and `HPPowerBi` establish the pattern for **out-of-process bridges**:
- A standalone WPF desktop app (`net8.0-windows`) that manages connection to the host process, hosts the Named Pipe server (`PipeListener`), and provides a UI with safety checkboxes.
- A .NET 10 console server (`HP*.Mcp.Server`) speaking JSON-RPC stdio to LLMs/clients, backed by `HPRebar.Mcp.Server.Core`.
- Architectural isolation: each deliverable references only `../McpShared/` and never cross-references sibling hosts.
- Shared contracts: `PipeNaming.cs`, `HostScriptContracts.cs`, `ContextMessages.cs`, `JsonRpcMethods.cs`.

### 1.2 Excel Interop Peculiarities & Constraints
1. **.NET 8 COM Marshaling**: In modern .NET (`net8.0-windows`), `Marshal.GetActiveObject` is not present in `System.Runtime.InteropServices.Marshal`. Late binding or P/Invoke (`oleaut32.dll!GetActiveObject` or `ole32.dll!GetRunningObjectTable`) is required.
2. **Excel ROT Registration**: Excel registers running workbooks in the Windows Running Object Table (ROT) under file paths (e.g. `C:\Docs\Sheet.xlsx` or `!Book1`). ROT traversal (`IRunningObjectTable`, `IEnumMoniker`) allows enumerating and attaching to any open workbook and locating its parent `Excel.Application` PID.
3. **STA Thread Requirement**: Excel COM is an STA (Single-Threaded Apartment) OLE Automation server. Calling Excel COM methods from multi-threaded task pools results in `0x8001010E (RPC_E_WRONG_THREAD)`. All COM calls must be marshaled to a dedicated STA worker thread (exactly as done in `HPEtabs.McpBridge` via `EtabsAttachment`).
4. **Excel Modal & Edit States**: When a user is actively editing a cell formula or when a modal dialog is open, Excel rejects COM requests with `0x80010001 (RPC_E_CALL_REJECTED)` or `0x8001010A (RPC_E_SERVERCALL_RETRYLATER)`. A message filter (`IMessageFilter`) or quiescence check is required.
5. **RCW Leaks**: Unreleased COM references (`Marshal.ReleaseComObject`) prevent `EXCEL.EXE` from terminating when closed by the user, creating zombie processes.
6. **Headless ClosedXML**: `ClosedXML` (.NET 8 / standard) operates on `.xlsx` files directly via OpenXML without requiring `EXCEL.EXE`, COM registration, or Microsoft Office installation. However, ClosedXML cannot execute live VBA macros (`run_macro`) or interact with an active unsaved UI session.

---

## 2. Logic Chain & Technical Design

### 2.1 Hybrid COM Interop + ClosedXML Architectural Model

```
                     ┌─────────────────────────────────────────────────────┐
                     │            LLM / Client (MCP Stdio)                 │
                     └──────────────────────────┬──────────────────────────┘
                                                │ Stdio JSON-RPC
                                                ▼
                     ┌─────────────────────────────────────────────────────┐
                     │              HPExcel.Mcp.Server (net10)             │
                     │   - Profile: ExcelHostProfile (hpexcel-mcp-2026)    │
                     │   - Core Tools + 12 Seed Tools Catalog              │
                     └──────────────────────────┬──────────────────────────┘
                                                │ Named Pipe (hpexcel-mcp-2026)
                                                ▼
                     ┌─────────────────────────────────────────────────────┐
                     │         HPExcel.McpBridge (.NET 8 Windows)          │
                     │   - WPF MaterialDesign UI + Safety Checkboxes       │
                     │   - PipeListener + RequestDispatcher                │
                     │   - 3-Tier Safety Gating (R / W / D)                │
                     │   - ExcelSnapshotManager (.hpexcel_snapshots/)      │
                     └───────────────┬─────────────────────┬───────────────┘
                                     │                     │
                    Target File Open │                     │ Target File Closed
                    in Active Excel  │                     │ or Excel Not Running
                                     ▼                     ▼
          ┌───────────────────────────────────┐  ┌───────────────────────────────────┐
          │         COM Interop Engine        │  │         ClosedXML Engine          │
          │   - Dedicated STA Worker Thread   │  │   - Direct OpenXML File Access    │
          │   - ROT Traversal & Pid Binding   │  │   - In-Memory XLWorkbook          │
          │   - IMessageFilter (Retry/Busy)   │  │   - 100% Headless & Thread-Safe   │
          │   - Live UI Session Interaction   │  │   - Zero Office Install Needed    │
          │   - VBA Macro Execution           │  │   - Fast Batch Calculation        │
          └───────────────────────────────────┘  └───────────────────────────────────┘
```

#### Hybrid Routing Rules
1. **Target Identification**:
   - Every tool accepts optional `workbook` parameter (file path, file name, or omitted for active workbook).
2. **Routing Decision**:
   - If `workbook` is omitted or matches the currently active workbook in the connected `Excel.Application` -> **Route to COM Interop**.
   - If `workbook` is a file path:
     - Check ROT: Is this file currently open in an active `EXCEL.EXE` process?
     - If **Yes** -> Route to COM Interop (preserves unsaved modifications and reflects live in GUI).
     - If **No** -> Route to **ClosedXML** (headless open, modification, and save without popping up Excel windows).
   - If `Excel.Application` is not running on the system:
     - All tools targeting closed `.xlsx` files automatically execute via **ClosedXML** seamlessly!
     - Only `run_macro` returns an informative error: `"VBA macro execution requires a running Microsoft Excel instance."`

#### STA Worker & COM Lifecycle
- Bridge starts a background thread with `ApartmentState.STA`:
  ```csharp
  var thread = new Thread(ProcessPump) { IsBackground = true };
  thread.SetApartmentState(ApartmentState.STA);
  thread.Start();
  ```
- Uses `IOleMessageFilter` registration on the STA thread to handle `SERVERCALL_RETRYLATER` (retries up to 10 seconds before failing).
- Cleanup: After COM script execution, proper RCW cleanup via `Marshal.FinalReleaseComObject` and `GC.Collect()`.

---

### 2.2 Core Tools Architecture

#### 1. `get_excel_context`
- **Purpose**: Provides the AI with immediate situational awareness of the Excel session without multiple round-trips.
- **Method Wire**: `excel.context`
- **Context DTO Structure**:
  ```csharp
  public sealed record ExcelInfo(
      bool IsConnected,
      int? AttachedPid,
      string? ExcelVersion,
      string? ActiveWorkbook,
      string? ActiveWorksheet,
      string? SelectionAddress,
      int WorkbookCount,
      IReadOnlyList<string> OpenWorkbooks,
      bool WriteEnabled,
      bool DestructiveEnabled,
      string CalculationMode, // "Automatic", "Manual", "Semiautomatic"
      string Mode // "COM" or "Headless"
  );
  ```
- **Context Service Response**:
  ```json
  {
    "host": "excel",
    "hostVersion": "2026",
    "docTitle": "StructuralAnalysis_Rev3.xlsx",
    "docPath": "C:\\Projects\\Tower\\StructuralAnalysis_Rev3.xlsx",
    "isModifiable": true,
    "isReadOnly": false,
    "executionEnabled": true,
    "excel": {
      "isConnected": true,
      "attachedPid": 14280,
      "excelVersion": "16.0.17328",
      "activeWorkbook": "StructuralAnalysis_Rev3.xlsx",
      "activeWorksheet": "RebarSchedule",
      "selectionAddress": "$B$2:$E$15",
      "workbookCount": 2,
      "openWorkbooks": ["StructuralAnalysis_Rev3.xlsx", "MaterialSpecs.xlsx"],
      "writeEnabled": true,
      "destructiveEnabled": false,
      "calculationMode": "Automatic",
      "mode": "COM"
    }
  }
  ```

#### 2. `execute_excel_code`
- **Purpose**: Executes arbitrary C# Roslyn script code with host globals and security guards.
- **Method Wire**: `excel.execute`
- **Globals Available to Scripts**:
  - `excel`: `dynamic` (active `Excel.Application` COM root in COM mode; bridge helper in Headless mode)
  - `workbook`: `dynamic` (active COM `Workbook` or ClosedXML `IXLWorkbook`)
  - `worksheet`: `dynamic` (active COM `Worksheet` or ClosedXML `IXLWorksheet`)
  - `closedXml`: `ClosedXmlHelper` (utility to load/save offline workbooks directly)
  - `ct`: `CancellationToken`
  - `log`: `Action<string>`
  - `progress`: `Action<int, int?, string?>`
  - `args`: `ScriptArgs`
- **Default Imports** (`HostScriptContracts.ExcelImports`):
  ```csharp
  "System", "System.Linq", "System.Collections.Generic", "System.IO", "System.Text",
  "ClosedXML.Excel",
  "HPRebar.McpBridge.Core.Scripting"
  ```
- **Execution Safeguards**:
  - `ScriptGuard` checks deny-list: `System.Diagnostics.Process`, `System.Reflection.Emit`, `#r`/`#load` external binaries.
  - Automatic tier classification (R, W, D).
  - Snapshot created before W/D script runs; snapshot file name returned in `ExecuteResult.Snapshot`.

---

### 2.3 The 12 Embedded Seed Tools — Complete Specifications

Below are the complete specifications for all 12 embedded seed tools, including JSON schemas, safety classifications, and Roslyn script implementations.

```
Seed Tools Catalog Summary:
├── Data (5 tools)
│   ├── read_range (Tier R, none, 30s)
│   ├── find_cells (Tier R, none, 30s)
│   ├── read_table (Tier R, none, 30s)
│   ├── write_range (Tier W, auto, 60s, snapshot)
│   └── create_table (Tier W, auto, 60s, snapshot)
├── Workbook (2 tools)
│   ├── read_worksheet_info (Tier R, none, 30s)
│   └── manage_worksheet (Tier W/D, auto, 60s, snapshot)
├── Format (1 tool)
│   └── format_range (Tier W, auto, 60s, snapshot)
├── Chart (1 tool)
│   └── create_chart (Tier W, auto, 60s, snapshot)
├── Calculation (1 tool)
│   └── evaluate_formula (Tier R, none, 30s)
├── Export (1 tool)
│   └── export_worksheet (Tier W, auto, 60s, snapshot)
└── Automation (1 tool)
    └── run_macro (Tier D, auto, 120s, snapshot)
```

---

#### Seed 1: `read_range`
- **Category**: `Data`
- **Name**: `read_range`
- **Tier**: `R` (Read-only)
- **Transaction**: `"none"`
- **Timeout**: 30s
- **Tags**: `["range", "read", "cells", "values"]`
- **Description**: "Read cell values, formulas, or formatted text from a worksheet range (e.g. 'A1:D10') or named range into JSON. Supports fast batch array reads. Read-only."
- **Notes**: "COM: Uses Range.Value2 batch 2D array read for maximum performance. Headless: Uses ClosedXML IXLRange."
- **Input Schema (`tool.json`)**:
  ```json
  {
    "type": "object",
    "properties": {
      "range": {
        "type": "string",
        "description": "Cell range address (e.g. 'A1:C10', 'Sheet2!B5:E20') or named range. If omitted, reads the worksheet's used range."
      },
      "sheet": {
        "type": "string",
        "description": "Worksheet name. Defaults to active worksheet."
      },
      "workbook": {
        "type": "string",
        "description": "Workbook name or file path. Defaults to active workbook."
      },
      "outputFormat": {
        "type": "string",
        "enum": ["values", "formulas", "formatted"],
        "default": "values",
        "description": "Output representation: 'values' (raw values), 'formulas' (formulas if present), 'formatted' (display text)."
      },
      "hasHeaders": {
        "type": "boolean",
        "default": false,
        "description": "If true, treats the first row as headers and returns an array of JSON objects."
      },
      "maxRows": {
        "type": "integer",
        "default": 1000,
        "description": "Maximum number of rows to return."
      }
    },
    "additionalProperties": false
  }
  ```
- **Roslyn Script Code (`code.cs`)**:
  ```csharp
  string rangeAddr = args.String("range", null);
  string sheetName = args.String("sheet", null);
  string outputFormat = args.String("outputFormat", "values");
  bool hasHeaders = args.Bool("hasHeaders", false);
  int maxRows = args.Int("maxRows", 1000);

  dynamic ws = string.IsNullOrEmpty(sheetName) ? workbook.ActiveSheet : workbook.Worksheets[sheetName];
  dynamic targetRange = string.IsNullOrEmpty(rangeAddr) ? ws.UsedRange : ws.Range[rangeAddr];

  int rowCount = Math.Min((int)targetRange.Rows.Count, maxRows);
  int colCount = (int)targetRange.Columns.Count;
  string actualAddr = (string)targetRange.Address;

  object[,] rawValues = null;
  if (rowCount == 1 && colCount == 1)
  {
      rawValues = new object[2, 2];
      rawValues[1, 1] = outputFormat == "formulas" ? targetRange.Formula : (outputFormat == "formatted" ? targetRange.Text : targetRange.Value2);
  }
  else
  {
      dynamic slice = targetRange.Resize[rowCount, colCount];
      rawValues = outputFormat == "formulas" ? (object[,])slice.Formula : (outputFormat == "formatted" ? (object[,])slice.Text : (object[,])slice.Value2);
  }

  var headers = new List<string>();
  var rows = new List<object>();

  if (hasHeaders && rowCount > 1)
  {
      for (int c = 1; c <= colCount; c++)
      {
          headers.Add(rawValues[1, c]?.ToString() ?? $"Col_{c}");
      }
      for (int r = 2; r <= rowCount; r++)
      {
          var rowObj = new Dictionary<string, object>();
          for (int c = 1; c <= colCount; c++)
          {
              rowObj[headers[c - 1]] = rawValues[r, c];
          }
          rows.Add(rowObj);
      }
  }
  else
  {
      for (int r = 1; r <= rowCount; r++)
      {
          var rowList = new List<object>();
          for (int c = 1; c <= colCount; c++)
          {
              rowList.Add(rawValues[r, c]);
          }
          rows.Add(rowList);
      }
  }

  log($"Read {rowCount}x{colCount} from {ws.Name}!{actualAddr}");
  return new
  {
      sheet = (string)ws.Name,
      address = actualAddr,
      rowCount,
      colCount,
      hasHeaders,
      headers = hasHeaders ? headers : null,
      data = rows,
      summary = $"Read {rowCount} rows x {colCount} cols from {ws.Name}!{actualAddr}"
  };
  ```

---

#### Seed 2: `read_worksheet_info`
- **Category**: `Workbook`
- **Name**: `read_worksheet_info`
- **Tier**: `R` (Read-only)
- **Transaction**: `"none"`
- **Timeout**: 30s
- **Tags**: `["worksheet", "metadata", "tables", "charts"]`
- **Description**: "Inspect worksheets in the workbook: names, indices, visibility state, used range dimensions, tables, and charts. Read-only."
- **Notes**: "Enumerate Worksheets collection and their child ListObjects and ChartObjects."
- **Input Schema (`tool.json`)**:
  ```json
  {
    "type": "object",
    "properties": {
      "sheet": {
        "type": "string",
        "description": "Specific sheet name to inspect. If omitted, inspects all sheets in workbook."
      },
      "workbook": {
        "type": "string",
        "description": "Workbook name or file path. Defaults to active workbook."
      }
    },
    "additionalProperties": false
  }
  ```
- **Roslyn Script Code (`code.cs`)**:
  ```csharp
  string sheetName = args.String("sheet", null);
  var sheets = new List<object>();

  var targetSheets = new List<dynamic>();
  if (!string.IsNullOrEmpty(sheetName))
  {
      targetSheets.Add(workbook.Worksheets[sheetName]);
  }
  else
  {
      foreach (dynamic s in workbook.Worksheets) targetSheets.Add(s);
  }

  foreach (dynamic ws in targetSheets)
  {
      var tables = new List<string>();
      foreach (dynamic tbl in ws.ListObjects) tables.Add((string)tbl.Name);

      var charts = new List<string>();
      foreach (dynamic ch in ws.ChartObjects()) charts.Add((string)ch.Name);

      dynamic used = ws.UsedRange;
      string usedAddr = used != null ? (string)used.Address : "$A$1";
      int rows = used != null ? (int)used.Rows.Count : 0;
      int cols = used != null ? (int)used.Columns.Count : 0;
      int vis = (int)ws.Visible; // -1: xlSheetVisible, 0: xlSheetHidden, 2: xlSheetVeryHidden
      string visStr = vis == -1 ? "Visible" : (vis == 0 ? "Hidden" : "VeryHidden");

      sheets.Add(new
      {
          name = (string)ws.Name,
          index = (int)ws.Index,
          visibility = visStr,
          usedRange = usedAddr,
          rowCount = rows,
          colCount = cols,
          tables,
          charts,
          isProtected = (bool)ws.ProtectContents
      });
  }

  string activeName = (string)workbook.ActiveSheet.Name;
  log($"Workbook '{workbook.Name}' has {sheets.Count} worksheet(s); active: {activeName}");
  return new
  {
      workbook = (string)workbook.Name,
      activeSheet = activeName,
      sheetCount = sheets.Count,
      sheets,
      summary = $"Workbook contains {sheets.Count} worksheet(s); active sheet: '{activeName}'"
  };
  ```

---

#### Seed 3: `find_cells`
- **Category**: `Data`
- **Name**: `find_cells`
- **Tier**: `R` (Read-only)
- **Transaction**: `"none"`
- **Timeout**: 30s
- **Tags**: `["find", "search", "cells", "query"]`
- **Description**: "Search for matching text, numbers, or formula patterns across a worksheet or entire workbook. Returns matched cell addresses and values. Read-only."
- **Notes**: "Uses Range.Find loop with proper wrap detection."
- **Input Schema (`tool.json`)**:
  ```json
  {
    "type": "object",
    "required": ["query"],
    "properties": {
      "query": {
        "type": "string",
        "description": "Text or value to search for."
      },
      "sheet": {
        "type": "string",
        "description": "Worksheet name to search. Defaults to active sheet."
      },
      "workbook": {
        "type": "string",
        "description": "Workbook name or file path. Defaults to active workbook."
      },
      "lookIn": {
        "type": "string",
        "enum": ["values", "formulas"],
        "default": "values",
        "description": "Look in cell values or formulas."
      },
      "exactMatch": {
        "type": "boolean",
        "default": false,
        "description": "Match entire cell contents vs substring."
      },
      "searchAllSheets": {
        "type": "boolean",
        "default": false,
        "description": "If true, searches across all worksheets in the workbook."
      },
      "maxResults": {
        "type": "integer",
        "default": 100,
        "description": "Maximum number of matched cells to return."
      }
    },
    "additionalProperties": false
  }
  ```
- **Roslyn Script Code (`code.cs`)**:
  ```csharp
  string query = args.String("query");
  string sheetName = args.String("sheet", null);
  string lookInStr = args.String("lookIn", "values");
  bool exactMatch = args.Bool("exactMatch", false);
  bool searchAllSheets = args.Bool("searchAllSheets", false);
  int maxResults = args.Int("maxResults", 100);

  int lookIn = lookInStr == "formulas" ? -4123 : -4163; // xlFormulas (-4123) vs xlValues (-4163)
  int lookAt = exactMatch ? 1 : 2; // xlWhole (1) vs xlPart (2)

  var sheetsToSearch = new List<dynamic>();
  if (searchAllSheets)
  {
      foreach (dynamic s in workbook.Worksheets) sheetsToSearch.Add(s);
  }
  else
  {
      sheetsToSearch.Add(string.IsNullOrEmpty(sheetName) ? workbook.ActiveSheet : workbook.Worksheets[sheetName]);
  }

  var matches = new List<object>();
  foreach (dynamic ws in sheetsToSearch)
  {
      dynamic used = ws.UsedRange;
      if (used == null) continue;

      dynamic current = used.Find(query, Type.Missing, lookIn, lookAt, 1, 1, false, Type.Missing, Type.Missing);
      if (current == null) continue;

      string firstAddress = current.Address;
      do
      {
          matches.Add(new
          {
              sheet = (string)ws.Name,
              address = (string)current.Address,
              row = (int)current.Row,
              col = (int)current.Column,
              value = current.Value2,
              formula = current.HasFormula ? (string)current.Formula : null
          });

          if (matches.Count >= maxResults) break;
          current = used.FindNext(current);
      } while (current != null && (string)current.Address != firstAddress);

      if (matches.Count >= maxResults) break;
  }

  log($"Found {matches.Count} match(es) for query '{query}'");
  return new
  {
      query,
      matchCount = matches.Count,
      matches,
      summary = $"Found {matches.Count} matching cell(s) for '{query}'"
  };
  ```

---

#### Seed 4: `read_table`
- **Category**: `Data`
- **Name**: `read_table`
- **Tier**: `R` (Read-only)
- **Transaction**: `"none"`
- **Timeout**: 30s
- **Tags**: `["table", "listobject", "data", "records"]`
- **Description**: "Read structured Excel table (ListObject) headers, data rows as JSON records, and totals row. Read-only."
- **Notes**: "Reads ListObject.HeaderRowRange, DataBodyRange, and TotalsRowRange."
- **Input Schema (`tool.json`)**:
  ```json
  {
    "type": "object",
    "required": ["tableName"],
    "properties": {
      "tableName": {
        "type": "string",
        "description": "Name of the Excel table (e.g. 'Table1', 'RebarList')."
      },
      "sheet": {
        "type": "string",
        "description": "Worksheet containing the table. If omitted, searches all worksheets."
      },
      "workbook": {
        "type": "string",
        "description": "Workbook name or file path. Defaults to active workbook."
      },
      "maxRows": {
        "type": "integer",
        "default": 1000,
        "description": "Maximum number of data rows to return."
      }
    },
    "additionalProperties": false
  }
  ```
- **Roslyn Script Code (`code.cs`)**:
  ```csharp
  string tableName = args.String("tableName");
  string sheetName = args.String("sheet", null);
  int maxRows = args.Int("maxRows", 1000);

  dynamic targetTable = null;
  dynamic targetSheet = null;

  if (!string.IsNullOrEmpty(sheetName))
  {
      targetSheet = workbook.Worksheets[sheetName];
      try { targetTable = targetSheet.ListObjects[tableName]; } catch { }
  }
  else
  {
      foreach (dynamic ws in workbook.Worksheets)
      {
          try
          {
              targetTable = ws.ListObjects[tableName];
              targetSheet = ws;
              break;
          }
          catch { }
      }
  }

  if (targetTable == null)
  {
      throw new ArgumentException($"Table '{tableName}' not found in workbook.");
  }

  var columns = new List<string>();
  foreach (dynamic col in targetTable.ListColumns)
  {
      columns.Add((string)col.Name);
  }

  dynamic body = targetTable.DataBodyRange;
  var rows = new List<Dictionary<string, object>>();
  int rowCount = 0;

  if (body != null)
  {
      rowCount = Math.Min((int)body.Rows.Count, maxRows);
      int colCount = columns.Count;
      object[,] vals = rowCount == 1 && colCount == 1
          ? new object[,] { { null, null }, { null, body.Value2 } }
          : (object[,])body.Resize[rowCount, colCount].Value2;

      for (int r = 1; r <= rowCount; r++)
      {
          var row = new Dictionary<string, object>();
          for (int c = 1; c <= colCount; c++)
          {
              row[columns[c - 1]] = vals[r, c];
          }
          rows.Add(row);
      }
  }

  var totals = new Dictionary<string, object>();
  bool hasTotals = (bool)targetTable.ShowTotals;
  if (hasTotals && targetTable.TotalsRowRange != null)
  {
      dynamic tRange = targetTable.TotalsRowRange;
      for (int c = 1; c <= columns.Count; c++)
      {
          totals[columns[c - 1]] = tRange.Cells[1, c].Value2;
      }
  }

  log($"Table '{tableName}' read: {rows.Count} row(s), {columns.Count} column(s)");
  return new
  {
      tableName = (string)targetTable.Name,
      sheet = (string)targetSheet.Name,
      range = (string)targetTable.Range.Address,
      columns,
      rowCount = rows.Count,
      hasTotalsRow = hasTotals,
      totals = hasTotals ? totals : null,
      rows,
      summary = $"Table '{tableName}' on sheet '{targetSheet.Name}': {rows.Count} rows x {columns.Count} columns"
  };
  ```

---

#### Seed 5: `write_range`
- **Category**: `Data`
- **Name**: `write_range`
- **Tier**: `W` (Write)
- **Transaction**: `"auto"`
- **Timeout**: 60s
- **Tags**: `["write", "range", "cells", "values", "batch"]`
- **Description**: "Batch write 2D values, arrays, or formulas to a range starting at target cell. Auto-sizes destination range. Takes an automatic snapshot before writing."
- **Notes**: "Uses Range.Resize and 2D array assignment in a single COM call for speed."
- **Input Schema (`tool.json`)**:
  ```json
  {
    "type": "object",
    "required": ["startCell", "values"],
    "properties": {
      "startCell": {
        "type": "string",
        "description": "Starting cell address (e.g. 'A1', 'B5')."
      },
      "values": {
        "type": "array",
        "description": "2D array of rows and columns to write: [[row1_col1, row1_col2], [row2_col1, row2_col2]].",
        "items": {
          "type": "array",
          "items": {}
        }
      },
      "sheet": {
        "type": "string",
        "description": "Target worksheet. Defaults to active sheet."
      },
      "workbook": {
        "type": "string",
        "description": "Workbook name or file path. Defaults to active workbook."
      },
      "autoFitColumns": {
        "type": "boolean",
        "default": false,
        "description": "Auto-fit column widths after writing."
      },
      "isFormula": {
        "type": "boolean",
        "default": false,
        "description": "If true, treats string values starting with '=' as active formulas."
      }
    },
    "additionalProperties": false
  }
  ```
- **Roslyn Script Code (`code.cs`)**:
  ```csharp
  string startCell = args.String("startCell");
  string sheetName = args.String("sheet", null);
  bool autoFit = args.Bool("autoFitColumns", false);
  bool isFormula = args.Bool("isFormula", false);

  var rawList = args.Raw("values");
  if (!rawList.HasValue || rawList.Value.ValueKind != System.Text.Json.JsonValueKind.Array)
  {
      throw new ArgumentException("Parameter 'values' must be a non-empty 2D array.");
  }

  var rowsArray = rawList.Value.EnumerateArray().ToList();
  int numRows = rowsArray.Count;
  if (numRows == 0) throw new ArgumentException("Parameter 'values' cannot be empty.");

  int numCols = 0;
  foreach (var r in rowsArray)
  {
      if (r.ValueKind == System.Text.Json.JsonValueKind.Array)
      {
          int c = r.GetArrayLength();
          if (c > numCols) numCols = c;
      }
  }
  if (numCols == 0) throw new ArgumentException("Parameter 'values' must contain at least one row with columns.");

  object[,] data = new object[numRows, numCols];
  for (int r = 0; r < numRows; r++)
  {
      var cols = rowsArray[r].EnumerateArray().ToList();
      for (int c = 0; c < numCols; c++)
      {
          if (c < cols.Count)
          {
              var val = cols[c];
              data[r, c] = val.ValueKind switch
              {
                  System.Text.Json.JsonValueKind.Number => val.TryGetInt64(out var l) ? (object)l : val.GetDouble(),
                  System.Text.Json.JsonValueKind.True => true,
                  System.Text.Json.JsonValueKind.False => false,
                  System.Text.Json.JsonValueKind.Null => null,
                  _ => val.GetString()
              };
          }
          else data[r, c] = null;
      }
  }

  dynamic ws = string.IsNullOrEmpty(sheetName) ? workbook.ActiveSheet : workbook.Worksheets[sheetName];
  dynamic origin = ws.Range[startCell];
  dynamic target = origin.Resize[numRows, numCols];

  if (isFormula) target.Formula = data;
  else target.Value2 = data;

  if (autoFit) target.Columns.AutoFit();

  string targetAddr = (string)target.Address;
  log($"Wrote {numRows} rows x {numCols} cols to {ws.Name}!{targetAddr}");
  return new
  {
      success = true,
      sheet = (string)ws.Name,
      targetRange = targetAddr,
      rowsWritten = numRows,
      colsWritten = numCols,
      summary = $"Wrote {numRows} rows x {numCols} cols to {ws.Name}!{targetAddr}; snapshot saved"
  };
  ```

---

#### Seed 6: `format_range`
- **Category**: `Format`
- **Name**: `format_range`
- **Tier**: `W` (Write)
- **Transaction**: `"auto"`
- **Timeout**: 60s
- **Tags**: `["format", "style", "color", "font", "border", "numberformat"]`
- **Description**: "Apply formatting to a range: number format, font styling (bold, size, color), fill background color (hex), borders, and alignment. Takes an automatic snapshot before writing."
- **Notes**: "Translates Hex colors to BGR integers for Excel Range.Interior.Color and Font.Color."
- **Input Schema (`tool.json`)**:
  ```json
  {
    "type": "object",
    "required": ["range"],
    "properties": {
      "range": {
        "type": "string",
        "description": "Cell range address (e.g. 'A1:E1', 'B2:B20')."
      },
      "sheet": {
        "type": "string",
        "description": "Worksheet name. Defaults to active sheet."
      },
      "workbook": {
        "type": "string",
        "description": "Workbook name or file path. Defaults to active workbook."
      },
      "numberFormat": {
        "type": "string",
        "description": "Number format string (e.g. '$#,##0.00', '0.0%', 'YYYY-MM-DD', '#,##0')."
      },
      "bold": { "type": "boolean" },
      "italic": { "type": "boolean" },
      "fontSize": { "type": "number" },
      "fontColor": {
        "type": "string",
        "description": "Hex color for text (e.g. '#FFFFFF', '#1A1A1A')."
      },
      "backgroundColor": {
        "type": "string",
        "description": "Hex fill background color (e.g. '#003366', '#E2EFDA')."
      },
      "horizontalAlignment": {
        "type": "string",
        "enum": ["left", "center", "right", "justify"]
      },
      "wrapText": { "type": "boolean" },
      "border": {
        "type": "string",
        "enum": ["all_thin", "outline_thick", "bottom_double", "none"]
      }
    },
    "additionalProperties": false
  }
  ```
- **Roslyn Script Code (`code.cs`)**:
  ```csharp
  string rangeAddr = args.String("range");
  string sheetName = args.String("sheet", null);

  dynamic ws = string.IsNullOrEmpty(sheetName) ? workbook.ActiveSheet : workbook.Worksheets[sheetName];
  dynamic target = ws.Range[rangeAddr];

  // Helper for hex color to Excel BGR integer
  int HexToBgr(string hex)
  {
      hex = hex.TrimStart('#');
      if (hex.Length == 6)
      {
          int r = Convert.ToInt32(hex.Substring(0, 2), 16);
          int g = Convert.ToInt32(hex.Substring(2, 2), 16);
          int b = Convert.ToInt32(hex.Substring(4, 2), 16);
          return (b << 16) | (g << 8) | r;
      }
      return 0;
  }

  if (args.Has("numberFormat")) target.NumberFormat = args.String("numberFormat");
  if (args.Has("bold")) target.Font.Bold = args.Bool("bold");
  if (args.Has("italic")) target.Font.Italic = args.Bool("italic");
  if (args.Has("fontSize")) target.Font.Size = args.Double("fontSize");
  if (args.Has("fontColor")) target.Font.Color = HexToBgr(args.String("fontColor"));
  if (args.Has("backgroundColor")) target.Interior.Color = HexToBgr(args.String("backgroundColor"));
  if (args.Has("wrapText")) target.WrapText = args.Bool("wrapText");

  if (args.Has("horizontalAlignment"))
  {
      string align = args.String("horizontalAlignment").ToLowerInvariant();
      target.HorizontalAlignment = align switch
      {
          "center" => -4108, // xlCenter
          "right" => -4152,  // xlRight
          "justify" => -4130,// xlJustify
          _ => -4131         // xlLeft
      };
  }

  if (args.Has("border"))
  {
      string bType = args.String("border");
      if (bType == "all_thin")
      {
          dynamic borders = target.Borders;
          borders.LineStyle = 1; // xlContinuous
          borders.Weight = 2;    // xlThin
      }
      else if (bType == "outline_thick")
      {
          target.BorderAround(1, 4, -4105, Type.Missing); // xlContinuous, xlThick, xlColorIndexAutomatic
      }
      else if (bType == "bottom_double")
      {
          dynamic b = target.Borders[9]; // xlEdgeBottom
          b.LineStyle = -4119; // xlDouble
      }
      else if (bType == "none")
      {
          target.Borders.LineStyle = -4142; // xlLineStyleNone
      }
  }

  string actual = (string)target.Address;
  log($"Formatted range {ws.Name}!{actual}");
  return new
  {
      success = true,
      sheet = (string)ws.Name,
      range = actual,
      summary = $"Formatted range {ws.Name}!{actual}; snapshot saved"
  };
  ```

---

#### Seed 7: `manage_worksheet`
- **Category**: `Workbook`
- **Name**: `manage_worksheet`
- **Tier**: `W` (for add, rename, duplicate, hide) / `D` (for delete)
- **Transaction**: `"auto"`
- **Timeout**: 60s
- **Tags**: `["worksheet", "add", "rename", "delete", "hide", "duplicate"]`
- **Description**: "Manage workbook sheets: add, rename, duplicate, delete, or change visibility (visible, hidden, very_hidden). Deleting a sheet is DESTRUCTIVE. Takes an automatic snapshot before execution."
- **Notes**: "Deleting a worksheet turns on destructive tier gating. Sheet renaming validates invalid chars (: \\ / ? * [ ])."
- **Input Schema (`tool.json`)**:
  ```json
  {
    "type": "object",
    "required": ["action", "sheet"],
    "properties": {
      "action": {
        "type": "string",
        "enum": ["add", "rename", "duplicate", "delete", "hide", "unhide"],
        "description": "Action to perform on worksheet."
      },
      "sheet": {
        "type": "string",
        "description": "Target sheet name."
      },
      "newSheetName": {
        "type": "string",
        "description": "New sheet name (required for 'rename' and optional for 'add'/'duplicate')."
      },
      "workbook": {
        "type": "string",
        "description": "Workbook name or file path. Defaults to active workbook."
      },
      "visibility": {
        "type": "string",
        "enum": ["visible", "hidden", "very_hidden"],
        "description": "Target visibility for 'hide'/'unhide'."
      }
    },
    "additionalProperties": false
  }
  ```
- **Roslyn Script Code (`code.cs`)**:
  ```csharp
  string action = args.String("action").ToLowerInvariant();
  string sheetName = args.String("sheet");
  string newName = args.String("newSheetName", null);
  string vis = args.String("visibility", "visible");

  dynamic targetSheet = null;
  if (action != "add")
  {
      targetSheet = workbook.Worksheets[sheetName];
  }

  string resultMsg = "";
  if (action == "add")
  {
      dynamic ws = workbook.Worksheets.Add(Type.Missing, workbook.Worksheets[workbook.Worksheets.Count]);
      if (!string.IsNullOrEmpty(newName)) ws.Name = newName;
      resultMsg = $"Added new worksheet '{ws.Name}'";
  }
  else if (action == "rename")
  {
      if (string.IsNullOrEmpty(newName)) throw new ArgumentException("newSheetName is required for rename action.");
      string old = (string)targetSheet.Name;
      targetSheet.Name = newName;
      resultMsg = $"Renamed worksheet '{old}' to '{newName}'";
  }
  else if (action == "duplicate")
  {
      targetSheet.Copy(Type.Missing, targetSheet);
      dynamic copy = workbook.ActiveSheet;
      if (!string.IsNullOrEmpty(newName)) copy.Name = newName;
      resultMsg = $"Duplicated worksheet '{sheetName}' as '{copy.Name}'";
  }
  else if (action == "delete")
  {
      // Destructive operation: suppress Excel UI alert prompt
      excel.DisplayAlerts = false;
      try { targetSheet.Delete(); }
      finally { excel.DisplayAlerts = true; }
      resultMsg = $"Deleted worksheet '{sheetName}'";
  }
  else if (action == "hide")
  {
      targetSheet.Visible = vis == "very_hidden" ? 2 : 0; // xlSheetVeryHidden (2) vs xlSheetHidden (0)
      resultMsg = $"Set worksheet '{sheetName}' visibility to {vis}";
  }
  else if (action == "unhide")
  {
      targetSheet.Visible = -1; // xlSheetVisible
      resultMsg = $"Unhid worksheet '{sheetName}'";
  }

  log(resultMsg);
  return new
  {
      success = true,
      action,
      sheet = sheetName,
      newSheetName = newName,
      summary = $"{resultMsg}; snapshot saved"
  };
  ```

---

#### Seed 8: `create_table`
- **Category**: `Data`
- **Name**: `create_table`
- **Tier**: `W` (Write)
- **Transaction**: `"auto"`
- **Timeout**: 60s
- **Tags**: `["table", "listobject", "create", "format", "style"]`
- **Description**: "Convert a range of cells into a structured Excel Table (ListObject) with headers, applied styling, and optional totals row. Takes an automatic snapshot before writing."
- **Notes**: "Uses Worksheets.ListObjects.Add with xlSrcRange."
- **Input Schema (`tool.json`)**:
  ```json
  {
    "type": "object",
    "required": ["range", "tableName"],
    "properties": {
      "range": {
        "type": "string",
        "description": "Cell range to convert to table (e.g. 'A1:E50')."
      },
      "tableName": {
        "type": "string",
        "description": "Unique table name (e.g. 'RebarSchedule', 'Expenses')."
      },
      "sheet": {
        "type": "string",
        "description": "Worksheet name. Defaults to active sheet."
      },
      "workbook": {
        "type": "string",
        "description": "Workbook name or file path. Defaults to active workbook."
      },
      "hasHeaders": {
        "type": "boolean",
        "default": true,
        "description": "Whether first row contains headers."
      },
      "tableStyle": {
        "type": "string",
        "default": "TableStyleMedium2",
        "description": "Excel built-in table style name."
      },
      "showTotalsRow": {
        "type": "boolean",
        "default": false,
        "description": "Whether to display the table totals row."
      }
    },
    "additionalProperties": false
  }
  ```
- **Roslyn Script Code (`code.cs`)**:
  ```csharp
  string rangeAddr = args.String("range");
  string tableName = args.String("tableName");
  string sheetName = args.String("sheet", null);
  bool hasHeaders = args.Bool("hasHeaders", true);
  string tableStyle = args.String("tableStyle", "TableStyleMedium2");
  bool showTotals = args.Bool("showTotalsRow", false);

  dynamic ws = string.IsNullOrEmpty(sheetName) ? workbook.ActiveSheet : workbook.Worksheets[sheetName];
  dynamic targetRange = ws.Range[rangeAddr];

  int xlSrcRange = 1;
  int xlHeaders = hasHeaders ? 1 : 2; // xlYes (1) vs xlNo (2)

  dynamic tbl = ws.ListObjects.Add(xlSrcRange, targetRange, Type.Missing, xlHeaders, Type.Missing);
  tbl.Name = tableName;
  tbl.TableStyle = tableStyle;
  tbl.ShowTotals = showTotals;

  string finalAddr = (string)tbl.Range.Address;
  log($"Created table '{tableName}' at {ws.Name}!{finalAddr}");
  return new
  {
      success = true,
      tableName,
      sheet = (string)ws.Name,
      range = finalAddr,
      tableStyle,
      showTotalsRow = showTotals,
      summary = $"Created table '{tableName}' on {ws.Name}!{finalAddr}; snapshot saved"
  };
  ```

---

#### Seed 9: `create_chart`
- **Category**: `Chart`
- **Name**: `create_chart`
- **Tier**: `W` (Write)
- **Transaction**: `"auto"`
- **Timeout**: 60s
- **Tags**: `["chart", "graph", "plot", "visualization"]`
- **Description**: "Create an embedded chart (Column, Line, Pie, Bar, Area, Scatter) linked to a data range. Custom title, size, and location. Takes an automatic snapshot before writing."
- **Notes**: "Uses ChartObjects.Add and Chart.SetSourceData."
- **Input Schema (`tool.json`)**:
  ```json
  {
    "type": "object",
    "required": ["dataRange", "chartType"],
    "properties": {
      "dataRange": {
        "type": "string",
        "description": "Cell range containing chart source data (e.g. 'A1:D10')."
      },
      "chartType": {
        "type": "string",
        "enum": ["ColumnClustered", "ColumnStacked", "Line", "LineMarkers", "Pie", "BarClustered", "Area", "Scatter"],
        "description": "Chart type to create."
      },
      "title": {
        "type": "string",
        "description": "Chart title text."
      },
      "sheet": {
        "type": "string",
        "description": "Worksheet name. Defaults to active sheet."
      },
      "workbook": {
        "type": "string",
        "description": "Workbook name or file path. Defaults to active workbook."
      },
      "targetCell": {
        "type": "string",
        "default": "E2",
        "description": "Top-left cell where the chart should be placed."
      },
      "width": {
        "type": "number",
        "default": 400,
        "description": "Chart width in points."
      },
      "height": {
        "type": "number",
        "default": 250,
        "description": "Chart height in points."
      },
      "hasLegend": {
        "type": "boolean",
        "default": true
      }
    },
    "additionalProperties": false
  }
  ```
- **Roslyn Script Code (`code.cs`)**:
  ```csharp
  string dataRangeAddr = args.String("dataRange");
  string chartTypeStr = args.String("chartType");
  string title = args.String("title", null);
  string sheetName = args.String("sheet", null);
  string targetCell = args.String("targetCell", "E2");
  double width = args.Double("width", 400);
  double height = args.Double("height", 250);
  bool hasLegend = args.Bool("hasLegend", true);

  dynamic ws = string.IsNullOrEmpty(sheetName) ? workbook.ActiveSheet : workbook.Worksheets[sheetName];
  dynamic sourceData = ws.Range[dataRangeAddr];
  dynamic placeCell = ws.Range[targetCell];

  double left = (double)placeCell.Left;
  double top = (double)placeCell.Top;

  dynamic chartObj = ws.ChartObjects().Add(left, top, width, height);
  dynamic ch = chartObj.Chart;

  int xlType = chartTypeStr switch
  {
      "ColumnStacked" => 52, // xlColumnStacked
      "Line" => 4,           // xlLine
      "LineMarkers" => 65,   // xlLineMarkers
      "Pie" => 5,            // xlPie
      "BarClustered" => 57,  // xlBarClustered
      "Area" => 1,           // xlArea
      "Scatter" => -4169,    // xlXYScatter
      _ => 51                // xlColumnClustered
  };

  ch.ChartType = xlType;
  ch.SetSourceData(sourceData);
  ch.HasLegend = hasLegend;

  if (!string.IsNullOrEmpty(title))
  {
      ch.HasTitle = true;
      ch.ChartTitle.Text = title;
  }

  log($"Created {chartTypeStr} chart at {ws.Name}!{targetCell}");
  return new
  {
      success = true,
      chartName = (string)chartObj.Name,
      chartType = chartTypeStr,
      title,
      sheet = (string)ws.Name,
      dataRange = (string)sourceData.Address,
      placedAt = targetCell,
      summary = $"Created {chartTypeStr} chart '{(title ?? chartObj.Name)}' on {ws.Name}!{targetCell}; snapshot saved"
  };
  ```

---

#### Seed 10: `evaluate_formula`
- **Category**: `Calculation`
- **Name**: `evaluate_formula`
- **Tier**: `R` (Read-only)
- **Transaction**: `"none"`
- **Timeout**: 30s
- **Tags**: `["formula", "evaluate", "calculate", "expression"]`
- **Description**: "Evaluate an Excel formula expression dynamically in the context of the active worksheet or workbook. Returns evaluated value and data type. Read-only."
- **Notes**: "Uses Application.Evaluate or Worksheet.Evaluate."
- **Input Schema (`tool.json`)**:
  ```json
  {
    "type": "object",
    "required": ["formula"],
    "properties": {
      "formula": {
        "type": "string",
        "description": "Excel formula string (e.g. 'SUM(A1:A10)', 'VLOOKUP(\"Rebar\", B1:F20, 3, FALSE)'). Leading '=' optional."
      },
      "sheet": {
        "type": "string",
        "description": "Context worksheet. Defaults to active sheet."
      },
      "workbook": {
        "type": "string",
        "description": "Workbook name or file path. Defaults to active workbook."
      }
    },
    "additionalProperties": false
  }
  ```
- **Roslyn Script Code (`code.cs`)**:
  ```csharp
  string formula = args.String("formula");
  string sheetName = args.String("sheet", null);

  if (formula.StartsWith("=", StringComparison.Ordinal))
  {
      formula = formula.Substring(1);
  }

  dynamic ws = string.IsNullOrEmpty(sheetName) ? workbook.ActiveSheet : workbook.Worksheets[sheetName];
  object evalResult = ws.Evaluate(formula);

  // Check if result is an Excel error code (CVErr)
  string resultType = evalResult?.GetType().Name ?? "null";
  bool isError = false;
  string errorName = null;

  if (evalResult is int errCode && errCode < 0)
  {
      isError = true;
      errorName = errCode switch
      {
          -2146826281 => "#DIV/0!",
          -2146826246 => "#N/A",
          -2146826259 => "#NAME?",
          -2146826288 => "#NULL!",
          -2146826252 => "#NUM!",
          -2146826265 => "#REF!",
          -2146826273 => "#VALUE!",
          _ => $"#ERROR({errCode})"
      };
  }

  log($"Evaluated formula '{formula}' -> {evalResult}");
  return new
  {
      formula = "=" + formula,
      sheet = (string)ws.Name,
      result = isError ? (object)errorName : evalResult,
      resultType = isError ? "ExcelError" : resultType,
      isError,
      summary = $"Formula '={formula}' evaluated to {evalResult ?? "null"}"
  };
  ```

---

#### Seed 11: `export_worksheet`
- **Category**: `Export`
- **Name**: `export_worksheet`
- **Tier**: `W` (Write)
- **Transaction**: `"auto"`
- **Timeout**: 60s
- **Tags**: `["export", "pdf", "csv", "publish"]`
- **Description**: "Export a worksheet or entire workbook to PDF or CSV format on disk. Returns generated file path and size. Takes an automatic snapshot before export."
- **Notes**: "Uses Worksheet.ExportAsFixedFormat for PDF and SaveAs with xlCSV for CSV export."
- **Input Schema (`tool.json`)**:
  ```json
  {
    "type": "object",
    "required": ["format"],
    "properties": {
      "format": {
        "type": "string",
        "enum": ["pdf", "csv"],
        "description": "Export format: 'pdf' or 'csv'."
      },
      "outputPath": {
        "type": "string",
        "description": "Target destination file path. If omitted, exports beside the workbook."
      },
      "sheet": {
        "type": "string",
        "description": "Worksheet to export. Required for CSV; optional for PDF (exports full workbook if omitted)."
      },
      "workbook": {
        "type": "string",
        "description": "Workbook name or file path. Defaults to active workbook."
      },
      "landscape": {
        "type": "boolean",
        "default": false,
        "description": "Set orientation to landscape (PDF only)."
      },
      "fitToPage": {
        "type": "boolean",
        "default": true,
        "description": "Fit content to 1 page wide (PDF only)."
      }
    },
    "additionalProperties": false
  }
  ```
- **Roslyn Script Code (`code.cs`)**:
  ```csharp
  string format = args.String("format").ToLowerInvariant();
  string outputPath = args.String("outputPath", null);
  string sheetName = args.String("sheet", null);
  bool landscape = args.Bool("landscape", false);
  bool fitToPage = args.Bool("fitToPage", true);

  string wbPath = (string)workbook.Path;
  string wbName = (string)workbook.Name;
  if (string.IsNullOrEmpty(outputPath))
  {
      string folder = string.IsNullOrEmpty(wbPath) ? Path.GetTempPath() : wbPath;
      string baseName = Path.GetFileNameWithoutExtension(wbName);
      string ext = format == "pdf" ? ".pdf" : ".csv";
      string tag = !string.IsNullOrEmpty(sheetName) ? $"_{sheetName}" : "";
      outputPath = Path.Combine(folder, $"{baseName}{tag}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}");
  }

  dynamic targetObj = !string.IsNullOrEmpty(sheetName) ? workbook.Worksheets[sheetName] : workbook;

  if (format == "pdf")
  {
      if (!string.IsNullOrEmpty(sheetName))
      {
          dynamic ws = workbook.Worksheets[sheetName];
          if (landscape) ws.PageSetup.Orientation = 2; // xlLandscape
          if (fitToPage)
          {
              ws.PageSetup.Zoom = false;
              ws.PageSetup.FitToPagesWide = 1;
              ws.PageSetup.FitToPagesTall = false;
          }
      }
      targetObj.ExportAsFixedFormat(0, outputPath, 0, true, false, Type.Missing, Type.Missing, false, Type.Missing); // xlTypePDF (0), xlQualityStandard (0)
  }
  else if (format == "csv")
  {
      if (string.IsNullOrEmpty(sheetName)) sheetName = (string)workbook.ActiveSheet.Name;
      dynamic ws = workbook.Worksheets[sheetName];
      ws.Copy();
      dynamic tempWb = excel.ActiveWorkbook;
      excel.DisplayAlerts = false;
      try
      {
          tempWb.SaveAs(outputPath, 6); // xlCSV (6)
      }
      finally
      {
          tempWb.Close(false);
          excel.DisplayAlerts = true;
      }
  }

  long sizeBytes = File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0;
  log($"Exported to {format.ToUpperInvariant()} at {outputPath} ({sizeBytes} bytes)");
  return new
  {
      success = true,
      format,
      filePath = outputPath,
      fileSizeBytes = sizeBytes,
      sheet = sheetName,
      summary = $"Exported to {format.ToUpperInvariant()} at {outputPath} ({sizeBytes / 1024} KB); snapshot saved"
  };
  ```

---

#### Seed 12: `run_macro`
- **Category**: `Automation`
- **Name**: `run_macro`
- **Tier**: `D` (Destructive)
- **Transaction**: `"auto"`
- **Timeout**: 120s
- **Tags**: `["macro", "vba", "run", "destructive"]`
- **Description**: "DESTRUCTIVE: Execute an existing VBA macro / Sub procedure in the active workbook with optional arguments. Requires explicit Destructive toggle enabled in Bridge UI. Takes an automatic snapshot before running."
- **Notes**: "Uses Application.Run. Only available in COM mode with running Excel instance."
- **Input Schema (`tool.json`)**:
  ```json
  {
    "type": "object",
    "required": ["macroName"],
    "properties": {
      "macroName": {
        "type": "string",
        "description": "Name of the VBA macro to execute (e.g. 'RefreshAllData', 'Module1.ProcessSchedule')."
      },
      "args": {
        "type": "array",
        "description": "Optional parameters to pass to the macro.",
        "items": {}
      },
      "workbook": {
        "type": "string",
        "description": "Workbook name or file path containing the macro. Defaults to active workbook."
      }
    },
    "additionalProperties": false
  }
  ```
- **Roslyn Script Code (`code.cs`)**:
  ```csharp
  string macroName = args.String("macroName");
  var macroArgs = args.Raw("args");

  // Fully qualify macro with workbook name if not already qualified
  string wbName = (string)workbook.Name;
  string qualifiedMacro = macroName.Contains("!") ? macroName : $"'{wbName}'!{macroName}";

  object result = null;
  if (!macroArgs.HasValue || macroArgs.Value.GetArrayLength() == 0)
  {
      result = excel.Run(qualifiedMacro);
  }
  else
  {
      var rawElements = macroArgs.Value.EnumerateArray().ToList();
      var parsed = rawElements.Select(e => e.ValueKind switch
      {
          System.Text.Json.JsonValueKind.Number => e.TryGetInt64(out var l) ? (object)l : e.GetDouble(),
          System.Text.Json.JsonValueKind.True => true,
          System.Text.Json.JsonValueKind.False => false,
          _ => (object)e.GetString()
      }).ToArray();

      // Application.Run supports up to 30 arguments
      result = parsed.Length switch
      {
          1 => excel.Run(qualifiedMacro, parsed[0]),
          2 => excel.Run(qualifiedMacro, parsed[0], parsed[1]),
          3 => excel.Run(qualifiedMacro, parsed[0], parsed[1], parsed[2]),
          4 => excel.Run(qualifiedMacro, parsed[0], parsed[1], parsed[2], parsed[3]),
          _ => excel.Run(qualifiedMacro, parsed[0], parsed[1], parsed[2], parsed[3], parsed[4])
      };
  }

  log($"Executed VBA macro '{qualifiedMacro}'");
  return new
  {
      success = true,
      macroName,
      returnValue = result,
      summary = $"Executed VBA macro '{macroName}'; snapshot saved"
  };
  ```

---

### 2.4 3-Tier Safety & Snapshot Engine

The safety architecture mirrors the proven 3-tier patterns of `HPEtabs` and `HPPowerBi`:
1. **Tier R (Read)**:
   - Tools: `read_range`, `read_worksheet_info`, `find_cells`, `read_table`, `evaluate_formula`, `get_excel_context`.
   - Requires: `IsExecutionEnabled` toggle = true.
   - Behavior: No snapshot overhead, runs immediately.
2. **Tier W (Write)**:
   - Tools: `write_range`, `format_range`, `create_table`, `create_chart`, `export_worksheet`, `manage_worksheet` (add, rename, duplicate, hide).
   - Requires: `IsExecutionEnabled` AND `IsWriteEnabled` toggles = true.
   - Behavior: Takes an automatic `.xlsx` snapshot backup before execution; returns snapshot filename in `ExecuteResult.Snapshot`.
3. **Tier D (Destructive)**:
   - Tools: `run_macro`, `manage_worksheet` (delete action), or custom scripts calling destructive methods.
   - Requires: `IsExecutionEnabled` AND `IsWriteEnabled` AND `IsDestructiveEnabled` toggles = true.
   - Behavior: Strict refusal if disabled (`BridgeErrorCode.ExecutionDisabled`, `-32001`); auto-snapshot before running.

#### Snapshot Engine (`ExcelSnapshotManager`)
- Directory: `.hpexcel_snapshots/` located in the same folder as the workbook. If the workbook is unsaved or untitled (`Book1`), uses `%TEMP%\.hpexcel_snapshots\`.
- Filename format: `{WorkbookStem}_{yyyyMMdd_HHmmss}_{ActionTag}.xlsx` (or `.xlsm` if macro-enabled).
- Retention Policy: Auto-prunes snapshots exceeding 30 days or keeping the most recent 25 snapshots per workbook.
- In-memory / ClosedXML Fallback: For ClosedXML headless operations, the snapshot is taken by copying the source file before modifying it.

---

### 2.5 Test Suites Architecture & Patterns

The test suite structure follows the strict patterns established in `HPEtabs.Mcp.Server.Tests` and `HPPowerBi.McpBridge.Tests`.

```
HPExcel Tests Architecture:
├── HPExcel.Mcp.Server.Tests (.NET 10 xUnit v3)
│   ├── ExcelHostProfileTests.cs
│   │   ├── Validates PipeNaming ("hpexcel-mcp-2026")
│   │   ├── Validates Categories, CoreToolNames, ScriptImports
│   │   └── Validates ToolDescriptions and MaxTimeoutSeconds (600s)
│   ├── ExcelToolsOverPipeTests.cs
│   │   ├── FakeRevitExecutor round-trip for get_excel_context
│   │   ├── FakeRevitExecutor round-trip for execute_excel_code
│   │   ├── Safety refusal test when AI execution is disabled
│   │   ├── Destructive refusal test when destructive is disabled
│   │   ├── Timeout clamp test (600s ceiling)
│   │   └── Static preview test (PREVIEW diagnostic)
│   ├── SeedLibraryStructureTests.cs
│   │   ├── Verifies exactly 12 seeds are embedded
│   │   ├── Verifies 5 Data, 2 Workbook, 1 Format, 1 Chart, 1 Calc, 1 Export, 1 Auto
│   │   ├── Validates all tool.json schemas (name regex, required, descriptions >= 40 chars)
│   │   ├── Validates code.cs line count (<= 120 lines) and byte size (< 32KB)
│   │   ├── Validates code ends with return statement
│   │   ├── Validates code reads ONLY declared arguments from schema
│   │   ├── Validates examples.json (>= 2 examples with differing args)
│   │   └── Validates ScriptGuard.Check has 0 errors
│   └── SeedLibraryCompileTests.cs
│       └── Compiles all 12 seed code.cs scripts via Roslyn with ClosedXML references
│
└── HPExcel.McpBridge.Tests (.NET 8 Windows xUnit v3)
    ├── SafetyGatingTests.cs
    │   ├── Initial state: all gated off
    │   ├── Enable execution: read passes, write fails (-32001)
    │   ├── Enable write: write passes, destructive fails (-32001)
    │   ├── Enable destructive: destructive passes
    │   └── Disabling execution auto-disables write & destructive
    ├── ExcelSnapshotManagerTests.cs
    │   ├── Backup file created with correct naming pattern
    │   ├── Destination folder created automatically
    │   ├── Rollback / restore test: corrupt file restored to original
    │   └── Pruning test: keeps last 25 snapshots
    ├── ClosedXmlHeadlessTests.cs
    │   ├── Read range from headless .xlsx without Excel running
    │   ├── Write range and verify values persisted to disk
    │   ├── Formula evaluation using ClosedXML calculation engine
    │   └── Create structured table in headless .xlsx
    └── ExcelPipeDispatcherTests.cs
        └── Named pipe dispatcher round-trip with fake executor
```

---

## 3. Caveats & Edge Cases

1. **Active In-Cell Editing in Excel**:
   - If the user has double-clicked a cell and the cursor is blinking in the Formula Bar, Excel enters modal edit mode. COM calls fail with `RPC_E_CALL_REJECTED`.
   - Mitigation: `EtabsAttachment`-style quiescence check or message filter retry loop. In addition, the bridge window can indicate `"Excel Busy (in-cell edit mode)"` to the user.
2. **Multi-Workbook Active Window Switching**:
   - If user switches between multiple workbooks in Excel while an MCP tool is running, relying purely on `excel.ActiveWorkbook` can target the wrong document.
   - Mitigation: All 12 seed tools accept an optional `workbook` parameter to explicitly bind to the target workbook by title or path rather than assuming the active window.
3. **ClosedXML vs Excel Dynamic Arrays / Spilled Formulas**:
   - Modern Excel features such as `XLOOKUP`, `UNIQUE`, `FILTER`, and spilled array formulas (`#SPILL!`) are natively evaluated by Excel COM, whereas ClosedXML's calculation engine may have limited support for newer dynamic array functions. For advanced formula evaluation, COM mode is preferred.
4. **VBA Macro Security Settings in Excel**:
   - `run_macro` will fail if the user's Excel Trust Center settings disable all macros without notification or block programmatic access to the VBA project object model. Clear diagnostic error messages must be provided.
5. **Excel Localization & Formula Language**:
   - In non-English Excel versions (e.g. French, German, Vietnamese), Excel formulas in the GUI may be localized (`SOMME` vs `SUM`). However, COM automation uses English formula names (`Range.Formula` in US English standard format). Tools standardize strictly on US English formulas (`Formula` / `Formula2`).

---

## 4. Conclusion

1. **Hybrid Architecture is the Optimal Path**: Combining COM Interop for interactive live session manipulation with ClosedXML for fast, headless, offline workbook processing provides 100% functionality without compromising developer experience or requiring Excel to be open for basic file operations.
2. **The 12 Embedded Seed Tools Catalog** covers the complete lifecycle of spreadsheet work: querying data, inspecting workbook structure, searching cells, reading/writing structured tables, batch writing, rich formatting, worksheet lifecycle, dynamic charting, formula evaluation, publishing to PDF/CSV, and executing VBA macros.
3. **Safety & Governance**: The 3-tier model (R / W / D) coupled with automatic `.xlsx` snapshots in `.hpexcel_snapshots/` guarantees that users cannot unintentionally corrupt critical financial or structural spreadsheets.
4. **Isolated & Standardized**: Following `McpShared` conventions, `HPExcel` will build cleanly without dependencies on other host projects, adhering strictly to the repository's architectural guardrails.

---

## 5. Verification Method

### 5.1 Static Architecture Verification
1. Inspect `HPExcel.slnx` references: ensure only `../McpShared/HPRebar.Mcp.Contracts` and `../McpShared/HPRebar.Mcp.Server.Core` are referenced.
2. Run Roslyn compiler check on all 12 seed scripts in `SeedLibraryCompileTests`.

### 5.2 Automated Unit & Integration Tests
```powershell
# Run MCP Server tests (seed validation, FakeExecutor round-trip, Roslyn compile)
dotnet test HPExcel/HPExcel.Mcp.Server.Tests

# Run MCP Bridge tests (3-tier safety, snapshot engine, ClosedXML headless tests)
dotnet test HPExcel/HPExcel.McpBridge.Tests

# Verify McpShared core tests continue to pass with 0 regressions
dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests
```

### 5.3 Live Verification Scenarios (Harness)
1. **Headless Verification**: Run `read_range`, `write_range`, `create_table`, and `evaluate_formula` on a test `.xlsx` file while `EXCEL.EXE` is terminated.
2. **Live COM Verification**: Open Excel 2026, attach bridge, execute `get_excel_context`, `format_range`, and `create_chart`; verify live updates appear in the active Excel window.
3. **Safety Gating**: Attempt `write_range` with Write toggle unchecked -> verify `-32001` error code and descriptive refusal text. Attempt `run_macro` with Destructive unchecked -> verify `-32001` refusal.
4. **Snapshot Recovery**: Run `write_range` with Write toggle checked -> verify `.hpexcel_snapshots/` contains a valid timestamped `.xlsx` backup that can be opened and verified.
