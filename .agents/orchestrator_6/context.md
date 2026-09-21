# Orchestrator 6 Context - HPExcel Subsystem

Project: Complete Excel MCP Subsystem (HPExcel)
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPExcel
Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar
Orchestrator Workspace: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_6

## Deliverables
- HPExcel.McpBridge: Standalone desktop application (.NET 8 Windows, MaterialDesignThemes 5.3.2) listening on Named Pipe (`hpexcel-mcp-2026`). Connects out-of-process to live running Microsoft Excel instances via COM Interop (`Excel.Application`), with hybrid support for direct headless workbook operations via `ClosedXML` when targeting closed `.xlsx` files.
- HPExcel.Mcp.Server: .NET 10 console application speaking standard MCP over stdio, connecting to the bridge via Named Pipe client, powered by `HPRebar.Mcp.Server.Core`.
- Architectural isolation: `HPExcel/` references only `../McpShared/` and never cross-references other host projects (`HPRebar`, `HPAutoCad`, `HPNavis`, `HPEtabs`, `HPCivil3d`, `HPSap2000`, `HPPowerBi`). Register `PipeNaming.ExcelHost` in `McpShared`.
- 12 Embedded seed tools + Core tools (`get_excel_context`, `execute_excel_code`) + 8 registry meta tools.
- 3-tier safety engine (Tier R / Tier W / Tier D) + Automatic snapshot engine (.hpexcel_snapshots/) + Roslyn Guard Profile.
- Automated test suites: `HPExcel.Mcp.Server.Tests` (.NET 10 xUnit) and `HPExcel.McpBridge.Tests` (.NET 8 Windows xUnit) passing 100%.
- Solution: `HPExcel/HPExcel.slnx` building cleanly with 0 errors.
- Documentation: Skill file `.agents/skills/hp-mcp-excel/SKILL.md` and repository registration in `AGENTS.md`.
