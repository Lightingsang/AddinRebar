# Task Assignment: Milestone M5 — Skill & Repository Documentation

## Working Directory
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m5_1`

## Mandatory Reading Before Starting Work
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (§ 2026-09-21T09:44:29Z)
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_6\PROJECT.md` (§ Milestone M5)
3. Reference skills:
   - `.agents/skills/hp-mcp-etabs/SKILL.md`
   - `.agents/skills/hp-mcp-powerbi/SKILL.md`
   - `.agents/skills/hp-mcp-revit/SKILL.md`
4. Reference documentation: `AGENTS.md`

## Objective
Implement comprehensive skill documentation and repository registration for HPExcel:
1. Create `.agents/skills/hp-mcp-excel/SKILL.md`:
   - YAML frontmatter:
     - `name`: `hp-mcp-excel`
     - `description`: "Kết nối và điều khiển Microsoft Excel qua HPExcel MCP (server hprebar-excel, tool mcp__hprebar-excel__*): đọc dữ liệu (read_range, find_cells, read_table, read_worksheet_info), ghi và định dạng (write_range, format_range, create_table, manage_worksheet), biểu đồ và công thức (create_chart, evaluate_formula, export_worksheet, run_macro), viết C# Excel COM / ClosedXML qua execute_excel_code với 3-tier R/W/D và snapshot .xlsx tự động, registry tool (propose/test/publish). TRIGGER when: user nhắc 'Excel', 'xlsx', 'workbook', 'worksheet', 'sheet', 'cell', 'range', 'hprebar-excel', 'hpexcel-mcp-2026', 'ClosedXML', 'VBA', 'macro', hoặc lỗi -32001/-32002 từ tool Excel. Keywords: excel, xlsx, workbook, worksheet, range, cell, closedxml, vba, macro, mcp, bridge, snapshot, formula, chart."
     - `metadata`: `author: hoang`, `version: 1.0.0`, `mcp-server: hprebar-excel`
   - Detailed guide sections:
     - Portable host contract
     - Overview: Claude -> HPExcel.Mcp.Server (stdio) -> Named Pipe `hpexcel-mcp-2026` -> HPExcel.McpBridge.exe (WPF standalone) -> Excel COM Interop & ClosedXML
     - Step 0: Connection checklist (start bridge, attach to Excel or open workbook, tick Allow AI code execution, tick Allow destructive operations if needed, verify `get_excel_context`)
     - Workflow decision tree: Read (Tier R) -> Write (Tier W, auto snapshot) -> Destructive (Tier D, UI toggle + snapshot) -> Registry toolify
     - Catalog of all 12 Seed Tools with parameter tables and usage examples:
       - Data: `read_range`, `find_cells`, `read_table`, `write_range`, `create_table`
       - Workbook: `read_worksheet_info`, `manage_worksheet`
       - Format: `format_range`
       - Chart: `create_chart`
       - Calculation: `evaluate_formula`
       - Export: `export_worksheet`
       - Automation: `run_macro`
     - Core tools: `get_excel_context`, `execute_excel_code`
     - 3-Tier Safety Engine & Snapshot backup guide (.hpexcel_snapshots/ adjacent or %TEMP%, 20-file retention, -32001/-32002 error codes)
     - Headless ClosedXML vs Live COM Interop guide
     - Troubleshooting guide & common error remedies

2. Update `AGENTS.md`:
   - In `## Repository Layout`, add `HPExcel/` row to the deliverables table:
     `| HPExcel/ | The **Excel MCP** (standalone WPF desktop bridge HPExcel.McpBridge net8.0-windows + stdio server HPExcel.Mcp.Server net10; connects out-of-process to Microsoft Excel via COM Interop and headless via ClosedXML; 3-tier safety R/W/D with automatic .xlsx snapshots in .hpexcel_snapshots/ or %TEMP%; pipe hpexcel-mcp-2026; MaterialDesignThemes 5.3.2 UI; 24 tools = 4 core + 8 registry + 12 embedded seeds). Own HPExcel.slnx + global.json + Directory.Build.props; references ../McpShared/ only. Tests HPExcel.McpBridge.Tests (110) + HPExcel.Mcp.Server.Tests (90). | C# / net8.0-windows · net10 / Microsoft.Office.Interop.Excel 15.0 + ClosedXML 0.104.2 |`
   - Add a dedicated section `## HPExcel — Current State & Architecture` documenting:
     - Projects in `HPExcel.slnx`
     - Pipe naming and versioning
     - STA worker and COM message filter
     - ClosedXML headless engine
     - 3-tier safety engine and snapshot manager
     - Build and test commands

3. Verification:
   - Ensure markdown syntax is valid and files are clean.
   - Run `dotnet build HPExcel/HPExcel.slnx -c Debug` to confirm no regressions.

## Output Requirements
Document all created and updated documentation files in:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m5_1\handoff.md`

## 2026-09-21T11:30:51Z
<USER_REQUEST>
You are Worker M5 (worker_m5_1) for Milestone M5 (Skill & Repository Documentation) in the HPExcel MCP Ecosystem.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m5_1

MANDATORY: Read the full original user request first:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (§ 2026-09-21T09:44:29Z).
Also read your task assignment at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m5_1\DISPATCH.md
Reference existing skills at:
.agents/skills/hp-mcp-etabs/SKILL.md and .agents/skills/hp-mcp-powerbi/SKILL.md
Reference repository guidelines at:
AGENTS.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Scope to Implement:
1. Create .agents/skills/hp-mcp-excel/SKILL.md:
   - YAML frontmatter: name hp-mcp-excel, description (comprehensive with trigger keywords), metadata (author: hoang, version: 1.0.0, mcp-server: hprebar-excel).
   - Portable host contract.
   - Comprehensive overview of HPExcel MCP (bridge + server).
   - Named Pipe protocol (hpexcel-mcp-2026), process detection, COM attachment, ClosedXML headless mode.
   - 3-Tier Safety Engine (ReadOnly, Write, Destructive) and automatic .xlsx snapshots in .hpexcel_snapshots/.
   - Detailed documentation of all 12 Seed Tools across 7 categories (Data, Workbook, Format, Chart, Calculation, Export, Automation) with parameter tables, descriptions, and examples.
   - Core tools (get_excel_context, execute_excel_code).
   - Troubleshooting guide and error codes (-32000, -32001, -32002).
2. Update AGENTS.md:
   - In ## Repository Layout table, add HPExcel/ row.
   - Add a dedicated section ## HPExcel — Current State & Architecture documenting projects, layout, bridge, safety engine, snapshots, and build/test commands.
3. Verify formatting and run a quick verification build of HPExcel.slnx to ensure 0 errors.

Write your complete report to:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m5_1\handoff.md
Send a completion message when finished.
</USER_REQUEST>
