# HPExcel — Microsoft Excel MCP Subsystem

HPExcel is a Model Context Protocol (MCP) subsystem providing seamless AI automation and inspection for Microsoft Excel within the HP ecosystem.

## Architecture

1. **`HPExcel.McpBridge`** (.NET 8.0 Windows, WPF):
   - Standalone desktop bridge application listening on named pipe `hpexcel-mcp-2026`.
   - Hybrid execution model:
     - **COM Interop (`Microsoft.Office.Interop.Excel`)**: Connects to live running Excel instances via STA worker thread, ROT / `GetActiveObject`, and `IMessageFilter` retry logic.
     - **Headless OpenXML (`ClosedXML`)**: Direct high-speed offline reading and writing for closed `.xlsx` workbooks without requiring Excel installation.
   - 3-Tier Safety Engine:
     - **Tier R (Read)**: Range reads, worksheet structure queries, table enumeration, cell searches.
     - **Tier W (Write)**: Range modifications, formatting, table/chart generation. Gated by Write toggle + automatic `.xlsx` snapshot backup.
     - **Tier D (Destructive)**: Sheet deletions, clearing large ranges, macro runs. Gated by Destructive toggle + automatic `.xlsx` snapshot backup.
   - Automatic Snapshot Engine: Saves pre-mutation `.xlsx` backups into `.hpexcel_snapshots/` adjacent to workbooks (or `%TEMP%\.hpexcel_snapshots\`), retaining newest 20 snapshots.
   - Modern MaterialDesign 5.3.2 UI styled with Microsoft Excel brand green (`#107C41` / `#21A366`) and Windows light/dark theme synchronization.

2. **`HPExcel.Mcp.Server`** (.NET 10.0, Stdio Console):
   - Stdio MCP server exposing core tools (`get_excel_context`, `execute_excel_code`), registry meta-tools, and 12 embedded seed tools.
   - Powered by `HPRebar.Mcp.Server.Core`.

3. **Shared Contracts & Engine (`McpShared/`)**:
   - Zero cross-host coupling: references only `McpShared/HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, and `HPRebar.Mcp.Server.Core`.

## Build & Test

```bash
# Build the bridge project
dotnet build HPExcel/HPExcel.McpBridge/HPExcel.McpBridge.csproj

# Run tests
dotnet test HPExcel/HPExcel.McpBridge.Tests
dotnet test HPExcel/HPExcel.Mcp.Server.Tests
```
