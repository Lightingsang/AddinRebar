# Explorer M3 Dispatch — Milestone 3 (Revit Client & Pipe Engine Survey)

## Identity
- Role: `teamwork_preview_explorer`
- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m3_1_gen2`
- Parent: Orchestrator Gen 2 (`18c3ad8c-1fc9-42cc-83e7-40ab9000587e`)

## Context & Inputs
- Original Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MUST read first)
- Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10_gen2\PROJECT.md`
- `McpShared/` folder: `HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`
- Target: `HPBim/src/HPBim.Client/` (`HPBim.Client.csproj` targeting `net8.0;net10.0`) and `HPBim/tests/HPBim.Client.Tests/`

## Mission
1. Investigate how `HPBim.Client` should connect to the Named Pipe `\\.\pipe\hprebar-mcp-r2026`:
   - Inspect `McpShared/HPRebar.Mcp.Contracts` for RPC request/response wire format.
   - Inspect existing named pipe clients in `McpShared` or establish standard `NamedPipeClientStream` + NDJSON framing.
2. Investigate the 8-Stage BIM generation script generators:
   - Stage 1: Levels (`doc.Create.NewLevel`)
   - Stage 2: Grids (`Grid.Create(doc, line)`)
   - Stage 3: Columns (`doc.Create.NewFamilyInstance`), Beams (`doc.Create.NewFamilyInstance`), Slabs/Floors (`Floor.Create`)
   - Stage 4: Walls (`Wall.Create`)
   - Stage 5: Doors/Windows hosted on walls (`doc.Create.NewFamilyInstance(location, symbol, hostWall, StructuralType.NonStructural)`)
   - Stage 6: Rooms (`doc.Create.NewRoom(level, uv)`)
   - Stage 7: MEP fixtures & Pipes (`Pipe.Create`)
   - Stage 8: Rebars (`Rebar.CreateFromCurves`)
3. Investigate Dynamic Family/Type Resolution:
   - Duplicating WallType to 110mm / 220mm if not existing.
   - Duplicating Column FamilySymbol to 220x220mm if not existing.
   - Symbol activation: `if (!symbol.IsActive) symbol.Activate();`.
4. Investigate dryRun rollback mechanism:
   - Wrapping in `using (var tg = new TransactionGroup(doc, "HPBim dryRun")) { tg.Start(); ... tg.RollBack(); }`.
5. Design class decomposition for `HPBim.Client` and integration tests in `HPBim.Client.Tests` strictly adhering to Clean Code (classes <= 300 lines, methods <= 50 lines).
6. Do NOT write implementation code. Produce a comprehensive design report in `handoff.md`.


## 2026-10-06T01:47:43Z
[Message] sender=18c3ad8c-1fc9-42cc-83e7-40ab9000587e priority=MESSAGE_PRIORITY_HIGH
Investigate Milestone 3 (Revit Client & Pipe Engine):
1. Inspect McpShared/ for Named Pipe \\.\pipe\hprebar-mcp-r2026 communication contracts and framing.
2. Design the 8-Stage BIM generation script generators (Levels, Grids, Structure, Walls, Doors/Windows, Rooms, MEP, Rebars).
3. Design dynamic Type/Family resolution (WallType 110/220, Column C220x220) and dryRun rollback.
4. Design class layout for HPBim.Client and HPBim.Client.Tests ensuring clean code limits (class <= 300 lines, method <= 50 lines).
Write your analysis to handoff.md and report back via send_message.
