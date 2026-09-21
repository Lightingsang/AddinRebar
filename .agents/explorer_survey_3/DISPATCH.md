# Task Assignment: Survey Explorer 3 — Excel Ecosystem, Tool Catalog & Headless ClosedXML

## Identity
- Role: Explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_3
- Parent Orchestrator: orchestrator_6

## Objective
Investigate Excel integration technologies (COM Interop via `Excel.Application` and headless operations via `ClosedXML`), the complete 12 embedded seed tools catalog, core tools (`get_excel_context`, `execute_excel_code`), and test suite patterns from `HPPowerBi.Mcp.Server.Tests` / `HPEtabs.Mcp.Server.Tests`.

## Context & Inputs
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (§ 2026-09-21T09:44:29Z)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPPowerBi\HPPowerBi.Mcp.Server\
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPEtabs\HPEtabs.Mcp.Server\
- Reference seed libraries in other MCPs (HPPowerBi, HPEtabs, HPRebar)

## Scope of Investigation
1. COM Interop vs ClosedXML hybrid model:
   - COM connection to running Excel instances (`Marshal.GetActiveObject("Excel.Application")` or ROT traversal).
   - Headless workbook operations via `ClosedXML` (reading/writing `.xlsx` without Excel running).
   - When to use COM vs ClosedXML.
2. The 12 Embedded Seed Tools specifications:
   - `read_range`, `read_worksheet_info`, `find_cells`, `read_table`, `write_range`, `format_range`, `manage_worksheet`, `create_table`, `create_chart`, `evaluate_formula`, `export_worksheet`, `run_macro`.
   - Tool names, descriptions, input schemas (parameters), output formats, and safety tier classification (R, W, D).
3. Core Tools:
   - `get_excel_context`: structure of returned Excel context (active workbook, sheet, selection, open workbooks, version).
   - `execute_excel_code`: Roslyn scripting environment, globals (`excel`), default usings.
4. Test Architecture & Fake Executor:
   - How `FakeExecutor` is implemented in `HPPowerBi.Mcp.Server.Tests` / `HPEtabs.Mcp.Server.Tests`.
   - Seed tool validation tests, round-trip tests, compilation tests, and headless ClosedXML tests.

## Output
Write detailed report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_3\handoff.md`.
Send a completion message back when done.

## 2026-09-21T09:46:59Z
You are Survey Explorer 3 for the HPExcel MCP Ecosystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_3

MANDATORY: Read the full user request first:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (§ 2026-09-21T09:44:29Z).
Also read your task assignment at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_3\DISPATCH.md

Your task is to investigate Excel COM Interop & ClosedXML hybrid models, all 12 seed tools, core tools, and test suites:
1. COM Interop (Excel.Application) vs ClosedXML:
   - Dynamic COM / late binding / Microsoft.Office.Interop.Excel vs ClosedXML (ClosedXML NuGet for headless workbook manipulation).
   - How get_excel_context and execute_excel_code should work with active Excel instances and closed workbooks.
2. The 12 Embedded Seed Tools specifications:
   - read_range, read_worksheet_info, find_cells, read_table, write_range, format_range, manage_worksheet, create_table, create_chart, evaluate_formula, export_worksheet, run_macro.
   - For each tool: input schema (properties, required, types), description, tier (R, W, D), sample C# implementation / Roslyn script code.
3. Core tools:
   - get_excel_context: context DTO (active workbook, sheet, selection, open files, Excel version).
   - execute_excel_code: script globals, imports, result formatting.
4. Test Suite patterns:
   - HPExcel.Mcp.Server.Tests (catalog completeness, schema validation, FakeExecutor round-trip, Roslyn compilation of all 12 seeds).
   - HPExcel.McpBridge.Tests (safety tiers, snapshot engine, ClosedXML headless tests, pipe dispatcher without Excel).

Write your complete findings and seed specifications to:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_3\handoff.md
Send a completion message back when done.
